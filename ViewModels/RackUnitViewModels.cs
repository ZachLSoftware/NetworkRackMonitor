using RackMonitor.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RackMonitor.ViewModels
{
    /// <summary>
    /// A base class for any ViewModel that needs to notify the UI of property changes.
    /// </summary>
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// Represents a single slot within a RackUnitViewModel that can hold one device.
    /// </summary>
    public class SlotViewModel : ViewModelBase
    {
        private RackDevice _device;
        public RackDevice Device
        {
            get => _device;
            set
            {
                if (_device != value)
                {
                    _device = value;
                    OnPropertyChanged(nameof(Device));
                }
            }
        }
    }

    /// <summary>
    /// Represents a single, unified row in the rack.
    /// </summary>
    public class RackUnitViewModel : ViewModelBase
    {
        private int _unitNumber;
        public int UnitNumber
        {
            get => _unitNumber;
            set { _unitNumber = value; OnPropertyChanged(); }
        }

        public string Label => $"U {UnitNumber}";

        //Slots that hold devices in the rack unit.
        public ObservableCollection<SlotViewModel> Slots { get; set; } = new ObservableCollection<SlotViewModel>();
    }
}

