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
        [PropertyVisibility(false)]
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
        [Order(11)]
        [FriendlyName("Wake on LAN")]
        public bool IsWolEnabled
        {
            get => _isWoLEnabled;
            set
            {
                _isWoLEnabled = value;
            }
        }

        private bool _allowRemoteShutdown;
        [Order(6)]
        [FriendlyName("Allow Remote Shutdown")]
        public bool AllowRemoteShutdown
        {
            get => _allowRemoteShutdown;
            set
            {
                if (_allowRemoteShutdown != value)
                {
                    _allowRemoteShutdown = value;
                    OnPropertyChanged(nameof(AllowRemoteShutdown));
                }
            }
        }
        public ComputerDevice() : base()
        {
            DeviceType = "Computer";
            IsWolEnabled = false;
            AllowRemoteShutdown = true;

        }
    }
}
