using System.Threading;
using System.Threading.Tasks;

namespace LandscapeClassifier.Model.Classification.Algorithms
{
    public abstract class AbstractLandCoverClassifier : ILandCoverClassifier
    {
        protected CancellationTokenSource CancellationTokenSource;

        public abstract Task Train(ClassificationModel classificationModel);
        public abstract LandcoverType Predict(FeatureVector feature);
        public abstract double PredictionProbabilty(FeatureVector feature);
        public abstract int[] Predict(double[][] features);

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
