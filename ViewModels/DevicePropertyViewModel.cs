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
        public RackDevice SourceDevice { get; }

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
        private string _category = "General";
        public string Category
        {
            get => _category;
            set
            {
                if (_category != value)
                {
                    _category = value;
                    OnPropertyChanged(nameof(Category));
                }
            }
        }

        private bool _isVisible = true;
        public bool IsVisible
        {
            get => _isVisible;
            set { _isVisible = value; OnPropertyChanged(); }
        }

        public DevicePropertyViewModel(RackDevice source, PropertyInfo propertyInfo, string category)
        {
            PropertyInfo = propertyInfo;
            _propertyValue = propertyInfo.GetValue(source);
            SourceDevice = source;
            PropertyName = propertyInfo.Name;
            DisplayName = (source as ComputerDevice).GetFriendlyName(propertyInfo.Name);
            Category = category;
        }

        public DevicePropertyViewModel(PropertyInfo propInfo, string displayName, string StringProperty, bool visible, string category)
        {
            DisplayName = displayName;
            PropertyValue = StringProperty;
            PropertyInfo = propInfo;
            PropertyName = propInfo.Name;
            IsVisible = visible;
            Category = category;
        }


        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
