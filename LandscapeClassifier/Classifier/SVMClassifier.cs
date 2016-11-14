using System;
using System.Linq;
using System.Windows.Media.Imaging;
using Accord;
using Accord.MachineLearning.DecisionTrees;
using Accord.MachineLearning.DecisionTrees.Learning;
using Accord.MachineLearning.VectorMachines;
using Accord.MachineLearning.VectorMachines.Learning;
using Accord.Statistics.Kernels;
using LandscapeClassifier.Model;
using LandscapeClassifier.Model.Classification;

namespace LandscapeClassifier.Classifier
{
    public class SVMClassifier : ILandCoverClassifier
    {
        private MulticlassSupportVectorMachine<Gaussian> _svm;
        private MulticlassSupportVectorLearning<Gaussian> _calibration;

        public void Train(ClassificationModel classificationModel)
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

            // Create a one-vs-one multi-class SVM learning algorithm 
            var teacher = new MulticlassSupportVectorLearning<Gaussian>()
            {
                // using LIBLINEAR's L2-loss SVC dual for each SVM
                Learner = (p) => new SequentialMinimalOptimization<Gaussian>()
                {
                    UseKernelEstimation = true,
                    Tolerance = 5,
                }
            };

            _svm = teacher.Learn(input, responses);

            /*
            // Create the multi-class learning algorithm for the machine
            _calibration = new MulticlassSupportVectorLearning<Gaussian>()
            {
                Model = _svm, // We will start with an existing machine

                // Configure the learning algorithm to use SMO to train the
                //  underlying SVMs in each of the binary class subproblems.
                Learner = (param) => new SequentialMinimalOptimization<Gaussian>()
                {
                    Model = param.Model // Start with an existing machine
                }
            };

            _calibration.Learn(input, responses);
            */
            
        }

        public LandcoverType Predict(FeatureVector feature)
        {
            var features = Array.ConvertAll(feature.BandIntensities, s => (double) s/ushort.MaxValue);
           
            return (LandcoverType) _svm.Decide(features);
        }

        public double PredictionProbabilty(FeatureVector feature)
        {
            var features = Array.ConvertAll(feature.BandIntensities, s => (double)s / ushort.MaxValue);
            return _svm.Score(features);
        }

        public int[] Predict(double[][] features)
        {
            return _svm.Decide(features);
        }
    }
}
