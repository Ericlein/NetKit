using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Shell;

namespace msOps;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly HttpService _httpService;
    private readonly SslChecker _sslChecker;
    private readonly RedirectChecker _redirectChecker;

    public MainWindow()
    {
        try
        {
            InitializeComponent();
            _httpService = new HttpService();
            _sslChecker = new SslChecker();
            _redirectChecker = new RedirectChecker();

            // Enable borderless window chrome with resize and caption area
            var chrome = new WindowChrome
            {
                CaptionHeight = 42,
                CornerRadius = new CornerRadius(0),
                GlassFrameThickness = new Thickness(0),
                ResizeBorderThickness = new Thickness(6),
                UseAeroCaptionButtons = false
            };
            WindowChrome.SetWindowChrome(this, chrome);
        }
        catch (Exception ex)
        {
            CustomMessageBox.Show($"MainWindow Constructor Error: {ex.Message}\n\nStack Trace: {ex.StackTrace}", "Window Error");
            throw;
        }
    }

    private void ProtocolRadioButton_Changed(object sender, RoutedEventArgs e)
    {
        // Ensure UI elements are initialized before accessing them
        if (SslPortTextBox == null) return;

        if (SslRadioButton?.IsChecked == true)
        {
            SslPortTextBox.Text = "443";
        }
        else if (HttpRadioButton?.IsChecked == true)
        {
            SslPortTextBox.Text = "80";
        }
    }

    private void HttpUrlTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            HttpGetButton_Click(sender, new RoutedEventArgs());
        }
    }

    private void HttpPostDataTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            HttpPostButton_Click(sender, new RoutedEventArgs());
        }
    }

    private void SslHostnameTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            SslCheckButton_Click(sender, new RoutedEventArgs());
        }
    }

    private void SslPortTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            SslCheckButton_Click(sender, new RoutedEventArgs());
        }
    }

    private void RedirectUrlTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            RedirectCheckButton_Click(sender, new RoutedEventArgs());
        }
    }

    private async void HttpGetButton_Click(object sender, RoutedEventArgs e)
    {
        var url = HttpUrlTextBox.Text;
        if (string.IsNullOrWhiteSpace(url))
        {
            CustomMessageBox.Show(this, "Please enter a URL");
            return;
        }

        HttpStatusTextBlock.Text = "Making GET request...";
        HttpSummaryTextBox.Text = "";
        HttpResponseTextBox.Text = "";

        // Collapse expanded view when making new request
        HttpResponseTextBox.Visibility = Visibility.Collapsed;
        ExpandButton.Content = "Show Full Response";

        try
        {
            var result = await _httpService.GetAsync(url);

            if (result.IsSuccess)
            {
                DisplayHttpResult(result);
                HttpStatusTextBlock.Text = $"GET request completed - {result.StatusCode} {result.StatusText}";
                HttpStatusTextBlock.Foreground = (SolidColorBrush)FindResource("SuccessBrush");
            }
            else if (!string.IsNullOrEmpty(result.Error))
            {
                HttpSummaryTextBox.Text = $"Error: {result.Error}";
                HttpStatusTextBlock.Text = "GET request failed";
                HttpStatusTextBlock.Foreground = (SolidColorBrush)FindResource("ErrorBrush");
            }
            else
            {
                DisplayHttpResult(result);
                HttpStatusTextBlock.Text = $"GET request completed - {result.StatusCode} {result.StatusText}";
                HttpStatusTextBlock.Foreground = new SolidColorBrush(Colors.Orange);
            }
        }
        catch (Exception ex)
        {
            HttpSummaryTextBox.Text = $"Error: {ex.Message}";
            HttpStatusTextBlock.Text = "GET request failed";
            HttpStatusTextBlock.Foreground = (SolidColorBrush)FindResource("ErrorBrush");
        }
    }

    private async void HttpPostButton_Click(object sender, RoutedEventArgs e)
    {
        var url = HttpUrlTextBox.Text;
        if (string.IsNullOrWhiteSpace(url))
        {
            CustomMessageBox.Show(this, "Please enter a URL");
            return;
        }

        HttpStatusTextBlock.Text = "Making POST request...";
        HttpSummaryTextBox.Text = "";
        HttpResponseTextBox.Text = "";

        // Collapse expanded view when making new request
        HttpResponseTextBox.Visibility = Visibility.Collapsed;
        ExpandButton.Content = "Show Full Response";

        try
        {
            object? data = null;
            var postDataText = HttpPostDataTextBox.Text;

            if (!string.IsNullOrWhiteSpace(postDataText))
            {
                try
                {
                    data = JsonSerializer.Deserialize<object>(postDataText);
                }
                catch
                {
                    data = postDataText;
                }
            }

            var result = await _httpService.PostAsync(url, data);

            if (result.IsSuccess)
            {
                DisplayHttpResult(result);
                HttpStatusTextBlock.Text = $"POST request completed - {result.StatusCode} {result.StatusText}";
                HttpStatusTextBlock.Foreground = (SolidColorBrush)FindResource("SuccessBrush");
            }
            else if (!string.IsNullOrEmpty(result.Error))
            {
                HttpSummaryTextBox.Text = $"Error: {result.Error}";
                HttpStatusTextBlock.Text = "POST request failed";
                HttpStatusTextBlock.Foreground = (SolidColorBrush)FindResource("ErrorBrush");
            }
            else
            {
                DisplayHttpResult(result);
                HttpStatusTextBlock.Text = $"POST request completed - {result.StatusCode} {result.StatusText}";
                HttpStatusTextBlock.Foreground = new SolidColorBrush(Colors.Orange);
            }
        }
        catch (Exception ex)
        {
            HttpSummaryTextBox.Text = $"Error: {ex.Message}";
            HttpStatusTextBlock.Text = "POST request failed";
            HttpStatusTextBlock.Foreground = (SolidColorBrush)FindResource("ErrorBrush");
        }
    }

    private async void SslCheckButton_Click(object sender, RoutedEventArgs e)
    {
        var hostname = SslHostnameTextBox.Text;
        if (string.IsNullOrWhiteSpace(hostname))
        {
            CustomMessageBox.Show(this, "Please enter a hostname");
            return;
        }

        if (!int.TryParse(SslPortTextBox.Text, out var port))
        {
            port = SslRadioButton.IsChecked == true ? 443 : 80;
        }

        var isSSL = SslRadioButton.IsChecked == true;
        var protocolName = isSSL ? "SSL/TLS" : "HTTP";

        SslStatusTextBlock.Text = $"Checking {protocolName} connection to {hostname}:{port}...";
        SslResultsTextBox.Text = "";

        try
        {
            if (isSSL)
            {
                var result = await _sslChecker.CheckSslCertificateAsync(hostname, port);

                if (result.IsValid)
                {
                    var sb = new StringBuilder();
                    sb.AppendLine($"SSL Certificate Valid: YES");
                    sb.AppendLine($"Subject: {result.Subject}");
                    sb.AppendLine($"Issuer: {result.Issuer}");
                    sb.AppendLine($"Valid From: {result.NotBefore:yyyy-MM-dd HH:mm:ss}");
                    sb.AppendLine($"Valid Until: {result.NotAfter:yyyy-MM-dd HH:mm:ss}");
                    sb.AppendLine($"Expired: {(result.IsExpired ? "YES" : "NO")}");
                    sb.AppendLine($"Days Until Expiry: {result.DaysUntilExpiry}");
                    sb.AppendLine($"Thumbprint: {result.Thumbprint}");

                    SslResultsTextBox.Text = sb.ToString();
                    SslStatusTextBlock.Text = result.IsExpired ? "Certificate is EXPIRED!" : "Certificate is valid";
                    SslStatusTextBlock.Foreground = result.IsExpired ? (SolidColorBrush)FindResource("ErrorBrush") : (SolidColorBrush)FindResource("SuccessBrush");
                }
                else
                {
                    SslResultsTextBox.Text = $"SSL Certificate Check Failed: {result.Error}";
                    SslStatusTextBlock.Text = "SSL check failed";
                    SslStatusTextBlock.Foreground = (SolidColorBrush)FindResource("ErrorBrush");
                }
            }
            else
            {
                // HTTP connection test
                var result = await _sslChecker.CheckHttpConnectionAsync(hostname, port);

                if (result.IsValid)
                {
                    var sb = new StringBuilder();
                    sb.AppendLine($"HTTP Connection: SUCCESS");
                    sb.AppendLine($"Host: {hostname}:{port}");
                    sb.AppendLine($"Response Time: {result.ResponseTimeMs}ms");
                    if (!string.IsNullOrEmpty(result.ServerHeader))
                    {
                        sb.AppendLine($"Server: {result.ServerHeader}");
                    }

                    SslResultsTextBox.Text = sb.ToString();
                    SslStatusTextBlock.Text = "HTTP connection successful";
                    SslStatusTextBlock.Foreground = (SolidColorBrush)FindResource("SuccessBrush");
                }
                else
                {
                    SslResultsTextBox.Text = $"HTTP Connection Failed: {result.Error}";
                    SslStatusTextBlock.Text = "HTTP connection failed";
                    SslStatusTextBlock.Foreground = (SolidColorBrush)FindResource("ErrorBrush");
                }
            }
        }
        catch (Exception ex)
        {
            SslResultsTextBox.Text = $"Error: {ex.Message}";
            SslStatusTextBlock.Text = $"{protocolName} check failed";
            SslStatusTextBlock.Foreground = (SolidColorBrush)FindResource("ErrorBrush");
        }
    }

    private async void RedirectCheckButton_Click(object sender, RoutedEventArgs e)
    {
        var url = RedirectUrlTextBox.Text;
        if (string.IsNullOrWhiteSpace(url))
        {
            CustomMessageBox.Show(this, "Please enter a URL");
            return;
        }

        RedirectStatusTextBlock.Text = "Checking redirects...";
        RedirectResultsTextBox.Text = "";

        try
        {
            var result = await _redirectChecker.CheckRedirectsAsync(url);

            if (result.IsSuccess)
            {
                DisplayRedirectResult(result);
                var statusMessage = result.RedirectChain.Any(s => s.StatusText == "ERR_TOO_MANY_REDIRECTS")
                    ? $"Redirect check completed - {result.TotalRedirects} redirect(s) found, maximum limit reached in {result.TotalTime.TotalMilliseconds:F0}ms"
                    : $"Redirect check completed - {result.TotalRedirects} redirect(s) found in {result.TotalTime.TotalMilliseconds:F0}ms";

                RedirectStatusTextBlock.Text = statusMessage;
                RedirectStatusTextBlock.Foreground = (SolidColorBrush)FindResource("SuccessBrush");
            }
            else
            {
                RedirectResultsTextBox.Text = $"Error: {result.Error}";
                RedirectStatusTextBlock.Text = "Redirect check failed";
                RedirectStatusTextBlock.Foreground = (SolidColorBrush)FindResource("ErrorBrush");
            }
        }
        catch (Exception ex)
        {
            RedirectResultsTextBox.Text = $"Error: {ex.Message}";
            RedirectStatusTextBlock.Text = "Redirect check failed";
            RedirectStatusTextBlock.Foreground = (SolidColorBrush)FindResource("ErrorBrush");
        }
    }

    private void DisplayRedirectResult(RedirectResult result)
    {
        var output = new StringBuilder();
        output.AppendLine($"=== REDIRECT CHAIN ANALYSIS ===");
        output.AppendLine($"Total Redirects: {result.TotalRedirects}");
        output.AppendLine($"Total Time: {result.TotalTime.TotalMilliseconds:F0}ms");
        output.AppendLine($"Final URL: {result.FinalUrl}");
        output.AppendLine();

        if (result.RedirectChain.Count > 0)
        {
            for (int i = 0; i < result.RedirectChain.Count; i++)
            {
                var step = result.RedirectChain[i];
                output.AppendLine($"=== STEP {i + 1} ===");
                output.AppendLine($"From: {step.FromUrl}");

                if (!string.IsNullOrEmpty(step.ToUrl) && step.ToUrl != step.FromUrl)
                {
                    if (step.StatusCode == 0)
                    {
                        output.AppendLine($"Error: {step.ToUrl}");
                        output.AppendLine($"Status: {step.StatusText}");
                    }
                    else
                    {
                        output.AppendLine($"To: {step.ToUrl}");
                        output.AppendLine($"Status: {step.StatusCode} {step.StatusText}");
                    }
                }
                else
                {
                    output.AppendLine($"Status: {step.StatusCode} {step.StatusText}");
                }

                if (step.ResponseTime > TimeSpan.Zero)
                {
                    output.AppendLine($"Response Time: {step.ResponseTime.TotalMilliseconds:F0}ms");
                }

                // Show important headers
                if (step.Headers.ContainsKey("Location"))
                    output.AppendLine($"Location: {step.Headers["Location"]}");

                if (step.Headers.ContainsKey("Server"))
                    output.AppendLine($"Server: {step.Headers["Server"]}");

                if (step.Headers.ContainsKey("Cache-Control"))
                    output.AppendLine($"Cache-Control: {step.Headers["Cache-Control"]}");

                output.AppendLine();
            }
        }
        else
        {
            output.AppendLine("No redirects found - URL responded directly.");
        }

        RedirectResultsTextBox.Text = output.ToString();
    }

    private void DisplayHttpResult(HttpResult result)
    {
        var summary = new StringBuilder();
        summary.AppendLine($"Status: {result.StatusCode} {result.StatusText}");

        // Key headers
        if (result.Headers.ContainsKey("Date"))
            summary.AppendLine($"Date: {result.Headers["Date"]}");

        if (result.Headers.ContainsKey("Server"))
            summary.AppendLine($"Server: {result.Headers["Server"]}");

        if (result.ContentHeaders.ContainsKey("Content-Type"))
            summary.AppendLine($"Content-Type: {result.ContentHeaders["Content-Type"]}");

        if (result.ContentHeaders.ContainsKey("Content-Length"))
            summary.AppendLine($"Content-Length: {result.ContentHeaders["Content-Length"]}");

        if (result.Headers.ContainsKey("Cache-Control"))
            summary.AppendLine($"Cache-Control: {result.Headers["Cache-Control"]}");

        HttpSummaryTextBox.Text = summary.ToString();

        // Full response for expand
        var fullResponse = new StringBuilder();
        fullResponse.AppendLine($"HTTP Status: {result.StatusCode} {result.StatusText}");
        fullResponse.AppendLine();

        fullResponse.AppendLine("=== RESPONSE HEADERS ===");
        foreach (var header in result.Headers)
        {
            fullResponse.AppendLine($"{header.Key}: {header.Value}");
        }

        fullResponse.AppendLine();
        fullResponse.AppendLine("=== CONTENT HEADERS ===");
        foreach (var header in result.ContentHeaders)
        {
            fullResponse.AppendLine($"{header.Key}: {header.Value}");
        }

        fullResponse.AppendLine();
        fullResponse.AppendLine("=== RESPONSE BODY ===");
        fullResponse.AppendLine(FormatJson(result.Content));

        HttpResponseTextBox.Text = fullResponse.ToString();
    }

    private void ExpandButton_Click(object sender, RoutedEventArgs e)
    {
        if (HttpResponseTextBox.Visibility == Visibility.Collapsed)
        {
            HttpResponseTextBox.Visibility = Visibility.Visible;
            ExpandButton.Content = "Hide Full Response";
        }
        else
        {
            HttpResponseTextBox.Visibility = Visibility.Collapsed;
            ExpandButton.Content = "Show Full Response";
        }
    }

    private static string FormatJson(string json)
    {
        try
        {
            var jsonElement = JsonSerializer.Deserialize<JsonElement>(json);
            return JsonSerializer.Serialize(jsonElement, new JsonSerializerOptions { WriteIndented = true });
        }
        catch
        {
            return json;
        }
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        try
        {
            if (e.ClickCount == 2)
            {
                ToggleMaximize();
            }
            else
            {
                DragMove();
            }
        }
        catch
        {
            // ignore drag exceptions
        }
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void MaximizeButton_Click(object sender, RoutedEventArgs e)
    {
        ToggleMaximize();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void ToggleMaximize()
    {
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    }

    protected override void OnClosed(EventArgs e)
    {
        _httpService?.Dispose();
        _redirectChecker?.Dispose();
        base.OnClosed(e);
    }
}

