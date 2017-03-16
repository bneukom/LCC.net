using System.Collections.Generic;
using System.Threading.Tasks;
using Accord.MachineLearning;
using Accord.Statistics.Analysis;
using GalaSoft.MvvmLight;
using LandscapeClassifier.Model;
using LandscapeClassifier.Model.Classification;
using LandscapeClassifier.Model.Classification.Algorithms;

namespace LandscapeClassifier.ViewModel.MainWindow.Classification.Algorithms
{
    public abstract class ClassifierViewModelBase : ViewModelBase
    {
        public abstract bool PropertyAffectsOptions(string propertyName);

        public abstract void GridSearchAsync(ClassificationModel model);

        protected abstract ILandCoverClassifier Classifier { get; }

        public int Predict(FeatureVector featureVector)
        {
            return Classifier.Predict(featureVector);
        }

        public double Probabilty(FeatureVector featureVector, int landCoverClass)
        {
            return Classifier.Probabilty(featureVector, landCoverClass);
        }

        public Task TrainAsync(ClassificationModel classificationModel)
        {
            return Classifier.TrainAsync(classificationModel);
        }

        public int[] Predict(double[][] features)
        {
            return Classifier.Predict(features);
        }

        public double[][] Probabilities(double[][] features)
        {
            return Classifier.Probabilities(features);
        }

        public Task<GeneralConfusionMatrix> ComputeConfusionMatrixAsync(ClassificationModel model)
        {
            return Classifier.ComputeConfusionMatrixAsync(model);
        }

        public Task<List<GeneralConfusionMatrix>> ComputeFoldedConfusionMatrixAsync(ClassificationModel model, int folds)
        {
            return Classifier.ComputeFoldedConfusionMatrixAsync(model, folds);
        }


    }
}
