using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Security;

using System.Management;
using System.Diagnostics;

namespace RackMonitor.Services
{
    public static class ShutdownService
    {

        public static async Task<string> ShutdownComputerAsync(string targetIpAddress, string userName, SecureString password, int flag = 5)
        {
            if (string.IsNullOrWhiteSpace(targetIpAddress))
            {
                return "Target IP Address cannot be empty.";
            }
            //if (string.IsNullOrWhiteSpace(userName) || password == null || password.Length == 0)
            //{
            //    return "Username and password are required for remote WMI operations.";
            //}

            // Wrap the synchronous WMI code in Task.Run to execute it on a background thread
#pragma warning disable CS8603 // Possible null reference return.
            return await Task.Run(() =>
            {
                SecureString securePassword = new SecureString();

                try
                {
                    var options = new ConnectionOptions
                    {
                        Username = userName,
                        SecurePassword = password,
                        Impersonation = ImpersonationLevel.Impersonate,
                        Authentication = AuthenticationLevel.PacketPrivacy,
                        EnablePrivileges = true // Required for shutdown
                    };

                    var scope = new ManagementScope($@"\\{targetIpAddress}\root\cimv2", options);
                    scope.Connect();


                    var query = new SelectQuery("Win32_OperatingSystem");
                    using (var searcher = new ManagementObjectSearcher(scope, query))
                    {
                        bool commandSent = false;
                        foreach (ManagementObject os in searcher.Get())
                        {
                            ManagementBaseObject inParams = os.GetMethodParameters("Win32Shutdown");
                            // Flags: 1=Shutdown, 4=Force, 2=Reboot. Combine as needed. 5 = Shutdown + Force
                            inParams["Flags"] = flag;
                            inParams["Reserved"] = "0";

                            Debug.WriteLine("WMI: Sending shutdown command...");

                            // Invoke the method (blocks)
                            ManagementBaseObject outParams = os.InvokeMethod("Win32Shutdown", inParams, null);

                            // Check the return value (0 usually indicates success)
                            uint returnValue = (uint)outParams["returnValue"];
                            if (returnValue == 0)
                            {
                                Debug.WriteLine("WMI: Shutdown command sent successfully (ReturnValue=0).");
                                commandSent = true;
                                break; // Exit loop once sent
                            }
                            else
                            {
                                Debug.WriteLine($"WMI: Shutdown command failed on target (ReturnValue={returnValue}).");
                                return $"Shutdown command failed on target (ReturnValue={returnValue}). Check permissions and WMI configuration.";
                            }
                        }

                        if (!commandSent)
                        {
                            // This might happen if the Win32_OperatingSystem query returns no results
                            return "Could not find operating system object via WMI to invoke shutdown.";
                        }
                    }
                    return null; 
                }
                catch (UnauthorizedAccessException ex)
                {
                    Debug.WriteLine($"WMI Unauthorized: {ex.Message}");
                    return $"WMI Unauthorized: Access denied connecting to {targetIpAddress}. Check credentials and permissions. Details: {ex.Message}";
                }
                catch (System.Runtime.InteropServices.COMException ex)
                {
                    Debug.WriteLine($"WMI COM Error: {ex.Message}");
                    return $"WMI Connection Error to {targetIpAddress}. Check network, firewall, and WMI service. Details: {ex.Message}";
                }
                catch (ManagementException ex) 
                {
                    Debug.WriteLine($"WMI Management Error: {ex.Message}");
                    return $"WMI Error on {targetIpAddress}. Details: {ex.Message}";
                }
                catch (Exception ex) 
                {
                    Debug.WriteLine($"WMI operation failed unexpectedly: {ex.Message}");
                    return $"WMI operation failed unexpectedly: {ex.Message}";
                }
            });
#pragma warning restore CS8603 // Possible null reference return.
        }
    }

}
