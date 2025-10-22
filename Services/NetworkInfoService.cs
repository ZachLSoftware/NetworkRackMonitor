using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace RackMonitor.Services
{
    /// <summary>
    /// A service to query information about the local machine's network interfaces.
    /// </summary>
    public class NetworkInfoService
    {
        /// <summary>
        /// Finds the subnet mask of the local network interface that is on the same subnet as a given remote IP address.
        /// </summary>
        /// <param name="remoteIpAddress">The IP address of the remote device on the local network (e.g., "192.168.2.15").</param>
        /// <returns>The subnet mask of the matching local network interface (e.g., "255.255.255.0") or null if no match is found.</returns>
        public static string GetSubnetMaskForRemoteIpOnLan(string remoteIpAddress)
        {
            if (!IPAddress.TryParse(remoteIpAddress, out IPAddress remoteIp) || remoteIp.AddressFamily != AddressFamily.InterNetwork)
            {
                return null; 
            }

            byte[] remoteIpBytes = remoteIp.GetAddressBytes();

            // Iterate through all active, non-loopback network interfaces on the local machine.
            foreach (var ua in NetworkInterface.GetAllNetworkInterfaces()
                .Where(ni => ni.OperationalStatus == OperationalStatus.Up && ni.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                .SelectMany(ni => ni.GetIPProperties().UnicastAddresses))
            {
                // We only care about IPv4 addresses that have a subnet mask.
                if (ua.Address.AddressFamily != AddressFamily.InterNetwork || ua.IPv4Mask == null)
                {
                    continue;
                }

                byte[] localIpBytes = ua.Address.GetAddressBytes();
                byte[] subnetMaskBytes = ua.IPv4Mask.GetAddressBytes();

                // Perform the bitwise AND to determine the network address for the remote IP using the current adapter's subnet mask.
                byte[] remoteNetwork = new byte[remoteIpBytes.Length];
                for (int i = 0; i < remoteIpBytes.Length; i++)
                {
                    remoteNetwork[i] = (byte)(remoteIpBytes[i] & subnetMaskBytes[i]);
                }

                // Perform the bitwise AND to determine the network address for the local adapter.
                byte[] localNetwork = new byte[localIpBytes.Length];
                for (int i = 0; i < localIpBytes.Length; i++)
                {
                    localNetwork[i] = (byte)(localIpBytes[i] & subnetMaskBytes[i]);
                }

                // If the network addresses are identical, we've found the correct interface.
                if (remoteNetwork.SequenceEqual(localNetwork))
                {
                    return ua.IPv4Mask.ToString();
                }
            }

            // No local network interface was found on the same subnet as the remote IP.
            return null;
        }

        public static string GetBroadcastAddress(string ipAddress, string subnetMask)
        {
            if (string.IsNullOrEmpty(ipAddress) || string.IsNullOrEmpty(subnetMask) ||
                !IPAddress.TryParse(ipAddress, out IPAddress ip) ||
                !IPAddress.TryParse(subnetMask, out IPAddress mask))
            {
                return string.Empty;
            }

            byte[] ipBytes = ip.GetAddressBytes();
            byte[] maskBytes = mask.GetAddressBytes();

            if (ipBytes.Length != 4 || maskBytes.Length != 4)
            {
                return string.Empty; // Only for IPv4
            }

            // Perform the bitwise calculation: Broadcast = IP | (~Subnet)
            byte[] broadcastBytes = new byte[4];
            for (int i = 0; i < 4; i++)
            {
                broadcastBytes[i] = (byte)(ipBytes[i] | (byte)~maskBytes[i]);
            }

            return new IPAddress(broadcastBytes).ToString();
        }

        public static int CalculateCIDR(string SubnetMask)
        {
            if (!IPAddress.TryParse(SubnetMask, out IPAddress maskAddress))
            {
                return 0;
            }
            byte[] bytes = maskAddress.GetAddressBytes();
            int cidr = 0;
            bool endOfMask = false;

            for (int i = 0; i < bytes.Length; i++)
            {
                for (int j = 7; j >= 0; j--)
                {
                    // Check if the j-th bit is set
                    bool bitIsSet = (bytes[i] & (1 << j)) != 0;

                    if (bitIsSet)
                    {
                        if (endOfMask)
                        {
                            // Invalid mask: a '1' appeared after a '0'
                            return 0;
                        }
                        cidr++;
                    }
                    else
                    {
                        endOfMask = true;
                    }
                }
            }
            return cidr;
        }

    }
}

