using System;
using LandscapeClassifier.ViewModel.MainWindow.Classification.Algorithms;

namespace LandscapeClassifier.Model.Classification.Algorithms
{
    public static class ClassifierExtensions
    {
        public static ClassifierViewModelBase CreateClassifierViewModel(this Classifier classifier)
        {
            switch (classifier)
            {
                case Classifier.DecisionTrees:
                    return new DecisionTreeClassierViewModel();
                case Classifier.Bayes:
                    return null;
                case Classifier.SVM:
                    return new SvmClassifierViewModel();
                default:
                    throw new ArgumentOutOfRangeException(nameof(classifier), classifier, null);
            }
        }
    }
}