using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Accord;
using Accord.MachineLearning.DecisionTrees;
using Accord.MachineLearning.DecisionTrees.Learning;
using LandscapeClassifier.Model;
using LandscapeClassifier.Model.Classification;
using LandscapeClassifier.Model.Classification.Options;

namespace LandscapeClassifier.Classifier
{
    public class DecisionTreeClassifier : AbstractLandCoverClassifier<DecisionTreeOptions>
    {
        private C45Learning _id3Learning;
        private DecisionTree _tree;

        public override Task Train(ClassificationModel<DecisionTreeOptions> classificationModel)
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

        public override double PredictionProbabilty(FeatureVector feature)
        {
            return 0.0;
        }

        public override int[] Predict(double[][] features)
        {
            return _tree.Decide(features);
        }
    }
}
