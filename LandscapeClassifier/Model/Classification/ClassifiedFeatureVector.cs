
using System.Windows;

namespace LandscapeClassifier.Model.Classification
{
    public class ClassifiedFeatureVector
    {
        public LandcoverType Type { get; set; }
        public FeatureVector FeatureVector { get; set; }
        public Point Position { get; set; }

        public ClassifiedFeatureVector(LandcoverType type, FeatureVector featureVector, Point position)
        {
            Type = type;
            FeatureVector = featureVector;
            Position = position;
        }
    }
}
