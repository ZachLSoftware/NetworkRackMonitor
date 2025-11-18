using RackMonitor.Data;
using RackMonitor.Models;
using RackMonitor.Security;
using RackMonitor.Services;
using RackMonitor.UserControls.IPControls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;


namespace RackMonitor.ViewModels
{
    public partial class MainViewModel : INotifyPropertyChanged
    {
       
        public MainViewModel(RackRepository repository)
        {
            {
                //Initiate Repo and load racks
                _repository = repository;
                List<RackStateDto> rackStateDtos = _repository.LoadAllRackData();
                AllRacks = new ObservableCollection<RackViewModel>();
                foreach (RackStateDto rackDto in rackStateDtos)
                {
                    if (!RackNames.Contains(rackDto.RackName)) { RackNames.Add(rackDto.RackName); }
                    AllRacks.Add(new RackViewModel(_repository, rackDto));
                }

                //Get default rack or select first
                var defaultRack = AllRacks.FirstOrDefault(vm => vm.IsDefault);
                SelectedRackViewModel = defaultRack ?? AllRacks.FirstOrDefault();

                //Initialize settings panel
                IsPingServiceRunning = SelectedRackViewModel?.IsPingServiceRunning ?? false;
                IsWoLServiceRunning = SelectedRackViewModel?.IsWoLServiceRunning ?? false;
                NumberOfUnits = SelectedRackViewModel?.NumberOfUnits ?? 12;
    
               
                //Command Bindings
                ToggleWoLServiceCommand = new RelayCommand(ExecuteToggleWoL);
                TogglePingServiceCommand = new RelayCommand(ExecuteTogglePing);
                OpenGlobalCredentialsPopupCommand = new RelayCommand(ExecuteOpenGlobalCredentialsPopup);
                SaveCredentialsCommand = new RelayCommand(ExecuteSaveGlobalCredentials, CanExecuteSaveGlobalCredentials);
                CancelCredentialsCommand = new RelayCommand(ExecuteCancelGlobalCredentials);
                ShutdownAllSelectedRackPCsCommand = new RelayCommand(ExecuteShutdownAll);
                ToggleSettingsPanelCommand = new RelayCommand(ExecuteToggleSettingsPanel);
                AddNewRackCommand = new RelayCommand(ExecuteAddNewRack);
                DeleteSelectedRackCommand = new RelayCommand(ExecuteDeleteSelectedRack, CanDeleteSelectedRack);
                ToggleDefaultCommand = new RelayCommand(ExecuteToggleDefault, CanToggleDefault);

                //proxy commands
                DropItemCommand = new RelayCommand(ExecuteDropItem, CanExecuteOnSelectedRack);
                ShowDeviceDetailsCommand = new RelayCommand(ExecuteShowDeviceDetails, CanExecuteOnSelectedRack);
                ChangeDeviceTypeCommand = new RelayCommand(ExecuteChangeDeviceType, CanExecuteOnSelectedRack);
                AddSlotCommand = new RelayCommand(ExecuteAddSlot, CanExecuteOnSelectedRack);
                MergeUnitCommand = new RelayCommand(ExecuteMergeUnit, CanExecuteOnSelectedRack);
            }
        }

        /// <summary>
        /// Contains the shutdown logic for a single device, designed to be run concurrently.
        /// </summary>
        private async Task ShutdownDeviceInternalAsync(ComputerDevice computer)
        {
            // NOTE: This assumes ComputerDevice and RackRepository have the required credential properties
            // e.g., computer.UseGlobalCredentials, _repository.GlobalCredentials, computer.pcCredentials
            try
            {
                computer.HasStatus = true;
                computer.StatusMessage = "Attempting Shutdown...";
                computer.IsShuttingDown = true;
                string targetIp = computer.IPAddressInfo?.Address;

                // Determine which credentials to use
                Credentials credentials = computer.UseGlobalCredentials ? _repository.GlobalCredentials : computer.pcCredentials;

                if (string.IsNullOrEmpty(targetIp))
                {
                    computer.StatusMessage = "Failed: No IP Address";
                    return; // Can't proceed
                }
                if (credentials == null || string.IsNullOrEmpty(credentials.Username) || string.IsNullOrEmpty(credentials.EncryptedPassword))
                {
                    computer.StatusMessage = "Failed: No Credentials";
                    return; // Can't proceed
                }

                SecureString password = new SecureString();
                string plainTextPassword = null;
                string result;

                try
                {
                    // 1. Decrypt the stored password
                    plainTextPassword = ProtectionHelper.UnprotectString(credentials.EncryptedPassword);
                    if (plainTextPassword == null)
                    {
                        throw new Exception("Failed to decrypt password. (Invalid or wrong user?)");
                    }

                    // 2. Convert to SecureString
                    foreach (char c in plainTextPassword)
                    {
                        password.AppendChar(c);
                    }
                    password.MakeReadOnly();

                    // 3. Await the shutdown for THIS device
                    result = await ShutdownService.ShutdownComputerAsync(targetIp, credentials.Username, password);
                }
                finally
                {
                    password.Dispose();
                    if (plainTextPassword != null) plainTextPassword = null; // Clear from memory
                }

                // 4. Update status based on result
                if (!string.IsNullOrEmpty(result)) // Error
                {
                    computer.StatusMessage = $"Failed: {result}";
                }
                else // Success
                {
                    computer.StatusMessage = "Shutdown Command Sent";
                }
            }
            catch (Exception ex)
            {
                computer.StatusMessage = $"Failed: {ex.Message}";
            }
            finally
            {
                computer.IsShuttingDown = false; // Reset flag for this device
            }
        }
        

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }


    }
