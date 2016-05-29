using System.Windows;

namespace LandscapeClassifier.Model
{
    public class FeatureVector
    {
        public LandcoverType Type { get; }
        public Point LV95Position { get; }
        public float Altitude { get; }
        public float Luma { get; }
        public float Aspect { get; }
        public float Slope { get; }

        public FeatureVector(LandcoverType type, Point lv95Position, float altitude, float luma, float aspect, float slope)
        {
            Type = type;
            LV95Position = lv95Position;
            Altitude = altitude;
            Luma = luma;
            Aspect = aspect;
            Slope = slope;
        }

        public override string ToString()
        {
            return Type + " " + LV95Position + " " + Altitude;
        }
    }
}
