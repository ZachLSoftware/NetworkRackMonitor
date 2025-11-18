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
    public partial class RackViewModel : INotifyPropertyChanged
    {
        #region properties
       
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
            DeleteDeviceCommand = new RelayCommand(ExecuteDeleteDevice, CanDeleteDevice);

            //Create the initial rack
            //_repository.UpdateRackSize(NumberOfUnits);
        }

        public void stopMonitoring()
        {
            monitor.StopMonitoring();
        }
        public void startMonitoring()
        {
            monitor.StartMonitoring();
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
                computer.AllowRemoteShutdown = dto.RemoteShutdown;
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
                Ping = device.Ping,
                RemoteShutdown = (device as ComputerDevice)?.AllowRemoteShutdown ?? true
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

        

        #region command_executions

        

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



        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

