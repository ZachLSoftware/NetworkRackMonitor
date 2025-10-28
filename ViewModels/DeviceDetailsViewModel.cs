using RackMonitor.Extensions;
using RackMonitor.Models;
using RackMonitor.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security;
using System.Windows;
using System.Windows.Input;

namespace RackMonitor.ViewModels
{
    public class DeviceDetailsViewModel : INotifyPropertyChanged
    {
        private readonly RackDevice _originalDevice;
        public RackDevice CurrentDevice { get; }
        public event Action Saved;
        public ObservableCollection<DevicePropertyViewModel> DeviceProperties { get; } = new ObservableCollection<DevicePropertyViewModel>();
        public List<string> AvailableAdderModels { get; } = new List<string> { "Other", "ASP001", "ALIF4000T", "ALIF2100T" };
        public bool IsBusy = false;

        public ICommand SaveCommand { get; }
        public ICommand ShutdownCommand { get; }

        public List<string> hiddenFields = new List<string>
        {
            "DeviceType",
            "Connected",
            "Connected2"
        };

        public DeviceDetailsViewModel(RackDevice device)
        {
            _originalDevice = device;
            //Get actual device
            CurrentDevice = device;

            // Use Reflection to populate the properties list
            PopulateProperties(device);

            SaveCommand = new RelayCommand(ExecuteSave);
            ShutdownCommand = new RelayCommand(ExecuteShutdown, CanShutdown);
        }

        public bool CanShutdown(object parameter)
        {
            return this.CurrentDevice is ComputerDevice && !IsBusy;
        }

        public async void ExecuteShutdown(object parameter) // parameter is not used here
        {
            // Use the ViewModel's CurrentDevice property
            if (this.CurrentDevice is ComputerDevice computer)
            {
                string targetIp = computer.IPAddressInfo?.Address;

                if (!string.IsNullOrEmpty(targetIp))
                {
                    // --- !!! WARNING: HARDCODED CREDENTIALS - REPLACE WITH SECURE METHOD !!! ---
                    //string username = "YourDomain\\YourAdminUser"; // e.g., "MYDOMAIN\\Admin" or ".\LocalAdmin"
                    //SecureString password = new SecureString();
                    // You MUST get the password securely (e.g., from PasswordBox) and append char by char
                    // Example: foreach (char c in plainTextPassword) { password.AppendChar(c); }
                    //"YourSecurePassword".ToList().ForEach(password.AppendChar); // Temporary, insecure way for testing only!
                    //password.MakeReadOnly();
                    // --- !!! END WARNING !!! ---

                    IsBusy = true; // Set busy flag

                    // Call the service asynchronously
                    string result = await ShutdownService.ShutdownComputerAsync(targetIp, "test", null);

                    IsBusy = false; // Clear busy flag

                    // Dispose SecureString immediately after use
                    //password.Dispose();

                    // Show result message
                    if (!string.IsNullOrEmpty(result)) // An error occurred
                    {
                        MessageBox.Show($"Shutdown attempt failed:\n{result}", "Shutdown Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                    else // Success
                    {
                        MessageBox.Show($"Shutdown command sent successfully to {targetIp}.", "Shutdown Initiated", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                else
                {
                    MessageBox.Show("Cannot execute shutdown: IP Address is missing for this device.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            // No 'else' needed as CanExecute should prevent this unless CurrentDevice changes unexpectedly
        }

        private void PopulateProperties(RackDevice device)
        {
            DeviceProperties.Clear();
            Type type = device.GetType();

            // Get all public, instance properties from the device object.
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(x => x.GetPropertyVisibility()).OrderBy(p => p.GetOrder());

            foreach (var propInfo in properties)
            {

                    if(propInfo.PropertyType == typeof(IPAddressInfo) && propInfo.CanRead && propInfo.CanWrite)
                    {
                        {
                            var ipInfo = (IPAddressInfo)propInfo.GetValue(_originalDevice);
                            if (ipInfo != null)
                            {

                                DeviceProperties.Add(new DevicePropertyViewModel(propInfo, "IP Address", ipInfo.Address, propInfo.Name == "IPAddressInfo2" ? device.SecondIPAddress : true, propInfo.GetTabCategory()));
                            }

                        }
                    }
                    else if (propInfo.CanRead && propInfo.CanWrite)
                    {
                        DeviceProperties.Add(new DevicePropertyViewModel(device, propInfo, propInfo.GetTabCategory()));
                    }
                

                
            }
        }

        private void ExecuteSave(object parameter)
        {
            // Use reflection to write the edited values back to the original device object.
            foreach (var propVM in DeviceProperties)
            {
                // Convert the value from the textbox (which is a string) to the property's actual type.
                if (propVM.PropertyName == "IPAddressInfo")
                {
                    CurrentDevice.IPAddressInfo.Address = (string)propVM.PropertyValue as string;
                    CurrentDevice.IPAddressInfo.SubnetMask = NetworkInfoService.GetSubnetMaskForRemoteIpOnLan((string)propVM.PropertyValue);
                }
                else if (propVM.PropertyName == "IPAddressInfo2")
                {
                    CurrentDevice.IPAddressInfo2.Address = (string)propVM.PropertyValue as string;
                    CurrentDevice.IPAddressInfo2.SubnetMask = NetworkInfoService.GetSubnetMaskForRemoteIpOnLan((string)propVM.PropertyValue);
                }
                else
                {

                    var targetType = propVM.PropertyInfo.PropertyType;
                    var convertedValue = Convert.ChangeType(propVM.PropertyValue, targetType);

                    // Set the value on the original device object.
                    propVM.PropertyInfo.SetValue(CurrentDevice, convertedValue);
                }
            }
            

            Saved?.Invoke();
            PopulateProperties(_originalDevice);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

