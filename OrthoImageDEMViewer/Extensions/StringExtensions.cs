using System.Text.RegularExpressions;

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
