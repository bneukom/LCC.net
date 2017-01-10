using System;
using System.Threading.Tasks;
using Accord.MachineLearning;
using Accord.MachineLearning.Bayes;

namespace LandscapeClassifier.Model.Classification.Algorithms
{
    public class BayesClassifier : AbstractLandCoverClassifier
    {
        private NaiveBayes _bayes;

        public override Task Train(ClassificationModel classificationModel)
        {
            int numFeatures = classificationModel.ClassifiedFeatureVectors.Count;

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

            NaiveBayesLearning learning = new NaiveBayesLearning();



            return Task.Factory.StartNew(() =>
            {
                learning.Options.InnerOption.UseLaplaceRule = true;

                _bayes = learning.Learn(input, responses);
            });

        }

        public override LandcoverType Predict(FeatureVector feature)
        {
            return (LandcoverType)_bayes.Decide(Array.ConvertAll(feature.BandIntensities, s => (int)s));
        }

        public override double Probabilty(FeatureVector feature)
        {
            throw new NotImplementedException();
        }

        public override double Probabilty(FeatureVector feature, int classIndex)
        {
            throw new NotImplementedException();
        }

        public override int[] Predict(double[][] features)
        {
            throw new NotImplementedException();
        }

        public override double[] Probability(double[][] features)
        {
            throw new NotImplementedException();
        }

        public override double[][] Probabilities(double[][] features)
        {
            throw new NotImplementedException();
        }

        public override Task GridSearchAsync(ClassificationModel classificationModel)
        {
            throw new NotImplementedException();
        }
    }
}
