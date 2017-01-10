using System;
using System.Threading.Tasks;
using Accord.MachineLearning;
using Accord.MachineLearning.Bayes;
using Accord.Statistics.Analysis;
using Accord.Statistics.Distributions.Fitting;
using Accord.Statistics.Distributions.Univariate;

namespace LandscapeClassifier.Model.Classification.Algorithms
{
    public class BayesClassifier : AbstractLandCoverClassifier
    {
        private NaiveBayes<NormalDistribution> _bayes;

        public override Task TrainAsync(ClassificationModel classificationModel)
        {
            int numFeatures = classificationModel.ClassifiedFeatureVectors.Count;

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

            NaiveBayesLearning<NormalDistribution> learning = new NaiveBayesLearning<NormalDistribution>();

            return Task.Factory.StartNew(() =>
            {
                learning.Options.InnerOption = new NormalOptions() { Regularization = 1e-5 };

                _bayes = learning.Learn(input, responses);
            });

        }

        public override LandcoverType Predict(FeatureVector feature)
        {
            return (LandcoverType)_bayes.Decide(Array.ConvertAll(feature.BandIntensities, s => (double)s / ushort.MaxValue));
        }
        public override double Probabilty(FeatureVector feature)
        {
            return _bayes.Probability(Array.ConvertAll(feature.BandIntensities, s => (double)s / ushort.MaxValue));
        }

        public override double Probabilty(FeatureVector feature, int classIndex)
        {
            return _bayes.Probability(Array.ConvertAll(feature.BandIntensities, s => (double)s / ushort.MaxValue), classIndex);
        }

        public override int[] Predict(double[][] features)
        {
            return new int[0];
        }

        public override double[] Probability(double[][] features)
        {
            return new double[0];
        }

        public override double[][] Probabilities(double[][] features)
        {
            return new double[0][];
        }

        public override Task<GridSearchParameterCollection> GridSearchAsync(ClassificationModel classificationModel)
        {
            throw new NotImplementedException();
        }

        public override Task<GeneralConfusionMatrix> ComputeConfusionMatrixAsync(ClassificationModel classificationModel)
        {
            throw new NotImplementedException();
        }
    }
}
