using System.Diagnostics;
using System.Net.Http;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;

namespace NetKit;

public class SslChecker
{
    public async Task<SslCheckResult> CheckSslCertificateAsync(string hostname, int port = 443)
    {
        try
        {
            using var tcpClient = new TcpClient();
            await tcpClient.ConnectAsync(hostname, port);

            using var sslStream = new SslStream(tcpClient.GetStream());
            await sslStream.AuthenticateAsClientAsync(hostname);

            var certificate = sslStream.RemoteCertificate as X509Certificate2;
            if (certificate == null)
            {
                return new SslCheckResult
                {
                    IsValid = false,
                    Error = "No certificate found"
                };
            }

            return new SslCheckResult
            {
                IsValid = true,
                Subject = certificate.Subject,
                Issuer = certificate.Issuer,
                NotBefore = certificate.NotBefore,
                NotAfter = certificate.NotAfter,
                IsExpired = certificate.NotAfter < DateTime.Now,
                DaysUntilExpiry = (certificate.NotAfter - DateTime.Now).Days,
                Thumbprint = certificate.Thumbprint
            };
        }
        catch (Exception ex)
        {
            return new SslCheckResult
            {
                IsValid = false,
                Error = ex.Message
            };
        }
    }

    public async Task<SslCheckResult> CheckHttpConnectionAsync(string hostname, int port = 80)
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();

            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(10);

            var url = $"http://{hostname}:{port}";
            var response = await httpClient.GetAsync(url);

            stopwatch.Stop();

            var serverHeader = response.Headers.Server?.ToString() ?? "";

            return new SslCheckResult
            {
                IsValid = true,
                ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds,
                ServerHeader = serverHeader
            };
        }
        catch (Exception ex)
        {
            return new SslCheckResult
            {
                IsValid = false,
                Error = ex.Message
            };
        }
    }
}

public class SslCheckResult
{
    public bool IsValid { get; set; }
    public string? Subject { get; set; }
    public string? Issuer { get; set; }
    public DateTime NotBefore { get; set; }
    public DateTime NotAfter { get; set; }
    public bool IsExpired { get; set; }
    public int DaysUntilExpiry { get; set; }
    public string? Thumbprint { get; set; }
    public string? Error { get; set; }
    public int ResponseTimeMs { get; set; }
    public string? ServerHeader { get; set; }
}
