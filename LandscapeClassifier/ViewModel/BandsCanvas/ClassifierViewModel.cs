using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using GalaSoft.MvvmLight.Command;
using LandscapeClassifier.Annotations;
using LandscapeClassifier.Classifier;
using LandscapeClassifier.Model;
using LandscapeClassifier.Model.Classification;
using MathNet.Numerics.LinearAlgebra;

namespace LandscapeClassifier.ViewModel.BandsCanvas
{
    public class ClassifierViewModel : INotifyPropertyChanged
    {
        private Point _mouseScreenPoisition;
        private Point _mouseWorldPoisition;

        private string _trainingStatusText;
        private SolidColorBrush _trainingStatusBrush;
        private bool _isAllPredicted;
        private bool _isTrained;

        private Classifier.Classifier _selectededClassifier = Classifier.Classifier.DecisionTrees;
        private ILandCoverClassifier _currentClassifier;

        private ClassifiedFeatureVectorViewModel _selectedFeatureVector;

        /// <summary>
        /// Export predictions.
        /// </summary>
        public ICommand ExportPredictionsCommand { set; get; }

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
        /// Predict all.
        /// </summary>
        public object PredictAllCommand { set; get; }

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
        public readonly string ProjectionName;

        /// <summary>
        /// Conversion from screen to world coordinates.
        /// </summary>
        public readonly Matrix<double> ScreenToWorld;

        /// <summary>
        /// Conversion from world to screen coordinates.
        /// </summary>
        public readonly Matrix<double> WorldToScreen;

        /// <summary>
        /// The bands of the image.
        /// </summary>
        public ObservableCollection<BandViewModel> Bands { get; set; }

        /// <summary>
        /// Whether multiple bands can be visible or not.
        /// </summary>
        public bool MultipleBandsEnabled { get; set; }

        /// <summary>
        /// Mouse screen position.
        /// </summary>
        public Point MouseScreenPoisition
        {
            get { return _mouseScreenPoisition; }
            set { _mouseScreenPoisition = value; OnPropertyChanged(nameof(MouseScreenPoisition)); }
        }

        /// <summary>
        /// Mouse world position.
        /// </summary>
        public Point MouseWorldPoisition
        {
            get { return _mouseWorldPoisition; }
            set { _mouseWorldPoisition = value; OnPropertyChanged(nameof(MouseWorldPoisition)); }
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
                    OnPropertyChanged(nameof(SelectedFeatureVector));
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
                    OnPropertyChanged(nameof(SelectededClassifier));
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
                    OnPropertyChanged(nameof(TrainingStatusText));
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
                    OnPropertyChanged(nameof(TrainingStatusText));
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
                    OnPropertyChanged(nameof(IsAllPredicted));
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
                    OnPropertyChanged(nameof(IsTrained));
                }
            }
        }

        /// <summary>
        /// The current land cover type.
        /// </summary>
        public LandcoverType SelectedLandCoverType { get; set; }

        /// <summary>
        /// Possible land cover types.
        /// </summary>
        public IEnumerable<string> LandCoverTypesEnumerable { get; set; }

        /// <summary>
        /// Possible classifiers.
        /// </summary>
        public IEnumerable<string> ClassifiersEnumerable { get; set; }


        public ClassifierViewModel(string projectionName, Matrix<double> screenToWorld)
        {
            ProjectionName = projectionName;
            ScreenToWorld = screenToWorld;
            WorldToScreen = screenToWorld.Inverse();

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

            Features.CollectionChanged += (sender, args) => { MarkClassifierNotTrained(); };

            MarkClassifierNotTrained();

            ExportFeaturesCommand = new RelayCommand(ExportTrainingSet, CanExportTrainingSet);
            ImportFeatureCommand = new RelayCommand(ImportTrainingSet, CanImportTrainingSet);

            TrainCommand = new RelayCommand(Train, CanTrain);

            PredictAllCommand = new RelayCommand(PredictAll, CanPredictAll);
            ExportPredictionsCommand = new RelayCommand(ExportPredictions, CanExportPredictions);
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

            BandViewModel changedBand = (BandViewModel) sender;
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

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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
            TrainingStatusBrush = new SolidColorBrush(Colors.White);
        }

        private void ExportTrainingSet()
        {
            /*
            // Create an instance of the open file dialog box.
            var saveFileDialog = new SaveFileDialog()
            {
                Filter = "CSV Files (.csv)|*.csv",
                FilterIndex = 1,
            };

            // Call the ShowDialog method to show the dialog box.
            var userClickedOk = saveFileDialog.ShowDialog();

            // Process input if the user clicked OK.
            if (userClickedOk == true)
            {
                // Write CSV Path
                var csvPath = saveFileDialog.FileName;
                using (var outputStreamWriter = new StreamWriter(csvPath))
                {
                    foreach (var feature in Features.Select(f => f.ClassifiedFeatureVector))
                    {
                        outputStreamWriter.WriteLine(
                            feature.Type + ";" +
                            feature.FeatureVector.Color + ";" +
                            feature.FeatureVector.AverageNeighbourhoodColor + ";" +
                            feature.FeatureVector.Altitude + ";" +
                            feature.FeatureVector.Aspect + ";" +
                            feature.FeatureVector.Slope);
                    }
                }
            }
            */
        }

        private bool CanExportTrainingSet()
        {
            return Features.Count > 0;
        }

        private void ImportTrainingSet()
        {
            /*
            // Create an instance of the open file dialog box.
            var openFileDialog = new OpenFileDialog()
            {
                Filter = "CSV Files (.csv)|*.csv",
                FilterIndex = 1,
            };

            // Call the ShowDialog method to show the dialog box.
            var userClickedOk = openFileDialog.ShowDialog();


            // Process input if the user clicked OK.
            if (userClickedOk == true)
            {
                Features.Clear();

                var path = openFileDialog.FileName;
                var lines = File.ReadAllLines(path);
                foreach (var line in lines.Select(line => line.Split(';')))
                {
                    LandcoverType type;
                    Enum.TryParse<LandcoverType>(line[0], true, out type);
                    var color = (Color)ColorConverter.ConvertFromString(line[1]);
                    var averageNeighbourhoodColor = (Color)ColorConverter.ConvertFromString(line[2]);
                    var altitude = float.Parse(line[3]);
                    var aspect = float.Parse(line[4]);
                    var slope = float.Parse(line[5]);

                    Features.Add(new ClassifiedFeatureVectorViewModel(new ClassifiedFeatureVector(type,
                        new FeatureVector(altitude, color, averageNeighbourhoodColor, aspect, slope))));
                }
            }
            */
        }

        private bool CanImportTrainingSet()
        {
            return false;
        }

        private void Train()
        {

            var classifiedFeatureVectors = Features.Select(f => f.ClassifiedFeatureVector).ToList();
            var bands = Bands.Select(b => b.BandNumber).ToList();

            _currentClassifier.Train(new ClassificationModel(ProjectionName, bands, classifiedFeatureVectors));
            IsTrained = true;

            MarkClassifierTrained();
        }


        private bool CanTrain()
        {
            return Features.Count > 0;
        }

        private void ExportPredictions()
        {
            /*
            var chooseFolderDialog = new CommonOpenFileDialog
            {
                Title = "Choose Export Folder",
                IsFolderPicker = true,
                AddToMostRecentlyUsedList = false,
                AllowNonFileSystemItems = false,
                EnsureFileExists = true,
                EnsurePathExists = true,
                EnsureReadOnly = false,
                EnsureValidNames = true,
                Multiselect = false,
                ShowPlacesList = true
            };

            if (chooseFolderDialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                var folder = chooseFolderDialog.FileName;

                // Write prediction image
                using (var fileStream = new FileStream(Path.Combine(folder, "classification.png"), FileMode.Create))
                {
                    BitmapEncoder encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(PredictedLandcoverImage));
                    encoder.Save(fileStream);
                }

                // Write ortho image
                var width = AscFile.Ncols;
                var height = AscFile.Nrows;
                var dpi = 96d;

                var stride = width*4; // 4 bytes per pixel

                byte[] orthoImageData = new byte[stride*height];
                Int32Rect sourceRect = new Int32Rect((int) ViewportRect.X, (int) ViewportRect.Y,
                    (int) ViewportRect.Width, (int) ViewportRect.Height);
                OrthoImage.CopyPixels(sourceRect, orthoImageData, stride, 0);

                // Write prediction image
                using (var fileStream = new FileStream(Path.Combine(folder, "orthophoto.png"), FileMode.Create))
                {
                    var encoder = new PngBitmapEncoder();
                    var orthoImage = BitmapSource.Create(width, height, dpi, dpi, PixelFormats.Bgra32, null,
                        orthoImageData, stride);
                    encoder.Frames.Add(BitmapFrame.Create(orthoImage));
                    encoder.Save(fileStream);
                }

                // Write Layers
                var layerData = new List<byte[]>();
                // var colorMapData = new List<byte[]>();

                var LandCovers = new List<LandcoverType>() {LandcoverType.Tree};
                var LandCoverColors = new Dictionary<Color, int>();

                for (var layerIndex = 0; layerIndex < LandCovers.Count; ++layerIndex)
                {
                    LandCoverColors.Add(LandCovers[layerIndex].GetColor(), layerIndex);

                    var layerDataArray = new byte[stride*height];
                    var colorMapDataArray = new byte[stride * height];
                    layerData.Add(layerDataArray);
                    // colorMapData.Add(colorMapDataArray);
                }

                byte[] predictionImageData = new byte[stride*height];
                _predictedLandcoverImage.CopyPixels(predictionImageData, stride, 0);

                for (int dataIndex = 0; dataIndex < predictionImageData.Length; dataIndex += 4)
                {
                    var b = predictionImageData[dataIndex + 0];
                    var g = predictionImageData[dataIndex + 1];
                    var r = predictionImageData[dataIndex + 2];
                    var a = predictionImageData[dataIndex + 3];
                    var color = Color.FromArgb(a, r, g, b);

                    int layerIndex = LandCoverColors[color];

                    layerData[layerIndex][dataIndex + 0] = 255;
                    layerData[layerIndex][dataIndex + 1] = 255;
                    layerData[layerIndex][dataIndex + 2] = 255;
                    layerData[layerIndex][dataIndex + 3] = 255;

                }

                for (int layerIndex = 0; layerIndex < layerData.Count; ++layerIndex)
                {
                    var data = layerData[layerIndex];
                    var bitmapImage = BitmapSource.Create(width, height, dpi, dpi, PixelFormats.Bgra32, null, data,
                        stride);

                    // write layer
                    using (
                        var fileStream = new FileStream(Path.Combine(folder, "layer" + layerIndex + ".png"),
                            FileMode.Create))
                    {
                        BitmapEncoder encoder = new PngBitmapEncoder();
                        encoder.Frames.Add(BitmapFrame.Create(bitmapImage));
                        encoder.Save(fileStream);
                    }

                }
            }
            */
        }

        private bool CanExportPredictions()
        {
            return false;
        }

        private void PredictAll()
        {
            /*
            // @TODO create transformation matrix
            var left = (AscFile.Xllcorner - WorldFile.X)/WorldFile.PixelSizeX;
            var topWorldCoordinates = AscFile.Yllcorner + AscFile.Cellsize*AscFile.Nrows;
            var topScreenCoordinates = (topWorldCoordinates - WorldFile.Y)/WorldFile.PixelSizeY;

            var width = (int)(AscFile.Ncols * _ascFile.Cellsize / _worldFile.PixelSizeX);
            var height = (int)(AscFile.Nrows * _ascFile.Cellsize / _worldFile.PixelSizeY);

            width = 1000;
            height = 1000;
        
            var dpi = 96d;

            var stride = width * 4; // 4 bytes per pixel
            var pixelData = new byte[stride * height];

            FeatureVector[,] features = new FeatureVector[height, width];

            // Create features
            Parallel.For(0, height, y =>
            {
                for (int x = 0; x < width; ++x)
                {
                    var position = new Point(x, y);
                    var altitude = GetAscDataAt(position);
                    var color = GetColorAt(position);
                    var averageNeighborhoodColor = GetAverageNeighborhoodColor(position);
                    var slopeAndAspectAt = GetSlopeAndAspectAt(position);

                    features[y, x] = new FeatureVector(altitude, color, averageNeighborhoodColor,
                        slopeAndAspectAt.Aspect, slopeAndAspectAt.Slope);
                }
            });
           

            // Predict
            var prediction = _currentClassifier.Predict(features);
            PredictedLandcoverImage = prediction;
            */
        }

        private bool CanPredictAll()
        {
            return IsTrained;
        }
    }
}
