namespace LandscapeClassifier.Model.Classification
{
    public class ClassifiedFeatureVector
    {
        public LandcoverType Type { get; set; }
        public FeatureVector FeatureVector { get; set; }

        public ClassifiedFeatureVector(LandcoverType type, FeatureVector featureVector)
        {
            Type = type;
            FeatureVector = featureVector;
        }
    }
}
