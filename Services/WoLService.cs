using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net.Mail;
using System.Globalization;
using System.Net;
using RackMonitor.Models;

namespace RackMonitor.Services
{
    public class WoLService
    {

        public WoLService() { }

        public async void WakeOnLan(IPAddressInfo IPAddress, string macAddress)
        {
            if (string.IsNullOrWhiteSpace(macAddress))
            {
                throw new ArgumentException("MAC address cannot be null or empty.", nameof(macAddress));
            }

            byte[] macBytes = ConvertMacToBytes(macAddress);
            byte[] magicPacket = CreateMagicPacket(macBytes);

            using (var client = new UdpClient())
            {
                await client.SendAsync(magicPacket, magicPacket.Length, IPAddress.Broadcast, 9);
            }
        }

        public byte[] ConvertMacToBytes(string mac)
        {
            string[] macParts = mac.Split(new[] { ':', '-' });
            if (macParts.Length != 6)
            {
                throw new ArgumentException("MAC address must have 6 parts.", nameof(mac));
            }
            byte[] macBytes = macParts.Select(part => byte.Parse(part, NumberStyles.HexNumber)).ToArray();
            return macBytes;
        }

        public byte[] CreateMagicPacket(byte[] mac)
        {
            var packetPayload = new List<byte>();
            packetPayload.AddRange(Enumerable.Repeat((byte)0xFF, 6));

            for (int i = 0; i < 16; i++)
            {
                packetPayload.AddRange(mac);
            }
            return packetPayload.ToArray();
        }
    }

    //    public string GetBroadcastAddress(string ipAddress)
    //    {
    //        // Find the index of the last period in the IP string.
    //        int lastDotIndex = ipAddress.LastIndexOf('.');

    //        // If the format is invalid, return an empty string or throw an exception.
    //        if (lastDotIndex <= 0) return string.Empty;

    //        // Find the index of the second-to-last period by searching backwards from the last one.
    //        int secondToLastDotIndex = ipAddress.LastIndexOf('.', lastDotIndex - 1);

    //        // If the format is invalid, return an empty string or throw an exception.
    //        if (secondToLastDotIndex < 0) return string.Empty;

    //        // Take the base of the IP address (the first two octets).
    //        string ipBase = ipAddress.Substring(0, secondToLastDotIndex);

    //        // Append ".255.255" to create the broadcast address.
    //        return ipBase + ".255.255";
    //    }
    //    //public string GetBroadcastAddress(string IP)
    //    //{
    //    //    int lastOctetIndex = IP.LastIndexOf(".") + 1;
    //    //    string ipBase = IP.Substring(0, lastOctetIndex);
    //    //    return ipBase + "255";
    //    //}
    //}
}
