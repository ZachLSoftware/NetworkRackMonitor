using RackMonitor.ViewModels;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace RackMonitor.UserControls
{
    /// <summary>
    /// Interaction logic for DeviceDetailsView.xaml
    /// </summary>
    public partial class DeviceDetailsView : UserControl
    {
        public DeviceDetailsView()
        {
            InitializeComponent();
        }

        private void General_Filter(object sender, FilterEventArgs e)
        {
            if (e.Item is DevicePropertyViewModel propVM)
            {
                e.Accepted = string.IsNullOrEmpty(propVM.Category) || propVM.Category.Equals("General", StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                e.Accepted = false;
            }
        }
        private void Network_Filter(object sender, FilterEventArgs e)
        {
            if (e.Item is DevicePropertyViewModel propVM)
            {
                e.Accepted = string.IsNullOrEmpty(propVM.Category) || propVM.Category.Equals("Network", StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                e.Accepted = false;
            }
        }
    }
}
