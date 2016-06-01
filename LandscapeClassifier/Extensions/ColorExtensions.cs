using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace LandscapeClassifier.Extensions
{
    public static class ColorExtensions
    {
        public static int GetARGB(this Color color)
        {
            return (color.A << 24) | (color.R << 16) | (color.G << 8)  | color.B;
        }
    }
}
