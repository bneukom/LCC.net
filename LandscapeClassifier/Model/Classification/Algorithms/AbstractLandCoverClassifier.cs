using System.Threading;
using System.Threading.Tasks;

namespace LandscapeClassifier.Model.Classification.Algorithms
{
    public abstract class AbstractLandCoverClassifier : ILandCoverClassifier
    {
        protected CancellationTokenSource CancellationTokenSource;

        public abstract Task Train(ClassificationModel classificationModel);
        public abstract LandcoverType Predict(FeatureVector feature);
        public abstract double Probabilty(FeatureVector feature);
        public abstract double Probabilty(FeatureVector feature, int classIndex);
        public abstract int[] Predict(double[][] features);
        public abstract double[] Probability(double[][] features);
        public abstract double[][] Probabilities(double[][] features);

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
