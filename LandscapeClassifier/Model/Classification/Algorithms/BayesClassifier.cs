using System;
using System.Threading.Tasks;
using Accord.MachineLearning;
using Accord.MachineLearning.Bayes;

namespace LandscapeClassifier.Model.Classification.Algorithms
{
    public class BayesClassifier : AbstractLandCoverClassifier
    {
        private NaiveBayesLearning _learning;
        private NaiveBayes _bayes;

        public override Task Train(ClassificationModel classificationModel)
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


            return Task.Factory.StartNew(() =>
            {
                _learning.Options.InnerOption.UseLaplaceRule = true;

                _learning.Learn(input, responses);
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
