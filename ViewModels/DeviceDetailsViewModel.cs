using RackMonitor.Models;
using RackMonitor.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace RackMonitor.ViewModels
{
    public class DeviceDetailsViewModel : INotifyPropertyChanged
    {
        private readonly RackDevice _originalDevice;
        public RackDevice CurrentDevice { get; }
        public event Action Saved;
        public ObservableCollection<DevicePropertyViewModel> DeviceProperties { get; } = new ObservableCollection<DevicePropertyViewModel>();

        public ICommand SaveCommand { get; }

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
        }

        private void PopulateProperties(RackDevice device)
        {
            DeviceProperties.Clear();

            // Get all public, instance properties from the device object.
            var properties = device.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var propInfo in properties)
            {
                // We don't want to edit the DeviceType, and we can ignore the base class properties.
                if (!hiddenFields.Contains(propInfo.Name))
                {
                    if(propInfo.PropertyType == typeof(IPAddressInfo) && propInfo.CanRead && propInfo.CanWrite)
                    {
                        {
                            var ipInfo = (IPAddressInfo)propInfo.GetValue(_originalDevice);
                            if (ipInfo != null)
                            {

                                DeviceProperties.Add(new DevicePropertyViewModel(propInfo, "IP Address", ipInfo.Address, propInfo.Name == "IPAddressInfo2" ? device.SecondIPAddress : true));
                            }

                        }
                    }
                    else if (propInfo.CanRead && propInfo.CanWrite)
                    {
                        DeviceProperties.Add(new DevicePropertyViewModel(device, propInfo));
                    }
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

