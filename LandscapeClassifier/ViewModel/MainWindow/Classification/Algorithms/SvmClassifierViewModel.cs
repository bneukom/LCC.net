using System.Threading.Tasks;
using Accord.MachineLearning;
using LandscapeClassifier.Model;
using LandscapeClassifier.Model.Classification;
using LandscapeClassifier.Model.Classification.Algorithms;
using LandscapeClassifier.ViewModel.MainWindow.Classification.Algorithms.Attributes;

namespace LandscapeClassifier.ViewModel.MainWindow.Classification.Algorithms
{
    public class SvmClassifierViewModel : ClassifierViewModelBase
    {
        private readonly SvmClassifier _classifier = new SvmClassifier();

        protected override ILandCoverClassifier Classifier => _classifier;

        public override bool PropertyAffectsOptions(string propertyName)
        {
            return propertyName == nameof(Kernel);
        }

        public override async void GridSearchAsync(ClassificationModel model)
        {
            var collection = await Classifier.GridSearchAsync(model);

            if (collection.Contains("complexity")) Complexity = collection["complexity"].Value;
            if (collection.Contains("gamma")) Complexity = collection["gamma"].Value;
        }

        [Option]
        public Kernel Kernel
        {
            get { return _classifier.Kernel; }
            set
            {
                _classifier.Kernel = value;
                RaisePropertyChanged();
            }
        }

        [Option]
        [BiggerThan(0)]
        public double Complexity
        {
            get { return _classifier.Complexity; }
            set
            {
                _classifier.Complexity = value;
                RaisePropertyChanged();
            }
        }

        [Option]
        [VisibleWhen(nameof(Kernel), Kernel.Gaussian)]
        [BiggerThan(0)]
        public double Gamma
        {
            get { return _classifier.Gamma; }
            set
            {
                _classifier.Gamma = value;
                RaisePropertyChanged();
            }
        }

    }
}