using System.Windows;
using System.Windows.Media;
using LandscapeClassifier.Util;

namespace LandscapeClassifier.Model
{
    public struct FeatureVector
    {
        public float Altitude { get; }
        public Color AverageNeighbourhoodColor { get; }
        public Color Color { get; }
        public float Aspect { get; }
        public float Slope { get; }

        public FeatureVector(float altitude, Color color, Color averageNeighbourhoodColor, float aspect, float slope)
        {
            Altitude = altitude;
            Color = color;
            AverageNeighbourhoodColor = averageNeighbourhoodColor;
            Aspect = aspect;
            Slope = slope;
        }

        public override string ToString()
        {
            return (int)Altitude + "m " + (int)MoreMath.ToDegrees(Aspect) + "° " + (int)MoreMath.ToDegrees(Slope) + "°";
        }
    }
}
