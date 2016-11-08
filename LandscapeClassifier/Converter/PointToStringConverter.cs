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
    public class PointToStringConverter : IValueConverter
    {
        private int _decimalPlaces = 2;
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Point input = (Point) value;

            return "(" + Math.Round(input.X, _decimalPlaces) + ", " + Math.Round(input.Y, _decimalPlaces) + ")";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
