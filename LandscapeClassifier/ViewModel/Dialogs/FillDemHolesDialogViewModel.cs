using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using LandscapeClassifier.Util;
using LandscapeClassifier.ViewModel.MainWindow.Classification;
using Microsoft.Win32;

namespace LandscapeClassifier.ViewModel.Dialogs
{
    public class FillDemHolesDialogViewModel : ViewModelBase
    {
        private string _digitalElevationModelPath;
        private double _heightThreshold = 200;
        private bool _isInputSet;

        public string DigitalElevationModelPath
        {
            get { return _digitalElevationModelPath; }
            set { _digitalElevationModelPath = value; RaisePropertyChanged(); }
        }

        public double HeightThreshold
        {
            get { return _heightThreshold; }
            set { _heightThreshold = value; RaisePropertyChanged(); }
        }

        public bool IsInputSet
        {
            get { return _isInputSet; }
            set { _isInputSet = value; RaisePropertyChanged(); }
        }


        public ICommand BrowseDigitalElevationModelPathCommand { get; set; }

        public ICommand FillHolesCommand { get; set; }

        public FillDemHolesDialogViewModel()
        {
            BrowseDigitalElevationModelPathCommand = new RelayCommand(BrowserDigitalElevationModelPath, () => true);
            FillHolesCommand = new RelayCommand(FlattenWaterBodies, () => !string.IsNullOrEmpty(DigitalElevationModelPath));
        }

        private void FlattenWaterBodies()
        {
           
        }



        private void BrowserDigitalElevationModelPath()
        {
            var openFileDialog = new OpenFileDialog()
            {
                Filter = "PNG Files (.png)|*.png",
                FilterIndex = 1,
            };

            var userClickedOk = openFileDialog.ShowDialog();

            // Process input if the user clicked OK.
            if (userClickedOk == true)
            {
                DigitalElevationModelPath = openFileDialog.FileName;
            }
        }

    }

    public enum FillHoleMode
    {
        Linear,
    }
}
