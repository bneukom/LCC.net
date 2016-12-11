using System;
using System.Linq;
using System.Threading.Tasks;
using Accord.MachineLearning.DecisionTrees;
using Accord.MachineLearning.DecisionTrees.Learning;

namespace LandscapeClassifier.Model.Classification.Algorithms
{
    public class DecisionTreeClassifier : AbstractLandCoverClassifier
    {
        private C45Learning _id3Learning;
        private DecisionTree _tree;

        public override Task Train(ClassificationModel classificationModel)
        {
            int numFeatures = classificationModel.ClassifiedFeatureVectors.Count;
            DecisionVariable[]  decisionVariables = classificationModel.Bands.Select(b => DecisionVariable.Continuous(b.ToString())).ToArray();

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

            return Task.Factory.StartNew(() =>
            {
                _tree = new DecisionTree(decisionVariables, Enum.GetValues(typeof(LandcoverType)).Length);
                _id3Learning = new C45Learning(_tree);
                _id3Learning.Learn(input, responses);
            });
        }

        public override LandcoverType Predict(FeatureVector feature)
        {
            return (LandcoverType) _tree.Decide(Array.ConvertAll(feature.BandIntensities, s => (double)s / ushort.MaxValue));
        }

        public override double Probabilty(FeatureVector feature)
        {
            return 0.0;
        }

        public override double Probabilty(FeatureVector feature, int classIndex)
        {
            throw new NotImplementedException();
        }

        public override int[] Predict(double[][] features)
        {
            return _tree.Decide(features);
        }

        public override double[] Probability(double[][] features)
        {
            throw new NotImplementedException();
        }

        public override double[][] Probabilities(double[][] features)
        {
            throw new NotImplementedException();
        }
    }
}
