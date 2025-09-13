using System;
using System.Drawing;
using System.Windows;
using System.Windows.Forms;

namespace msOps.Tray
{
    public class TrayIconManager : IDisposable
    {
        private NotifyIcon? _notifyIcon;

        public void Initialize()
        {
            _notifyIcon = new NotifyIcon
            {
                Icon = SystemIcons.Application,
                Visible = true,
                Text = "msOps Helper"
            };

            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("Open", null, OnOpenClicked);
            contextMenu.Items.Add("Exit", null, OnExitClicked);

            _notifyIcon.ContextMenuStrip = contextMenu;
        }

        private void OnOpenClicked(object? sender, EventArgs e)
        {
            var window = new msOps.MainWindow();
            window.Show();
            window.Activate();
        }

        private void OnExitClicked(object? sender, EventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }

        public void Dispose()
        {
            _notifyIcon?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
