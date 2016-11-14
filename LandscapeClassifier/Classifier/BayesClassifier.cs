using System;
using System.Linq;
using System.Windows.Media.Imaging;
using Accord;
using Accord.MachineLearning.Bayes;
using Accord.MachineLearning.DecisionTrees;
using Accord.MachineLearning.DecisionTrees.Learning;
using Accord.Statistics.Distributions.Univariate;
using LandscapeClassifier.Model;
using LandscapeClassifier.Model.Classification;

namespace LandscapeClassifier.Classifier
{
    public class BayesClassifier : ILandCoverClassifier
    {
        private NaiveBayesLearning _learning;
        private NaiveBayes _bayes;

        public void Train(ClassificationModel classificationModel)
        {
            int numFeatures = classificationModel.ClassifiedFeatureVectors.Count;
            int numClasses = Enum.GetValues(typeof(LandcoverType)).Length;

            int[][] input = new int[numFeatures][];
            int[] responses = new int[numFeatures];

            int[] symbols = new int[numFeatures];
            for (int i = 0; i < symbols.Length; ++i) symbols[i] = ushort.MaxValue;

            _bayes = new NaiveBayes(numClasses, symbols);

            for (int featureIndex = 0;
                featureIndex < classificationModel.ClassifiedFeatureVectors.Count;
                ++featureIndex)
            {
                var featureVector = classificationModel.ClassifiedFeatureVectors[featureIndex];
                input[featureIndex] = Array.ConvertAll(featureVector.FeatureVector.BandIntensities, s => (int) s);
                responses[featureIndex] = (int) featureVector.Type;
            }

            _learning = new NaiveBayesLearning()
            {
                Model = _bayes,
            };

            _learning.Options.InnerOption.UseLaplaceRule = true;


            _learning.Learn(input, responses);

        }

        public LandcoverType Predict(FeatureVector feature)
        {
            return (LandcoverType)_bayes.Decide(Array.ConvertAll(feature.BandIntensities, s => (int)s));
        }

        public double PredictionProbabilty(FeatureVector feature)
        {
            throw new NotImplementedException();
        }

        public int[] Predict(double[][] features)
        {
            throw new NotImplementedException();
        }
    }
}
