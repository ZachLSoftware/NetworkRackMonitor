using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace RackMonitor
{
    class WindowCloseConverter : MarkupExtension, IMultiValueConverter
    {
        private static WindowCloseConverter _instance;

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            return values.Clone();
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            if (value is object[] values && values.Length == 2 && values[0] is Window window && values[1] is bool dialogResult)
            {
                window.DialogResult = dialogResult;
                window.Close();
            }
            return null;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return _instance ?? (_instance = new WindowCloseConverter());
        }

        public static WindowCloseConverter Instance => _instance ?? (_instance = new WindowCloseConverter());
    }
}
