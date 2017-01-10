using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace LandscapeClassifier.Converter
{
    public class DoubleToPersistantStringConverter : IValueConverter
    {
        private string lastConvertBackString;

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (!(value is double)) return null;

            var stringValue = lastConvertBackString ?? value.ToString();
            lastConvertBackString = null;

            return stringValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (!(value is string)) return null;

            double result;
            if (double.TryParse((string)value, out result))
            {
                lastConvertBackString = (string)value;
                return result;
            }

            return null;
        }
    }
}
