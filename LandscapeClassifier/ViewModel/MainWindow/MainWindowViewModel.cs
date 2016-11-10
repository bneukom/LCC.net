using GalaSoft.MvvmLight;
using LandscapeClassifier.ViewModel.BandsCanvas;

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
