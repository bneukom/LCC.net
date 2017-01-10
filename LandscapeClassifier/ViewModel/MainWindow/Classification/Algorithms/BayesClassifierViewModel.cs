using LandscapeClassifier.Model.Classification.Algorithms;
using LandscapeClassifier.ViewModel.MainWindow.Classification.Algorithms.Attributes;

namespace LandscapeClassifier.ViewModel.MainWindow.Classification.Algorithms
{
    public class BayesClassifierViewModel : ClassifierViewModelBase
    {
        private readonly BayesClassifier _classifier = new BayesClassifier();

        public override ILandCoverClassifier Classifier => _classifier;

        public override bool PropertyAffectsOptions(string propertyName)
        {
            return false;
        }

    }
}