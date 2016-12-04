using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GalaSoft.MvvmLight;
using LandscapeClassifier.ViewModel.MainWindow.Classification;

namespace LandscapeClassifier.ViewModel.Dialogs
{
    public class BrowseLayerDialogViewModel : ViewModelBase
    {
        private LayerViewModel _selectedLayer;
        private bool _isLayerSelected;
        public ObservableCollection<LayerViewModel> Layers { get; set; } = new ObservableCollection<LayerViewModel>();

        public LayerViewModel SelectedLayer
        {
            get { return _selectedLayer; }
            set
            {
                _selectedLayer = value;
                IsLayerSelected = value != null;
                RaisePropertyChanged();
            }
        }

        public bool IsLayerSelected
        {
            get { return _isLayerSelected; }
            set { _isLayerSelected = value; RaisePropertyChanged(); }
        }


        public void Reset()
        {
            Layers.Clear();
            SelectedLayer = null;
        }
    }


}
