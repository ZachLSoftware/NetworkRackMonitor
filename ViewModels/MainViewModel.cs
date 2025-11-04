using Microsoft.PowerShell.Commands;
using RackMonitor.Data;
using RackMonitor.Models;
using RackMonitor.Security;
using RackMonitor.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace RackMonitor.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly RackRepository _repository;
        public ICommand OpenGlobalCredentialsPopupCommand { get; }

        public ICommand SaveCredentialsCommand { get; }
        public ICommand CancelCredentialsCommand { get; }
        public ICommand ShutdownAllSelectedRackPCsCommand { get; }
        public ICommand ToggleSettingsPanelCommand { get; }
        public ICommand ToggleWoLServiceCommand { get; }
        public ICommand TogglePingServiceCommand { get; }

        public EventHandler<PingServiceToggledEventArgs> PingToggled;
        public EventHandler<WoLServiceToggledEventArgs> WoLToggled;

        // --- PROXY COMMANDS ---
        public ICommand DropItemCommand { get; }
        public ICommand ShowDeviceDetailsCommand { get; }
        public ICommand ChangeDeviceTypeCommand { get; }
        public ICommand AddSlotCommand { get; }
        public ICommand MergeUnitCommand { get; }
        // --- END PROXY COMMANDS ---

        public ObservableCollection<RackViewModel> AllRacks = new ObservableCollection<RackViewModel>();
        public List<string> RackNames = new List<string>();
        private int _numberOfUnits = 12;
        public int NumberOfUnits
        {
            get => _numberOfUnits;
            set
            {
                if (_numberOfUnits != value)
                {
                    _numberOfUnits = value;
                    OnPropertyChanged(nameof(NumberOfUnits));
                }
            }
        }

        private bool _isPingServiceRunning = true;
        public bool IsPingServiceRunning
        {
            get => _isPingServiceRunning;
            set
            {
                _isPingServiceRunning = value;
                OnPropertyChanged(nameof(IsPingServiceRunning));
            }
        }

        private bool _isSettingsPanelOpen = true;
        public bool IsSettingsPanelOpen
        {
            get => _isSettingsPanelOpen;
            set
            {

                if (_isSettingsPanelOpen != value)
                {
                    _isSettingsPanelOpen = value;
                    OnPropertyChanged(nameof(IsSettingsPanelOpen));
                }
            }
        }

        private Credentials _globalCredentials;
        public Credentials GlobalCredentials
        {
            get => _globalCredentials;
            set
            {
                if (value != _globalCredentials)
                {
                    _globalCredentials = value;
                    OnPropertyChanged(nameof(GlobalCredentials));
                }
            }
        }

        private bool _isWoLServiceRunning = true;
        public bool IsWoLServiceRunning
        {
            get => _isWoLServiceRunning;
            set
            {
                _isWoLServiceRunning = value;
                OnPropertyChanged(nameof(IsWoLServiceRunning));
            }
        }
        private RackViewModel _selectedRackViewModel;
        public RackViewModel SelectedRackViewModel
        {
            get => _selectedRackViewModel;
            set
            {
                if (_selectedRackViewModel != value)
                {
                    _selectedRackViewModel = value;
                    OnPropertyChanged(nameof(SelectedRackViewModel));
                    // When selected rack changes, update global toggles to reflect its state
                    if (_selectedRackViewModel != null)
                    {
                        IsPingServiceRunning = _selectedRackViewModel.IsPingServiceRunning;
                        IsWoLServiceRunning = _selectedRackViewModel.IsWoLServiceRunning;
                    }
                }
            }
        }

        private bool _isGlobalCredentialsPopupOpen = false;
        public bool IsGlobalCredentialsPopupOpen
        {
            get => _isGlobalCredentialsPopupOpen;
            set { _isGlobalCredentialsPopupOpen = value; OnPropertyChanged(nameof(IsGlobalCredentialsPopupOpen)); }
        }
        private string _popupUsername;
        public string PopupUsername
        {
            get => _popupUsername;
            set { _popupUsername = value; OnPropertyChanged(nameof(PopupUsername)); }
        }

        private SecureString _popupPassword;
        public SecureString PopupPassword
        {
            get => _popupPassword;
            set { _popupPassword = value; OnPropertyChanged(nameof(PopupPassword)); }
        }

        // Read-only property to show status in MainWindow
        public bool HasEncryptedPassword =>
            !string.IsNullOrEmpty(_repository.GlobalCredentials.Username) && !string.IsNullOrEmpty(_repository.GlobalCredentials.EncryptedPassword);


        public MainViewModel(RackRepository repository)
        {
            {
                _repository = repository;
                List<RackStateDto> rackStateDtos = _repository.LoadAllRackData();
                AllRacks = new ObservableCollection<RackViewModel>();
                foreach (RackStateDto rackDto in rackStateDtos)
                {
                    if (!RackNames.Contains(rackDto.RackName)) { RackNames.Add(rackDto.RackName); }
                    AllRacks.Add(new RackViewModel(_repository, rackDto));
                }

                SelectedRackViewModel = AllRacks.FirstOrDefault();
                IsPingServiceRunning = SelectedRackViewModel?.IsPingServiceRunning ?? false;
                IsWoLServiceRunning = SelectedRackViewModel?.IsWoLServiceRunning ?? false;
                NumberOfUnits = SelectedRackViewModel?.NumberOfUnits ?? 12;
                

                //Command Bindings
               
                ToggleWoLServiceCommand = new RelayCommand(ExecuteToggleWoL);
                TogglePingServiceCommand = new RelayCommand(ExecuteTogglePing);
                OpenGlobalCredentialsPopupCommand = new RelayCommand(ExecuteOpenGlobalCredentialsPopup);
                SaveCredentialsCommand = new RelayCommand(ExecuteSaveGlobalCredentials, CanExecuteSaveGlobalCredentials);
                CancelCredentialsCommand = new RelayCommand(ExecuteCancelGlobalCredentials);
                ShutdownAllSelectedRackPCsCommand = new RelayCommand(ExecuteShutdownAll);

                //proxy commands
                DropItemCommand = new RelayCommand(ExecuteDropItem, CanExecuteOnSelectedRack);
                ShowDeviceDetailsCommand = new RelayCommand(ExecuteShowDeviceDetails, CanExecuteOnSelectedRack);
                ChangeDeviceTypeCommand = new RelayCommand(ExecuteChangeDeviceType, CanExecuteOnSelectedRack);
                AddSlotCommand = new RelayCommand(ExecuteAddSlot, CanExecuteOnSelectedRack);
                MergeUnitCommand = new RelayCommand(ExecuteMergeUnit, CanExecuteOnSelectedRack);


                //Create the initial rack
                //_repository.UpdateRackSize(NumberOfUnits);
            }
        }


        // --- PROXY COMMAND PREDICATE ---
        private bool CanExecuteOnSelectedRack(object parameter)
        {
            // This single predicate works for all proxy commands
            return SelectedRackViewModel != null;
        }

        // --- PROXY COMMAND EXECUTION METHODS ---
        private void ExecuteDropItem(object parameter)
        {
            SelectedRackViewModel?.DropItemCommand.Execute(parameter);
        }

        private void ExecuteShowDeviceDetails(object parameter)
        {
            SelectedRackViewModel?.ShowDeviceDetailsCommand.Execute(parameter);
        }

        private void ExecuteChangeDeviceType(object parameter)
        {
            SelectedRackViewModel?.ChangeDeviceTypeCommand.Execute(parameter);
        }

        private void ExecuteAddSlot(object parameter)
        {
            SelectedRackViewModel?.AddSlotCommand.Execute(parameter);
        }

        private void ExecuteMergeUnit(object parameter)
        {
            SelectedRackViewModel?.MergeUnitCommand.Execute(parameter);
        }

        private bool CanExecuteSaveGlobalCredentials(object parameter)
        {
            // Same validation logic
            return !string.IsNullOrWhiteSpace(PopupUsername) && PopupPassword != null && PopupPassword.Length > 0;
        }
        private void ExecuteOpenGlobalCredentialsPopup(object parameter)
        {
            // Pre-populate with current global values
            PopupUsername = _repository.GlobalCredentials.Username;

            // Always clear password input field
            PopupPassword?.Dispose();
            PopupPassword = new SecureString();
            OnPropertyChanged(nameof(PopupPassword)); // Notify assistant to clear PasswordBox

            IsGlobalCredentialsPopupOpen = true;
            OnPropertyChanged(nameof(HasEncryptedPassword)); // Update status
        }

        public async void ExecuteShutdownAll(object parameter)
        {

            // 1. Find all devices to shut down
            var devicesToShutdown = SelectedRackViewModel.RackUnits
                .SelectMany(unit => unit.Slots)
                .Select(slot => slot.Device)
                .OfType<ComputerDevice>() // Get only ComputerDevices
                .Where(computer => computer.AllowRemoteShutdown) // Check flag
                .ToList();

            if (devicesToShutdown.Count == 0)
            {
                MessageBox.Show("No devices are marked for remote shutdown.", "Global Shutdown", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (MessageBox.Show($"This will attempt to shut down {devicesToShutdown.Count} computer(s). Are you sure?",
                               "Confirm Global Shutdown", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No)
            {
                return;
            }

            // 2. Create a list of tasks
            List<Task> shutdownTasks = new List<Task>();
            // IsBusy = true; // TODO: Add IsBusy property to RackViewModel if needed

            Debug.WriteLine($"Attempting to shut down {devicesToShutdown.Count} device(s)...");
            foreach (var computer in devicesToShutdown)
            {
                // 3. Add the async helper method call (which returns a Task) to the list.
                // This starts the task.
                shutdownTasks.Add(ShutdownDeviceInternalAsync(computer));
            }

            // 4. Await all tasks to complete concurrently
            try
            {
                await Task.WhenAll(shutdownTasks);
                Debug.WriteLine("All shutdown tasks completed.");
                MessageBox.Show("All shutdown commands have been sent.", "Global Shutdown", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                // This catch is for errors in Task.WhenAll, though individual errors
                // are handled inside ShutdownDeviceInternalAsync
                Debug.WriteLine($"Error during Task.WhenAll: {ex.Message}");
            }
            finally
            {
                // IsBusy = false; // Clear busy flag
            }
        }

        /// <summary>
        /// Contains the shutdown logic for a single device, designed to be run concurrently.
        /// </summary>
        private async Task ShutdownDeviceInternalAsync(ComputerDevice computer)
        {
            // NOTE: This assumes ComputerDevice and RackRepository have the required credential properties
            // e.g., computer.UseGlobalCredentials, _repository.GlobalCredentials, computer.pcCredentials
            try
            {
                computer.HasStatus = true;
                computer.StatusMessage = "Attempting Shutdown...";
                computer.IsShuttingDown = true;
                string targetIp = computer.IPAddressInfo?.Address;

                // Determine which credentials to use
                Credentials credentials = computer.UseGlobalCredentials ? _repository.GlobalCredentials : computer.pcCredentials;

                if (string.IsNullOrEmpty(targetIp))
                {
                    computer.StatusMessage = "Failed: No IP Address";
                    return; // Can't proceed
                }
                if (credentials == null || string.IsNullOrEmpty(credentials.Username) || string.IsNullOrEmpty(credentials.EncryptedPassword))
                {
                    computer.StatusMessage = "Failed: No Credentials";
                    return; // Can't proceed
                }

                SecureString password = new SecureString();
                string plainTextPassword = null;
                string result;

                try
                {
                    // 1. Decrypt the stored password
                    plainTextPassword = ProtectionHelper.UnprotectString(credentials.EncryptedPassword);
                    if (plainTextPassword == null)
                    {
                        throw new Exception("Failed to decrypt password. (Invalid or wrong user?)");
                    }

                    // 2. Convert to SecureString
                    foreach (char c in plainTextPassword)
                    {
                        password.AppendChar(c);
                    }
                    password.MakeReadOnly();

                    // 3. Await the shutdown for THIS device
                    result = await ShutdownService.ShutdownComputerAsync(targetIp, credentials.Username, password);
                }
                finally
                {
                    password.Dispose();
                    if (plainTextPassword != null) plainTextPassword = null; // Clear from memory
                }

                // 4. Update status based on result
                if (!string.IsNullOrEmpty(result)) // Error
                {
                    computer.StatusMessage = $"Failed: {result}";
                }
                else // Success
                {
                    computer.StatusMessage = "Shutdown Command Sent";
                }
            }
            catch (Exception ex)
            {
                computer.StatusMessage = $"Failed: {ex.Message}";
            }
            finally
            {
                computer.IsShuttingDown = false; // Reset flag for this device
            }
        }
        private void ExecuteSaveGlobalCredentials(object parameter)
        {
            if (!CanExecuteSaveGlobalCredentials(null)) return;

            string plainTextPassword = null;
            string encryptedPassword = null;
            bool passwordChanged = PopupPassword != null && PopupPassword.Length > 0; // Check if user entered a password

            try
            {
                // Only encrypt if a new password was entered
                if (passwordChanged)
                {
                    plainTextPassword = ConvertToUnsecureString(PopupPassword);
                    encryptedPassword = ProtectionHelper.ProtectString(plainTextPassword);

                    if (encryptedPassword == null)
                    {
                        MessageBox.Show("Failed to encrypt password.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    // Save new encrypted password to repository
                    GlobalCredentials.EncryptedPassword = encryptedPassword;
                }

                // Always save username
                GlobalCredentials.Username = PopupUsername;

                // Save changes to disk
                _repository.SaveGlobalCredentials( GlobalCredentials);

                IsGlobalCredentialsPopupOpen = false;
                OnPropertyChanged(nameof(HasEncryptedPassword)); // Update "[Set]" status
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving global credentials: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // Clear sensitive data
                PopupPassword?.Dispose();
                PopupPassword = new SecureString();
                OnPropertyChanged(nameof(PopupPassword));
                if (plainTextPassword != null) plainTextPassword = null;
            }
        }

        private static string ConvertToUnsecureString(SecureString securePassword)
        {
            if (securePassword == null) return string.Empty;
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

        private void ExecuteCancelGlobalCredentials(object parameter)
        {
            IsGlobalCredentialsPopupOpen = false;
            // Clear temporary properties
            PopupUsername = null; OnPropertyChanged(nameof(PopupUsername));
            PopupPassword?.Dispose();
            PopupPassword = new SecureString();
            OnPropertyChanged(nameof(PopupPassword));
        }

        private void ExecuteToggleWoL(object parameter)
        {
            IsWoLServiceRunning = !IsWoLServiceRunning;

        }
        private void ExecuteTogglePing(object parameter)
        {
            IsPingServiceRunning = !IsPingServiceRunning;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }


    }
