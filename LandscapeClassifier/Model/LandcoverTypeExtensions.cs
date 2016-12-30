using System;
using System.Windows.Media;

namespace LandscapeClassifier.Model
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
                case LandcoverType.Agriculture:
                    return Colors.AntiqueWhite;
                case LandcoverType.None:
                    return Colors.White;
                case LandcoverType.Settlement:
                    return Colors.Black;
                case LandcoverType.Soil:
                    return Colors.SaddleBrown;
                default:
                    throw new ArgumentOutOfRangeException(nameof(self), self, null);
            }
        }
    }
}
