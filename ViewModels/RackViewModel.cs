using RackMonitor.Data;
using RackMonitor.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
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
        public EventHandler<PingServiceToggledEventArgs> PingToggled;
        public EventHandler<WoLServiceToggledEventArgs> WoLToggled;


        public RackViewModel(RackRepository repository)
        {
            _repository = repository;

            //Command Bindings
            UpdateRackSizeCommand = new RelayCommand(ExecuteUpdateRackSize);
            AddSlotCommand = new RelayCommand(ExecuteAddSlot, CanExecuteAddSlot);
            MergeUnitCommand = new RelayCommand(ExecuteMergeUnit, CanExecuteMergeUnit);
            ChangeDeviceTypeCommand = new RelayCommand(ExecuteChangeDeviceType, CanExecuteChangeDeviceType);
            ShowDeviceDetailsCommand = new RelayCommand(ExecuteShowDeviceDetails);
            GetAllIPsCommand = new RelayCommand(ExecuteGetAllIPs);
            ToggleWoLServiceCommand = new RelayCommand(ExecuteToggleWoL);
            TogglePingServiceCommand = new RelayCommand(ExecuteTogglePing);

            //Create the initial rack
            //_repository.UpdateRackSize(NumberOfUnits);
        }

        #region execution_checks

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

        private void ExecuteShowDeviceDetails(object parameter)
        {
            if (parameter is SlotViewModel slot && slot.Device != null)
            {
                var detailsViewModel = new DeviceDetailsViewModel(slot.Device);
                detailsViewModel.Saved += () =>
                {
                    _repository.SaveState();
                    _repository.CheckDeviceState(slot.Device);
                };

                var detailsWindow = new DeviceDetailsWindow
                {
                    DataContext = detailsViewModel
                };
                detailsWindow.ShowDialog();
            }
        }
        
        private void ExecuteToggleWoL(object parameter)
        {
            WoLToggled?.Invoke(this, new WoLServiceToggledEventArgs(IsWoLServiceRunning));
        }
        private void ExecuteTogglePing(object parameter)
        {
            PingToggled?.Invoke(this, new PingServiceToggledEventArgs(IsPingServiceRunning));
        }

        #endregion


        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

