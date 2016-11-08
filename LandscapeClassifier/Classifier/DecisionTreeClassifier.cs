using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Accord;
using Accord.MachineLearning.DecisionTrees;
using Accord.MachineLearning.DecisionTrees.Learning;
using Accord.Statistics.Filters;
using LandscapeClassifier.Model;
using LandscapeClassifier.Model.Classification;

namespace LandscapeClassifier.Classifier
{
    class DecisionTreeClassifier : ILandCoverClassifier
    {
        ID3Learning id3Learning;

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

            DecisionTree tree = new DecisionTree(decisionVariables, Enum.GetValues(typeof(LandcoverType)).Length);

            id3Learning = new ID3Learning(tree);
            id3Learning.Learn(input, responses);
        }

        public LandcoverType Predict(FeatureVector feature)
        {
            throw new NotImplementedException();
        }

        public BitmapSource Predict(FeatureVector[,] features)
        {
            throw new NotImplementedException();
        }
    }
}
