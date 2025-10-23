using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using RackMonitor.Models;
using RackMonitor.ViewModels;

namespace RackMonitor
{
    public class DeviceProperty
    {
        public string DisplayName { get; set; }
        public string PropertyName { get; set; } 
        public object PropertyValue { get; set; }
    }


    public class PropertyTemplateSelector : DataTemplateSelector
    {

        public DataTemplate NormalTemplate { get; set; }


        public DataTemplate IpAddressTemplate { get; set; }
        public DataTemplate BooleanTemplate { get; set; }
        public DataTemplate ModelComboBoxTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {


            if (item is DevicePropertyViewModel deviceProperty)
            {
                var currentDevice = deviceProperty.SourceDevice;

                // NEW: Check if it's the 'Model' property of an AdderDevice
                if (currentDevice is AdderDevice && deviceProperty.DisplayName == "Model")
                {
                    return ModelComboBoxTemplate;
                }

                if (deviceProperty.DisplayName == "IP Address" || deviceProperty.DisplayName == "Subnet Mask")
                {
                    return IpAddressTemplate;
                }
                else if (deviceProperty.PropertyInfo.PropertyType == typeof(bool))
                {
                    return BooleanTemplate;
                }
            }
            return NormalTemplate;
        }
    }
}
