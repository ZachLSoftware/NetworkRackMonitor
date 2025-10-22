using RackMonitor.Extensions;
using RackMonitor.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace RackMonitor.ViewModels
{
    public class DevicePropertyViewModel : INotifyPropertyChanged
    {
        public PropertyInfo PropertyInfo { get; }
        private string _displayName;
        public string DisplayName 
        { 
            get => _displayName; 
            set 
            { 
                if( _displayName != value)
                {
                    _displayName = value;
                }
            } 
        }
        private string _propertyName;
        public string PropertyName
        {
            get => _propertyName;
            set
            {
                if (_propertyName != value)
                {
                    _propertyName = value;
                    OnPropertyChanged(nameof(PropertyName));
                }
            }
        }

        private object _propertyValue;
        public object PropertyValue
        {
            get => _propertyValue;
            set
            {
                if (_propertyValue != value)
                {
                    _propertyValue = value;
                    OnPropertyChanged(nameof(PropertyValue));
                }
            }
        }

        private bool _isVisible = true;
        public bool IsVisible
        {
            get => _isVisible;
            set { _isVisible = value; OnPropertyChanged(); }
        }

        public DevicePropertyViewModel(object source, PropertyInfo propertyInfo)
        {
            PropertyInfo = propertyInfo;
            _propertyValue = propertyInfo.GetValue(source);
            PropertyName = propertyInfo.Name;
            DisplayName = (source as ComputerDevice).GetFriendlyName(propertyInfo.Name);
        }

        public DevicePropertyViewModel(PropertyInfo propInfo, string displayName, string StringProperty, bool visible)
        {
            DisplayName = displayName;
            PropertyValue = StringProperty;
            PropertyInfo = propInfo;
            PropertyName = propInfo.Name;
            IsVisible = visible;
            IsVisible = visible;
        }

        public string FriendlyDisplayName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) { return string.Empty; }
            StringBuilder sb = new StringBuilder();
            sb.Append(name[0]);

            for (int i = 1; i<name.Length; i++)
            {
                if (char.IsUpper(name[i]))
                {
                    if (((name[i-1] != ' ' && !char.IsUpper(name[i - 1]))) || (name[i] == 'A' && name[i-1] == 'P' && name[i-2] == 'I'))
                    {
                        sb.Append(" ");
                    }

                }
                sb.Append(name[i]);
            }
            return sb.ToString();
        }


        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
