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
    public class DecisionTreeClassifier : ILandCoverClassifier
    {
        private C45Learning _id3Learning;
        private DecisionTree _tree;

        public void Train(ClassificationModel classificationModel)
        {
            int numFeatures = classificationModel.ClassifiedFeatureVectors.Count;
            DecisionVariable[]  decisionVariables = classificationModel.Bands.Select(
                b => DecisionVariable.Continuous(b.ToString())).ToArray();

            double[][] input = new double[numFeatures][];
            int[] responses = new int[numFeatures];
            
            for (int featureIndex = 0;
                featureIndex < classificationModel.ClassifiedFeatureVectors.Count;
                ++featureIndex)
            {
                var featureVector = classificationModel.ClassifiedFeatureVectors[featureIndex];
                input[featureIndex] = Array.ConvertAll(featureVector.FeatureVector.BandIntensities, s => (double)s / ushort.MaxValue);
                responses[featureIndex] = (int) featureVector.Type;
            }

            _tree = new DecisionTree(decisionVariables, Enum.GetValues(typeof(LandcoverType)).Length);
            _id3Learning = new C45Learning(_tree);
            _id3Learning.SplitStep = 1;
            _id3Learning.Learn(input, responses);

        }

        public LandcoverType Predict(FeatureVector feature)
        {
            return (LandcoverType) _tree.Decide(Array.ConvertAll(feature.BandIntensities, s => (double)s / ushort.MaxValue));
        }

        public double PredictionProbabilty(FeatureVector feature)
        {
            return 0.0;
        }

        public int[] Predict(double[][] features)
        {
            return _tree.Decide(features);
        }
    }
}
