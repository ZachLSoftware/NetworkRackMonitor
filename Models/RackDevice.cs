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
        private bool _secondIPAddress;
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
