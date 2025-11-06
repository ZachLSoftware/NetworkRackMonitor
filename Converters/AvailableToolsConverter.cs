using RackMonitor.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace RackMonitor.Converters
{
    public class AvailableToolsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is RackDevice device && parameter is string tType)
            {
                if (device.GetType().Name.Equals(tType, StringComparison.OrdinalIgnoreCase))
                {
                    return Visibility.Visible;
                }
            }

            // If it's not a RackDevice, or the type doesn't match, collapse it.
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
