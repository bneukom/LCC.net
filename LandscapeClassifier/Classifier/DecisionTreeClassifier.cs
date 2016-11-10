using System;
using System.Linq;
using System.Windows.Media.Imaging;
using Accord;
using Accord.MachineLearning.DecisionTrees;
using Accord.MachineLearning.DecisionTrees.Learning;
using LandscapeClassifier.Model;
using LandscapeClassifier.Model.Classification;

namespace LandscapeClassifier.Classifier
{
    class DecisionTreeClassifier : ILandCoverClassifier
    {
        private ID3Learning _id3Learning;
        private DecisionTree _tree;

        public void Train(ClassificationModel classificationModel)
        {
            int numFeatures = classificationModel.ClassifiedFeatureVectors.Count;
            DecisionVariable[]  decisionVariables = classificationModel.Bands.Select(
                b => new DecisionVariable(b.ToString(), new IntRange(0, ushort.MaxValue))).ToArray();

            int[][] input = new int[numFeatures][];
            int[] responses = new int[numFeatures];

            for (int featureIndex = 0;
                featureIndex < classificationModel.ClassifiedFeatureVectors.Count;
                ++featureIndex)
            {
                var featureVector = classificationModel.ClassifiedFeatureVectors[featureIndex];
                input[featureIndex] = Array.ConvertAll(featureVector.FeatureVector.BandIntensities, s => (int) s);
                responses[featureIndex] = (int) featureVector.Type;
            }

            _tree = new DecisionTree(decisionVariables, Enum.GetValues(typeof(LandcoverType)).Length);

            _id3Learning = new ID3Learning(_tree);
            _id3Learning.Learn(input, responses);
            
        }

        public LandcoverType Predict(FeatureVector feature)
        {
            return (LandcoverType) _tree.Decide(Array.ConvertAll(feature.BandIntensities, s => (int)s));
        }

        public BitmapSource Predict(FeatureVector[,] features)
        {
            throw new NotImplementedException();
        }
    }
}
