using RackMonitor.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.DirectoryServices.ActiveDirectory;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RackMonitor.Models
{
    public abstract class RackDevice : INotifyPropertyChanged
    {
        private string _deviceType;
        [PropertyVisibility(false)]
        public string DeviceType
        {
            get => _deviceType;
            set
            {
                if (_deviceType != value)
                {
                    _deviceType = value;
                    OnPropertyChanged(nameof(DeviceType));
                }
            }
        }
        private string _name;
        [Order(0)]
        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged(nameof(Name));
                }
            }
        }
        private IPAddressInfo _ipAddressInfo = new IPAddressInfo();
        [Order(5)]
        [FriendlyName("IP Address")]
        [TabCategory("Network")]
        public IPAddressInfo IPAddressInfo
        {
            get => _ipAddressInfo;
            set
            {
                if (_ipAddressInfo != value)
                {
                    _ipAddressInfo = value;
                    OnPropertyChanged(nameof(IPAddressInfo));
                }
            }
        }

        private bool _connected;
        [PropertyVisibility(false)]
        public bool Connected
        {
            get => _connected;
            set
            {
                if (_connected != value)
                {
                    _connected = value;
                    OnPropertyChanged(nameof(Connected));
                }
            }
        }

        private bool _connected2;
        [PropertyVisibility(false)]
        public bool Connected2
        {
            get => _connected2;
            set
            {
                if (_connected2 != value)
                {
                    _connected2 = value;
                    OnPropertyChanged(nameof(Connected2));
                }
            }
        }
        private bool _ping;
        [Order(10)]
        [TabCategory("Network")]
        public bool Ping
        {
            get => _ping;
            set
            {
                if (_ping != value)
                {
                    _ping = value;
                    OnPropertyChanged(nameof(Ping));
                }
            }
        }

        private bool _hasStatus = false;
        [PropertyVisibility(false)]
        public bool HasStatus
        {
            get => _hasStatus;
            set
            {
                if (_hasStatus != value)
                {
                    _hasStatus = value;
                    OnPropertyChanged(nameof(HasStatus));
                }
            }
        }


        private string _statusMessage = "";
        [PropertyVisibility(false)]
        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                if (_statusMessage != value)
                {
                    _statusMessage = value;
                    OnPropertyChanged(nameof(StatusMessage));
                }
            }
        }
        private bool _secondIPAddress;
        [Order(15)]
        [FriendlyName("Enable 2nd IP")]
        [TabCategory("Network")]
        public bool SecondIPAddress
        {
            get => _secondIPAddress;
            set
            {
                if (_secondIPAddress != value)
                {
                    _secondIPAddress = value;
                    OnPropertyChanged(nameof(SecondIPAddress));
                }
            }
        }
        private IPAddressInfo _ipAddressInfo2 = new IPAddressInfo();
        [Order(20)]
        [FriendlyName("IP Address 2")]
        [TabCategory("Network")]
        public IPAddressInfo IPAddressInfo2
        {
            get => _ipAddressInfo2;
            set
            {
                if (_ipAddressInfo2 != value)
                {
                    _ipAddressInfo2 = value;
                    OnPropertyChanged(nameof(IPAddressInfo2));
                }
            }
        }

        public RackDevice()
        {
            Connected = false;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}
