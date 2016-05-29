using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LandscapeClassifier.Model;

namespace LandscapeClassifier.Classifier
{
    public interface ILandCoverClassifier
    {
        /// <summary>
        /// Trains the classifier with the given already classified features.
        /// </summary>
        /// <param name="samples"></param>
        void Train(List<ClassifiedFeatureVector> samples);

        /// <summary>
        /// Predicts the land cover type for the given feature vector. <see cref="Train"/> must be called first.
        /// </summary>
        /// <param name="feature"></param>
        /// <returns></returns>
        LandcoverType Predict(FeatureVector feature);
    }
}
