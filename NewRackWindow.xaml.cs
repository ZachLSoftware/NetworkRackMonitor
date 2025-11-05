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

    public partial class NewRackWindow : Window
    {
        public string RackName { get; private set; }
        public List<string> RackNames { get; set; }

        public NewRackWindow()
        {
            InitializeComponent();
            RackNameTextBox.Focus();
        }

        public NewRackWindow(List<string> rackNames)
        {
            RackNames = rackNames;
            InitializeComponent();
            RackNameTextBox.Focus();
        }

        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            string rackName = RackNameTextBox.Text;
            if (string.IsNullOrWhiteSpace(rackName))
            {
                MessageBox.Show("Please enter a name for the rack.", "Name Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Check for invalid file name characters
            if (rackName.IndexOfAny(System.IO.Path.GetInvalidFileNameChars()) >= 0)
            {
                MessageBox.Show("The name contains invalid characters. Please avoid characters like \\ / : * ? \" < > |", "Invalid Name", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (RackNames != null && RackNames.Contains(rackName))
            {
                MessageBox.Show("Please enter Unique name for the rack.", "Name Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            this.RackName = rackName;
            this.DialogResult = true; // Signals "OK"
            this.Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false; // Signals "Cancel"
            this.Close();
        }
    }
}
