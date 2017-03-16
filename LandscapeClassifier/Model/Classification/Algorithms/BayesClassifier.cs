using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Accord.MachineLearning;
using Accord.MachineLearning.Bayes;
using Accord.MachineLearning.VectorMachines.Learning;
using Accord.Math;
using Accord.Math.Optimization.Losses;
using Accord.Statistics.Analysis;
using Accord.Statistics.Distributions.Fitting;
using Accord.Statistics.Distributions.Univariate;
using Accord.Statistics.Kernels;

namespace LandscapeClassifier.Model.Classification.Algorithms
{
    public class BayesClassifier : AbstractLandCoverClassifier
    {
        private NaiveBayes<NormalDistribution> _bayes;

        public override Task TrainAsync(ClassificationModel classificationModel)
        {
            int numFeatures = classificationModel.FeatureVectors.Count;

            double[][] input = new double[numFeatures][];
            int[] responses = new int[numFeatures];

            for (int featureIndex = 0;
                featureIndex < classificationModel.FeatureVectors.Count;
                ++featureIndex)
            {
                var featureVector = classificationModel.FeatureVectors[featureIndex];
                input[featureIndex] = Array.ConvertAll(featureVector.FeatureVector.BandIntensities, s => (double)s / ushort.MaxValue);
                responses[featureIndex] = (int) featureVector.FeatureClass;
            }

            NaiveBayesLearning<NormalDistribution> learning = new NaiveBayesLearning<NormalDistribution>();

            return Task.Factory.StartNew(() =>
            {
                learning.Options.InnerOption = new NormalOptions() { Regularization = 1e-5 };

                _bayes = learning.Learn(input, responses);
            });

        }

        public override int Predict(FeatureVector feature)
        {
            return _bayes.Decide(Array.ConvertAll(feature.BandIntensities, s => (double)s / ushort.MaxValue));
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

        public override Task<List<GeneralConfusionMatrix>> ComputeFoldedConfusionMatrixAsync(ClassificationModel classificationModel, int folds)
        {
            return Task.Factory.StartNew(() =>
            {
                int numFeatures = classificationModel.FeatureVectors.Count;

                double[][] input = new double[numFeatures][];
                int[] responses = new int[numFeatures];

                for (int featureIndex = 0; featureIndex < classificationModel.FeatureVectors.Count; ++featureIndex)
                {
                    var featureVector = classificationModel.FeatureVectors[featureIndex];

                    input[featureIndex] = Array.ConvertAll(featureVector.FeatureVector.BandIntensities, s => (double)s / ushort.MaxValue);
                    responses[featureIndex] = featureVector.FeatureClass;
                }

                List<GeneralConfusionMatrix> confusionMatrices = new List<GeneralConfusionMatrix>();

                // Create a new Cross-validation algorithm passing the data set size and the number of folds
                var crossvalidation = new CrossValidation(input.Length, folds);

                crossvalidation.Fitting = delegate (int k, int[] indicesTrain, int[] indicesValidation)
                {
                    // Lets now grab the training data:
                    var trainingInputs = input.Get(indicesTrain);
                    var trainingOutputs = responses.Get(indicesTrain);

                    // And now the validation data:
                    var validationInputs = input.Get(indicesValidation);
                    var validationOutputs = responses.Get(indicesValidation);

                    NaiveBayesLearning<NormalDistribution> learning = new NaiveBayesLearning<NormalDistribution>();
                    var bayes = learning.Learn(trainingInputs, trainingOutputs);

                    var predictedTraining = bayes.Decide(trainingInputs);
                    var predictedValidation = bayes.Decide(validationInputs);

                    double trainingError = new ZeroOneLoss(trainingOutputs).Loss(predictedTraining);
                    double validationError = new ZeroOneLoss(validationOutputs).Loss(predictedValidation);

                    GeneralConfusionMatrix confusionMatrix = new GeneralConfusionMatrix(Enum.GetValues(typeof(LandcoverTypeViewModel)).Length - 1, validationOutputs, predictedValidation);
                    confusionMatrices.Add(confusionMatrix);

                    // Return a new information structure containing the model and the errors achieved.
                    return new CrossValidationValues(trainingError, validationError);
                };

                crossvalidation.Compute();

                return confusionMatrices;
            });
        }
    }
}
