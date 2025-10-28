using RackMonitor.Behaviors;
using RackMonitor.Data;
using RackMonitor.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using System.Windows.Input;

namespace RackMonitor.ViewModels
{
    public class RackViewModel : INotifyPropertyChanged
    {
        private readonly RackRepository _repository;

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
        private DeviceDetailsViewModel _selectedDeviceDetails;
        public DeviceDetailsViewModel SelectedDeviceDetails
        {
            get => _selectedDeviceDetails;
            set
            {
                if (_selectedDeviceDetails != value)
                {
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

        public ObservableCollection<RackUnitViewModel> RackUnits => _repository.RackUnits;

        public ICommand UpdateRackSizeCommand { get; }
        public ICommand AddSlotCommand { get; }
        public ICommand MergeUnitCommand { get; }
        public ICommand ChangeDeviceTypeCommand { get; }
        public ICommand ShowDeviceDetailsCommand { get; }
        public ICommand GetAllIPsCommand { get; }
        public ICommand ToggleWoLServiceCommand { get; }
        public ICommand TogglePingServiceCommand { get; }
        public ICommand ToggleSettingsPanelCommand { get; }
        public ICommand CloseDetailsPanelCommand { get; }
        public ICommand DropItemCommand { get; }

        public EventHandler<PingServiceToggledEventArgs> PingToggled;
        public EventHandler<WoLServiceToggledEventArgs> WoLToggled;


        public RackViewModel(RackRepository repository)
        {
            _repository = repository;
            IsPingServiceRunning = _repository.GlobalPingEnabled;
            IsWoLServiceRunning = _repository.GlobalWoLEnabled;
            NumberOfUnits = _repository.UnitNum;

            //Command Bindings
            UpdateRackSizeCommand = new RelayCommand(ExecuteUpdateRackSize);
            AddSlotCommand = new RelayCommand(ExecuteAddSlot, CanExecuteAddSlot);
            MergeUnitCommand = new RelayCommand(ExecuteMergeUnit, CanExecuteMergeUnit);
            ChangeDeviceTypeCommand = new RelayCommand(ExecuteChangeDeviceType, CanExecuteChangeDeviceType);
            ShowDeviceDetailsCommand = new RelayCommand(ExecuteShowDeviceDetails);
            GetAllIPsCommand = new RelayCommand(ExecuteGetAllIPs);
            ToggleWoLServiceCommand = new RelayCommand(ExecuteToggleWoL);
            TogglePingServiceCommand = new RelayCommand(ExecuteTogglePing);
            ToggleSettingsPanelCommand = new RelayCommand(ExecuteToggleSettingsPanel);
            CloseDetailsPanelCommand = new RelayCommand(ExecuteCloseDetailsPanel);
            DropItemCommand = new RelayCommand(ExecuteDropItem, CanExecuteDropItem);

            //Create the initial rack
            //_repository.UpdateRackSize(NumberOfUnits);
        }

        #region execution_checks
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
        private void ExecuteUpdateRackSize(object parameter)
        {
            _repository.UpdateRackSize(NumberOfUnits);
        }

        private void ExecuteGetAllIPs(object parameter)
        {
            var ipList = _repository.GetAllDevices();
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

        private void ExecuteAddSlot(object parameter)
        {
            if (parameter is RackUnitViewModel unit)
            {
                _repository.AddSlotToUnit(unit);
            }
        }



        private void ExecuteMergeUnit(object parameter)
        {
            if (parameter is RackUnitViewModel unit)
            {
                _repository.MergeUnitToSingleSlot(unit);
            }
        }



        private void ExecuteChangeDeviceType(object parameter)
        {
            if (!(parameter is ValueTuple<object, string> tuple)) return;
            if (!(tuple.Item1 is SlotViewModel slot)) return;
            var deviceType = tuple.Item2;

            _repository.ChangeDeviceType(slot, deviceType);
        }
        private void ExecuteToggleSettingsPanel(object parameter)
        {
            IsSettingsPanelOpen = !IsSettingsPanelOpen;
        }

        private void ExecuteShowDeviceDetails(object parameter)
        {
            if (parameter is SlotViewModel slot && slot.Device != null)
            {
                // Create the ViewModel for the selected device
                var detailsViewModel = new DeviceDetailsViewModel(slot.Device);

                // Hook up the Saved event to trigger repository save and check
                detailsViewModel.Saved += () =>
                {
                    _repository.SaveState();
                    _repository.CheckDeviceState(slot.Device);
                    // Optionally refresh properties in the main list view if needed
                    // OnPropertyChanged(nameof(RackUnits)); // Might be too broad
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

        private void ExecuteToggleWoL(object parameter)
        {
            WoLToggled?.Invoke(this, new WoLServiceToggledEventArgs(IsWoLServiceRunning));
            _repository.GlobalWoLEnabled = IsWoLServiceRunning;
            _repository.SaveState();
        }
        private void ExecuteTogglePing(object parameter)
        {
            PingToggled?.Invoke(this, new PingServiceToggledEventArgs(IsPingServiceRunning));
            _repository.GlobalPingEnabled = IsPingServiceRunning;
            _repository.SaveState();
        }
        private void ExecuteCloseDetailsPanel(object parameter)
        {
            IsDetailsPanelOpen = false;
            // SelectedDeviceDetails is automatically cleared by the IsDetailsPanelOpen setter
        }



        #endregion

        public void ExecuteDropItem(object parameter)
        {

            Debug.WriteLine("ExecuteDropItem called"); // Debug output
            if (parameter is DragDropData data && data.SourceSlot != null && data.TargetSlot != null)
            {
                Debug.WriteLine($"Attempting move from slot {data.SourceSlot.GetHashCode()} to {data.TargetSlot.GetHashCode()}"); // More debug
                _repository.MoveOrSwapDevice(data.SourceSlot, data.TargetSlot);
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

