using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Windows.Interop;

namespace NetKit;

public class GlobalHotkey : IDisposable
{
    private const int WM_HOTKEY = 0x0312;
    private const int MOD_CONTROL = 0x0002;
    private const int MOD_ALT = 0x0001;
    private const int MOD_WIN = 0x0008;
    private const int MOD_SHIFT = 0x0004;

    private readonly int _id;
    private readonly IntPtr _hWnd;
    private bool _disposed = false;
    private bool _isRegistered = false;

    private int _currentModifiers = MOD_WIN;
    private int _currentVirtualKey = 0xDE;

    public event Action? HotkeyPressed;

    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vlc);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    public GlobalHotkey(IntPtr windowHandle, int hotkeyId = 1)
    {
        _hWnd = windowHandle;
        _id = hotkeyId;
    }

    public bool Register()
    {
        return Register(_currentModifiers, _currentVirtualKey);
    }

    public bool Register(int modifiers, int virtualKey)
    {
        try
        {
            // Unregister existing hotkey if registered
            if (_isRegistered)
            {
                UnregisterHotKey(_hWnd, _id);
                _isRegistered = false;
            }

            _currentModifiers = modifiers;
            _currentVirtualKey = virtualKey;

            var success = RegisterHotKey(_hWnd, _id, modifiers, virtualKey);
            _isRegistered = success;
            return success;
        }
        catch
        {
            return false;
        }
    }

    public bool UpdateHotkey(bool useCtrl, bool useAlt, bool useWin, bool useShift, int virtualKey)
    {
        int modifiers = 0;
        if (useCtrl) modifiers |= MOD_CONTROL;
        if (useAlt) modifiers |= MOD_ALT;
        if (useWin) modifiers |= MOD_WIN;
        if (useShift) modifiers |= MOD_SHIFT;

        return Register(modifiers, virtualKey);
    }

    public void Unregister()
    {
        try
        {
            if (_isRegistered)
            {
                UnregisterHotKey(_hWnd, _id);
                _isRegistered = false;
            }
        }
        catch
        {
            // Ignore unregister errors
        }
    }

    public bool ProcessHotkey(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY && wParam.ToInt32() == _id)
        {
            HotkeyPressed?.Invoke();
            handled = true;
            return true;
        }
        return false;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            Unregister();
            _disposed = true;
        }
    }
}