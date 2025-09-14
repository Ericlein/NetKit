using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using DnsClient;

namespace NetKit;

public class DnsResult
{
    public bool IsSuccess { get; set; }
    public string Error { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public string RecordType { get; set; } = string.Empty;
    public List<DnsRecord> Records { get; set; } = new();
    public TimeSpan QueryTime { get; set; }
    public string DnsServer { get; set; } = string.Empty;
}

public class DnsRecord
{
    public string Type { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public int TTL { get; set; }
    public int Priority { get; set; } // For MX records
}

public class DnsLookup : IDisposable
{
    private readonly LookupClient _dnsClient;
    private readonly SemaphoreSlim _concurrencySemaphore;
    private bool _disposed;
    private CancellationTokenSource? _shutdownCts;

    public DnsLookup()
    {
        // Configure DNS client with timeouts and limits
        var options = new LookupClientOptions
        {
            Timeout = TimeSpan.FromSeconds(10), // Prevent hanging queries
            Retries = 2,
            UseCache = true,
            CacheFailedResults = true
        };

        _dnsClient = new LookupClient(options);

        // Limit concurrent DNS operations to prevent resource exhaustion
        _concurrencySemaphore = new SemaphoreSlim(5, 5);
        _shutdownCts = new CancellationTokenSource();
    }

    public async Task<DnsResult> LookupAsync(string domain, string recordType = "A")
    {
        var result = new DnsResult
        {
            Domain = domain,
            RecordType = recordType
        };

        var startTime = DateTime.UtcNow;

        // Check if we're disposed/shutting down
        if (_disposed || _shutdownCts?.Token.IsCancellationRequested == true)
        {
            result.Error = "DNS lookup cancelled - service is shutting down";
            return result;
        }

        // Acquire semaphore to limit concurrent operations
        if (!await _concurrencySemaphore.WaitAsync(TimeSpan.FromSeconds(5), _shutdownCts?.Token ?? CancellationToken.None).ConfigureAwait(false))
        {
            result.Error = "DNS lookup timeout - too many concurrent operations";
            return result;
        }

        try
        {
            if (string.IsNullOrWhiteSpace(domain))
            {
                result.Error = "Domain cannot be empty";
                return result;
            }

            // Validate domain length to prevent memory issues
            if (domain.Length > 255)
            {
                result.Error = "Domain name too long (max 255 characters)";
                return result;
            }

            // Clean up domain (remove protocol if present)
            domain = domain.Replace("http://", "").Replace("https://", "").Split('/')[0];

            result.DnsServer = GetDnsServers().FirstOrDefault() ?? "System Default";

            switch (recordType.ToUpper())
            {
                case "A":
                    await LookupARecords(domain, result);
                    break;
                case "AAAA":
                    await LookupAAAARecords(domain, result);
                    break;
                case "CNAME":
                    await LookupCNAMERecords(domain, result);
                    break;
                case "MX":
                    await LookupMXRecords(domain, result);
                    break;
                case "TXT":
                    await LookupTXTRecords(domain, result);
                    break;
                case "NS":
                    await LookupNSRecords(domain, result);
                    break;
                case "PTR":
                    await LookupPTRRecords(domain, result);
                    break;
                case "ALL":
                    await LookupAllRecords(domain, result);
                    break;
                default:
                    result.Error = $"Unsupported record type: {recordType}";
                    return result;
            }

            result.QueryTime = DateTime.UtcNow - startTime;
            result.IsSuccess = result.Records.Count > 0 || string.IsNullOrEmpty(result.Error);
        }
        catch (OperationCanceledException)
        {
            result.Error = "DNS lookup was cancelled";
            result.QueryTime = DateTime.UtcNow - startTime;
        }
        catch (ObjectDisposedException)
        {
            result.Error = "DNS service is disposed";
            result.QueryTime = DateTime.UtcNow - startTime;
        }
        catch (Exception ex)
        {
            result.Error = $"DNS lookup failed: {ex.Message}";
            result.QueryTime = DateTime.UtcNow - startTime;
        }
        finally
        {
            // Always release the semaphore
            _concurrencySemaphore?.Release();
        }

        return result;
    }

    private async Task LookupARecords(string domain, DnsResult result)
    {
        try
        {
            // Use ConfigureAwait(false) for better performance in async contexts
            var addresses = await Dns.GetHostAddressesAsync(domain).ConfigureAwait(false);
            foreach (var address in addresses.Where(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork))
            {
                result.Records.Add(new DnsRecord
                {
                    Type = "A",
                    Name = domain,
                    Value = address.ToString(),
                    TTL = 0 // .NET doesn't provide TTL info easily
                });
            }
        }
        catch (Exception ex)
        {
            result.Error = ex.Message;
        }
    }

    private async Task LookupAAAARecords(string domain, DnsResult result)
    {
        try
        {
            var addresses = await Dns.GetHostAddressesAsync(domain).ConfigureAwait(false);
            foreach (var address in addresses.Where(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6))
            {
                result.Records.Add(new DnsRecord
                {
                    Type = "AAAA",
                    Name = domain,
                    Value = address.ToString(),
                    TTL = 0
                });
            }
        }
        catch (Exception ex)
        {
            result.Error = ex.Message;
        }
    }

    private async Task LookupCNAMERecords(string domain, DnsResult result)
    {
        try
        {
            var hostEntry = await Dns.GetHostEntryAsync(domain).ConfigureAwait(false);
            if (hostEntry.HostName != domain && !string.IsNullOrEmpty(hostEntry.HostName))
            {
                result.Records.Add(new DnsRecord
                {
                    Type = "CNAME",
                    Name = domain,
                    Value = hostEntry.HostName,
                    TTL = 0
                });
            }
        }
        catch (Exception ex)
        {
            result.Error = ex.Message;
        }
    }

    private async Task LookupMXRecords(string domain, DnsResult result)
    {
        try
        {
            _shutdownCts?.Token.ThrowIfCancellationRequested();

            var queryResult = await _dnsClient.QueryAsync(domain, QueryType.MX, cancellationToken: _shutdownCts?.Token ?? CancellationToken.None).ConfigureAwait(false);

            const int maxRecords = 50; // Limit to prevent memory issues
            var recordCount = 0;

            foreach (var record in queryResult.Answers.MxRecords())
            {
                if (recordCount >= maxRecords)
                {
                    result.Error = $"Too many MX records (showing first {maxRecords})";
                    break;
                }

                // Validate record data to prevent issues
                var exchangeValue = record.Exchange?.Value ?? "Invalid Exchange";
                if (exchangeValue.Length > 500)
                {
                    exchangeValue = exchangeValue.Substring(0, 497) + "...";
                }

                result.Records.Add(new DnsRecord
                {
                    Type = "MX",
                    Name = domain,
                    Value = exchangeValue,
                    TTL = Math.Max(0, Math.Min(record.TimeToLive, int.MaxValue)),
                    Priority = Math.Max(0, Math.Min((int)record.Preference, 65535))
                });

                recordCount++;
            }

            if (!result.Records.Any() && string.IsNullOrEmpty(result.Error))
            {
                result.Error = "No MX records found";
            }
        }
        catch (OperationCanceledException)
        {
            result.Error = "MX lookup was cancelled";
        }
        catch (Exception ex)
        {
            result.Error = $"MX lookup failed: {ex.Message}";
        }
    }

    private async Task LookupTXTRecords(string domain, DnsResult result)
    {
        try
        {
            _shutdownCts?.Token.ThrowIfCancellationRequested();

            var queryResult = await _dnsClient.QueryAsync(domain, QueryType.TXT, cancellationToken: _shutdownCts?.Token ?? CancellationToken.None).ConfigureAwait(false);

            const int maxRecords = 25; // Lower limit for TXT as they can be very large
            var recordCount = 0;

            foreach (var record in queryResult.Answers.TxtRecords())
            {
                if (recordCount >= maxRecords)
                {
                    result.Error = $"Too many TXT records (showing first {maxRecords})";
                    break;
                }

                // TXT records can be extremely large, limit them
                var txtValue = string.Join(" ", record.Text);
                if (txtValue.Length > 2000) // Prevent huge TXT records from consuming memory
                {
                    txtValue = txtValue.Substring(0, 1997) + "...";
                }

                result.Records.Add(new DnsRecord
                {
                    Type = "TXT",
                    Name = domain,
                    Value = txtValue,
                    TTL = Math.Max(0, Math.Min(record.TimeToLive, int.MaxValue))
                });

                recordCount++;
            }

            if (!result.Records.Any() && string.IsNullOrEmpty(result.Error))
            {
                result.Error = "No TXT records found";
            }
        }
        catch (OperationCanceledException)
        {
            result.Error = "TXT lookup was cancelled";
        }
        catch (Exception ex)
        {
            result.Error = $"TXT lookup failed: {ex.Message}";
        }
    }

    private async Task LookupNSRecords(string domain, DnsResult result)
    {
        try
        {
            var queryResult = await _dnsClient.QueryAsync(domain, QueryType.NS).ConfigureAwait(false);

            foreach (var record in queryResult.Answers.NsRecords())
            {
                result.Records.Add(new DnsRecord
                {
                    Type = "NS",
                    Name = domain,
                    Value = record.NSDName.Value,
                    TTL = Math.Max(0, Math.Min(record.TimeToLive, int.MaxValue))
                });
            }

            if (!result.Records.Any())
            {
                result.Error = "No NS records found";
            }
        }
        catch (Exception ex)
        {
            result.Error = $"NS lookup failed: {ex.Message}";
        }
    }

    private async Task LookupPTRRecords(string domain, DnsResult result)
    {
        try
        {
            // Assume domain is an IP address for PTR lookup
            if (IPAddress.TryParse(domain, out var ipAddress))
            {
                var hostEntry = await Dns.GetHostEntryAsync(ipAddress).ConfigureAwait(false);
                result.Records.Add(new DnsRecord
                {
                    Type = "PTR",
                    Name = domain,
                    Value = hostEntry.HostName,
                    TTL = 0
                });
            }
            else
            {
                result.Error = "PTR lookup requires an IP address";
            }
        }
        catch (Exception ex)
        {
            result.Error = ex.Message;
        }
    }

    private async Task LookupAllRecords(string domain, DnsResult result)
    {
        // Run all lookups concurrently for better performance
        var tasks = new List<Task<DnsResult>>
        {
            LookupRecordTypeAsync(domain, "A"),
            LookupRecordTypeAsync(domain, "AAAA"),
            LookupRecordTypeAsync(domain, "CNAME"),
            LookupRecordTypeAsync(domain, "MX"),
            LookupRecordTypeAsync(domain, "TXT"),
            LookupRecordTypeAsync(domain, "NS"),
            LookupRecordTypeAsync(domain, "PTR") // Try reverse lookup too if it's an IP
        };

        var results = await Task.WhenAll(tasks);

        // Combine all successful results
        foreach (var lookupResult in results)
        {
            if (lookupResult.Records.Count > 0)
            {
                result.Records.AddRange(lookupResult.Records);
            }
        }

        // Clear error if we got some results
        if (result.Records.Count > 0)
        {
            result.Error = string.Empty;
        }
        else
        {
            result.Error = "No records found for any record type";
        }
    }

    private async Task<DnsResult> LookupRecordTypeAsync(string domain, string recordType)
    {
        var tempResult = new DnsResult
        {
            Domain = domain,
            RecordType = recordType
        };

        try
        {
            switch (recordType.ToUpper())
            {
                case "A":
                    await LookupARecords(domain, tempResult);
                    break;
                case "AAAA":
                    await LookupAAAARecords(domain, tempResult);
                    break;
                case "CNAME":
                    await LookupCNAMERecords(domain, tempResult);
                    break;
                case "MX":
                    await LookupMXRecords(domain, tempResult);
                    break;
                case "TXT":
                    await LookupTXTRecords(domain, tempResult);
                    break;
                case "NS":
                    await LookupNSRecords(domain, tempResult);
                    break;
                case "PTR":
                    await LookupPTRRecords(domain, tempResult);
                    break;
            }
        }
        catch
        {
            // Ignore individual lookup failures for ALL mode
        }

        return tempResult;
    }

    private List<string> GetDnsServers()
    {
        var dnsServers = new List<string>();

        try
        {
            var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
            foreach (var networkInterface in networkInterfaces)
            {
                if (networkInterface.OperationalStatus == OperationalStatus.Up)
                {
                    var ipProperties = networkInterface.GetIPProperties();
                    foreach (var dnsAddress in ipProperties.DnsAddresses)
                    {
                        if (!dnsServers.Contains(dnsAddress.ToString()))
                        {
                            dnsServers.Add(dnsAddress.ToString());
                        }
                    }
                }
            }
        }
        catch
        {
            // Fallback to common DNS servers
            dnsServers.AddRange(new[] { "8.8.8.8", "1.1.1.1" });
        }

        return dnsServers;
    }

    public void Shutdown()
    {
        try
        {
            // Signal all operations to cancel
            _shutdownCts?.Cancel();
        }
        catch
        {
            // Ignore cancellation exceptions during shutdown
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            try
            {
                // Cancel any ongoing operations
                Shutdown();

                // Wait briefly for operations to complete
                Task.Delay(1000).Wait();

                // Dispose resources (LookupClient doesn't implement IDisposable)
                _concurrencySemaphore?.Dispose();
                _shutdownCts?.Dispose();
            }
            catch
            {
                // Ignore exceptions during disposal
            }
            finally
            {
                _disposed = true;
            }
        }
    }
}