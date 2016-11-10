namespace LandscapeClassifier.Model.Classification
{
    public struct FeatureVector
    {
        public readonly ushort[] BandIntensities;

        public FeatureVector(ushort[] bandIntensities)
        {
            BandIntensities = bandIntensities;
        }

        public ushort this[int i] => BandIntensities[i];
    }
}