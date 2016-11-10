using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using LandscapeClassifier.Model;

namespace LandscapeClassifier.Extensions
{
    public static class LandCoverTypeExtensions
    {
        public static Brush GetDefaultBrush(this LandcoverType landcoverType)
        {
            switch (landcoverType)
            {
                case LandcoverType.Grass:
                    return new SolidColorBrush(Colors.LightGreen);
                case LandcoverType.Gravel:
                    return new SolidColorBrush(Colors.LightGray);
                case LandcoverType.Rock:
                    return new SolidColorBrush(Colors.DarkGray);
                case LandcoverType.Snow:
                    return new SolidColorBrush(Colors.White);
                case LandcoverType.Tree:
                    return new SolidColorBrush(Colors.DarkGreen);
                case LandcoverType.Water:
                    return new SolidColorBrush(Colors.DodgerBlue);
                default:
                    return new SolidColorBrush(Colors.White);
            }
        }
    }
}
