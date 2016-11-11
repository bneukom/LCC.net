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
    class SVMClassifier : ILandCoverClassifier
    {
        private MulticlassSupportVectorMachine<Linear> svm;

        public void Train(ClassificationModel classificationModel)
        {
            int numFeatures = classificationModel.ClassifiedFeatureVectors.Count;
            int numClasses = Enum.GetValues(typeof(LandcoverType)).Length;
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

            // Create a one-vs-one multi-class SVM learning algorithm 
            var teacher = new MulticlassSupportVectorLearning<Linear>()
            {
                // using LIBLINEAR's L2-loss SVC dual for each SVM
                Learner = (p) => new LinearDualCoordinateDescent()
                {
                    Loss = Loss.L2
                }
            };


            svm = teacher.Learn(input, responses);

            Console.WriteLine();


        }

        public LandcoverType Predict(FeatureVector feature)
        {
            return (LandcoverType) svm.Decide(Array.ConvertAll(feature.BandIntensities, s => (double)s / ushort.MaxValue));
        }

        public BitmapSource Predict(FeatureVector[,] features)
        {
            throw new NotImplementedException();
        }
    }
}
