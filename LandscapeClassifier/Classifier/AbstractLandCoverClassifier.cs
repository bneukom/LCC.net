using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LandscapeClassifier.Model;
using LandscapeClassifier.Model.Classification;

namespace LandscapeClassifier.Classifier
{
    public abstract class AbstractLandCoverClassifier<T> : ILandCoverClassifier<T>
    {
        protected CancellationTokenSource CancellationTokenSource;

        public abstract Task Train(ClassificationModel<T> classificationModel);
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
