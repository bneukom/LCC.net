using GalaSoft.MvvmLight;
using LandscapeClassifier.Model.Classification.Algorithms;

namespace LandscapeClassifier.ViewModel.MainWindow.Classification.Algorithms
{
    public abstract class ClassifierViewModelBase : ViewModelBase
    {
        public abstract ILandCoverClassifier Classifier { get; }

        public abstract bool PropertyAffectsOptions(string propertyName);
    }
}
