using System.Threading.Tasks;
using Accord.MachineLearning;
using LandscapeClassifier.Model;
using LandscapeClassifier.Model.Classification;
using LandscapeClassifier.Model.Classification.Algorithms;
using LandscapeClassifier.ViewModel.MainWindow.Classification.Algorithms.Attributes;

namespace LandscapeClassifier.ViewModel.MainWindow.Classification.Algorithms
{
    public class DecisionTreeClassierViewModel : ClassifierViewModelBase
    {
        private readonly DecisionTreeClassifier _classifier = new DecisionTreeClassifier();

        protected override ILandCoverClassifier Classifier => _classifier;

        [Option]
        public bool Boosting
        {
            get { return _classifier.Boosting; }
            set { _classifier.Boosting = value; RaisePropertyChanged(); }
        }

        [Option]
        [VisibleWhen(nameof(Boosting), true)]
        [BiggerThan(0)]
        public int Iterations
        {
            get { return _classifier.Iterations; }
            set { _classifier.Iterations = value; RaisePropertyChanged(); }
        }

        public override bool PropertyAffectsOptions(string propertyName)
        {
            return propertyName == nameof(Boosting);
        }

        public override void GridSearchAsync(ClassificationModel model)
        {
            throw new System.NotImplementedException();
        }

        
    }
}