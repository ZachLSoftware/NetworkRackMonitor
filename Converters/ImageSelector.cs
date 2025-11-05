using RackMonitor.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace RackMonitor.Converters
{
    public class ImageSelector : IMultiValueConverter
    {
        private static readonly string DefaultIconPath = "/Assets/Icons/Default.png"; // Make sure you have a default icon

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)

        {
            if (values == null || values.Length < 2 || values[0] is not RackDevice device)
            {
                return null;
            }

            string model = values[1] != DependencyProperty.UnsetValue ? values[1] as string : null;

            string iconName = "Default"; // Default icon name

            switch (device.DeviceType)
            {
                case "Computer":
                    iconName = "PC";
                    break;
                case "Network":
                    return null;
                case "Adder":
                    if (device is AdderDevice adder && !string.IsNullOrWhiteSpace(adder.Model))
                    {
                        List<string> AvailableAdderModels = new List<string> {"ASP001", "ALIF4000T", "ALIF2100T" };
                        // Basic sanitization (replace invalid path chars if necessary)
                        string safeModelName = adder.Model.Replace(" ", "_").Replace("/", "-");
                        iconName = safeModelName;
                        if (!AvailableAdderModels.Contains(iconName)) { return null; }
                    }
                    else
                    {
                        return null;
                    }
                    break;
                default:
                    return null;

            }

            string potentialPath = $"/Assets/Icons/{iconName}.png";

            return new BitmapImage(new Uri(potentialPath, UriKind.Relative));
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException(); // Not needed
        }
    }
}
