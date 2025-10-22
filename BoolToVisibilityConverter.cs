using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace RackMonitor
{
    public class BoolToVisibilityConverter : MarkupExtension, IValueConverter
    {
        private static BooleanToVisibilityConverter _instance;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value is bool b && b) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is Visibility v && v == Visibility.Visible;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return _instance ?? (_instance = new BooleanToVisibilityConverter());
        }

        public static BooleanToVisibilityConverter Instance => _instance ?? (_instance = new BooleanToVisibilityConverter());
    }
}
