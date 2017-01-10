using System.Threading.Tasks;
using Accord.MachineLearning;
using LandscapeClassifier.Model;
using LandscapeClassifier.Model.Classification;
using LandscapeClassifier.Model.Classification.Algorithms;
using LandscapeClassifier.ViewModel.MainWindow.Classification.Algorithms.Attributes;

namespace LandscapeClassifier.ViewModel.MainWindow.Classification.Algorithms
{
    public class BayesClassifierViewModel : ClassifierViewModelBase
    {
        protected override ILandCoverClassifier Classifier { get; } = new BayesClassifier();

        public override bool PropertyAffectsOptions(string propertyName)
        {
            return false;
        }

        public override void GridSearchAsync(ClassificationModel model)
        {
        }
    }
}