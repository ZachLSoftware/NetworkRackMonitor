using RackMonitor.Data;
using RackMonitor.Models;
using RackMonitor.ViewModels;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace RackMonitor.Services
{
    public class MonitoringService
    {
        private readonly RackViewModel _rackViewModel;
        private readonly PingService _pingService;
        private readonly ArpService _arpService;
        private readonly WoLService _wolService;
        public event Action updateDevices;

        private CancellationTokenSource _cancellationTokenSource;
        private List<ArpItem> _arpCache = null;
        private Dictionary<string, string> IpToMacDict = new Dictionary<string, string>();
        private bool updateDevicesFlag = false;
        private bool _isWoLRunning = true;
        public bool IsWoLRunning
        {
            get => _isWoLRunning;
            set { _isWoLRunning = value; }
        }
        private bool _isRunning = true;
        public bool IsRunning
        {
            get => _isRunning;
            set { _isRunning = value; }
        }

        public MonitoringService(RackViewModel rackViewModel)
        {
            _rackViewModel = rackViewModel;
            _pingService = new PingService();
            _arpService = new ArpService();
            _wolService = new WoLService();

            _pingService.PingCompleted += OnPingCompleted;
            _rackViewModel.DeviceSaved += OnDeviceSaved;
        }

        public void StartMonitoring()
        {
            IsRunning = true;
            if (_cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested)
            {
                Debug.WriteLine("[Monitoring Service] Service is already running.");
                return;
            }
            _cancellationTokenSource = new CancellationTokenSource();
            Task.Run(() => MonitorLoopAsync(_cancellationTokenSource.Token));
            
        }

        public void StopMonitoring()
        {
            _cancellationTokenSource.Cancel();
            IsRunning = false;
        }

        private async Task MonitorLoopAsync(CancellationToken cancellationToken)
        {
            Debug.WriteLine("Monitoring Started");
            while (!cancellationToken.IsCancellationRequested)
            {
                var devices = _rackViewModel.GetAllDevices();
                _arpCache = _arpService.GetArpResult();

                if (devices.Count > 0)
                {
                    foreach (var device in devices)
                    {
                        if (cancellationToken.IsCancellationRequested) break;
                        if ((device.IPAddressInfo.Address != null && device.IPAddressInfo.Address != "" && device.IPAddressInfo.Address != "..." && device.Ping))
                        {
                            _pingService.PingHost(device.IPAddressInfo.Address, 1);
                        } 
                                                    
                        if (device.SecondIPAddress && device.IPAddressInfo2.Address != null && device.IPAddressInfo2.Address != "" && device.IPAddressInfo2.Address != "..." && device.Ping)
                        {
                            Debug.WriteLine($"Second IP Ping {device.IPAddressInfo2.Address}");
                            _pingService.PingHost(device.IPAddressInfo2.Address, 2);
                        }

                        

                        //Debug.WriteLine($"[Monitoring Service] Ping {ip}: {(isUp ? "Success" : "Failed")}");
                    }
                }
                else
                {
                    Debug.WriteLine("[Monitoring Service] No devices to ping.");
                }
                try
                {

                    await Task.Delay(30000, cancellationToken); // Wait for 30 seconds
                }
                catch (TaskCanceledException)
                {
                    // This exception is expected when the task is cancelled. We can break the loop.
                    break;
                }
                if (updateDevicesFlag)
                {
                    Debug.WriteLine("Updating Devices");
                    updateDevices?.Invoke();
                    updateDevicesFlag = true;
                }
                else
                {
                    Debug.WriteLine("Skipping Updating Devices");
                }
            }
        }

        public void OnDeviceSaved(Object sender, DeviceSavedEventArgs e)
        {
            if (e.Device != null)
            {
                ManualCheck(e.Device);
            }
        }

        public async void ManualCheck(RackDevice device)
        {
            if ((device.IPAddressInfo.Address != null && device.IPAddressInfo.Address != "" && device.IPAddressInfo.Address != "..." && device.Ping))
            {
                _pingService.PingHost(device.IPAddressInfo.Address, 1);
            }

            if (device.SecondIPAddress && device.IPAddressInfo2.Address != null && device.IPAddressInfo2.Address != "" && device.IPAddressInfo2.Address != "..." && device.Ping)
            {
                _pingService.PingHost(device.IPAddressInfo2.Address, 2);
            }
        }

        private void OnPingCompleted(object sender, PingEventArgs e)
        {
            var device = _rackViewModel.FindDeviceByIP(e.IPAddress);
            if (device == null) return;

            Debug.WriteLine($"[Monitoring Service] Ping {e.IPAddress}: {(e.IsSuccessful ? "Success" : "Failed")}");

            Application.Current.Dispatcher.Invoke(() =>
            {
                if (e.InterfaceID == 1)
                {
                    device.Connected = e.IsSuccessful;
                }
                else
                {
                    device.Connected2 = e.IsSuccessful;
                }
                if (e.IsSuccessful && device is ComputerDevice computer)
                {

                    var macAddress = _arpCache
                        .FirstOrDefault(m => m.Ip == e.IPAddress)
                        ?.MacAddress.Replace('-', ':').ToUpper();

                    if (macAddress != null && macAddress != computer.MacAddress)
                    {
                        // 3. Update the model object directly.
                        computer.MacAddress = macAddress;

                        updateDevicesFlag = true;


                        Debug.WriteLine($"[Monitoring Service] Found MAC {macAddress} for {e.IPAddress} in cache.");
                    }
                }
                else if (!e.IsSuccessful && device is ComputerDevice comp && (device as ComputerDevice).IsWolEnabled && IsWoLRunning)
                {
                    if (device.Connected || device.Connected2) return;
                    try
                    {
                        if (comp.MacAddress != null)
                        {
                            _wolService.WakeOnLan(comp.IPAddressInfo, comp.MacAddress);
                            Debug.WriteLine("Attempting WOL");
                        }

                    }
                    catch
                    {
                        Debug.WriteLine("Error with WoL");
                    }
                }


            });
        }
    }
}
