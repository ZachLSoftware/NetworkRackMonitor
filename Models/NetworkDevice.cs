using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace RackMonitor.Models
{
    class NetworkDevice : RackDevice
    {
        public NetworkDevice() : base()
        {
            DeviceType = "Network";
        }

    }
}
