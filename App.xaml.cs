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

        var rackRepository = new RackRepository();
        var rackStateDtos = rackRepository.LoadAllRackData();
        if (!rackStateDtos.Any())
        {
            var newRackWindow = new NewRackWindow();
            bool? result = newRackWindow.ShowDialog();

            if (result == true)
            {
                rackRepository.CreateAndSaveNewRack(newRackWindow.RackName, true);
            }
            else
            {
                Application.Current.Shutdown();
                return;
            }
        }
        var mainViewModel = new MainViewModel(rackRepository);

        var mainWindow = new MainWindow
        {
            DataContext = mainViewModel
            
        };

        mainWindow.Show();

    }
}

