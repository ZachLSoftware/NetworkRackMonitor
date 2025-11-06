using RackMonitor.Data;
using RackMonitor.Models;
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
    public partial class MainViewModel
    {
        private readonly RackRepository _repository;

        #region ICommands
        public ICommand OpenGlobalCredentialsPopupCommand { get; }
        public ICommand DeleteSelectedRackCommand { get; }

        public ICommand SaveCredentialsCommand { get; }
        public ICommand CancelCredentialsCommand { get; }
        public ICommand ShutdownAllSelectedRackPCsCommand { get; }
        public ICommand ToggleSettingsPanelCommand { get; }
        public ICommand ToggleWoLServiceCommand { get; }
        public ICommand TogglePingServiceCommand { get; }
        public ICommand AddNewRackCommand { get; }
        public ICommand ToggleDefaultCommand { get; }


        // --- PROXY COMMANDS ---
        public ICommand DropItemCommand { get; }
        public ICommand ShowDeviceDetailsCommand { get; }
        public ICommand ChangeDeviceTypeCommand { get; }
        public ICommand AddSlotCommand { get; }
        public ICommand MergeUnitCommand { get; }
        #endregion

        #region Private
        private int _numberOfUnits = 12;
        private bool _isPingServiceRunning = true;
        private Credentials _globalCredentials;
        private bool _isWoLServiceRunning = true;
        private RackViewModel _selectedRackViewModel;
        private bool _isGlobalCredentialsPopupOpen = false;
        private string _popupUsername;
        private SecureString _popupPassword;
        #endregion

        #region Public
        public ObservableCollection<RackViewModel> AllRacks { get; set; }
        public List<string> RackNames = new List<string>();
        private bool _isSettingsPanelOpen = true;
        public int NumberOfUnits
        {
            get => _numberOfUnits;
            set
            {
                if (_numberOfUnits != value)
                {
                    _numberOfUnits = value;
                    OnPropertyChanged(nameof(NumberOfUnits));
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
        public bool IsSettingsPanelOpen
        {
            get => _isSettingsPanelOpen;
            set
            {

                if (_isSettingsPanelOpen != value)
                {
                    _isSettingsPanelOpen = value;
                    OnPropertyChanged(nameof(IsSettingsPanelOpen));
                }
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
        
        public RackViewModel SelectedRackViewModel
        {
            get => _selectedRackViewModel;
            set
            {
                if (_selectedRackViewModel != value)
                {
                    _selectedRackViewModel = value;
                    OnPropertyChanged(nameof(SelectedRackViewModel));
                    if (_selectedRackViewModel != null)
                    {
                        IsPingServiceRunning = _selectedRackViewModel.IsPingServiceRunning;
                        IsWoLServiceRunning = _selectedRackViewModel.IsWoLServiceRunning;
                    }
                }
            }
        }
        public bool IsGlobalCredentialsPopupOpen
        {
            get => _isGlobalCredentialsPopupOpen;
            set { _isGlobalCredentialsPopupOpen = value; OnPropertyChanged(nameof(IsGlobalCredentialsPopupOpen)); }
        }
        
        public string PopupUsername
        {
            get => _popupUsername;
            set { _popupUsername = value; OnPropertyChanged(nameof(PopupUsername)); }
        }

        
        public SecureString PopupPassword
        {
            get => _popupPassword;
            set { _popupPassword = value; OnPropertyChanged(nameof(PopupPassword)); }
        }

        public EventHandler<PingServiceToggledEventArgs> PingToggled;
        public EventHandler<WoLServiceToggledEventArgs> WoLToggled;

        public bool HasEncryptedPassword => !string.IsNullOrEmpty(_repository.GlobalCredentials.Username) && !string.IsNullOrEmpty(_repository.GlobalCredentials.EncryptedPassword);

        #endregion
    }
}
