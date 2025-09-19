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
using System.Windows.Interop;

namespace NetKit;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly HttpService _httpService;
    private readonly SslChecker _sslChecker;
    private readonly RedirectChecker _redirectChecker;
    private readonly DnsLookup _dnsLookup;
    private readonly SettingsService _settingsService;
    private readonly TerminalService _terminalService;

    public MainWindow(SettingsService settingsService)
    {
        try
        {
            InitializeComponent();
            _httpService = new HttpService();
            _sslChecker = new SslChecker();
            _redirectChecker = new RedirectChecker();
            _dnsLookup = new DnsLookup();
            _settingsService = settingsService;
            _terminalService = new TerminalService();

            // Setup terminal event handlers
            _terminalService.OutputReceived += OnTerminalOutputReceived;
            _terminalService.ErrorReceived += OnTerminalErrorReceived;
            _terminalService.ProcessExited += OnTerminalProcessExited;


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

            // Try to set the window icon to the app's EXE icon
            TrySetWindowIconFromExe();
        }
        catch (Exception ex)
        {
            CustomMessageBox.Show($"MainWindow Constructor Error: {ex.Message}\n\nStack Trace: {ex.StackTrace}", "Window Error");
            throw;
        }
    }



    public void ShowWindow()
    {
        if (Visibility == Visibility.Hidden)
        {
            Show();
            WindowState = WindowState.Normal;
            Activate();
            Focus();
        }
        else
        {
            HideWindow();
        }
    }

    private void HideWindow()
    {
        Hide();
    }

    private void MainWindow_Deactivated(object sender, EventArgs e)
    {
        // Hide window when it loses focus (click outside)
        HideWindow();
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

    private void DnsHostnameTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            DnsLookupButton_Click(sender, new RoutedEventArgs());
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

    private async void DnsLookupButton_Click(object sender, RoutedEventArgs e)
    {
        var hostname = DnsHostnameTextBox.Text;
        if (string.IsNullOrWhiteSpace(hostname))
        {
            CustomMessageBox.Show(this, "Please enter a domain name");
            return;
        }

        var recordType = ((ComboBoxItem)DnsRecordTypeComboBox.SelectedItem)?.Content?.ToString() ?? "A";

        DnsStatusTextBlock.Text = $"Looking up {recordType} records for {hostname}...";
        DnsResultsTextBox.Text = "";

        try
        {
            var result = await _dnsLookup.LookupAsync(hostname, recordType);

            if (result.IsSuccess)
            {
                DisplayDnsResult(result);
                DnsStatusTextBlock.Text = $"DNS lookup completed - {result.Records.Count} record(s) found in {result.QueryTime.TotalMilliseconds:F0}ms";
                DnsStatusTextBlock.Foreground = (SolidColorBrush)FindResource("SuccessBrush");
            }
            else
            {
                DnsResultsTextBox.Text = $"Error: {result.Error}";
                DnsStatusTextBlock.Text = "DNS lookup failed";
                DnsStatusTextBlock.Foreground = (SolidColorBrush)FindResource("ErrorBrush");
            }
        }
        catch (Exception ex)
        {
            DnsResultsTextBox.Text = $"Error: {ex.Message}";
            DnsStatusTextBlock.Text = "DNS lookup failed";
            DnsStatusTextBlock.Foreground = (SolidColorBrush)FindResource("ErrorBrush");
        }
    }

    private void DisplayDnsResult(DnsResult result)
    {
        var output = new StringBuilder();
        output.AppendLine($"=== DNS LOOKUP RESULTS ===");
        output.AppendLine($"Domain: {result.Domain}");
        output.AppendLine($"Record Type: {result.RecordType}");
        output.AppendLine($"DNS Server: {result.DnsServer}");
        output.AppendLine($"Query Time: {result.QueryTime.TotalMilliseconds:F0}ms");
        output.AppendLine($"Records Found: {result.Records.Count}");
        output.AppendLine();

        if (result.Records.Count > 0)
        {
            foreach (var record in result.Records)
            {
                output.AppendLine($"=== {record.Type} RECORD ===");
                output.AppendLine($"Name: {record.Name}");
                output.AppendLine($"Value: {record.Value}");

                if (record.TTL > 0)
                    output.AppendLine($"TTL: {record.TTL}");

                if (record.Priority > 0)
                    output.AppendLine($"Priority: {record.Priority}");

                output.AppendLine();
            }
        }
        else
        {
            output.AppendLine("No records found for this domain and record type.");
        }

        DnsResultsTextBox.Text = output.ToString();
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
        // In a tray app, close should hide so it can be re-opened from the tray/hotkey
        HideWindow();
    }

    private void ToggleMaximize()
    {
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    }

    private void TrySetWindowIconFromExe()
    {
        try
        {
            var exe = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
            if (!string.IsNullOrEmpty(exe))
            {
                using var icon = System.Drawing.Icon.ExtractAssociatedIcon(exe);
                if (icon != null)
                {
                    var source = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(
                        icon.Handle,
                        Int32Rect.Empty,
                        BitmapSizeOptions.FromEmptyOptions());
                    source.Freeze();
                    this.Icon = source;
                }
            }
        }
        catch
        {
            // ignore icon failures
        }
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        // Gracefully shutdown DNS operations before window closes
        try
        {
            _dnsLookup?.Shutdown();
        }
        catch
        {
            // Ignore shutdown errors
        }

        base.OnClosing(e);
    }

    protected override void OnClosed(EventArgs e)
    {
        _httpService?.Dispose();
        _redirectChecker?.Dispose();
        _dnsLookup?.Dispose();
        _terminalService?.Dispose();
        base.OnClosed(e);
    }

    private void HotkeyButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var app = (App)System.Windows.Application.Current;
            var settingsService = app.GetSettingsService();
            var globalHotkey = app.GetGlobalHotkey();
            var trayIconManager = app.GetTrayIconManager();

            if (settingsService == null || globalHotkey == null || trayIconManager == null)
            {
                CustomMessageBox.Show("Services not available", "Configuration Error");
                return;
            }

            var hotkeyWindow = new HotkeyConfigWindow(settingsService)
            {
                Owner = this
            };

            if (hotkeyWindow.ShowDialog() == true && hotkeyWindow.HotkeyChanged)
            {
                // Get the captured hotkey
                if (hotkeyWindow.GetCapturedHotkey(out bool useCtrl, out bool useAlt, out bool useWin, out bool useShift, out int virtualKey, out string keyDisplay))
                {
                    // Build modifiers for Win32 API
                    int modifiers = 0;
                    if (useCtrl) modifiers |= 0x0002; // MOD_CONTROL
                    if (useAlt) modifiers |= 0x0001; // MOD_ALT
                    if (useWin) modifiers |= 0x0008; // MOD_WIN
                    if (useShift) modifiers |= 0x0004; // MOD_SHIFT

                    // Try to register the new hotkey
                    if (globalHotkey.Register(modifiers, virtualKey))
                    {
                        // Save settings (store the display representation for persistence)
                        settingsService.UpdateHotkey(useCtrl, useAlt, useWin, useShift, virtualKey, keyDisplay);

                        // Update tray icon text
                        trayIconManager.UpdateHotkeyText(settingsService.GetHotkeyDisplayText());
                    }
                    else
                    {
                        // Registration failed - show error message
                        CustomMessageBox.Show("Failed to register the hotkey. The combination might already be in use by another application.", "Hotkey Registration Failed");

                        // Revert to previous settings
                        var prevSettings = settingsService.Settings.Hotkey;
                        globalHotkey.UpdateHotkey(prevSettings.UseCtrl, prevSettings.UseAlt, prevSettings.UseWin, prevSettings.UseShift, prevSettings.VirtualKeyCode);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            CustomMessageBox.Show($"Error configuring hotkey: {ex.Message}", "Configuration Error");
        }
    }

    // Terminal Methods
    private async void StartShellButton_Click(object sender, RoutedEventArgs e)
    {
        var shellType = GetSelectedShellType();
        TerminalStatusTextBlock.Text = $"Starting {shellType}...";

        var success = await _terminalService.StartShellAsync(shellType);

        if (success)
        {
            TerminalStatusTextBlock.Text = $"{shellType} running";
            TerminalStatusTextBlock.Foreground = (SolidColorBrush)FindResource("SuccessBrush");
            StartShellButton.IsEnabled = false;
            StopShellButton.IsEnabled = true;
            CommandInputTextBox.IsEnabled = true;
            ExecuteCommandButton.IsEnabled = true;
            PowerShellRadioButton.IsEnabled = false;
            CmdRadioButton.IsEnabled = false;
            WslRadioButton.IsEnabled = false;
            CommandInputTextBox.Focus();
        }
        else
        {
            TerminalStatusTextBlock.Text = $"Failed to start {shellType}";
            TerminalStatusTextBlock.Foreground = (SolidColorBrush)FindResource("ErrorBrush");
        }
    }

    private void StopShellButton_Click(object sender, RoutedEventArgs e)
    {
        _terminalService.Stop();
        TerminalStatusTextBlock.Text = "Shell stopped";
        TerminalStatusTextBlock.Foreground = (SolidColorBrush)FindResource("TextSecondaryBrush");
        StartShellButton.IsEnabled = true;
        StopShellButton.IsEnabled = false;
        CommandInputTextBox.IsEnabled = false;
        ExecuteCommandButton.IsEnabled = false;
        PowerShellRadioButton.IsEnabled = true;
        CmdRadioButton.IsEnabled = true;
        WslRadioButton.IsEnabled = true;
    }

    private void ClearTerminalButton_Click(object sender, RoutedEventArgs e)
    {
        TerminalOutputTextBox.Text = "";
        _terminalService.ClearOutput();
    }

    private async void ExecuteCommandButton_Click(object sender, RoutedEventArgs e)
    {
        await ExecuteCommand();
    }

    private async void CommandInputTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            await ExecuteCommand();
        }
    }

    private async Task ExecuteCommand()
    {
        var command = CommandInputTextBox.Text;
        if (string.IsNullOrWhiteSpace(command) || !_terminalService.IsRunning)
            return;

        // Display the command in the output
        Dispatcher.Invoke(() =>
        {
            TerminalOutputTextBox.AppendText($"> {command}\n");
            CommandInputTextBox.Text = "";
            ScrollToBottom();
        });

        await _terminalService.ExecuteCommandAsync(command);
    }

    private void OnTerminalOutputReceived(object? sender, string output)
    {
        Dispatcher.Invoke(() =>
        {
            TerminalOutputTextBox.AppendText(output + "\n");
            ScrollToBottom();
        });
    }

    private void OnTerminalErrorReceived(object? sender, string error)
    {
        Dispatcher.Invoke(() =>
        {
            TerminalOutputTextBox.AppendText($"ERROR: {error}\n");
            ScrollToBottom();
        });
    }

    private void OnTerminalProcessExited(object? sender, EventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            TerminalOutputTextBox.AppendText("\n[Process exited]\n");
            TerminalStatusTextBlock.Text = "Shell exited";
            TerminalStatusTextBlock.Foreground = (SolidColorBrush)FindResource("TextSecondaryBrush");
            StartShellButton.IsEnabled = true;
            StopShellButton.IsEnabled = false;
            CommandInputTextBox.IsEnabled = false;
            ExecuteCommandButton.IsEnabled = false;
            PowerShellRadioButton.IsEnabled = true;
            CmdRadioButton.IsEnabled = true;
            WslRadioButton.IsEnabled = true;
            ScrollToBottom();
        });
    }

    private void ShellTypeRadioButton_Changed(object sender, RoutedEventArgs e)
    {
        // Only handle selection changes if the terminal service is initialized and the UI is loaded
        if (_terminalService == null || !IsLoaded)
            return;

        // If shell is running, stop it when changing shell type
        if (_terminalService.IsRunning)
        {
            StopShellButton_Click(sender, new RoutedEventArgs());
        }
    }

    private string GetSelectedShellType()
    {
        if (PowerShellRadioButton?.IsChecked == true)
            return "powershell";
        else if (CmdRadioButton?.IsChecked == true)
            return "cmd";
        else if (WslRadioButton?.IsChecked == true)
            return "wsl";

        return "powershell";
    }

    private void ScrollToBottom()
    {
        TerminalScrollViewer?.ScrollToEnd();
    }
}
