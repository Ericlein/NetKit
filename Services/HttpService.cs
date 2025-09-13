using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace msOps;

public class HttpService
{
    private readonly HttpClient _httpClient;

    public HttpService()
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "msOps/1.0");
    }

    private string EnsureProtocol(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return url;

        url = url.Trim();

        if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return $"https://{url}";
        }

        return url;
    }

    public async Task<HttpResult> GetAsync(string url)
    {
        try
        {
            url = EnsureProtocol(url);
            var response = await _httpClient.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();

            return new HttpResult
            {
                IsSuccess = response.IsSuccessStatusCode,
                StatusCode = (int)response.StatusCode,
                StatusText = response.ReasonPhrase ?? "",
                Headers = response.Headers.ToDictionary(h => h.Key, h => string.Join(", ", h.Value)),
                ContentHeaders = response.Content.Headers.ToDictionary(h => h.Key, h => string.Join(", ", h.Value)),
                Content = content,
                ResponseTime = TimeSpan.Zero // We'll add timing later
            };
        }
        catch (Exception ex)
        {
            return new HttpResult
            {
                IsSuccess = false,
                Error = ex.Message
            };
        }
    }

    public async Task<HttpResult> PostAsync(string url, object? data = null)
    {
        try
        {
            url = EnsureProtocol(url);

            var content = data != null
                ? new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json")
                : new StringContent("");

            var response = await _httpClient.PostAsync(url, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            return new HttpResult
            {
                IsSuccess = response.IsSuccessStatusCode,
                StatusCode = (int)response.StatusCode,
                StatusText = response.ReasonPhrase ?? "",
                Headers = response.Headers.ToDictionary(h => h.Key, h => string.Join(", ", h.Value)),
                ContentHeaders = response.Content.Headers.ToDictionary(h => h.Key, h => string.Join(", ", h.Value)),
                Content = responseContent,
                ResponseTime = TimeSpan.Zero
            };
        }
        catch (Exception ex)
        {
            return new HttpResult
            {
                IsSuccess = false,
                Error = ex.Message
            };
        }
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}

public class HttpResult
{
    public bool IsSuccess { get; set; }
    public int StatusCode { get; set; }
    public string StatusText { get; set; } = "";
    public Dictionary<string, string> Headers { get; set; } = new();
    public Dictionary<string, string> ContentHeaders { get; set; } = new();
    public string Content { get; set; } = "";
    public TimeSpan ResponseTime { get; set; }
    public string? Error { get; set; }
}
