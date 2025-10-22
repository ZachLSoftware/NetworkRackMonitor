using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RackMonitor.Attributes;

namespace RackMonitor.Models
{
    public class ComputerDevice : RackDevice
    {
        private string _macAddress;
        public string MacAddress
        {
            get { return _macAddress;  }
            set
            {
                if (value != _macAddress)
                {
                    _macAddress = value;
                    OnPropertyChanged(nameof(MacAddress));
                }
            }
        }
        private bool _isWoLEnabled;
        [FriendlyName("Wake on LAN")]
        public bool IsWolEnabled
        {
            get => _isWoLEnabled;
            set
            {
                _isWoLEnabled = value;
            }
        }
        public ComputerDevice() : base()
        {
            DeviceType = "Computer";
            IsWolEnabled = false;

        }
    }
}
