using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using LandscapeClassifier.Annotations;
using LandscapeClassifier.Classifier;
using LandscapeClassifier.Extensions;
using LandscapeClassifier.Model;
using LandscapeClassifier.Model.Classification;
using LandscapeClassifier.Util;
using LandscapeClassifier.View.Open;
using MathNet.Numerics.LinearAlgebra;
using OSGeo.GDAL;
using Application = System.Windows.Application;

namespace LandscapeClassifier.ViewModel.MainWindow.Classification
{
    public class ClassifierViewModel : ViewModelBase
    {
        private Point _mouseScreenPoisition;
        private Point _mouseWorldPoisition;

        private string _trainingStatusText;
        private SolidColorBrush _trainingStatusBrush;
        private bool _isAllPredicted;
        private bool _isTrained;

        private Classifier.Classifier _selectededClassifier = Classifier.Classifier.DecisionTrees;
        private ILandCoverClassifier _currentClassifier;

        private LandcoverType _selectedLandCoverType;

        private ClassifiedFeatureVectorViewModel _selectedFeatureVector;

        private readonly MainWindowViewModel _mainWindowViewModel;
        
        private bool _previewBandIntensityScale = true;
        private bool _areBandsUnscaled;

        /// <summary>
        /// Export command.
        /// </summary>
        public ICommand ExportFeaturesCommand { set; get; }

        /// <summary>
        /// Train command.
        /// </summary>
        public ICommand TrainCommand { set; get; }

        /// <summary>
        /// Import features command.
        /// </summary>
        public ICommand ImportFeatureCommand { set; get; }

        /// <summary>
        /// Remove selected command.
        /// </summary>
        public ICommand RemoveSelectedFeatureVectorCommand { set; get; }

        /// <summary>
        /// Remove all features command.
        /// </summary>
        public ICommand RemoveAllFeaturesCommand { set; get; }

        /// <summary>
        /// Classified Features.
        /// </summary>
        public ObservableCollection<ClassifiedFeatureVectorViewModel> Features { get; set; }

        /// <summary>
        /// The projection of the band image.
        /// </summary>
        public string ProjectionName { get; set; }

        /// <summary>
        /// Conversion from screen to world coordinates.
        /// </summary>
        public Matrix<double> ScreenToWorld;

        /// <summary>
        /// Conversion from world to screen coordinates.
        /// </summary>
        public Matrix<double> WorldToScreen;

        /// <summary>
        /// The bands of the image.
        /// </summary>
        public ObservableCollection<BandViewModel> Bands { get; set; }

        /// <summary>
        /// Whether multiple bands can be visible or not.
        /// </summary>
        public bool MultipleBandsEnabled { get; set; }

        /// <summary>
        /// Used satellite type.
        /// </summary>
        public SatelliteType SatelliteType { get; set; }

        /// <summary>
        /// Contrast enhancement used for bands.
        /// </summary>
        public bool BandsContrastEnhancement { get; set; }

        /// <summary>
        /// Mouse screen position.
        /// </summary>
        public Point MouseScreenPoisition
        {
            get { return _mouseScreenPoisition; }
            set { _mouseScreenPoisition = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// Mouse world position.
        /// </summary>
        public Point MouseWorldPoisition
        {
            get { return _mouseWorldPoisition; }
            set { _mouseWorldPoisition = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// The currently selected feature vector.
        /// </summary>
        public ClassifiedFeatureVectorViewModel SelectedFeatureVector
        {
            get { return _selectedFeatureVector; }
            set
            {
                if (value != _selectedFeatureVector)
                {
                    _selectedFeatureVector = value;
                    RaisePropertyChanged();
                }
            }
        }

        /// <summary>
        /// The current classifier.
        /// </summary>
        public Classifier.Classifier SelectededClassifier
        {
            get { return _selectededClassifier; }
            set
            {
                if (value != _selectededClassifier)
                {
                    _selectededClassifier = value;
                    RaisePropertyChanged();
                }

            }
        }

        /// <summary>
        /// Status text for the training tab.
        /// </summary>
        public string TrainingStatusText
        {
            get { return _trainingStatusText; }
            set
            {
                if (value != _trainingStatusText)
                {
                    _trainingStatusText = value;
                    RaisePropertyChanged();
                }
            }
        }

        /// <summary>
        /// Brush used for the training status label.
        /// </summary>
        public SolidColorBrush TrainingStatusBrush
        {
            get { return _trainingStatusBrush; }
            set
            {
                if (value != _trainingStatusBrush)
                {
                    _trainingStatusBrush = value;
                    RaisePropertyChanged();
                }
            }
        }

        /// <summary>
        /// Returns if all pixels in the prediction tab have been predicted by the classifier.
        /// </summary>
        public bool IsAllPredicted
        {
            get { return _isAllPredicted; }
            set
            {
                if (value != _isAllPredicted)
                {
                    _isAllPredicted = value;
                    RaisePropertyChanged(nameof(IsAllPredicted));
                }
            }
        }

        /// <summary>
        /// True if the classifier has been trained.
        /// </summary>
        public bool IsTrained
        {
            get { return _isTrained; }
            set
            {
                if (value != _isTrained)
                {
                    _isTrained = value;
                    RaisePropertyChanged();
                }
            }
        }

        /// <summary>
        /// The current land cover type.
        /// </summary>
        public LandcoverType SelectedLandCoverType
        {
            get { return _selectedLandCoverType; }
            set { _selectedLandCoverType = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// Whether the preview of the band intensity should scale to min max.
        /// </summary>
        public bool PreviewBandIntensityScale
        {
            get { return _previewBandIntensityScale && AreBandsUnscaled; }
            set { _previewBandIntensityScale = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// Possible land cover types.
        /// </summary>
        public IEnumerable<string> LandCoverTypesEnumerable { get; set; }

        /// <summary>
        /// Possible classifiers.
        /// </summary>
        public IEnumerable<string> ClassifiersEnumerable { get; set; }

        /// <summary>
        /// The current used classifier.
        /// </summary>
        public ILandCoverClassifier CurrentClassifier => _currentClassifier;

        /// <summary>
        /// Whether the bands are unscaled or not.
        /// </summary>
        public bool AreBandsUnscaled
        {
            get { return _areBandsUnscaled; }
            set { _areBandsUnscaled = value; RaisePropertyChanged(); }
        }

        public ClassifierViewModel(MainWindowViewModel mainWindowViewModel)
        {
            _mainWindowViewModel = mainWindowViewModel;

            ScreenToWorld = Matrix<double>.Build.DenseIdentity(3);
            WorldToScreen = ScreenToWorld.Inverse();

            Bands = new ObservableCollection<BandViewModel>();
            Bands.CollectionChanged += BandsOnCollectionChanged;

            LandCoverTypesEnumerable = Enum.GetNames(typeof(LandcoverType));
            ClassifiersEnumerable = Enum.GetNames(typeof(Classifier.Classifier));

            Features = new ObservableCollection<ClassifiedFeatureVectorViewModel>();

            RemoveAllFeaturesCommand = new RelayCommand(() => Features.Clear(), () => Features.Count > 0);

            RemoveSelectedFeatureVectorCommand = new RelayCommand(RemoveSelectedFeature, CanRemoveSelectedFeature);

            PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == nameof(SelectededClassifier))
                {
                    _currentClassifier = SelectededClassifier.CreateClassifier();
                    MarkClassifierNotTrained();
                }
            };
            SelectededClassifier = Classifier.Classifier.DecisionTrees;
            _currentClassifier = SelectededClassifier.CreateClassifier();

            Features.CollectionChanged += (sender, args) =>
            {
                MarkClassifierNotTrained();
                foreach (var bandViewModel in Bands)
                {
                    bandViewModel.CanChangeIsFeature = Features.Count == 0;
                }
            };

            MarkClassifierNotTrained();

            ExportFeaturesCommand = new RelayCommand(ExportTrainingSet, CanExportTrainingSet);
            ImportFeatureCommand = new RelayCommand(ImportTrainingSet, CanImportTrainingSet);

            

            TrainCommand = new RelayCommand(Train, CanTrain);
        }

        private void RemoveSelectedFeature()
        {
            Features.Remove(SelectedFeatureVector);
        }

        private bool CanRemoveSelectedFeature()
        {
            return SelectedFeatureVector != null;
        }

        private void BandsOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs changedEvents)
        {
            if (changedEvents.NewItems != null)
            {
                foreach (BandViewModel bandViewModel in changedEvents.NewItems)
                {
                    bandViewModel.PropertyChanged += BandViewModelOnPropertyChanged;
                }
            }
            if (changedEvents.OldItems != null)
            {
                foreach (BandViewModel bandViewModel in changedEvents.OldItems)
                {
                    bandViewModel.PropertyChanged -= BandViewModelOnPropertyChanged;
                }
            }
        }

        private void BandViewModelOnPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            if (MultipleBandsEnabled) return;

            BandViewModel changedBand = (BandViewModel)sender;
            if (propertyChangedEventArgs.PropertyName == nameof(BandViewModel.IsVisible))
            {
                foreach (BandViewModel bandViewModel in Bands)
                {
                    if (bandViewModel == changedBand) continue;

                    bandViewModel.PropertyChanged -= BandViewModelOnPropertyChanged;
                    bandViewModel.IsVisible = false;
                    bandViewModel.PropertyChanged += BandViewModelOnPropertyChanged;
                }
            }
        }

        private void MarkClassifierNotTrained()
        {
            TrainingStatusText = "Classifier is NOT trained";
            TrainingStatusBrush = new SolidColorBrush(Colors.DarkRed);
            IsTrained = false;
        }

        private void MarkClassifierTrained()
        {
            TrainingStatusText = "Classifier is trained";
            TrainingStatusBrush = new SolidColorBrush(Colors.DarkGreen);
        }

        private void ExportTrainingSet()
        {
            // Create an instance of the open file dialog box.
            var saveFileDialog = new SaveFileDialog()
            {
                Filter = "Txt Files (.txt)|*.txt",
                FilterIndex = 1,
            };

            // Call the ShowDialog method to show the dialog box.
            var userClickedOk = saveFileDialog.ShowDialog();

            // Process input if the user clicked OK.
            if (userClickedOk == DialogResult.OK)
            {
                var csvPath = saveFileDialog.FileName;
                using (var outputStreamWriter = new StreamWriter(csvPath))
                {

                    var bands = Bands.Where(b => b.IsFeature).OrderBy(b => b.BandNumber);
                    outputStreamWriter.WriteLine(SatelliteType);
                    outputStreamWriter.WriteLine(BandsContrastEnhancement);
                    outputStreamWriter.WriteLine(bands.Aggregate("", (a, b) => a + b.BandPath + ";"));

                    foreach (var feature in Features.Select(f => f.ClassifiedFeatureVector))
                    {
                        var featureString = feature.FeatureVector.BandIntensities.Aggregate(feature.Type.ToString(), (accu, intensity) => accu + ";" + intensity);
                        outputStreamWriter.WriteLine(featureString);
                    }
                }
            }
        }

        private bool CanExportTrainingSet()
        {
            return Features.Count > 0;
        }

        private void ImportTrainingSet()
        {
            // Create an instance of the open file dialog box.
            var openFileDialog = new OpenFileDialog()
            {
                Filter = @"Txt Files (.txt)|*.txt",
                FilterIndex = 1,
            };

            // Call the ShowDialog method to show the dialog box.
            var userClickedOk = openFileDialog.ShowDialog();

            // Process input if the user clicked OK.
            if (userClickedOk == DialogResult.OK)
            {
                Features.Clear();

                var path = openFileDialog.FileName;
                var lines = File.ReadAllLines(path);

                SatelliteType satelliteType;
                Enum.TryParse(lines[0], true, out satelliteType);

                bool bandContrastEnhancement = bool.Parse(lines[1]);

                string[] bandPaths = lines[2].Split(';');

                var missingBands = bandPaths.Where(s => s.Trim().Length > 0).Where(s => Bands.All(b => b.BandPath != s)).Select(s =>
                {
                    int bandNumber = SatelliteType.GetBand(Path.GetFileName(s));
                    return new BandInfo(s, bandNumber == 4, bandNumber == 3, bandNumber == 2);
                }).ToList();

                // Check if some bands have to be added.
                if (missingBands.Count > 0)
                {
                    AddBandsDialog dialog = new AddBandsDialog();

                    if (dialog.ShowImportMissingBandsDialog(missingBands, bandContrastEnhancement, satelliteType) ==
                        true && dialog.DialogViewModel.Bands.Count > 0)
                    {
                        _mainWindowViewModel.AddBands(dialog.DialogViewModel);
                    }
                    else
                    {
                        // Abort import
                        return;
                    }
                }

                foreach (var line in lines.Skip(3).Select(line => line.Split(';')))
                {
                    LandcoverType type;
                    Enum.TryParse(line[0], true, out type);
                    var intensities = line.Skip(1).Select(ushort.Parse).ToArray();

                    Features.Add(new ClassifiedFeatureVectorViewModel(new ClassifiedFeatureVector(type,
                        new FeatureVector(intensities))));
                }
            }
        }

        private bool CanImportTrainingSet()
        {
            return true;
        }

        /// <summary>
        /// Trains the classifier.
        /// </summary>
        private void Train()
        {
            var classifiedFeatureVectors = Features.Select(f => f.ClassifiedFeatureVector).ToList();
            var bands = Bands.Where(b => b.IsFeature).Select(b => b.BandNumber).ToList();

            _currentClassifier.Train(new ClassificationModel(ProjectionName, bands, classifiedFeatureVectors));
            IsTrained = true;

            MarkClassifierTrained();
        }


        private bool CanTrain()
        {
            return Features.Count > 0;
        }

    }
}
