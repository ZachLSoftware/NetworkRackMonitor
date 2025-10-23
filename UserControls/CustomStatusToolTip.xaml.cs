using RackMonitor.Models;
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
    /// Interaction logic for CustomStatusToolTip.xaml
    /// </summary>
    public partial class CustomStatusToolTip : UserControl
    {
        public CustomStatusToolTip()
        {
            InitializeComponent();
        }
        public static readonly DependencyProperty DeviceProperty =
           DependencyProperty.Register("Device", typeof(RackDevice), typeof(CustomStatusToolTip), new PropertyMetadata(null));

        public RackDevice Device
        {
            get { return (RackDevice)GetValue(DeviceProperty); }
            set { SetValue(DeviceProperty, value); }
        }
    }
}
