using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.MachineLearning;
using Accord.MachineLearning.VectorMachines;
using Accord.MachineLearning.VectorMachines.Learning;
using Accord.Math;
using Accord.Math.Optimization.Losses;
using Accord.Statistics.Kernels;
using LandscapeClassifier.Util;

namespace LandscapeClassifier.Model.Classification.Algorithms
{
    public class SvmClassifier : AbstractLandCoverClassifier
    {
        private IEither<MulticlassSupportVectorMachine<Linear>, MulticlassSupportVectorMachine<Gaussian>> _svm;

        public double Complexity { get; set; } = 100;
        public double Gamma { get; set; } = 1;
        public Kernel Kernel { get; set; } = Kernel.Linear;

        public override Task Train(ClassificationModel classificationModel)
        {
            int numFeatures = classificationModel.ClassifiedFeatureVectors.Count;

            double[][] input = new double[numFeatures][];
            int[] responses = new int[numFeatures];

            for (int featureIndex = 0; featureIndex < classificationModel.ClassifiedFeatureVectors.Count; ++featureIndex)
            {
                var featureVector = classificationModel.ClassifiedFeatureVectors[featureIndex];

                input[featureIndex] = Array.ConvertAll(featureVector.FeatureVector.BandIntensities, s => (double)s / ushort.MaxValue);
                responses[featureIndex] = (int)featureVector.Type;
            }

            switch (Kernel)
            {
                case Kernel.Linear:
                    var linearLearning = new MulticlassSupportVectorLearning<Linear>
                    {
                        Learner = (p) => new LinearDualCoordinateDescent<Linear>()
                        {
                            Complexity = Complexity,
                            UseComplexityHeuristic = false,
                            Token = CancellationTokenSource.Token,
                        }
                    };

                    return Task.Factory.StartNew(() =>
                    {
                        _svm = Either.Left<MulticlassSupportVectorMachine<Linear>, MulticlassSupportVectorMachine<Gaussian>>(linearLearning.Learn(input, responses));
                    });

                case Kernel.Gaussian:
                    var gaussianLearning = new MulticlassSupportVectorLearning<Gaussian>
                    {
                        Kernel = Gaussian.FromGamma(Gamma),
                        Learner = (p) => new SequentialMinimalOptimization<Gaussian>()
                        {
                            Complexity = Complexity,
                            UseComplexityHeuristic = false,
                            UseKernelEstimation = false,
                            Token = CancellationTokenSource.Token,
                        }
                    };

                    return Task.Factory.StartNew(() =>
                    {
                        _svm = Either.Right<MulticlassSupportVectorMachine<Linear>, MulticlassSupportVectorMachine<Gaussian>>(gaussianLearning.Learn(input, responses));
                    });
                default:
                    throw new InvalidOperationException();
            }
        }

        public override LandcoverType Predict(FeatureVector feature)
        {
            var features = Array.ConvertAll(feature.BandIntensities, s => (double)s / ushort.MaxValue);

            return _svm.Case(l => (LandcoverType)l.Decide(features), r => (LandcoverType)r.Decide(features));
        }

        public override double Probabilty(FeatureVector feature)
        {
            var features = Array.ConvertAll(feature.BandIntensities, s => (double)s / ushort.MaxValue);
            return _svm.Case(l => l.Probability(features), r => r.Probability(features));
        }

        public override double Probabilty(FeatureVector feature, int classIndex)
        {
            var features = Array.ConvertAll(feature.BandIntensities, s => (double)s / ushort.MaxValue);
            return _svm.Case(l => l.Probability(features, classIndex), r => r.Probability(features, classIndex));
        }

        public override int[] Predict(double[][] features)
        {
            return _svm.Case(l => l.Decide(features), r => r.Decide(features));
        }

        public override double[] Probability(double[][] features)
        {
            return _svm.Case(l => l.Probability(features), r => r.Probability(features));
        }

        public override double[][] Probabilities(double[][] features)
        {
            return _svm.Case(l => l.Probabilities(features), r => r.Probabilities(features));
        }

        public override Task GridSearchAsync(ClassificationModel classificationModel)
        {
            return Task.Factory.StartNew(() =>
            {
                // Declare the parameters and ranges to be searched
                GridSearchRange[] ranges =
                {
                    new GridSearchRange("complexity", new[] {100.0,50,10}),
                    new GridSearchRange("gamma", new[] {1.0, 2.0, 5.0, 10.0, 20.0}),
                };

                int numFeatures = classificationModel.ClassifiedFeatureVectors.Count;

                double[][] input = new double[numFeatures][];
                int[] responses = new int[numFeatures];

                for (int featureIndex = 0; featureIndex < classificationModel.ClassifiedFeatureVectors.Count; ++featureIndex)
                {
                    var featureVector = classificationModel.ClassifiedFeatureVectors[featureIndex];

                    input[featureIndex] = Array.ConvertAll(featureVector.FeatureVector.BandIntensities, s => (double) s/ushort.MaxValue);
                    responses[featureIndex] = (int) featureVector.Type;
                }

                // Instantiate a new Grid Search algorithm for Kernel Support Vector Machines
                var gridsearch = new GridSearch<MulticlassSupportVectorMachine<Gaussian>>(ranges);

                // Set the fitting function for the algorithm
                gridsearch.Fitting = delegate(GridSearchParameterCollection parameters, out double error)
                {
                    // The parameters to be tried will be passed as a function parameter.
                    double complexity = parameters["complexity"].Value;
                    double gamma = parameters["gamma"].Value;

                    // Create a new Cross-validation algorithm passing the data set size and the number of folds
                    var crossvalidation = new CrossValidation(size: input.Length, folds: 3);

                    // Define a fitting function using Support Vector Machines. The objective of this
                    // function is to learn a SVM in the subset of the data indicated by cross-validation.
                    crossvalidation.Fitting = delegate(int k, int[] indicesTrain, int[] indicesValidation)
                    {
                        // The fitting function is passing the indices of the original set which
                        // should be considered training data and the indices of the original set
                        // which should be considered validation data.

                        // Lets now grab the training data:
                        var trainingInputs = input.Get(indicesTrain);
                        var trainingOutputs = responses.Get(indicesTrain);

                        // And now the validation data:
                        var validationInputs = input.Get(indicesValidation);
                        var validationOutputs = responses.Get(indicesValidation);

                        var gaussianLearningKfold = new MulticlassSupportVectorLearning<Gaussian>
                        {
                            Kernel = Gaussian.FromGamma(gamma),
                            Learner = (p) => new SequentialMinimalOptimization<Gaussian>()
                            {
                                UseKernelEstimation = false,
                                UseComplexityHeuristic = false,
                                Complexity = complexity,
                                Token = CancellationTokenSource.Token,
                                Tolerance = 0.01,
                            }
                        };

                        var svmKfold = gaussianLearningKfold.Learn(trainingInputs, trainingOutputs);

                        double trainingError = new ZeroOneLoss(trainingOutputs).Loss(svmKfold.Decide(trainingInputs));
                        double validationError = new ZeroOneLoss(validationOutputs).Loss(svmKfold.Decide(validationInputs));

                        // Return a new information structure containing the model and the errors achieved.
                        return new CrossValidationValues(svmKfold, trainingError, validationError);
                    };

                    // Compute the cross-validation
                    var result = crossvalidation.Compute();

                    // Finally, access the measured performance.
                    double trainingErrors = result.Training.Mean;
                    double validationErrors = result.Validation.Mean;

                    error = validationErrors;

                    return null; // Return the current model
                };


                // Declare some out variables to pass to the grid search algorithm
                GridSearchParameterCollection bestParameters;
                double minError;

                // Compute the grid search to find the best Support Vector Machine
                var bestModel = gridsearch.Compute(out bestParameters, out minError);
            });
        }
    }
}
