using System;
using System.Windows;
using System.Windows.Interop;

namespace NetKit;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
    private GlobalHotkey? _globalHotkey;
    private SettingsService? _settingsService;
    private TrayIconManager? _trayIconManager;
    private MainWindow? _mainWindow;

    protected override void OnStartup(StartupEventArgs e)
    {
        try
        {
            base.OnStartup(e);

            // Initialize services first
            _settingsService = new SettingsService();

            // Create main window but start hidden
            _mainWindow = new MainWindow(_settingsService);
            this.MainWindow = _mainWindow;
            _mainWindow.Closed += (s, _) => { _mainWindow = null; };

            // Initialize tray icon
            _trayIconManager = new TrayIconManager();
            _trayIconManager.ShowWindow += ShowOrCreateMainWindow;
            _trayIconManager.ExitApplication += () => this.Shutdown();

            // Set up global hotkey with a hidden helper window
            SetupGlobalHotkey();

            // Don't call Show() - window starts hidden
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Startup Error: {ex.Message}\n\nStack Trace: {ex.StackTrace}", "Application Error");
            this.Shutdown(1);
        }
    }

    private void SetupGlobalHotkey()
    {
        try
        {
            // Create a hidden helper window just for the hotkey handle
            var helperWindow = new Window
            {
                Width = 1,
                Height = 1,
                WindowStyle = WindowStyle.None,
                ShowInTaskbar = false,
                Visibility = Visibility.Hidden
            };

            // Force creation of window handle
            helperWindow.Show();
            var windowHandle = new WindowInteropHelper(helperWindow).Handle;
            helperWindow.Hide();

            // Initialize global hotkey
            _globalHotkey = new GlobalHotkey(windowHandle);
            var settings = _settingsService!.Settings.Hotkey;

            // Connect hotkey to show/hide functionality
            _globalHotkey.HotkeyPressed += ShowOrCreateMainWindow;

            // Add hook for hotkey messages
            var source = HwndSource.FromHwnd(windowHandle);
            source?.AddHook(WndProc);

            // Register the hotkey with saved settings
            if (_globalHotkey.UpdateHotkey(settings.UseCtrl, settings.UseAlt, settings.UseWin, settings.UseShift, settings.VirtualKeyCode))
            {
                _trayIconManager?.UpdateHotkeyText(_settingsService.GetHotkeyDisplayText());
            }
            else
            {
                // If saved hotkey fails, try default Win+'
                if (_globalHotkey.UpdateHotkey(false, false, true, false, 0xDE))
                {
                    _settingsService.UpdateHotkey(false, false, true, false, 0xDE, "'");
                    _trayIconManager?.UpdateHotkeyText(_settingsService.GetHotkeyDisplayText());
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Hotkey registration failed: {ex.Message}");
        }
    }

    private void ShowOrCreateMainWindow()
    {
        try
        {
            if (_mainWindow == null || !_mainWindow.IsLoaded)
            {
                _mainWindow = new MainWindow(_settingsService!);
                _mainWindow.Closed += (s, _) => { _mainWindow = null; };
                this.MainWindow = _mainWindow;
            }

            _mainWindow.ShowWindow();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to show/create MainWindow: {ex.Message}");
        }
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        _globalHotkey?.ProcessHotkey(hwnd, msg, wParam, lParam, ref handled);
        return IntPtr.Zero;
    }

    public GlobalHotkey? GetGlobalHotkey() => _globalHotkey;
    public SettingsService? GetSettingsService() => _settingsService;
    public TrayIconManager? GetTrayIconManager() => _trayIconManager;

    protected override void OnExit(ExitEventArgs e)
    {
        _globalHotkey?.Dispose();
        _trayIconManager?.Dispose();
        base.OnExit(e);
    }
}
