using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using LandscapeClassifier.Model;
using LandscapeClassifier.ViewModel.MainWindow.Classification;

namespace LandscapeClassifier.ViewModel.Dialogs
{
    public class EditLandcoverTypesDialogViewModel : ViewModelBase
    {
        private LandcoverTypeViewModel _selectedLandcoverTypeViewModel;
        private ObservableCollection<LandcoverTypeViewModel> _landcoverTypes = new ObservableCollection<LandcoverTypeViewModel>();

        public ObservableCollection<LandcoverTypeViewModel> LandcoverTypes
        {
            get { return _landcoverTypes; }
            set { _landcoverTypes = value; RaisePropertyChanged(); }
        }

        public LandcoverTypeViewModel SelectedLandcoverTypeViewModel
        {
            get { return _selectedLandcoverTypeViewModel; }
            set { _selectedLandcoverTypeViewModel = value; RaisePropertyChanged(); }
        }

        public ICommand AddCommand { get; }
        public ICommand RemoveCommand { get; }

        public EditLandcoverTypesDialogViewModel()
        {
            AddCommand = new RelayCommand(AddLandCoverType, () => true);
            RemoveCommand = new RelayCommand(RemoveLandCoverType, () => SelectedLandcoverTypeViewModel != null);
        }

        private void RemoveLandCoverType()
        {
            LandcoverTypes.Remove(SelectedLandcoverTypeViewModel);
            SelectedLandcoverTypeViewModel = null;
        }

        private void AddLandCoverType()
        {
            var unused = Enumerable.Range(0, _landcoverTypes.Count).Except(_landcoverTypes.Select(t => t.Id)).ToList();
            int newId = unused.Count > 0 ? unused.First() : _landcoverTypes.Count;
            LandcoverTypes.Add(new LandcoverTypeViewModel(newId, "LandCoverType", Colors.Black));
        }

        public void Reset()
        {
            LandcoverTypes.Clear();
        }
    }
}
