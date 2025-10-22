using RackMonitor.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RackMonitor
{
    public class PingServiceToggledEventArgs : EventArgs
    {
        public bool IsEnabled { get; }
        public PingServiceToggledEventArgs(bool isEnabled)
        {
            IsEnabled = isEnabled;
        }
    }

    public class WoLServiceToggledEventArgs : EventArgs
    {
        public bool IsEnabled { get; }
        public WoLServiceToggledEventArgs(bool isEnabled)
        {
            IsEnabled = isEnabled;
        }
    }

    public class DeviceSavedEventArgs : EventArgs
    {
        public RackDevice Device { get; }

        public DeviceSavedEventArgs(RackDevice device)
        {
            this.Device = device;
        }
    }
}
