using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.NetworkInformation;
using System.Security.Policy;
using RackMonitor.Services;
using System.Diagnostics;

namespace RackMonitor
{
    public class PingService
    {

        public event EventHandler<PingEventArgs> PingCompleted;
        public PingService() { }

        public void PingHost(string nameOrAddress, int interfaceID)
        {
            var pinger = new Ping();

            pinger.PingCompleted += (sender, e) =>
            {
                bool success = e.Reply?.Status == IPStatus.Success;

                PingCompleted?.Invoke(this, new PingEventArgs(nameOrAddress, success, interfaceID));

                pinger.Dispose();
            };

            pinger.SendAsync(nameOrAddress, null);
        }
    }

    public class PingEventArgs : EventArgs
    {
        public string IPAddress { get; }
        public int InterfaceID { get; }
        public bool IsSuccessful { get; }


        public PingEventArgs(string ipAddress, bool isSuccessful, int interfaceID)
            {
                IPAddress = ipAddress;
                IsSuccessful = isSuccessful;
                InterfaceID = interfaceID;
            }
    }
}
