using System.Windows;
using msOps.Tray;

namespace msOps
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        private TrayIconManager? _trayIconManager;

        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                base.OnStartup(e);

                _trayIconManager = new TrayIconManager();
                _trayIconManager.Initialize();

                var mainWindow = new MainWindow();
                this.MainWindow = mainWindow;
                mainWindow.Show();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Startup Error: {ex.Message}\n\nStack Trace: {ex.StackTrace}", "Application Error");
                this.Shutdown(1);
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _trayIconManager?.Dispose();
            base.OnExit(e);
        }
    }
}

