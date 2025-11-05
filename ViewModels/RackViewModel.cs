using Markdig.Extensions.SelfPipeline;
using RackMonitor.Behaviors;
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
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Security;
using System.Text.Json;
using System.Threading;
using System.Windows;
using System.Windows.Input;

namespace RackMonitor.ViewModels
{
    public class RackViewModel : INotifyPropertyChanged
    {
        #region properties
        private readonly RackRepository _repository;
        public ObservableCollection<RackUnitViewModel> RackUnits { get; }
        private RackStateDto _RackStateDto;
        private MonitoringService monitor;
        private string _rackName;
        public string RackName
        {
            get => _rackName;
            set
            {
                if (value != _rackName)
                {
                    _rackName = value;
                    OnPropertyChanged(nameof(RackName));
                }
            }
        }

        private bool _isDefault = false;
        public bool IsDefault
        {
            get => _isDefault;
            set
            {
                if (value != _isDefault)
                {
                    _isDefault = value;
                    OnPropertyChanged(nameof(IsDefault));
                    SaveRack();
                }
            }
        }
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
                    ExecuteUpdateRackSize(NumberOfUnits);
                }
            }
        }

        private DeviceDetailsViewModel _selectedDeviceDetails;
        public DeviceDetailsViewModel SelectedDeviceDetails
        {
            get => _selectedDeviceDetails;
            set
            {
                if (_selectedDeviceDetails != value)
                {
                    _selectedDeviceDetails?.Cleanup();
                    _selectedDeviceDetails = value;
                    OnPropertyChanged(nameof(SelectedDeviceDetails));
                }
            }
        }

        // NEW: Property to control the visibility of the details panel
        private bool _isDetailsPanelOpen;
        public bool IsDetailsPanelOpen
        {
            get => _isDetailsPanelOpen;
            set
            {
                if (_isDetailsPanelOpen != value)
                {
                    _isDetailsPanelOpen = value;
                    OnPropertyChanged(nameof(IsDetailsPanelOpen));
                    // If closing details, clear the selected device VM
                    if (!value)
                    {
                        SelectedDeviceDetails = null;
                    }
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


        public ICommand UpdateRackSizeCommand { get; }
        public ICommand AddSlotCommand { get; }
        public ICommand MergeUnitCommand { get; }
        public ICommand ChangeDeviceTypeCommand { get; }
        public ICommand ShowDeviceDetailsCommand { get; }
        public ICommand GetAllIPsCommand { get; }
        public ICommand ToggleWoLServiceCommand { get; }
        public ICommand TogglePingServiceCommand { get; }

        public ICommand CloseDetailsPanelCommand { get; }
        public ICommand DropItemCommand { get; }


        public EventHandler<PingServiceToggledEventArgs> PingToggled;
        public EventHandler<WoLServiceToggledEventArgs> WoLToggled;
        public event EventHandler<DeviceSavedEventArgs> DeviceSaved;
        #endregion

        public RackViewModel(RackRepository repository, RackStateDto rackStateDto)
        {
            _repository = repository;

            RackUnits = new ObservableCollection<RackUnitViewModel>();
            _RackStateDto = rackStateDto;
            RackName = rackStateDto.RackName;
            DtoToViewModel();
            NumberOfUnits = RackUnits.Count;
            monitor = new MonitoringService(this);
            monitor.StartMonitoring();
            monitor.updateDevices += () =>
            {
                SaveRack();
            };


            //Command Bindings
            UpdateRackSizeCommand = new RelayCommand(ExecuteUpdateRackSize);
            AddSlotCommand = new RelayCommand(ExecuteAddSlot, CanExecuteAddSlot);
            MergeUnitCommand = new RelayCommand(ExecuteMergeUnit, CanExecuteMergeUnit);
            ChangeDeviceTypeCommand = new RelayCommand(ExecuteChangeDeviceType, CanExecuteChangeDeviceType);
            ShowDeviceDetailsCommand = new RelayCommand(ExecuteShowDeviceDetails);
            GetAllIPsCommand = new RelayCommand(ExecuteGetAllIPs);
            CloseDetailsPanelCommand = new RelayCommand(ExecuteCloseDetailsPanel);
            DropItemCommand = new RelayCommand(ExecuteDropItem, CanExecuteDropItem);

            //Create the initial rack
            //_repository.UpdateRackSize(NumberOfUnits);
        }

        #region data_methods
        private void DtoToViewModel()
        {
            foreach (var unitDto in _RackStateDto.Units.OrderByDescending(u => u.UnitNumber))
            {
                var unitVM = new RackUnitViewModel { UnitNumber = unitDto.UnitNumber };
                
                foreach (var slotDto in unitDto.Slots)
                {
                    unitVM.Slots.Add(new SlotViewModel
                    {
                        Device = MapDtoToDevice(slotDto.Device)
                    });
                }
                RackUnits.Add(unitVM);
            }
            IsDefault = _RackStateDto.Default;
        }
        public RackDevice MapDtoToDevice(DeviceDto dto)
        {
            if (dto == null) return null;
            RackDevice device = dto.TypeName switch
            {
                nameof(ComputerDevice) => new ComputerDevice(),
                nameof(NetworkDevice) => new NetworkDevice(),
                nameof(AdderDevice) => new AdderDevice(),
                _ => null
            };

            if (device != null)
            {
                device.Name = dto.Name;
                device.IPAddressInfo.Address = dto.IPAddressInfo.Address;
                device.IPAddressInfo.SubnetMask = dto.IPAddressInfo.SubnetMask;
                device.IPAddressInfo2.Address = dto.IPAddressInfo2.Address;
                device.IPAddressInfo2.SubnetMask = dto.IPAddressInfo2.SubnetMask;
                device.SecondIPAddress = dto.SecondIPAddress;
                device.Ping = dto.Ping;
            }
            if (device is ComputerDevice computer)
            {
                computer.MacAddress = dto.MacAddress;
                computer.IsWolEnabled = dto.IsWoLEnabled;
                computer.pcCredentials = new Credentials(dto.Username, dto.Password);
            }
            if (device is AdderDevice)
            {
                (device as AdderDevice).Model = dto.AdderModel;
            }
            return device;
        }

        public void UpdateRackSize(int newSize)
        {
            if (newSize < 1) newSize = 1;
            if (newSize > 100) newSize = 100;

            int prevNum = RackUnits.Count;

            if (prevNum < newSize)
            {
                int itemsToAdd = newSize - prevNum;
                for (int i = 0; i < itemsToAdd; i++)
                {
                    int unitNumber = prevNum + i + 1;
                    var newUnit = new RackUnitViewModel { UnitNumber = unitNumber };
                    newUnit.Slots.Add(new SlotViewModel());
                    RackUnits.Insert(0, newUnit);
                }
            }
            else if (prevNum > newSize)
            {
                int itemsToRemove = prevNum - newSize;
                for (int i = 0; i < itemsToRemove; i++)
                {
                    RackUnits.RemoveAt(0);
                }
            }

            SaveRack();
        }

        public void SaveRack()
        {
            var rackStateDto = new RackStateDto
            {
                RackName = this.RackName,
                Default = this.IsDefault,
                Units = RackUnits.Select(UnitVM => new RackUnitDto
                {
                    UnitNumber = UnitVM.UnitNumber,
                    Slots = UnitVM.Slots.Select(slotVM => new SlotDto
                    {
                        Device = MapDeviceToDto(slotVM.Device)
                    }).ToList()
                }).ToList(),
            };
            _RackStateDto = rackStateDto;
            _repository.SaveRack(rackStateDto);
            
        }

        public void CheckDeviceState(RackDevice device)
        {
            DeviceSaved?.Invoke(this, new DeviceSavedEventArgs(device));
        }

        private DeviceDto MapDeviceToDto(RackDevice device)
        {
            if (device == null) return null;

            DeviceDto dto = new DeviceDto
            {
                TypeName = device.GetType().Name,
                Name = device.Name,
                IPAddressInfo = new IPAddressInfoDto(device.IPAddressInfo.Address, device.IPAddressInfo.SubnetMask),
                IPAddressInfo2 = new IPAddressInfoDto(device.IPAddressInfo2.Address, device.IPAddressInfo2.SubnetMask),
                SecondIPAddress = device.SecondIPAddress,
                MacAddress = (device as ComputerDevice)?.MacAddress,
                IsWoLEnabled = (device as ComputerDevice)?.IsWolEnabled ?? false,
                Username = (device as ComputerDevice)?.pcCredentials.Username ?? "",
                Password = (device as ComputerDevice)?.pcCredentials.EncryptedPassword ?? "",
                AdderModel = (device as AdderDevice)?.Model ?? "None",
                Ping = device.Ping
            };

            return dto;
        }

        public List<RackDevice> GetAllDevices()
        {
            return RackUnits
                .SelectMany(unit => unit.Slots)
                .Select(slot => slot.Device)
                .Where(device => device != null).ToList();
        }

        public RackDevice FindDeviceByIP(string ip)
        {

            return RackUnits
                .SelectMany(unit => unit.Slots)
                .Select(slot => slot.Device)
                .FirstOrDefault(device => device != null && (device.IPAddressInfo.Address == ip || device.IPAddressInfo2.Address == ip));
        }
        #endregion

        #region execution_checks

        private bool CanExecuteSaveGlobalCredentials(object parameter)
        {
            // Same validation logic
            return !string.IsNullOrWhiteSpace(PopupUsername) && PopupPassword != null && PopupPassword.Length > 0;
        }
        private bool CanExecuteDropItem(object parameter)
        {
            return parameter is DragDropData;
        }
        /// <summary>
        /// Allows adding up to 4 slots to avoid excessive columns
        /// </summary>
        private bool CanExecuteAddSlot(object parameter)
        {
            return parameter is RackUnitViewModel unit && unit.Slots.Count < 4;
        }

        /// <summary>
        /// Only execute if there are multiple slots on the unit.
        /// </summary>
        private bool CanExecuteMergeUnit(object parameter)
        {
            return parameter is RackUnitViewModel unit && unit.Slots.Count > 1;
        }


        /// <summary>
        /// Only allow adding devices when object is a slot and the device isn't already that type.
        /// Need to fix for changing type and handling gracefully.
        /// </summary>
        private bool CanExecuteChangeDeviceType(object parameter)
        {
            if (!(parameter is ValueTuple<object, string> tuple)) return false;
            if (!(tuple.Item1 is SlotViewModel slot)) return false;
            var targetDeviceType = tuple.Item2;
            return slot.Device?.DeviceType != targetDeviceType;
        }
        #endregion

        #region command_executions

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
       
        private void ExecuteCancelGlobalCredentials(object parameter)
        {
            IsGlobalCredentialsPopupOpen = false;
            // Clear temporary properties
            PopupUsername = null; OnPropertyChanged(nameof(PopupUsername));
            PopupPassword?.Dispose();
            PopupPassword = new SecureString();
            OnPropertyChanged(nameof(PopupPassword));
        }
        private void ExecuteUpdateRackSize(object parameter)
        {
            UpdateRackSize(NumberOfUnits);
        }

        private void ExecuteGetAllIPs(object parameter)
        {
            var ipList = GetAllDevices();
            Debug.WriteLine("--- All Device IPs ---");
            if (ipList.Any())
            {
                foreach (var ip in ipList)
                {
                    Debug.WriteLine(ip);
                }
            }
            else
            {
                Debug.WriteLine("No devices with IP addresses found.");
            }
            Debug.WriteLine("----------------------");
        }

        public void AddSlotToUnit(RackUnitViewModel unit)
        {
            if (unit != null && unit.Slots.Count < 4)
            {
                unit.Slots.Add(new SlotViewModel());
            }
            SaveRack();
        }

        public void MergeUnitToSingleSlot(RackUnitViewModel unit)
        {
            if (unit != null && unit.Slots.Count > 1)
            {
                // To preserve any device in the first slot, we can do this:
                var firstSlot = unit.Slots.FirstOrDefault();
                unit.Slots.Clear();
                unit.Slots.Add(firstSlot ?? new SlotViewModel());
            }
            SaveRack();
        }

        public void ChangeDeviceType(SlotViewModel slot, string deviceType)
        {
            if (slot == null) return;

            RackDevice newDevice = null;
            switch (deviceType)
            {
                case "ComputerDevice": newDevice = new ComputerDevice(); break;
                case "NetworkDevice": newDevice = new NetworkDevice(); break;
                case "AdderDevice": newDevice = new AdderDevice(); break;
            }

            if (newDevice != null)
            {
                slot.Device = newDevice;
            }
            SaveRack();
        }

        private void ExecuteAddSlot(object parameter)
        {
            if (parameter is RackUnitViewModel unit)
            {
                AddSlotToUnit(unit);
            }
        }



        private void ExecuteMergeUnit(object parameter)
        {
            if (parameter is RackUnitViewModel unit)
            {
                MergeUnitToSingleSlot(unit);
            }
        }



        private void ExecuteChangeDeviceType(object parameter)
        {
            if (!(parameter is ValueTuple<object, string> tuple)) return;
            if (!(tuple.Item1 is SlotViewModel slot)) return;
            var deviceType = tuple.Item2;

            ChangeDeviceType(slot, deviceType);
        }

        private void ExecuteShowDeviceDetails(object parameter)
        {
            if (parameter is SlotViewModel slot && slot.Device != null)
            {
                // Create the ViewModel for the selected device
                var detailsViewModel = new DeviceDetailsViewModel(slot.Device, _repository);

                // Hook up the Saved event to trigger repository save and check
                detailsViewModel.Saved += () =>
                {
                    SaveRack();
                    CheckDeviceState(slot.Device);
                };

                // Set the ViewModel for the panel
                SelectedDeviceDetails = detailsViewModel;

                // Open the panel
                IsDetailsPanelOpen = true;

                // Removed code that showed a separate window
            }
            else // If clicking an empty slot or invalid parameter, ensure panel closes
            {
                IsDetailsPanelOpen = false;
            }
        }

        private void ExecuteCloseDetailsPanel(object parameter)
        {
            IsDetailsPanelOpen = false;
        }

        public async void ExecuteShutdownAll(object parameter)
        {

            // 1. Find all devices to shut down
            var devicesToShutdown = RackUnits
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

        #endregion
        public void MoveOrSwapDevice(SlotViewModel sourceSlot, SlotViewModel targetSlot)
        {
            RackDevice sourceDevice = sourceSlot.Device;
            RackDevice targetDevice = targetSlot.Device;

            // Perform the swap/move
            targetSlot.Device = sourceDevice; // Move source device to target
            sourceSlot.Device = targetDevice; // Move target device (or null) to source

            // Save the updated state
            SaveRack();
        }
        public void ExecuteDropItem(object parameter)
        {

            Debug.WriteLine("ExecuteDropItem called"); // Debug output
            if (parameter is DragDropData data && data.SourceSlot != null && data.TargetSlot != null)
            {
                Debug.WriteLine($"Attempting move from slot {data.SourceSlot.GetHashCode()} to {data.TargetSlot.GetHashCode()}"); // More debug
                MoveOrSwapDevice(data.SourceSlot, data.TargetSlot);
            }
            else
            {
                Debug.WriteLine("Drop failed: Invalid DragDropData received."); // Debug invalid data
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

