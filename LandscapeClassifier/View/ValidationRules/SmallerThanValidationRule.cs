using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace LandscapeClassifier.View.ValidationRules
{
    public class SmallerThanValidationRule : ValidationRule
    {
        private readonly double _smallerThan;

        public SmallerThanValidationRule(double smallerThan)
        {
            _smallerThan = smallerThan;
        }

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            double result = 0;
            bool canConvert = double.TryParse(value as string, out result);

            return new ValidationResult(canConvert &&  result < _smallerThan, $"Value must be smaller than {_smallerThan}.");
        }
    }
}