using RackMonitor.Data;
using RackMonitor.Extensions;
using RackMonitor.Models;
using RackMonitor.Security;
using RackMonitor.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace RackMonitor.ViewModels
{
    public class DeviceDetailsViewModel : INotifyPropertyChanged
    {
        private readonly RackDevice _originalDevice;
        private readonly RackRepository _repository;
        public RackDevice CurrentDevice { get; }
        public event Action Saved;
        public ObservableCollection<DevicePropertyViewModel> DeviceProperties { get; } = new ObservableCollection<DevicePropertyViewModel>();
        public List<string> AvailableAdderModels { get; } = new List<string> { "Other", "ASP001", "ALIF4000T", "ALIF2100T" };
        public bool IsBusy = false;

        private bool _isCredentialsPopupOpen;
        public bool IsCredentialsPopupOpen
        {
            get => _isCredentialsPopupOpen;
            set { _isCredentialsPopupOpen = value; OnPropertyChanged(); }
        }

        private string _popupUsername;
        public string PopupUsername // Bound to TextBox
        {
            get => _popupUsername;
            set { _popupUsername = value; OnPropertyChanged(); }
        }
        public bool HasEncryptedPassword =>
            CurrentDevice is ComputerDevice cd && !string.IsNullOrEmpty(cd.pcCredentials.EncryptedPassword);

        private SecureString _popupPassword;
        public SecureString PopupPassword // Bound to PasswordBox via Assistant
        {
            get => _popupPassword;
            set { _popupPassword = value; OnPropertyChanged(); }
        }
        public bool IsComputerShuttingDown
        {
            get
            {
                if (CurrentDevice is ComputerDevice cd)
                {
                    return cd.IsShuttingDown;
                }
                return false;
            }
        }
        public ICommand SaveCommand { get; }
        public ICommand ShutdownCommand { get; }
        public ICommand OpenCredentialsPopupCommand { get; }
        public ICommand SaveCredentialsCommand { get; }
        public ICommand CancelCredentialsCommand { get; }

        public List<string> hiddenFields = new List<string>
        {
            "DeviceType",
            "Connected",
            "Connected2"
        };

        public DeviceDetailsViewModel(RackDevice device, RackRepository repo)
        {
            _originalDevice = device;
            //Get actual device
            CurrentDevice = device;
            _repository = repo;

            // Use Reflection to populate the properties list
            PopulateProperties(device);

            SaveCommand = new RelayCommand(ExecuteSave);
            ShutdownCommand = new RelayCommand(ExecuteShutdown, CanShutdown);
            OpenCredentialsPopupCommand = new RelayCommand(ExecuteOpenCredentialsPopup);
            SaveCredentialsCommand = new RelayCommand(ExecuteSaveCredentials, CanExecuteSaveCredentials);
            CancelCredentialsCommand = new RelayCommand(ExecuteCancelCredentials);
            OnPropertyChanged(nameof(HasEncryptedPassword));
        }

        public bool CanShutdown(object parameter)
        {
            return this.CurrentDevice is ComputerDevice && !IsBusy;
        }


        private void ExecuteOpenCredentialsPopup(object parameter)
        {
            // Pre-populate with current values if they exist on the device
            if (CurrentDevice is ComputerDevice cd)
            {
                PopupUsername = cd.pcCredentials.Username != null ? cd.pcCredentials.Username : ""; // Assumes Username property exists on ComputerDevice
                                             // Cannot easily pre-populate PasswordBox from encrypted string
                PopupPassword?.Clear(); // Clear any previous SecureString
                PopupPassword = new SecureString(); // Reset SecureString
                OnPropertyChanged(nameof(PopupPassword)); // Notify binding helper

            }
            IsCredentialsPopupOpen = true;
            OnPropertyChanged(nameof(HasEncryptedPassword));
        }
        private bool CanExecuteSaveCredentials(object parameter)
        {
            // Basic validation
            return !string.IsNullOrWhiteSpace(PopupUsername) && PopupPassword != null && PopupPassword.Length > 0;
        }

        private void ExecuteSaveCredentials(object parameter) // Parameter is not directly used here
        {
            if (!CanExecuteSaveCredentials(null)) return; // Double check validation

            string plainTextPassword = null;
            string encryptedPassword = null;

            try
            {
                plainTextPassword = ConvertToUnsecureString(PopupPassword);
                encryptedPassword = ProtectionHelper.ProtectString(plainTextPassword);

                if (encryptedPassword == null)
                {
                    MessageBox.Show("Failed to encrypt password.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var newCredentials = new Credentials(PopupUsername, encryptedPassword);
                UpdateDeviceCredentials(newCredentials);
                IsCredentialsPopupOpen = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving credentials: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // Clear sensitive data regardless of success/failure
                PopupPassword?.Clear();
                PopupPassword = new SecureString();
                OnPropertyChanged(nameof(PopupPassword)); 
                plainTextPassword = null;
            }
        }

        private void ExecuteCancelCredentials(object parameter)
        {
            IsCredentialsPopupOpen = false;
            // Clear temporary properties on cancel
            PopupUsername = null; OnPropertyChanged(nameof(PopupUsername));
            PopupPassword?.Clear();
            PopupPassword = new SecureString();
            OnPropertyChanged(nameof(PopupPassword));
        }
        private void UpdateDeviceCredentials(Credentials credentials)
        {
            // Assumes ComputerDevice has Username and EncryptedPassword properties
            if (CurrentDevice is ComputerDevice computer)
            {
                computer.pcCredentials.Username = credentials.Username;
                computer.pcCredentials.EncryptedPassword = credentials.EncryptedPassword;
                Debug.WriteLine($"Username: {computer.pcCredentials.Username} Password: {computer.pcCredentials.EncryptedPassword}");

                Saved?.Invoke();
            }
            else
            {
                Debug.WriteLine("Warning: Tried to set credentials on a non-computer device.");
            }
        }
        public async void ExecuteShutdown(object parameter) // parameter is not used here
        {
            // Use the ViewModel's CurrentDevice property
            if (this.CurrentDevice is ComputerDevice computer)
            {
                computer.HasStatus = true;
                computer.StatusMessage = "Attempting Shutdown";
                computer.IsShuttingDown = true;
                string targetIp = computer.IPAddressInfo?.Address;
                Credentials credentials = computer.UseGlobalCredentials ? _repository.GlobalCredentials : computer.pcCredentials;

                if (!string.IsNullOrEmpty(targetIp))
                {
                    IsBusy = true;
                    computer.IsShuttingDown = true;

                    SecureString password = new SecureString();
                    foreach(char c in ProtectionHelper.UnprotectString(credentials.EncryptedPassword))
                    {
                        password.AppendChar(c);
                    }
                    password.MakeReadOnly();
                    string result = await ShutdownService.ShutdownComputerAsync(targetIp, credentials.Username, password);

                    IsBusy = false;

                    password.Dispose();

                    // Show result message
                    if (!string.IsNullOrEmpty(result))
                    {
                        computer.StatusMessage = result;
                    }
                    else // Success
                    {
                        computer.StatusMessage = "Shutdown Command Sent";
                    }
                    computer.IsShuttingDown = false;
                }

            }
        }
        private static string ConvertToUnsecureString(SecureString securePassword)
        {
            if (securePassword == null)
                return string.Empty;

            IntPtr unmanagedString = IntPtr.Zero;
            try
            {
                unmanagedString = Marshal.SecureStringToGlobalAllocUnicode(securePassword);
                return Marshal.PtrToStringUni(unmanagedString);
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode(unmanagedString);
            }
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
        public void Cleanup()
        {
            CurrentDevice.PropertyChanged -= OnCurrentDevicePropertyChanged;
        }

        private void OnCurrentDevicePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(RackDevice.HasStatus):
                    OnPropertyChanged(nameof(RackDevice.HasStatus));
                    break;
                case nameof(RackDevice.StatusMessage):
                    OnPropertyChanged(nameof(RackDevice.StatusMessage));
                    break;
                case nameof(ComputerDevice.IsShuttingDown): // This name is safe to check
                    OnPropertyChanged(nameof(IsComputerShuttingDown));
                    break;
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

