using RackMonitor.Behaviors;
using RackMonitor.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace RackMonitor.ViewModels
{
    public partial class RackViewModel
    {
        #region execution_checks

        //private bool CanExecuteSaveGlobalCredentials(object parameter)
        //{
        //    // Same validation logic
        //    return !string.IsNullOrWhiteSpace(PopupUsername) && PopupPassword != null && PopupPassword.Length > 0;
        //}
        private bool CanExecuteDropItem(object parameter)
        {
            return parameter is DragDropData;
        }
        /// <summary>
        /// Allows adding up to 4 slots to avoid excessive columns
        /// </summary>
        private bool CanExecuteAddSlot(object parameter)
        {
            return parameter is RackUnitViewModel unit && unit.Slots.Count < 4;
        }

        /// <summary>
        /// Only execute if there are multiple slots on the unit.
        /// </summary>
        private bool CanExecuteMergeUnit(object parameter)
        {
            return parameter is RackUnitViewModel unit && unit.Slots.Count > 1;
        }


        /// <summary>
        /// Only allow adding devices when object is a slot and the device isn't already that type.
        /// Need to fix for changing type and handling gracefully.
        /// </summary>
        private bool CanExecuteChangeDeviceType(object parameter)
        {
            if (!(parameter is ValueTuple<object, string> tuple)) return false;
            if (!(tuple.Item1 is SlotViewModel slot)) return false;
            var targetDeviceType = tuple.Item2;
            return slot.Device?.DeviceType != targetDeviceType;
        }

        private bool CanDeleteDevice(object parameter)
        {
            return (parameter is SlotViewModel slot && slot.Device != null);
        }
        #endregion

        #region executions

        private void ExecuteAddSlot(object parameter)
        {
            if (parameter is RackUnitViewModel unit)
            {
                AddSlotToUnit(unit);
            }
        }

        private void ExecuteDeleteDevice(object parameter)
        {
            if (parameter is SlotViewModel slot)
            {
                slot.Device = null;
                SaveRack();
            }
        }

        private void ExecuteMergeUnit(object parameter)
        {
            if (parameter is RackUnitViewModel unit)
            {
                MergeUnitToSingleSlot(unit);
            }
        }



        private void ExecuteChangeDeviceType(object parameter)
        {
            if (!(parameter is ValueTuple<object, string> tuple)) return;
            if (!(tuple.Item1 is SlotViewModel slot)) return;
            var deviceType = tuple.Item2;

            ChangeDeviceType(slot, deviceType);
        }

        private void ExecuteShowDeviceDetails(object parameter)
        {
            if (parameter is SlotViewModel slot && slot.Device != null)
            {
                // Create the ViewModel for the selected device
                var detailsViewModel = new DeviceDetailsViewModel(slot.Device, _repository);

                // Hook up the Saved event to trigger repository save and check
                detailsViewModel.Saved += () =>
                {
                    SaveRack();
                    CheckDeviceState(slot.Device);
                };

                // Set the ViewModel for the panel
                SelectedDeviceDetails = detailsViewModel;

                // Open the panel
                IsDetailsPanelOpen = true;

                // Removed code that showed a separate window
            }
            else // If clicking an empty slot or invalid parameter, ensure panel closes
            {
                IsDetailsPanelOpen = false;
            }
        }

        private void ExecuteCloseDetailsPanel(object parameter)
        {
            IsDetailsPanelOpen = false;
        }

        public async void ExecuteShutdownAll(object parameter)
        {

            // 1. Find all devices to shut down
            var devicesToShutdown = RackUnits
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

        public void ExecuteDropItem(object parameter)
        {

            Debug.WriteLine("ExecuteDropItem called"); // Debug output
            if (parameter is DragDropData data && data.SourceSlot != null && data.TargetSlot != null)
            {
                Debug.WriteLine($"Attempting move from slot {data.SourceSlot.GetHashCode()} to {data.TargetSlot.GetHashCode()}"); // More debug
                MoveOrSwapDevice(data.SourceSlot, data.TargetSlot);
            }
            else
            {
                Debug.WriteLine("Drop failed: Invalid DragDropData received."); // Debug invalid data
            }
        }

        //private void ExecuteOpenGlobalCredentialsPopup(object parameter)
        //{
        //    // Pre-populate with current global values
        //    PopupUsername = _repository.GlobalCredentials.Username;

        //    // Always clear password input field
        //    PopupPassword?.Dispose();
        //    PopupPassword = new SecureString();
        //    OnPropertyChanged(nameof(PopupPassword)); // Notify assistant to clear PasswordBox

        //    IsGlobalCredentialsPopupOpen = true;
        //    OnPropertyChanged(nameof(HasEncryptedPassword)); // Update status
        //}

        //private void ExecuteCancelGlobalCredentials(object parameter)
        //{
        //    IsGlobalCredentialsPopupOpen = false;
        //    // Clear temporary properties
        //    PopupUsername = null; OnPropertyChanged(nameof(PopupUsername));
        //    PopupPassword?.Dispose();
        //    PopupPassword = new SecureString();
        //    OnPropertyChanged(nameof(PopupPassword));
        //}
        private void ExecuteUpdateRackSize(object parameter)
        {
            UpdateRackSize(NumberOfUnits);
        }

        private void ExecuteGetAllIPs(object parameter)
        {
            var ipList = GetAllDevices();
            Debug.WriteLine("--- All Device IPs ---");
            if (ipList.Any())
            {
                foreach (var ip in ipList)
                {
                    Debug.WriteLine(ip);
                }
            }
            else
            {
                Debug.WriteLine("No devices with IP addresses found.");
            }
            Debug.WriteLine("----------------------");
        }
        #endregion
    }
}
