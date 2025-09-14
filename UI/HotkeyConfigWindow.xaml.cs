using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace NetKit;

public partial class HotkeyConfigWindow : Window
{
    private bool _capturing = false;
    private readonly HashSet<Key> _pressedKeys = new();
    private readonly SettingsService _settingsService;
    private int _capturedModifiers = 0;
    private int _capturedVirtualKey = 0;
    private string _capturedKeyDisplay = "";

    public bool HotkeyChanged { get; private set; } = false;

    public HotkeyConfigWindow(SettingsService settingsService)
    {
        InitializeComponent();
        _settingsService = settingsService;

        // Set current hotkey display
        CurrentHotkeyTextBox.Text = _settingsService.GetHotkeyDisplayText();

        // Make window focusable and focus the capture box
        Loaded += (s, e) =>
        {
            Focus();
            CaptureBox.MouseLeftButtonDown += (sender, args) => StartCapturing();
        };
    }

    private void StartCapturing()
    {
        _capturing = true;
        _pressedKeys.Clear();
        _capturedModifiers = 0;
        _capturedVirtualKey = 0;
        _capturedKeyDisplay = "";

        CapturedHotkeyTextBox.Text = "Press keys now...";
        CapturedHotkeyTextBox.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 255));
        CaptureBox.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 208, 132));

        StatusTextBlock.Text = "";
        Focus();
    }

    private void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (!_capturing) return;

        e.Handled = true;

        // Add key to pressed keys set
        _pressedKeys.Add(e.Key);

        // Update display in real-time
        UpdateCapturedDisplay();
    }

    private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (!_capturing) return;
        e.Handled = true;
    }

    private void UpdateCapturedDisplay()
    {
        var modifiers = new List<string>();
        var regularKey = "";

        _capturedModifiers = 0;
        _capturedVirtualKey = 0;

        // Process all pressed keys
        foreach (var key in _pressedKeys)
        {
            switch (key)
            {
                case Key.LeftCtrl:
                case Key.RightCtrl:
                    if (!modifiers.Contains("Ctrl"))
                    {
                        modifiers.Add("Ctrl");
                        _capturedModifiers |= 0x0002; // MOD_CONTROL
                    }
                    break;

                case Key.LeftAlt:
                case Key.RightAlt:
                    if (!modifiers.Contains("Alt"))
                    {
                        modifiers.Add("Alt");
                        _capturedModifiers |= 0x0001; // MOD_ALT
                    }
                    break;

                case Key.LWin:
                case Key.RWin:
                    if (!modifiers.Contains("Win"))
                    {
                        modifiers.Add("Win");
                        _capturedModifiers |= 0x0008; // MOD_WIN
                    }
                    break;

                case Key.LeftShift:
                case Key.RightShift:
                    if (!modifiers.Contains("Shift"))
                    {
                        modifiers.Add("Shift");
                        _capturedModifiers |= 0x0004; // MOD_SHIFT
                    }
                    break;

                default:
                    // This is a regular key
                    if (string.IsNullOrEmpty(regularKey))
                    {
                        regularKey = GetKeyDisplayName(key);
                        _capturedVirtualKey = KeyToVirtualKey(key);
                        _capturedKeyDisplay = regularKey;
                    }
                    break;
            }
        }

        // Build display text
        var displayText = new StringBuilder();
        if (modifiers.Count > 0)
        {
            displayText.Append(string.Join(" + ", modifiers));
            if (!string.IsNullOrEmpty(regularKey))
            {
                displayText.Append(" + ");
                displayText.Append(regularKey);
            }
        }
        else if (!string.IsNullOrEmpty(regularKey))
        {
            displayText.Append(regularKey);
        }
        else
        {
            displayText.Append("Press keys...");
        }

        CapturedHotkeyTextBox.Text = displayText.ToString();

        // Validate combination
        if (_capturedModifiers != 0 && _capturedVirtualKey != 0)
        {
            StatusTextBlock.Text = "Valid hotkey combination captured!";
            StatusTextBlock.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 208, 132));
            ApplyButton.IsEnabled = true;
        }
        else if (_capturedModifiers == 0 && _capturedVirtualKey != 0)
        {
            StatusTextBlock.Text = "Warning: No modifier keys pressed. Add Ctrl, Alt, Win, or Shift.";
            StatusTextBlock.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 165, 0));
            ApplyButton.IsEnabled = false;
        }
        else
        {
            StatusTextBlock.Text = "";
            ApplyButton.IsEnabled = false;
        }
    }

    private string GetKeyDisplayName(Key key)
    {
        return key switch
        {
            Key.Space => "Space",
            Key.Tab => "Tab",
            Key.Enter => "Enter",
            Key.Escape => "Esc",
            Key.Back => "Backspace",
            Key.Delete => "Delete",
            Key.Insert => "Insert",
            Key.Home => "Home",
            Key.End => "End",
            Key.PageUp => "Page Up",
            Key.PageDown => "Page Down",
            Key.Up => "↑",
            Key.Down => "↓",
            Key.Left => "←",
            Key.Right => "→",
            Key.F1 => "F1",
            Key.F2 => "F2",
            Key.F3 => "F3",
            Key.F4 => "F4",
            Key.F5 => "F5",
            Key.F6 => "F6",
            Key.F7 => "F7",
            Key.F8 => "F8",
            Key.F9 => "F9",
            Key.F10 => "F10",
            Key.F11 => "F11",
            Key.F12 => "F12",
            Key.D0 => "0",
            Key.D1 => "1",
            Key.D2 => "2",
            Key.D3 => "3",
            Key.D4 => "4",
            Key.D5 => "5",
            Key.D6 => "6",
            Key.D7 => "7",
            Key.D8 => "8",
            Key.D9 => "9",
            Key.OemTilde => "`",
            Key.OemMinus => "-",
            Key.OemPlus => "=",
            Key.OemOpenBrackets => "[",
            Key.OemCloseBrackets => "]",
            Key.OemPipe => "\\",
            Key.OemSemicolon => ";",
            Key.OemQuotes => "'",
            Key.OemComma => ",",
            Key.OemPeriod => ".",
            Key.OemQuestion => "/",
            _ => key.ToString()
        };
    }

    private int KeyToVirtualKey(Key key)
    {
        return key switch
        {
            Key.A => 0x41,
            Key.B => 0x42,
            Key.C => 0x43,
            Key.D => 0x44,
            Key.E => 0x45,
            Key.F => 0x46,
            Key.G => 0x47,
            Key.H => 0x48,
            Key.I => 0x49,
            Key.J => 0x4A,
            Key.K => 0x4B,
            Key.L => 0x4C,
            Key.M => 0x4D,
            Key.N => 0x4E,
            Key.O => 0x4F,
            Key.P => 0x50,
            Key.Q => 0x51,
            Key.R => 0x52,
            Key.S => 0x53,
            Key.T => 0x54,
            Key.U => 0x55,
            Key.V => 0x56,
            Key.W => 0x57,
            Key.X => 0x58,
            Key.Y => 0x59,
            Key.Z => 0x5A,
            Key.D0 => 0x30,
            Key.D1 => 0x31,
            Key.D2 => 0x32,
            Key.D3 => 0x33,
            Key.D4 => 0x34,
            Key.D5 => 0x35,
            Key.D6 => 0x36,
            Key.D7 => 0x37,
            Key.D8 => 0x38,
            Key.D9 => 0x39,
            Key.F1 => 0x70,
            Key.F2 => 0x71,
            Key.F3 => 0x72,
            Key.F4 => 0x73,
            Key.F5 => 0x74,
            Key.F6 => 0x75,
            Key.F7 => 0x76,
            Key.F8 => 0x77,
            Key.F9 => 0x78,
            Key.F10 => 0x79,
            Key.F11 => 0x7A,
            Key.F12 => 0x7B,
            Key.Space => 0x20,
            Key.Tab => 0x09,
            Key.Enter => 0x0D,
            Key.Escape => 0x1B,
            Key.Back => 0x08,
            Key.Delete => 0x2E,
            Key.Insert => 0x2D,
            Key.Home => 0x24,
            Key.End => 0x23,
            Key.PageUp => 0x21,
            Key.PageDown => 0x22,
            Key.Up => 0x26,
            Key.Down => 0x28,
            Key.Left => 0x25,
            Key.Right => 0x27,
            Key.OemTilde => 0xC0,
            Key.OemMinus => 0xBD,
            Key.OemPlus => 0xBB,
            Key.OemOpenBrackets => 0xDB,
            Key.OemCloseBrackets => 0xDD,
            Key.OemPipe => 0xDC,
            Key.OemSemicolon => 0xBA,
            Key.OemQuotes => 0xDE,
            Key.OemComma => 0xBC,
            Key.OemPeriod => 0xBE,
            Key.OemQuestion => 0xBF,
            _ => 0
        };
    }

    private void ApplyButton_Click(object sender, RoutedEventArgs e)
    {
        if (_capturedModifiers == 0 || _capturedVirtualKey == 0)
        {
            StatusTextBlock.Text = "Please capture a valid hotkey first.";
            StatusTextBlock.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 85, 85));
            return;
        }

        HotkeyChanged = true;
        DialogResult = true;
        Close();
    }

    private void ResetButton_Click(object sender, RoutedEventArgs e)
    {
        _capturedModifiers = 0x0008; // MOD_WIN
        _capturedVirtualKey = 0xDE; // Apostrophe
        _capturedKeyDisplay = "'";

        CapturedHotkeyTextBox.Text = "Win + '";
        StatusTextBlock.Text = "Reset to default combination.";
        StatusTextBlock.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 208, 132));
        ApplyButton.IsEnabled = true;

        _capturing = false;
        CaptureBox.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(100, 100, 100));
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    public bool GetCapturedHotkey(out bool useCtrl, out bool useAlt, out bool useWin, out bool useShift, out int virtualKey, out string keyDisplay)
    {
        useCtrl = (_capturedModifiers & 0x0002) != 0;
        useAlt = (_capturedModifiers & 0x0001) != 0;
        useWin = (_capturedModifiers & 0x0008) != 0;
        useShift = (_capturedModifiers & 0x0004) != 0;
        virtualKey = _capturedVirtualKey;
        keyDisplay = _capturedKeyDisplay;

        return _capturedModifiers != 0 && _capturedVirtualKey != 0;
    }
}