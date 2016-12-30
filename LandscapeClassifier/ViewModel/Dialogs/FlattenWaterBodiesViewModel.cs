using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using GalaSoft.MvvmLight;
using LandscapeClassifier.ViewModel.MainWindow.Classification;

namespace LandscapeClassifier.ViewModel.Dialogs
{
    public class FlattenWaterBodiesViewModel : ViewModelBase
    {
        private string _digitalElevationModelPath;
        private string _waterMapPath;
        private double _minWaterArea;

        public string DigitalElevationModelPath
        {
            get { return _digitalElevationModelPath; }
            set { _digitalElevationModelPath = value; RaisePropertyChanged(); }
        }

        public string WaterMapPath
        {
            get { return _waterMapPath; }
            set { _waterMapPath = value; RaisePropertyChanged(); }
        }

        public double MinWaterArea
        {
            get { return _minWaterArea; }
            set { _minWaterArea = value; RaisePropertyChanged(); }
        }

        public ICommand BrowseDigitalElevationModelCommand { get; set; }

        public ICommand BrowseWaterMapPathCommand { get; set; }
    }
}
