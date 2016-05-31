using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace LandscapeClassifier.Converter
{
    public class DoubleRoundingConverter : IValueConverter
    {
        private int _decimalPlaces = 2;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double d = (double) value;
            return Math.Round(d, _decimalPlaces);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
