using RackMonitor.Models;
using RackMonitor.Security;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace RackMonitor.ViewModels
{
    public partial class MainViewModel
    {

        // --- PROXY COMMAND PREDICATE ---
        private bool CanExecuteOnSelectedRack(object parameter)
        {
            // This single predicate works for all proxy commands
            return SelectedRackViewModel != null;
        }

        // --- PROXY COMMAND EXECUTION METHODS ---
        private void ExecuteDropItem(object parameter)
        {
            SelectedRackViewModel?.DropItemCommand.Execute(parameter);
        }

        private void ExecuteShowDeviceDetails(object parameter)
        {
            SelectedRackViewModel?.ShowDeviceDetailsCommand.Execute(parameter);
        }

        private void ExecuteChangeDeviceType(object parameter)
        {
            SelectedRackViewModel?.ChangeDeviceTypeCommand.Execute(parameter);
        }

        private void ExecuteAddSlot(object parameter)
        {
            SelectedRackViewModel?.AddSlotCommand.Execute(parameter);
        }

        private void ExecuteMergeUnit(object parameter)
        {
            SelectedRackViewModel?.MergeUnitCommand.Execute(parameter);
        }
        private void ExecuteToggleSettingsPanel(object parameter)

        {

            IsSettingsPanelOpen = !IsSettingsPanelOpen;

        }

        private bool CanToggleDefault(object parameter)
        {
            return SelectedRackViewModel != null;
        }
        private bool CanDeleteSelectedRack(object parameter)
        {
            return SelectedRackViewModel != null && !SelectedRackViewModel.IsDefault;
        }
        private bool CanExecuteSaveGlobalCredentials(object parameter)
        {
            // Same validation logic
            return !string.IsNullOrWhiteSpace(PopupUsername) && PopupPassword != null && PopupPassword.Length > 0;
        }
        private void ExecuteOpenGlobalCredentialsPopup(object parameter)
        {
            // Pre-populate with current global values
            PopupUsername = _repository.GlobalCredentials.Username;

            // Always clear password input field
            PopupPassword?.Dispose();
            PopupPassword = new SecureString();
            OnPropertyChanged(nameof(PopupPassword)); // Notify assistant to clear PasswordBox

            IsGlobalCredentialsPopupOpen = true;
            OnPropertyChanged(nameof(HasEncryptedPassword)); // Update status
        }

        private void ExecuteToggleDefault(object parameter)
        {
            if (SelectedRackViewModel != null && SelectedRackViewModel.IsDefault)
            {
                var otherDefaults = AllRacks.Where(r => r != SelectedRackViewModel && r.IsDefault).ToList();

                foreach (RackViewModel rvm in otherDefaults)
                {
                    rvm.IsDefault = false;
                }
            }
        }
        private void ExecuteDeleteSelectedRack(object parameter)
        {
            if (MessageBox.Show($"This will Delete {SelectedRackViewModel.RackName}. Are you sure?",
                               "Delete Rack", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No)
            {
                return;
            }
            var tempRack = SelectedRackViewModel;
            AllRacks.Remove(tempRack);
            SelectedRackViewModel = AllRacks.FirstOrDefault();
            _repository.DeleteRack(tempRack.RackName);
            
        }
        private void ExecuteAddNewRack(object parameter)
        {
            var newRackWindow = new NewRackWindow(RackNames);
            bool? result = newRackWindow.ShowDialog();

            if (result == true)
            {
                var newRack = _repository.CreateAndSaveNewRack(newRackWindow.RackName);
                if (newRack != null)
                {
                    AllRacks.Add(new RackViewModel(_repository, newRack));
                    if (!RackNames.Contains(newRack.RackName)) { RackNames.Add(newRack.RackName); }
                    OnPropertyChanged(nameof(AllRacks));
                }

            }
            else
            {
                newRackWindow.Close();
            }
        }

        public async void ExecuteShutdownAll(object parameter)
        {

            // 1. Find all devices to shut down
            var devicesToShutdown = SelectedRackViewModel.RackUnits
                .SelectMany(unit => unit.Slots)
                .Select(slot => slot.Device)
                .OfType<ComputerDevice>() // Get only ComputerDevices
                .Where(computer => computer.AllowRemoteShutdown) // Check flag
                .ToList();

            if (devicesToShutdown.Count == 0)
            {
                MessageBox.Show("No devices are marked for remote shutdown.", "Global Shutdown", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (MessageBox.Show($"This will attempt to shut down {devicesToShutdown.Count} computer(s). Are you sure?",
                               "Confirm Global Shutdown", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No)
            {
                return;
            }

            // 2. Create a list of tasks
            List<Task> shutdownTasks = new List<Task>();
            // IsBusy = true; // TODO: Add IsBusy property to RackViewModel if needed

            Debug.WriteLine($"Attempting to shut down {devicesToShutdown.Count} device(s)...");
            foreach (var computer in devicesToShutdown)
            {
                // 3. Add the async helper method call (which returns a Task) to the list.
                // This starts the task.
                shutdownTasks.Add(ShutdownDeviceInternalAsync(computer));
            }

            // 4. Await all tasks to complete concurrently
            try
            {
                await Task.WhenAll(shutdownTasks);
                Debug.WriteLine("All shutdown tasks completed.");
                MessageBox.Show("All shutdown commands have been sent.", "Global Shutdown", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                // This catch is for errors in Task.WhenAll, though individual errors
                // are handled inside ShutdownDeviceInternalAsync
                Debug.WriteLine($"Error during Task.WhenAll: {ex.Message}");
            }
            finally
            {
                // IsBusy = false; // Clear busy flag
            }
        }
        private void ExecuteSaveGlobalCredentials(object parameter)
        {
            if (!CanExecuteSaveGlobalCredentials(null)) return;

            string plainTextPassword = null;
            string encryptedPassword = null;
            bool passwordChanged = PopupPassword != null && PopupPassword.Length > 0; // Check if user entered a password

            try
            {
                // Only encrypt if a new password was entered
                if (passwordChanged)
                {
                    plainTextPassword = ConvertToUnsecureString(PopupPassword);
                    encryptedPassword = ProtectionHelper.ProtectString(plainTextPassword);

                    if (encryptedPassword == null)
                    {
                        MessageBox.Show("Failed to encrypt password.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    // Save new encrypted password to repository
                    _repository.GlobalCredentials.EncryptedPassword = encryptedPassword;
                }

                // Always save username
                _repository.GlobalCredentials.Username = PopupUsername;

                // Save changes to disk
                _repository.SaveGlobalCredentials(_repository.GlobalCredentials);

                IsGlobalCredentialsPopupOpen = false;
                OnPropertyChanged(nameof(HasEncryptedPassword)); // Update "[Set]" status
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving global credentials: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // Clear sensitive data
                PopupPassword?.Dispose();
                PopupPassword = new SecureString();
                OnPropertyChanged(nameof(PopupPassword));
                if (plainTextPassword != null) plainTextPassword = null;
            }
        }

        private static string ConvertToUnsecureString(SecureString securePassword)
        {
            if (securePassword == null) return string.Empty;
            IntPtr unmanagedString = IntPtr.Zero;
            try
            {
                unmanagedString = Marshal.SecureStringToGlobalAllocUnicode(securePassword);
                return Marshal.PtrToStringUni(unmanagedString);
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode(unmanagedString);
            }
        }

        private void ExecuteCancelGlobalCredentials(object parameter)
        {
            IsGlobalCredentialsPopupOpen = false;
            // Clear temporary properties
            PopupUsername = null; OnPropertyChanged(nameof(PopupUsername));
            PopupPassword?.Dispose();
            PopupPassword = new SecureString();
            OnPropertyChanged(nameof(PopupPassword));
        }

        private void ExecuteToggleWoL(object parameter)
        {
            IsWoLServiceRunning = !IsWoLServiceRunning;

        }
        private void ExecuteTogglePing(object parameter)
        {
            IsPingServiceRunning = !IsPingServiceRunning;
            if (SelectedRackViewModel.IsPingServiceRunning)
            {
                SelectedRackViewModel.startMonitoring();
            }
            else
            {
                SelectedRackViewModel.stopMonitoring();
            }
        }
    }
}
