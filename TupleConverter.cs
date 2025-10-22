using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace RackMonitor
{
    public class TupleConverter : MarkupExtension, IMultiValueConverter
    {
        private static TupleConverter _instance;

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2) return null;
            return (values[0], values[1]?.ToString());
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return _instance ?? (_instance = new TupleConverter());
        }

        public static TupleConverter Instance => _instance ?? (_instance = new TupleConverter());
    }
}