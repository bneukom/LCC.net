using System.Threading.Tasks;

namespace LandscapeClassifier.Model.Classification.Algorithms
{
    public interface ILandCoverClassifier
    {
        /// <summary>
        /// Trains the classifier with the given already classified features.
        /// </summary>
        Task Train(ClassificationModel classificationModel);

        /// <summary>
        /// Predicts the land cover type for the given feature vector. <see cref="Train"/> must be called first.
        /// </summary>
        /// <param name="feature"></param>
        /// <returns></returns>
        LandcoverType Predict(FeatureVector feature);

        /// <summary>
        /// Returns the probabilty for the given feature vector prediction.
        /// </summary>
        /// <param name="feature"></param>
        /// <returns></returns>
        double PredictionProbabilty(FeatureVector feature);

        /// <summary>
        /// Predicts all land cover types for the given feature vectors. <see cref="Train"/> must be called first.
        /// </summary>
        /// <param name="features"></param>
        /// <returns></returns>
        int[] Predict(double[][] features);

        /// <summary>
        /// Cancels the classification process.
        /// </summary>
        void Cancel();

    }
}
