using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using GalaSoft.MvvmLight.CommandWpf;
using LandscapeClassifier.Annotations;
using LandscapeClassifier.Classifier;
using LandscapeClassifier.Controls;
using LandscapeClassifier.Extensions;
using LandscapeClassifier.Model;
using LandscapeClassifier.Util;
using LandscapeClassifier.View;
using LandscapeClassifier.View.Open;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using OSGeo.GDAL;
using OSGeo.OSR;
using Band = LandscapeClassifier.Model.Band;

namespace LandscapeClassifier.ViewModel
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private AscFile _ascFile;

        private ClassifiedFeatureVectorViewModel _selectedFeatureVector;
        private bool _isTrained;

        private BitmapSource _predictedLandcoverImage;
        private double _overlayOpacity = 0.5d;
        private string _trainingStatusText;
        private SolidColorBrush _trainingStatusBrush;
        private bool _isAllPredicted;
        private Classifier.Classifier _selectededClassifier;
        private ILandCoverClassifier _currentClassifier;
        private ImageBandViewModel _activeBandViewModel;

        /// <summary>
        /// Exports a height map
        /// </summary>
        public ICommand ExportSmoothedHeightImageCommand { set; get; }

        /// <summary>
        /// Exports a slope map from the loaded DEM.
        /// </summary>
        public ICommand ExportSlopeImageCommand { set; get; }

        /// <summary>
        /// Exports a slope map from the loaded DEM.
        /// </summary>
        public ICommand ExportCurvatureImageCommand { set; get; }

        /// <summary>
        /// Create slope texture from a DEM.
        /// </summary>
        public ICommand CreateSlopeFromDEM { set; get; }

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
        /// Open image bands.
        /// </summary>
        public ICommand OpenImagesCommand { set; get; }

        /// <summary>
        /// Predict all.
        /// </summary>
        public object PredictAllCommand { set; get; }

        /// <summary>
        /// Image BandInfo ImageBandViewModels
        /// </summary>
        public ObservableCollection<ImageBandViewModel> ImageBandViewModels { set; get; }

        /// <summary>
        /// 
        /// </summary>
        public ImageBandViewModel ActiveBandViewModel
        {
            set { _activeBandViewModel = value; OnPropertyChanged(nameof(ActiveBandViewModel)); }
            get { return _activeBandViewModel; }
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

                    IsAllPredicted = value != null;
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
        /// Property name of the <see cref="SelectededClassifier"/>
        /// </summary>
        public const string SelectedClassifierProperty = "SelectededClassifier";

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
                    NotifyPropertyChanged(SelectedClassifierProperty);
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
        /// Possible classifiers.
        /// </summary>
        public IEnumerable<string> ClassifiersEnumerable { get; set; }

        /// <summary>
        /// The current land cover type.
        /// </summary>
        public LandcoverType SelectedLandCoverType { get; set; }


        public MainWindowViewModel()
        {
            GdalConfiguration.ConfigureGdal();

            ImageBandViewModels = new ObservableCollection<ImageBandViewModel>();

            LandCoverTypesEnumerable = Enum.GetNames(typeof(LandcoverType));
            ClassifiersEnumerable = Enum.GetNames(typeof(Classifier.Classifier));

            Features = new ObservableCollection<ClassifiedFeatureVectorViewModel>();

            OpenImagesCommand = new RelayCommand(OpenBands, () => true);

            ExitCommand = new RelayCommand(() => Application.Current.Shutdown(), () => true);
            ExportFeaturesCommand = new RelayCommand(ExportTrainingSet, CanExportTrainingSet);
            ImportFeatureCommand = new RelayCommand(ImportTrainingSet, CanImportTrainingSet);

            ExportSlopeImageCommand = new RelayCommand(() => ExportSlopeFromDEMDialog.ShowDialog(_ascFile), () => AscFile != null);
            ExportCurvatureImageCommand = new RelayCommand(() => ExportCurvatureFromDEMDialog.ShowDialog(_ascFile), () => AscFile != null);
            RemoveAllFeaturesCommand = new RelayCommand(() => Features.Clear(), () => Features.Count > 0);
            ExportSmoothedHeightImageCommand = new RelayCommand(() => ExportSmoothedDEMDialog.ShowDialog(_ascFile), () => AscFile != null);
            CreateSlopeFromDEM = new RelayCommand(() => new CreateSlopeFromDEMDialog().ShowDialog(), () => true);

            RemoveSelectedFeatureVectorCommand = new RelayCommand(RemoveSelectedFeature, CanRemoveSelectedFeature);
            TrainCommand = new RelayCommand(Train, CanTrain);

            PredictAllCommand = new RelayCommand(PredictAll, CanPredictAll);
            ExportPredictionsCommand = new RelayCommand(ExportPredictions, CanExportPredictions);

            Features.CollectionChanged += (sender, args) => { MarkClassifierNotTrained(); };

            PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == SelectedClassifierProperty)
                {
                    _currentClassifier = SelectededClassifier.CreateClassifier();
                    MarkClassifierNotTrained();
                }
            };
            SelectededClassifier = Classifier.Classifier.Bayes;
            _currentClassifier = SelectededClassifier.CreateClassifier();

            MarkClassifierNotTrained();
        }

        private void OpenBands()
        {
            OpenImageDialog dialog = new OpenImageDialog();

            if (dialog.ShowDialog() == true && dialog.DialogViewModel.Bands.Count > 0)
            {
                var firstBand = dialog.DialogViewModel.Bands.FirstOrDefault(b => b.B || b.G || b.R)
                    ?? dialog.DialogViewModel.Bands.First();

                var firstDataSet = Gdal.Open(firstBand.Path, Access.GA_ReadOnly);
                int width = firstDataSet.GetRasterBand(1).XSize;
                int height = firstDataSet.GetRasterBand(1).YSize;

                byte[] bgra = new byte[width * height * 4];
                int rgbStride = width * 4;

                Task loadImages = Task.Factory.StartNew(() => Parallel.ForEach(dialog.DialogViewModel.Bands, (bandInfo, _, bandIndex) =>
                {
                    var dataSet = Gdal.Open(bandInfo.Path, Access.GA_ReadOnly);
                    var rasterBand = dataSet.GetRasterBand(1);

                    int stride = (rasterBand.XSize * 16 + 7) / 8;
                    IntPtr data = Marshal.AllocHGlobal(stride * rasterBand.YSize);
                    rasterBand.ReadRaster(0, 0, rasterBand.XSize, rasterBand.YSize, data, rasterBand.XSize, rasterBand.YSize, DataType.GDT_UInt16, 2, stride);

                    // Cutoff
                    int[] histogram = new int[ushort.MaxValue];
                    rasterBand.GetHistogram(0, ushort.MaxValue, ushort.MaxValue, histogram, 1, 0,
                        ProgressFunc, "");

                    double minCut = rasterBand.XSize * rasterBand.YSize * 0.02f;
                    int minCutValue = 0;
                    bool minCutSet = false;

                    double maxCut = rasterBand.XSize * rasterBand.YSize * 0.98f;
                    int maxCutValue = ushort.MaxValue;
                    bool maxCutSet = false;

                    int pixelCount = 0;
                    for (int bucket = 0; bucket < histogram.Length; ++bucket)
                    {
                        pixelCount += histogram[bucket];
                        if (pixelCount >= minCut && !minCutSet)
                        {
                            minCutValue = bucket;
                            minCutSet = true;
                        }
                        if (pixelCount >= maxCut && !maxCutSet)
                        {
                            maxCutValue = bucket;
                            maxCutSet = true;
                        }
                    }

                    // Add RGB
                    if (dialog.DialogViewModel.AddRgb)
                    {
                        // Apply RGB contrast enhancement
                        if (dialog.DialogViewModel.RgbContrastEnhancement && (bandInfo.B || bandInfo.G || bandInfo.R))
                        {
                            int colorOffset = bandInfo.B ? 0 : bandInfo.G ? 1 : bandInfo.R ? 2 : -1;
                            unsafe
                            {
                                ushort* dataPtr = (ushort*)data.ToPointer();
                                Parallel.ForEach(Partitioner.Create(0, rasterBand.XSize*rasterBand.YSize), (range) =>
                                {
                                    for (int dataIndex = range.Item1; dataIndex < range.Item2; ++dataIndex)
                                    {
                                        ushort current = *(dataPtr + dataIndex);
                                        byte val =
                                            (byte)
                                                MoreMath.Clamp(
                                                    (current - minCutValue)/(double) (maxCutValue - minCutValue)*
                                                    byte.MaxValue, 0, byte.MaxValue - 1);

                                        bgra[dataIndex*4 + colorOffset] = val;
                                        bgra[dataIndex*4 + 3] = 255;
                                    }
                                });
                            }
                        }
                    }

                    // Apply band contrast enhancement
                    if (dialog.DialogViewModel.BandContrastEnhancement)
                    {
                        unsafe
                        {
                            ushort* dataPtr = (ushort*)data.ToPointer();
                            Parallel.ForEach(Partitioner.Create(0, rasterBand.XSize*rasterBand.YSize), (range) =>
                            {
                                for (int dataIndex = range.Item1; dataIndex < range.Item2; ++dataIndex)
                                {
                                    ushort current = *(dataPtr + dataIndex);
                                    *(dataPtr + dataIndex) = (ushort)MoreMath.Clamp((current - minCutValue) / (double)(maxCutValue - minCutValue) * ushort.MaxValue, 0, ushort.MaxValue - 1);
                                }
                            });
                        }
                    }


                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        WriteableBitmap bandImage = new WriteableBitmap(rasterBand.XSize, rasterBand.YSize, 96, 96, PixelFormats.Gray16, null);
                        bandImage.Lock();

                        unsafe
                        {
                            Buffer.MemoryCopy(data.ToPointer(), bandImage.BackBuffer.ToPointer(), stride * rasterBand.YSize,
                                stride * rasterBand.YSize);
                        }

                        bandImage.AddDirtyRect(new Int32Rect(0, 0, rasterBand.XSize, rasterBand.YSize));
                        bandImage.Unlock();

                        // Transformation
                        double[] transform = new double[6];
                        dataSet.GetGeoTransform(transform);
                        double[,] matArray =
                        {
                            {1, transform[2], transform[0]},
                            {transform[4], -1, transform[3]},
                            {0, 0, 1}
                        };
                        var builder = MathNet.Numerics.LinearAlgebra.Matrix<double>.Build;
                        var transformMat = builder.DenseOfArray(matArray);

                        var vecBuilder = Vector<double>.Build;
                        var upperLeft = vecBuilder.DenseOfArray(new[] {transform[0], transform[3], 1});
                        var xRes = transform[1];
                        var yRes = transform[5];
                        var bottomRight = vecBuilder.DenseOfArray(new[] {upperLeft[0] + (rasterBand.XSize * xRes), upperLeft[1] + (rasterBand.YSize * yRes), 1});

                        Band band = new Band(dataSet.GetProjection(), transformMat, upperLeft, bottomRight);

                        var imageBandViewModel = new ImageBandViewModel("Band " + dialog.DialogViewModel.SateliteType.GetBand(Path.GetFileName(bandInfo.Path)), bandImage, band);
                        ImageBandViewModels.Add(imageBandViewModel);
                    });

                    Marshal.FreeHGlobal(data);
                }));

                // Load rgb image
                if (dialog.DialogViewModel.AddRgb)
                {
                    loadImages.ContinueWith(t =>
                    {
                        // Create RGB image
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            var rgbImage = BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgra32, null, bgra,
                                rgbStride);

                            // TODO model
                            ImageBandViewModels.Insert(0, new ImageBandViewModel("RGB", rgbImage, null));
                        });
                    });
                }
            }
        }

        public static int ProgressFunc(double Complete, IntPtr Message, IntPtr Data)
        {
            return 1;
        }

        #region Model Accessers

        /// <summary>
        /// Predicts the land cover type with the given feature vector.
        /// </summary>
        /// <param name="feature"></param>
        /// <returns></returns>
        public LandcoverType Predict(FeatureVector feature)
        {
            return _currentClassifier.Predict(feature);
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
            return _ascFile != null && Features.Count > 0;
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
                    var color = (Color)ColorConverter.ConvertFromString(line[1]);
                    var averageNeighbourhoodColor = (Color)ColorConverter.ConvertFromString(line[2]);
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
            _currentClassifier.Train(Features.Select(f => f.ClassifiedFeatureVector).ToList());
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
            return PredictedLandcoverImage != null;
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