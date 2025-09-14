using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace NetKit;

public class TrayIconManager : IDisposable
{
    private NotifyIcon? _notifyIcon;

    public event Action? ShowWindow;
    public event Action? ExitApplication;

    public TrayIconManager()
    {
        _notifyIcon = new NotifyIcon
        {
            Icon = LoadAppIcon() ?? SystemIcons.Application,
            Visible = true,
            Text = "NetKit (Win+' to toggle)"
        };

        var contextMenu = new ContextMenuStrip();
        contextMenu.Items.Add("Show/Hide Window (Win+')", null, OnShowHideClicked);
        contextMenu.Items.Add("-"); // Separator
        contextMenu.Items.Add("Exit", null, OnExitClicked);

        _notifyIcon.ContextMenuStrip = contextMenu;
        _notifyIcon.DoubleClick += OnDoubleClick;
    }

    private static Icon? LoadAppIcon()
    {
        try
        {
            var exe = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
            if (!string.IsNullOrEmpty(exe))
            {
                var icon = Icon.ExtractAssociatedIcon(exe);
                return icon;
            }
        }
        catch
        {
            // ignore
        }
        return null;
    }

    public void UpdateHotkeyText(string hotkeyText)
    {
        if (_notifyIcon != null)
        {
            _notifyIcon.Text = $"NetKit ({hotkeyText} to toggle)";

            // Update context menu item text too
            if (_notifyIcon.ContextMenuStrip?.Items.Count > 0)
            {
                _notifyIcon.ContextMenuStrip.Items[0].Text = $"Show/Hide Window ({hotkeyText})";
            }
        }
    }

    private void OnShowHideClicked(object? sender, EventArgs e)
    {
        ShowWindow?.Invoke();
    }

    private void OnDoubleClick(object? sender, EventArgs e)
    {
        ShowWindow?.Invoke();
    }

    private void OnExitClicked(object? sender, EventArgs e)
    {
        ExitApplication?.Invoke();
    }

    public void Dispose()
    {
        _notifyIcon?.Dispose();
        GC.SuppressFinalize(this);
    }
}
