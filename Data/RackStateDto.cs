using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RackMonitor.Data
{

    public class IPAddressInfoDto
    {
        public string Address { get; set; }
        public string SubnetMask { get; set; }
        public IPAddressInfoDto(string address, string subnetMask)
        {
            Address = address;
            SubnetMask = subnetMask;
        }
    }
    public class RackStateDto
    {
        public int NumberOfUnits { get; set; }
        public bool Ping { get; set; }
        public bool WoL { get; set; }
        public List<RackUnitDto> Units { get; set; } = new List<RackUnitDto>();
    }

    public class RackUnitDto
    {
        public int UnitNumber { get; set; }
        public List<SlotDto> Slots { get; set; } = new List<SlotDto>();
    }

    public class SlotDto
    {
        public DeviceDto Device { get; set; }
    }

    public class DeviceDto
    {
        public string TypeName { get; set; }

        public string Name { get; set; }
        public IPAddressInfoDto IPAddressInfo { get; set; }
        public IPAddressInfoDto IPAddressInfo2 { get; set; }
        public bool Ping { get; set; } = false;
        public bool SecondIPAddress { get; set; } = false;
        public string MacAddress { get; set; }

        // Specific properties
        public string AdderModel { get; set; }
        public string OperatingSystem { get; set; }
        public int PortCount { get; set; }
        public bool IsWoLEnabled { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }
    
}
