using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Accord.MachineLearning;
using Accord.Statistics.Analysis;

namespace LandscapeClassifier.Model.Classification.Algorithms
{
    public abstract class AbstractLandCoverClassifier : ILandCoverClassifier
    {
        protected CancellationTokenSource CancellationTokenSource;

        public abstract Task TrainAsync(ClassificationModel classificationModel);
        public abstract LandcoverType Predict(FeatureVector feature);
        public abstract double Probabilty(FeatureVector feature);
        public abstract double Probabilty(FeatureVector feature, int classIndex);
        public abstract int[] Predict(double[][] features);
        public abstract double[] Probability(double[][] features);
        public abstract double[][] Probabilities(double[][] features);
        public abstract Task<GridSearchParameterCollection> GridSearchAsync(ClassificationModel classificationModel);
        public abstract Task<GeneralConfusionMatrix> ComputeConfusionMatrixAsync(ClassificationModel classificationModel);
        public abstract Task<List<GeneralConfusionMatrix>> ComputeFoldedConfusionMatrixAsync(ClassificationModel classificationModel, int folds);

        protected AbstractLandCoverClassifier()
        {
            CancellationTokenSource = new CancellationTokenSource();
        }

        public void Cancel()
        {
            CancellationTokenSource.Cancel();
        }


        
    }
}
