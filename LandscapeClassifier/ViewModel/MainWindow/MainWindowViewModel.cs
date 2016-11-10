using System.Windows.Input;
using GalaSoft.MvvmLight;
using LandscapeClassifier.ViewModel.MainWindow.Classification;

namespace LandscapeClassifier.ViewModel.MainWindow
{
    public class MainWindowViewModel : ViewModelBase
    {
        public ClassifierViewModel ClassifierViewModel { get; set; }
        public MenuViewModel MenuViewModel { get; set; }

        public MainWindowViewModel()
        {
            ClassifierViewModel = new ClassifierViewModel();
            MenuViewModel = new MenuViewModel();
        }
    }
}
