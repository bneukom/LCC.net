namespace LandscapeClassifier.Model.Classification
{
    public struct FeatureVector
    {
        public readonly ushort[] BandIntensities;
        public readonly int[] BandIndices;

        public FeatureVector(ushort[] bandIntensities, int[] bandIndices)
        {
            this.BandIntensities = bandIntensities;
            this.BandIndices = bandIndices;
        }

    }
}
