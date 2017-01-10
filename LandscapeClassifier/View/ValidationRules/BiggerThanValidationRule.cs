using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace LandscapeClassifier.View.ValidationRules
{
    public class BiggerThanValidationRule : ValidationRule
    {
        private double _biggerThan;

        public BiggerThanValidationRule(double biggerThan)
        {
            _biggerThan = biggerThan;
        }

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            double result = 0;
            bool canConvert = double.TryParse(value as string, out result);

            return new ValidationResult(canConvert && result > _biggerThan, $"Value must be bigger than {_biggerThan}.");
        }
    }
}