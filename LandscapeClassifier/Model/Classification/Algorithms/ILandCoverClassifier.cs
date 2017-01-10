using System.Threading.Tasks;
using Accord.MachineLearning;
using Accord.Statistics.Analysis;

namespace LandscapeClassifier.Model.Classification.Algorithms
{
    public interface ILandCoverClassifier
    {
        /// <summary>
        /// Trains the classifier with the given already classified features.
        /// </summary>
        Task TrainAsync(ClassificationModel classificationModel);

        /// <summary>
        /// Predicts the land cover type for the given feature vector. <see cref="TrainAsync"/> must be called first.
        /// </summary>
        /// <param name="feature"></param>
        /// <returns></returns>
        LandcoverType Predict(FeatureVector feature);

        /// <summary>
        /// Returns the probabilty for the given feature vector prediction.
        /// </summary>
        /// <param name="feature"></param>
        /// <returns></returns>
        double Probabilty(FeatureVector feature);

        /// <summary>
        /// Returns the probabilty for the given feature vector prediction.
        /// </summary>
        /// <param name="feature"></param>
        /// <param name="classIndex"></param>
        /// <returns></returns>
        double Probabilty(FeatureVector feature, int classIndex);

        /// <summary>
        /// Predicts all land cover types for the given feature vectors. <see cref="TrainAsync"/> must be called first.
        /// </summary>
        /// <param name="features"></param>
        /// <returns></returns>
        int[] Predict(double[][] features);

        /// <summary>
        /// Returns the probabilty of each feature to belong to its predicted class.
        /// </summary>
        /// <param name="features"></param>
        /// <returns></returns>
        double[] Probability(double[][] features);

        /// <summary>
        /// Returns the probability for the given features to belong to the given classIndex.
        /// </summary>
        /// <param name="features"></param>
        /// <returns></returns>
        double[][] Probabilities(double[][] features);

        /// <summary>
        /// Grid-search with cross-validation the parameters for the classifier.
        /// </summary>
        /// <param name="classificationModel"></param>
        Task<GridSearchParameterCollection> GridSearchAsync(ClassificationModel classificationModel);

        /// <summary>
        /// Computes the confusion matrix using 2-fold cross validation.
        /// </summary>
        /// <param name="classificationModel"></param>
        /// <returns></returns>
        Task<GeneralConfusionMatrix> ComputeConfusionMatrixAsync(ClassificationModel classificationModel);

        /// <summary>
        /// Cancels the classification process.
        /// </summary>
        void Cancel();

    }
}
