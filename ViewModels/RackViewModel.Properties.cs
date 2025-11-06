using RackMonitor.Data;
using RackMonitor.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace RackMonitor.ViewModels
{
    public partial class RackViewModel
    {


        #region Services
        private readonly RackRepository _repository;
        private MonitoringService monitor;
        #endregion


        #region private
        private string _rackName;
        private bool _isDefault = false;
        private int _numberOfUnits = 12;
        private DeviceDetailsViewModel _selectedDeviceDetails;
        private bool _isDetailsPanelOpen;
        private bool _isPingServiceRunning = true;
        private bool _isWoLServiceRunning = true;
        private bool _isGlobalCredentialsPopupOpen = false;
        private SecureString _popupPassword;
        private RackStateDto _RackStateDto;
        #endregion


        #region public
        public ObservableCollection<RackUnitViewModel> RackUnits { get; }
        public string RackName
        {
            get => _rackName;
            set
            {
                if (value != _rackName)
                {
                    _rackName = value;
                    OnPropertyChanged(nameof(RackName));
                }
            }
        }

        
        public bool IsDefault
        {
            get => _isDefault;
            set
            {
                if (value != _isDefault)
                {
                    _isDefault = value;
                    OnPropertyChanged(nameof(IsDefault));
                    SaveRack();
                }
            }
        }
        
        public int NumberOfUnits
        {
            get => _numberOfUnits;
            set
            {
                if (_numberOfUnits != value)
                {
                    _numberOfUnits = value;
                    OnPropertyChanged(nameof(NumberOfUnits));
                    ExecuteUpdateRackSize(NumberOfUnits);
                }
            }
        }

        
        public DeviceDetailsViewModel SelectedDeviceDetails
        {
            get => _selectedDeviceDetails;
            set
            {
                if (_selectedDeviceDetails != value)
                {
                    _selectedDeviceDetails?.Cleanup();
                    _selectedDeviceDetails = value;
                    OnPropertyChanged(nameof(SelectedDeviceDetails));
                }
            }
        }

      
        public bool IsDetailsPanelOpen
        {
            get => _isDetailsPanelOpen;
            set
            {
                if (_isDetailsPanelOpen != value)
                {
                    _isDetailsPanelOpen = value;
                    OnPropertyChanged(nameof(IsDetailsPanelOpen));
                    if (!value)
                    {
                        SelectedDeviceDetails = null;
                    }
                }
            }
        }
        public bool IsPingServiceRunning
        {
            get => _isPingServiceRunning;
            set
            {
                _isPingServiceRunning = value;
                OnPropertyChanged(nameof(IsPingServiceRunning));
            }
        }

        public bool IsWoLServiceRunning
        {
            get => _isWoLServiceRunning;
            set
            {
                _isWoLServiceRunning = value;
                OnPropertyChanged(nameof(IsWoLServiceRunning));
            }
        }

      
        //public bool IsGlobalCredentialsPopupOpen
        //{
        //    get => _isGlobalCredentialsPopupOpen;
        //    set { _isGlobalCredentialsPopupOpen = value; OnPropertyChanged(nameof(IsGlobalCredentialsPopupOpen)); }
        //}
        //private string _popupUsername;
        //public string PopupUsername
        //{
        //    get => _popupUsername;
        //    set { _popupUsername = value; OnPropertyChanged(nameof(PopupUsername)); }
        //}
        //public SecureString PopupPassword
        //{
        //    get => _popupPassword;
        //    set { _popupPassword = value; OnPropertyChanged(nameof(PopupPassword)); }
        //}

        //// Read-only property to show status in MainWindow
        //public bool HasEncryptedPassword =>
        //    !string.IsNullOrEmpty(_repository.GlobalCredentials.Username) && !string.IsNullOrEmpty(_repository.GlobalCredentials.EncryptedPassword);
        #endregion

        #region ICommands

        public ICommand UpdateRackSizeCommand { get; }
        public ICommand DeleteDeviceCommand { get; }
        public ICommand AddSlotCommand { get; }
        public ICommand MergeUnitCommand { get; }
        public ICommand ChangeDeviceTypeCommand { get; }
        public ICommand ShowDeviceDetailsCommand { get; }
        public ICommand GetAllIPsCommand { get; }
        public ICommand ToggleWoLServiceCommand { get; }
        public ICommand TogglePingServiceCommand { get; }
        public ICommand CloseDetailsPanelCommand { get; }
        public ICommand DropItemCommand { get; }
        #endregion

        #region EventHandlers
        public EventHandler<PingServiceToggledEventArgs> PingToggled;
        public EventHandler<WoLServiceToggledEventArgs> WoLToggled;
        public event EventHandler<DeviceSavedEventArgs> DeviceSaved;
        #endregion
    }
}
