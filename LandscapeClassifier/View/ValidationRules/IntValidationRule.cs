using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace LandscapeClassifier.View.ValidationRules
{
    public class IntValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            int result = 0;
            bool canConvert = int.TryParse(value as string, out result);
            return new ValidationResult(canConvert, "Not a valid int.");
        }
    }
}
