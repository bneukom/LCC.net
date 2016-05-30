using System;
using System.Text.RegularExpressions;
using System.Windows.Media;

namespace LandscapeClassifier.Extensions
{
    public static class StringExtensions
    {
        public static string ConvertWhitespacesToSingleSpaces(this string value)
        {
            return Regex.Replace(value, @"\s+", " ");
        }


    }
}
