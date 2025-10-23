using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace RackMonitor.Converters
{
    public class DivideByValueConverter : MarkupExtension, IValueConverter
    {
        private static DivideByValueConverter _instance;

        public double Divisor { get; set; } = 2.0; // Default to dividing by 2 for radius

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double doubleValue)
            {
                if (parameter != null && double.TryParse(parameter.ToString(), out double divisorParam))
                {
                    return doubleValue / divisorParam;
                }
                return doubleValue / Divisor;
            }
            return DependencyProperty.UnsetValue; // Indicate conversion failure
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            // Return a shared instance for efficiency
            return _instance ?? (_instance = new DivideByValueConverter());
        }

        // Optional: Static instance property for easy access like {x:Static local:DivideByValueConverter.Instance}
        public static DivideByValueConverter Instance => _instance ?? (_instance = new DivideByValueConverter());
    }
}
