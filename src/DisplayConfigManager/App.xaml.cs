using System.Windows;
using System.Windows.Threading;
using DisplayConfigManager.UI;

namespace DisplayConfigManager;

public partial class App : System.Windows.Application
{
    private TrayApplicationContext? _trayContext;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DispatcherUnhandledException += (_, ex) =>
        {
            MessageBox.Show($"Unhandled error:\n\n{ex.Exception}", "Display Config Manager",
                MessageBoxButton.OK, MessageBoxImage.Error);
            ex.Handled = true;
        };

        AppDomain.CurrentDomain.UnhandledException += (_, ex) =>
        {
            MessageBox.Show($"Fatal error:\n\n{ex.ExceptionObject}", "Display Config Manager",
                MessageBoxButton.OK, MessageBoxImage.Error);
        };

        try
        {
            _trayContext = new TrayApplicationContext();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Startup failed:\n\n{ex}", "Display Config Manager",
                MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _trayContext?.Dispose();
        base.OnExit(e);
    }
}
