using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using LandscapeClassifier.Model;

namespace LandscapeClassifier.ViewModel
{
    public static class LandcoverTypeExtensions
    {
        public static Color GetColor(this LandcoverType self)
        {
            switch (self)
            {
                case LandcoverType.Grass:
                    return Colors.Green;
                case LandcoverType.Gravel:
                    return Colors.LightGray;
                case LandcoverType.Rock:
                    return Colors.DarkGray;
                case LandcoverType.Snow:
                    return Colors.White;
                case LandcoverType.Tree:
                    return Colors.DarkGreen;
                case LandcoverType.Water:
                    return Colors.Blue;
                default:
                    throw new ArgumentOutOfRangeException(nameof(self), self, null);
            }
        }
    }
}
