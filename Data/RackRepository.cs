using RackMonitor.Models;
using RackMonitor.ViewModels;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Automation;

namespace RackMonitor.Data
{
    /// <summary>
    /// Acts as the single source of truth for the rack's data and state.
    /// This class is shared between the UI (via RackViewModel) and any background services.
    /// </summary>
    public class RackRepository
    {
        public ObservableCollection<RackUnitViewModel> RackUnits { get; }
        private readonly string _saveFilePath;
        public event EventHandler<DeviceSavedEventArgs> DeviceSaved;
        public bool GlobalPingEnabled = false;
        public bool GlobalWoLEnabled = false;
        public int UnitNum = 12;

        public RackRepository()
        {
            RackUnits = new ObservableCollection<RackUnitViewModel>();
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string appFolder = Path.Combine(appDataPath, "RackMonitor");
            Directory.CreateDirectory(appFolder); // Ensure the folder exists.
            _saveFilePath = Path.Combine(appFolder, "rack_state.json");


            LoadState();
        }


        ///Save and Load
        public void SaveState()
        {
            UnitNum = RackUnits.Count;
            Debug.WriteLine($"Ping: {GlobalPingEnabled} WOL: {GlobalWoLEnabled}");
            var rackStateDto = new RackStateDto
            {
                NumberOfUnits = UnitNum,
                Units = RackUnits.Select(UnitVM => new RackUnitDto
                {
                    UnitNumber = UnitVM.UnitNumber,
                    Slots = UnitVM.Slots.Select(slotVM => new SlotDto
                    {
                        Device = MapDeviceToDto(slotVM.Device)
                    }).ToList()
                }).ToList(),
                Ping = GlobalPingEnabled,
                WoL = GlobalWoLEnabled
            };
            var options = new JsonSerializerOptions { WriteIndented = true };
            string jsonString = JsonSerializer.Serialize(rackStateDto, options);
            File.WriteAllText(_saveFilePath, jsonString);
        }


        private void LoadState()
        {
            if (!File.Exists(_saveFilePath))
            {
                // If no save file, create a default state.
                UpdateRackSize(12);
                return;
            }

            string jsonString = File.ReadAllText(_saveFilePath);
            if (string.IsNullOrWhiteSpace(jsonString)) return;

            var rackStateDto = JsonSerializer.Deserialize<RackStateDto>(jsonString);

            RackUnits.Clear();
            foreach (var unitDto in rackStateDto.Units.OrderByDescending(u => u.UnitNumber))
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
            GlobalPingEnabled = rackStateDto.Ping;
            GlobalWoLEnabled = rackStateDto.WoL;
            
            //Save after loading to include any updated parameters
            SaveState();

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
                AdderModel = (device as AdderDevice)?.Model ?? "None",
                Ping = device.Ping
            };

            return dto;
        }

        public void CheckDeviceState(RackDevice device)
        {
            DeviceSaved?.Invoke(this, new DeviceSavedEventArgs(device));
        }

        public void MoveOrSwapDevice(SlotViewModel sourceSlot, SlotViewModel targetSlot)
        {
            RackDevice sourceDevice = sourceSlot.Device;
            RackDevice targetDevice = targetSlot.Device;

            // Perform the swap/move
            targetSlot.Device = sourceDevice; // Move source device to target
            sourceSlot.Device = targetDevice; // Move target device (or null) to source

            // Save the updated state
            SaveState();
        }
        private RackDevice MapDtoToDevice(DeviceDto dto)
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
            if (device is ComputerDevice)
            {
                (device as ComputerDevice).MacAddress = dto.MacAddress;
                (device as ComputerDevice).IsWolEnabled = dto.IsWoLEnabled;
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

            SaveState();
        }

        public void AddSlotToUnit(RackUnitViewModel unit)
        {
            if (unit != null && unit.Slots.Count < 4)
            {
                unit.Slots.Add(new SlotViewModel());
            }
            SaveState();
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
            SaveState();
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
            SaveState();
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
                .FirstOrDefault(device => device!=null && (device.IPAddressInfo.Address == ip || device.IPAddressInfo2.Address == ip));
        }
    }
}
