using LandscapeClassifier.Model.Classification.Algorithms;

namespace LandscapeClassifier.ViewModel.MainWindow.Classification.Algorithms
{
    public class SvmClassifierViewModel : ClassifierViewModelBase
    {
        private readonly SvmClassifier _classifier = new SvmClassifier();

        public override ILandCoverClassifier Classifier => _classifier;

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
        public double Complexity
        {
            get { return _classifier.Complexity; }
            set
            {
                _classifier.Complexity = value;
                RaisePropertyChanged();
            }
        }

    }
}