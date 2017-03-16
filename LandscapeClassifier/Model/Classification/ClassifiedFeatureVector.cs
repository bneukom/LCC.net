
using System.Windows;

namespace LandscapeClassifier.Model.Classification
{
    public class ClassifiedFeatureVector
    {
        public int FeatureClass { get; set; }
        public FeatureVector FeatureVector { get; set; }
        public Point Position { get; set; }

        public ClassifiedFeatureVector(int featureClass, FeatureVector featureVector, Point position)
        {
            FeatureClass = featureClass;
            FeatureVector = featureVector;
            Position = position;
        }
    }
}
