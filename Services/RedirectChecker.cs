using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace NetKit;

public class RedirectResult
{
    public bool IsSuccess { get; set; }
    public string Error { get; set; } = string.Empty;
    public List<RedirectStep> RedirectChain { get; set; } = new();
    public string FinalUrl { get; set; } = string.Empty;
    public int TotalRedirects { get; set; }
    public TimeSpan TotalTime { get; set; }
}

public class RedirectStep
{
    public string FromUrl { get; set; } = string.Empty;
    public string ToUrl { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public string StatusText { get; set; } = string.Empty;
    public TimeSpan ResponseTime { get; set; }
    public Dictionary<string, string> Headers { get; set; } = new();
}

public class RedirectChecker : IDisposable
{
    private readonly HttpClient _httpClient;
    private bool _disposed;

    public RedirectChecker()
    {
        var handler = new HttpClientHandler()
        {
            AllowAutoRedirect = false // We want to handle redirects manually
        };

        _httpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(30)
        };

        _httpClient.DefaultRequestHeaders.Add("User-Agent",
            "NetKit-RedirectChecker/1.0 (Windows)");
    }

    public async Task<RedirectResult> CheckRedirectsAsync(string url, int maxRedirects = 10)
    {
        var result = new RedirectResult();
        var startTime = DateTime.UtcNow;

        try
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                result.Error = "URL cannot be empty";
                return result;
            }

            // Ensure URL has a protocol
            if (!url.StartsWith("http://") && !url.StartsWith("https://"))
            {
                url = "https://" + url;
            }

            var currentUrl = url;
            var redirectCount = 0;

            while (redirectCount < maxRedirects)
            {
                var stepStartTime = DateTime.UtcNow;

                try
                {
                    using var response = await _httpClient.GetAsync(currentUrl);
                    var stepTime = DateTime.UtcNow - stepStartTime;

                    var step = new RedirectStep
                    {
                        FromUrl = currentUrl,
                        StatusCode = (int)response.StatusCode,
                        StatusText = response.StatusCode.ToString(),
                        ResponseTime = stepTime
                    };

                    // Capture important headers
                    foreach (var header in response.Headers)
                    {
                        step.Headers[header.Key] = string.Join(", ", header.Value);
                    }

                    // Check for redirect status codes
                    if ((int)response.StatusCode >= 300 && (int)response.StatusCode < 400)
                    {
                        var locationHeader = response.Headers.Location;
                        if (locationHeader != null)
                        {
                            var nextUrl = locationHeader.IsAbsoluteUri
                                ? locationHeader.ToString()
                                : new Uri(new Uri(currentUrl), locationHeader).ToString();

                            step.ToUrl = nextUrl;
                            result.RedirectChain.Add(step);

                            currentUrl = nextUrl;
                            redirectCount++;
                            continue;
                        }
                        else
                        {
                            result.Error = $"Redirect response {response.StatusCode} without Location header";
                            return result;
                        }
                    }
                    else
                    {
                        // Final destination reached
                        step.ToUrl = currentUrl;
                        result.RedirectChain.Add(step);
                        result.FinalUrl = currentUrl;
                        result.TotalRedirects = redirectCount;
                        result.TotalTime = DateTime.UtcNow - startTime;
                        result.IsSuccess = true;
                        return result;
                    }
                }
                catch (HttpRequestException ex)
                {
                    result.Error = $"HTTP error at step {redirectCount + 1}: {ex.Message}";
                    return result;
                }
                catch (TaskCanceledException ex)
                {
                    result.Error = ex.InnerException is TimeoutException
                        ? "Request timeout"
                        : "Request canceled";
                    return result;
                }
            }

            // Max redirects reached - still show the chain
            result.TotalRedirects = redirectCount;
            result.TotalTime = DateTime.UtcNow - startTime;
            result.IsSuccess = true; // We want to show the results even if max reached
            result.FinalUrl = currentUrl;

            // Add a final step showing the redirect limit was reached
            result.RedirectChain.Add(new RedirectStep
            {
                FromUrl = currentUrl,
                ToUrl = "Maximum redirect limit reached",
                StatusCode = 0,
                StatusText = "ERR_TOO_MANY_REDIRECTS",
                ResponseTime = TimeSpan.Zero
            });

            return result;
        }
        catch (Exception ex)
        {
            result.Error = $"Unexpected error: {ex.Message}";
            return result;
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _httpClient?.Dispose();
            _disposed = true;
        }
    }
}