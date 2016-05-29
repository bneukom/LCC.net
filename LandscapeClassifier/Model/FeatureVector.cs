using System.Windows;
using System.Windows.Media;
using LandscapeClassifier.Util;

namespace LandscapeClassifier.Model
{
    public class FeatureVector
    {
        public LandcoverType Type { get; }
        public float Altitude { get; }
        public float Luma { get; }
        public Color Color { get; }
        public float Aspect { get; }
        public float Slope { get; }

        public FeatureVector(LandcoverType type, float altitude, float luma, Color color, float aspect, float slope)
        {
            Type = type;
            Altitude = altitude;
            Luma = luma;
            Color = color;
            Aspect = aspect;
            Slope = slope;
        }

        public override string ToString()
        {
            return Type + " " + (int)Altitude + "m " + (int)MoreMath.ToDegrees(Aspect) + "° " + (int)MoreMath.ToDegrees(Slope) + "°";
        }
    }
}
