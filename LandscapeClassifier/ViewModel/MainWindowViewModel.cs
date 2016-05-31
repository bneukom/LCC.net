using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using GalaSoft.MvvmLight.CommandWpf;
using LandscapeClassifier.Annotations;
using LandscapeClassifier.Classifier;
using LandscapeClassifier.Extensions;
using LandscapeClassifier.Model;
using LandscapeClassifier.Util;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace LandscapeClassifier.ViewModel
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private readonly ILandCoverClassifier _classifier;

        private AscFile _ascFile;
        private WorldFile _worldFile;

        private BitmapSource _bitmapImage;
        private byte[] _imageData;
        private Rect _viewportRect;

        private ClassifiedFeatureVectorViewModel _selectedFeatureVector;
        private bool _isTrained;

        private BitmapSource _predictedLandcoverImage;
        private double _overlayOpacity = 0.5d;
        private string _trainingStatusText;
        private SolidColorBrush _trainingStatusBrush;
        private bool _isAllPredicted;

        /// <summary>
        /// Export predictions.
        /// </summary>
        public ICommand ExportPredictionsCommand { set; get; }

        /// <summary>
        /// Export command.
        /// </summary>
        public ICommand ExportFeaturesCommand { set; get; }

        /// <summary>
        /// Exit command.
        /// </summary>
        public ICommand ExitCommand { set; get; }

        /// <summary>
        /// Remove selected command.
        /// </summary>
        public ICommand RemoveSelectedFeatureVectorCommand { set; get; }

        /// <summary>
        /// Remove all features command.
        /// </summary>
        public ICommand RemoveAllFeaturesCommand { set; get; }

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
                    NotifyPropertyChanged("IsTrained");
                }
            }
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
                    NotifyPropertyChanged("SelectedFeatureVector");
                }
            }
        }

        /// <summary>
        /// World file to the ortho image.
        /// </summary>
        public WorldFile WorldFile
        {
            get { return _worldFile; }
            set
            {
                if (value != _worldFile)
                {
                    _worldFile = value;
                    NotifyPropertyChanged("WorldFile");
                }
            }
        }

        /// <summary>
        /// ASC file which represents the DEM.
        /// </summary>
        public AscFile AscFile
        {
            get { return _ascFile; }
            set
            {
                if (value != _ascFile)
                {
                    _ascFile = value;
                    NotifyPropertyChanged("AscFile");
                }
            }
        }

        /// <summary>
        /// Viewport rectangle which contains all data (asc and orthoimage) used for classification.
        /// </summary>
        public Rect ViewportRect
        {
            get { return _viewportRect; }
            set
            {
                if (value != _viewportRect)
                {
                    _viewportRect = value;
                    NotifyPropertyChanged("ViewportRect");
                }
            }
        }

        /// <summary>
        /// The OrthoImage map.
        /// </summary>
        public BitmapSource OrthoImage
        {
            get { return _bitmapImage; }
            set
            {
                if (value != _bitmapImage)
                {
                    _bitmapImage = value;

                    var stride = (int) _bitmapImage.PixelWidth*(_bitmapImage.Format.BitsPerPixel/8);
                    _imageData = new byte[(int) _bitmapImage.PixelHeight*stride];

                    // @TODO waste of memory!
                    _bitmapImage.CopyPixels(_imageData, stride, 0);

                    NotifyPropertyChanged("OrthoImage");
                }
            }
        }

        /// <summary>
        /// Prediction bitmap overlay.
        /// </summary>
        public BitmapSource PredictedLandcoverImage
        {
            get { return _predictedLandcoverImage; }
            set
            {
                if (value != _predictedLandcoverImage)
                {
                    _predictedLandcoverImage = value;
                    NotifyPropertyChanged("PredictedLandcoverImage");
                }
            }
        }

        /// <summary>
        /// Opacity overlay.
        /// </summary>
        public double OverlayOpacity
        {
            get { return _overlayOpacity; }
            set
            {
                if (value != _overlayOpacity)
                {
                    _overlayOpacity = value;
                    NotifyPropertyChanged("OverlayOpacity");
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
                    NotifyPropertyChanged("TrainingStatusText");
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
                    NotifyPropertyChanged("TrainingStatusBrush");
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
                    NotifyPropertyChanged("IsAllPredicted");
                }
            }
        }


        /// <summary>
        /// Feature vectors.
        /// </summary>
        public ObservableCollection<ClassifiedFeatureVectorViewModel> Features { get; set; }

        /// <summary>
        /// Possible land cover types.
        /// </summary>
        public IEnumerable<string> LandCoverTypesEnumerable { get; set; }

        /// <summary>
        /// The current land cover type.
        /// </summary>
        public LandcoverType SelectedLandCoverType { get; set; }


        public MainWindowViewModel()
        {
            _classifier = new BayesLandCoverClassifier();

            LandCoverTypesEnumerable = Enum.GetNames(typeof(LandcoverType));

            Features = new ObservableCollection<ClassifiedFeatureVectorViewModel>();

            ExitCommand = new RelayCommand(() => Application.Current.Shutdown(), () => true);
            ExportFeaturesCommand = new RelayCommand(ExportTrainingSet, CanExportTrainingSet);
            ImportFeatureCommand = new RelayCommand(ImportTrainingSet, CanImportTrainingSet);
            RemoveAllFeaturesCommand = new RelayCommand(() => Features.Clear(), () => Features.Count > 0);

            RemoveSelectedFeatureVectorCommand = new RelayCommand(RemoveSelectedFeature, CanRemoveSelectedFeature);
            TrainCommand = new RelayCommand(Train, CanTrain);

            PredictAllCommand = new RelayCommand(PredictAll, CanPredictAll);
            ExportPredictionsCommand = new RelayCommand(ExportPredictions, CanExportPredictions);

            Features.CollectionChanged += (sender, args) => { MarkClassifierNotTrained(); };

            MarkClassifierNotTrained();
        }

        #region Model Accessers

        /// <summary>
        /// Predicts the land cover type with the given feature vector.
        /// </summary>
        /// <param name="feature"></param>
        /// <returns></returns>
        public LandcoverType Predict(FeatureVector feature)
        {
            return _classifier.Predict(feature);
        }

        // @TODO coordinate system?
        /// <summary>
        /// Returns whether the given position is within the bounds of the ASC file.
        /// </summary>
        /// <param name="position">Pixel position in the </param>
        /// <returns></returns>
        public bool IsInAscBounds(Point position)
        {
            if (_ascFile == null) throw new InvalidOperationException();

            // Position in LV95 coordinate system
            var lv95X = (int) (position.X*WorldFile.PixelSizeX + WorldFile.X);
            var lv95Y = (int) (position.Y*WorldFile.PixelSizeY + WorldFile.Y);

            return lv95X > _ascFile.Xllcorner && lv95X < _ascFile.Xllcorner + _ascFile.Ncols*_ascFile.Cellsize &&
                   lv95Y > _ascFile.Yllcorner && lv95Y < _ascFile.Yllcorner + _ascFile.Nrows*_ascFile.Cellsize;
        }

        // @TODO coordinate system?
        /// <summary>
        /// Returns the data from the ASC file at the given position.
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public short GetAscDataAt(Point position)
        {
            if (_ascFile == null || _worldFile == null) throw new InvalidOperationException();

            // Position in LV95 coordinate system
            var lv95X = (int) (position.X*_worldFile.PixelSizeX + _worldFile.X);
            var lv95Y = (int) (position.Y*_worldFile.PixelSizeY + _worldFile.Y);

            if (lv95X > _ascFile.Xllcorner && lv95X < _ascFile.Xllcorner + _ascFile.Ncols*_ascFile.Cellsize &&
                lv95Y > _ascFile.Yllcorner && lv95Y < _ascFile.Yllcorner + _ascFile.Nrows*_ascFile.Cellsize)
            {
                var indexX = (int) ((lv95X - _ascFile.Xllcorner)/_ascFile.Cellsize);
                var indexY = (int) ((lv95Y - _ascFile.Yllcorner)/_ascFile.Cellsize);
                return _ascFile.Data[_ascFile.Nrows - indexY - 1, indexX];
            }
            else
            {
                return _ascFile.NoDataValue;
            }
        }

        /// <summary>
        /// Returns the color at the given pixel position.
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public Color GetColorAt(Point position)
        {
            if (_bitmapImage == null) throw new InvalidOperationException();

            // @TODO http://stackoverflow.com/questions/3745824/loading-image-into-imagesource-incorrect-width-and-height
            if (position.Y < 0 || position.Y < 0 || position.X >= _bitmapImage.Width ||
                position.Y >= _bitmapImage.Height)
                return Colors.Black;

            var stride = (int) _bitmapImage.PixelWidth*(_bitmapImage.Format.BitsPerPixel/8);
            var index = (int) position.Y*stride + _bitmapImage.Format.BitsPerPixel/8*(int) position.X;

            var B = _imageData[index];
            var G = _imageData[index + 1];
            var R = _imageData[index + 2];
            var A = _imageData[index + 3];

            return Color.FromArgb(A, R, G, B);
        }

        /// <summary>
        /// Returns the average color of the eight neighborhood surrounding the pixel at the given position.
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public Color GetAverageNeighborhoodColor(Point position)
        {
            if (_bitmapImage == null) throw new InvalidOperationException();

            // @TODO http://stackoverflow.com/questions/3745824/loading-image-into-imagesource-incorrect-width-and-height
            if (position.Y < 1 || position.Y < 1
                || position.X >= _bitmapImage.Width - 1
                || position.Y >= _bitmapImage.Height - 1)
                return Colors.Black;

            byte R = 0, G = 0, B = 0, A = 0;
            for (var x = -1; x <= 1; ++x)
            {
                for (var y = -1; y <= 1; ++y)
                {
                    var stride = (int) _bitmapImage.PixelWidth*(_bitmapImage.Format.BitsPerPixel/8);
                    var index = ((int) position.Y + y)*stride +
                                _bitmapImage.Format.BitsPerPixel/8*((int) position.X + x);

                    B += _imageData[index];
                    G += _imageData[index + 1];
                    R += _imageData[index + 2];
                    A += _imageData[index + 3];
                }
            }

            return Color.FromArgb((byte) (A/8), (byte) (R/8), (byte) (G/8), (byte) (B/8));
        }

        /// <summary>
        /// Returns the luminance at the given pixel position.
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public float GetLuminance(Point position)
        {
            var color = GetColorAt(position);
            return 0.2126f*color.R + 0.7152f*color.G + 0.0722f*color.B;
        }

        // @TODO coordinate system?
        /// <summary>
        /// Returns the slope at the given position or 0 if not available.
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public AspectSlope GetSlopeAndAspectAt(Point position)
        {
            var left = (_ascFile.Xllcorner - _worldFile.X)/_worldFile.PixelSizeX;

            var topWorldCoordinates = _ascFile.Yllcorner +
                                      _ascFile.Cellsize*_ascFile.Nrows;

            var topScreenCoordinates = (topWorldCoordinates - _worldFile.Y)/
                                       _worldFile.PixelSizeY;

            int positionX = (int) (position.X - left);
            int positionY = (int) (position.Y - topScreenCoordinates);

            if (positionX - 1 < 0 || positionX + 1 >= _ascFile.Ncols || positionY - 1 < 0 ||
                positionY + 1 >= _ascFile.Nrows)
            {
                return new AspectSlope(0, 0);
            }

            float Z1 = _ascFile.Data[positionY - 1, positionX - 1];
            float Z2 = _ascFile.Data[positionY - 1, positionX];
            float Z3 = _ascFile.Data[positionY - 1, positionX + 1];

            float Z4 = _ascFile.Data[positionY, positionX - 1];
            float Z5 = _ascFile.Data[positionY, positionX];
            float Z6 = _ascFile.Data[positionY, positionX + 1];

            float Z7 = _ascFile.Data[positionY + 1, positionX - 1];
            float Z8 = _ascFile.Data[positionY + 1, positionX];
            float Z9 = _ascFile.Data[positionY + 1, positionX + 1];


            float b = (Z3 + 2*Z6 + Z9 - Z1 - 2*Z4 - Z7)/(8*_ascFile.Cellsize);
            float c = (Z1 + 2*Z2 + Z3 - Z7 - 2*Z8 - Z9)/(8*_ascFile.Cellsize);

            float slope = (float) Math.Atan(Math.Sqrt(b*b + c*c));
            double aspect;

            if (MoreMath.AlmostZero(c))
            {
                aspect = 0;
            }
            else
            {
                aspect = (float) Math.Atan(b/c);

                if (c > 0)
                {
                    aspect += Math.PI;
                }
                else if (c < 0 && b > 0)
                {
                    aspect += (2*Math.PI);
                }
            }

            return new AspectSlope(slope, (float) aspect);
        }

        #endregion

        #region Command Implementations

        private void ExportTrainingSet()
        {
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
        }

        private bool CanExportTrainingSet()
        {
            return _ascFile != null && _bitmapImage != null && Features.Count > 0;
        }

        private void ImportTrainingSet()
        {
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
                    var color = (Color) ColorConverter.ConvertFromString(line[1]);
                    var averageNeighbourhoodColor = (Color) ColorConverter.ConvertFromString(line[2]);
                    var altitude = float.Parse(line[3]);
                    var aspect = float.Parse(line[4]);
                    var slope = float.Parse(line[5]);

                    Features.Add(new ClassifiedFeatureVectorViewModel(new ClassifiedFeatureVector(type,
                        new FeatureVector(altitude, color, averageNeighbourhoodColor, aspect, slope))));
                }
            }
        }

        private bool CanImportTrainingSet()
        {
            return true;
        }

        private void Train()
        {
            _classifier.Train(Features.Select(f => f.ClassifiedFeatureVector).ToList());
            IsTrained = true;

            MarkClassifierTrained();
        }


        private bool CanTrain()
        {
            return Features.Count > 0 && Features.GroupBy(f => f.ClassifiedFeatureVector.Type).Count() >= 2;
        }

        private void RemoveSelectedFeature()
        {
            Features.Remove(SelectedFeatureVector);
        }

        private bool CanRemoveSelectedFeature()
        {
            return SelectedFeatureVector != null;
        }

        private void ExportPredictions()
        {
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

                // Write Layer
                var layerData = new List<byte[]>();

                var width = AscFile.Ncols;
                var height = AscFile.Nrows;
                var dpi = 96d;

                var stride = width*4; // 4 bytes per pixel

                for (var layerIndex = 0; layerIndex < Enum.GetValues(typeof(LandcoverType)).Length; ++layerIndex)
                {
                    var pixelData = new byte[stride*height];
                    layerData.Add(pixelData);
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

                    int layerIndex = (from type in Enum.GetValues(typeof(LandcoverType)).Cast<LandcoverType>()
                        where color == type.GetColor()
                        select (int) type).FirstOrDefault();

                    layerData[layerIndex][dataIndex + 0] = b;
                    layerData[layerIndex][dataIndex + 1] = g;
                    layerData[layerIndex][dataIndex + 2] = r;
                    layerData[layerIndex][dataIndex + 3] = a;
                }

                for (int layerIndex = 0; layerIndex < layerData.Count; ++layerIndex)
                {
                    byte[] data = layerData[layerIndex];
                    var bitmapImage = BitmapSource.Create(width, height, dpi, dpi, PixelFormats.Bgra32, null, data,
                        stride);

                    using (
                        var fileStream = new FileStream(Path.Combine(folder, "layer" + layerIndex + ".png"),
                            FileMode.Create))
                    {
                        BitmapEncoder encoder = new PngBitmapEncoder();
                        encoder.Frames.Add(BitmapFrame.Create(bitmapImage));
                        encoder.Save(fileStream);
                    }
                }


                // Write ortho image
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
            }
        }

        private bool CanExportPredictions()
        {
            return PredictedLandcoverImage != null;
        }

        private void PredictAll()
        {
            // @TODO create transformation matrix
            var left = (AscFile.Xllcorner - WorldFile.X)/WorldFile.PixelSizeX;
            var topWorldCoordinates = AscFile.Yllcorner + AscFile.Cellsize*AscFile.Nrows;
            var topScreenCoordinates = (topWorldCoordinates - WorldFile.Y)/WorldFile.PixelSizeY;

            // @TODO only works because AscFile#CellSize == WorldFile#CellSize
            var width = AscFile.Ncols;
            var height = AscFile.Nrows;

            // Create features
            FeatureVector[,] features = new FeatureVector[height, width];
            for (int y = 0; y < height; ++y)
            {
                for (int x = 0; x < width; ++x)
                {
                    var position = new Point(x + left, y + topScreenCoordinates);
                    var altitude = GetAscDataAt(position);
                    var color = GetColorAt(position);
                    var averageNeighborhoodColor = GetAverageNeighborhoodColor(position);
                    var slopeAndAspectAt = GetSlopeAndAspectAt(position);
                    features[y, x] = new FeatureVector(altitude, color, averageNeighborhoodColor,
                        slopeAndAspectAt.Aspect, slopeAndAspectAt.Slope);
                }
            }
            // Predict
            var prediction = _classifier.Predict(features);
            PredictedLandcoverImage = prediction;
        }

        private bool CanPredictAll()
        {
            return IsTrained;
        }

        #endregion

        #region Property Change Support

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void NotifyPropertyChanged(string info)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(info));
        }

        private void MarkClassifierNotTrained()
        {
            TrainingStatusText = "Classifier is NOT trained";
            TrainingStatusBrush = new SolidColorBrush(Colors.DarkRed);
            IsTrained = false;
            PredictedLandcoverImage = null;
        }

        private void MarkClassifierTrained()
        {
            TrainingStatusText = "Classifier is trained";
            TrainingStatusBrush = new SolidColorBrush(Colors.White);
        }

        #endregion
    }
}