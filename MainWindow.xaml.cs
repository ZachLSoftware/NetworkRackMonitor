using RackMonitor.ViewModels;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace RackMonitor;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private ToolTip StatusToolTip = new ToolTip();
    public MainWindow()
    {
        InitializeComponent();
    }
    private void NumberOfUnits_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            // Get the TextBox
            TextBox tBox = (TextBox)sender;

            // Get the binding expression
            BindingExpression binding = tBox.GetBindingExpression(TextBox.TextProperty);

            // Manually update the source (your ViewModel property)
            if (binding != null)
            {
                binding.UpdateSource();
            }

            // Move focus off the TextBox to make it feel "submitted"
            Keyboard.ClearFocus();
        }
    }

    private void Unit_Up_Button_Click(object sender, RoutedEventArgs e)
    {
        // Get the ViewModel from the window's DataContext
        if (this.DataContext is RackViewModel vm)
        {
            vm.NumberOfUnits++;

            // Manually trigger the update command if you want it to run immediately
            if (vm.UpdateRackSizeCommand.CanExecute(null))
            {
                vm.UpdateRackSizeCommand.Execute(null);
            }
        }
    }

    private void Unit_Down_Button_Click(object sender, RoutedEventArgs e)
    {
        if (this.DataContext is RackViewModel vm)
        {
            // Add a check to prevent going below 1
            if (vm.NumberOfUnits > 1)
            {
                vm.NumberOfUnits--;

                if (vm.UpdateRackSizeCommand.CanExecute(null))
                {
                    vm.UpdateRackSizeCommand.Execute(null);
                }
            }
        }
    }
}