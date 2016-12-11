using System;
using System.Threading.Tasks;
using Accord.MachineLearning.VectorMachines;
using Accord.MachineLearning.VectorMachines.Learning;
using Accord.Statistics.Kernels;
using LandscapeClassifier.Util;

namespace LandscapeClassifier.Model.Classification.Algorithms
{
    public class SvmClassifier : AbstractLandCoverClassifier
    {
        private IEither<MulticlassSupportVectorMachine<Linear>, MulticlassSupportVectorMachine<Gaussian>> _svm;

        public double Complexity { get; set; } = 100;
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
                        Learner = (p) => new SequentialMinimalOptimization<Gaussian>()
                        {
                            Complexity = Complexity,
                            Token = CancellationTokenSource.Token
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
    }
}
