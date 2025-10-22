using RackMonitor.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace RackMonitor.Models
{
    public class IPAddressInfo : INotifyPropertyChanged
    {
        private string _address;
        public string Address
        {
            get => _address;
            set
            {
                if (_address != value)
                {
                    _address = value; 
                    OnPropertyChanged(nameof(Address));
                    OnPropertyChanged(nameof(CIDRNotation));
                }
            }
        }

        private string _subnetMask = "255.255.0.0";
        public string SubnetMask
        {
            get => _subnetMask;
            set
            {
                if (_subnetMask != value)
                {
                    _subnetMask = value;
                    OnPropertyChanged(nameof(SubnetMask));
                    OnPropertyChanged(nameof(CIDR));
                }
            }
        }

        public int CIDR
        {
            get => NetworkInfoService.CalculateCIDR(SubnetMask);
            set
            {
                CIDR = CIDR;
            }
        }
        public string Broadcast
        {
            get => NetworkInfoService.GetBroadcastAddress(Address, SubnetMask);
            set
            {
                Broadcast = Broadcast;
            }
        }

        public string CIDRNotation
        {
            get => Address + "/" + CIDR;
            set
            {
                CIDRNotation = CIDRNotation;
                OnPropertyChanged(nameof(CIDRNotation));
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
