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
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnIsOnChanged
                )
            );

        // Standard .NET property wrapper
        public bool IsOn
        {
            get { return (bool)GetValue(IsOnProperty); }
            set { SetValue(IsOnProperty, value); }
        }

        public static readonly DependencyProperty CommandProperty =
        DependencyProperty.Register("Command", typeof(ICommand), typeof(ToggleButton), new PropertyMetadata(null));

        public ICommand Command
        {
            get { return (ICommand)GetValue(CommandProperty); }
            set { SetValue(CommandProperty, value); }
        }

        
        public static readonly DependencyProperty CommandParameterProperty =
            DependencyProperty.Register("CommandParameter", typeof(object), typeof(ToggleButton), new PropertyMetadata(null));

        public object CommandParameter
        {
            get { return (object)GetValue(CommandParameterProperty); }
            set { SetValue(CommandParameterProperty, value); }
        }

        private static void OnIsOnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ToggleButton toggleButton && toggleButton.Command != null)
            {
                // Use CommandParameter if provided, otherwise pass the new boolean state (e.NewValue)
                object parameter = toggleButton.CommandParameter ?? e.NewValue;

                if (toggleButton.Command.CanExecute(parameter))
                {
                    toggleButton.Command.Execute(parameter);
                }
            }
        }
    }
}

