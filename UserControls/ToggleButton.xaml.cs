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
    /// Interaction logic for ToggleButton.xaml
    /// </summary>
    public partial class ToggleButton : UserControl
    {
        public ToggleButton()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty IsOnProperty =
            DependencyProperty.Register(
                "IsOn",                         // Property name
                typeof(bool),                   // Property type
                typeof(ToggleButton),           // Owner class
                new FrameworkPropertyMetadata(
                    false,                      // Default value
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault // Enable TwoWay binding
                )
            );

        // Standard .NET property wrapper
        public bool IsOn
        {
            get { return (bool)GetValue(IsOnProperty); }
            set { SetValue(IsOnProperty, value); }
        }
    }
}

