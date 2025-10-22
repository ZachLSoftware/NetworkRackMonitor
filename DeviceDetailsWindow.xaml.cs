using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace RackMonitor
{
    /// <summary>
    /// Interaction logic for DeviceDetailsWindow.xaml
    /// </summary>
    public partial class DeviceDetailsWindow : Window
    {
        public DeviceDetailsWindow()
        {
            InitializeComponent();
        }

        private void OnBoolPropertyChanged(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is ViewModels.DeviceDetailsViewModel viewModel)
            {
                if (viewModel.SaveCommand.CanExecute(null))
                {
                    viewModel.SaveCommand.Execute(null);
                }
            }
        }
    }
}
