using RackMonitor.Data;
using RackMonitor.Services;
using RackMonitor.ViewModels;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Windows;

namespace RackMonitor;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 1. Create a single, shared instance of the repository.
        var rackRepository = new RackRepository();

        // 2. Create the main ViewModel and give it the repository.
        var rackViewModel = new MainViewModel(rackRepository);


        // 3. Create the main window and set its DataContext.
        var mainWindow = new MainWindow
        {
            DataContext = rackViewModel
            
        };

        mainWindow.Show();

        MonitoringService monitor = new MonitoringService(rackRepository);
        monitor.StartMonitoring();
        monitor.updateDevices += () =>
        {
            rackRepository.SaveState();
        };

        rackViewModel.PingToggled += (object sender, PingServiceToggledEventArgs e) =>
        {
            Debug.WriteLine($"In event handler. e: {e.IsEnabled}, monitor: {monitor.IsRunning}");
            if (e.IsEnabled && !monitor.IsRunning)
            {
                monitor.StartMonitoring();
            }
            else if (!e.IsEnabled && monitor.IsRunning)
            {
                monitor.StopMonitoring();
            }
        };

        rackViewModel.WoLToggled += (object sender, WoLServiceToggledEventArgs e) =>
        {
            monitor.IsWoLRunning = e.IsEnabled;
        };

        // 4. In the future, your monitoring service would be created here
        //    and also be given the SAME 'rackRepository' instance.
        //
        // var monitoringService = new MonitoringService(rackRepository);
        // monitoringService.StartMonitoring();
    }
}

