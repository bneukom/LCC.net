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
using GalaSoft.MvvmLight.Command;
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

        private LandcoverType _selectedLandCoverType;

        private ClassifiedFeatureVectorViewModel _selectedFeatureVector;

        /// <summary>
        /// Open image bands.
        /// </summary>
        public ICommand OpenImagesCommand { set; get; }

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
        public LandcoverType SelectedLandCoverType
        {
            get { return _selectedLandCoverType; }
            set { _selectedLandCoverType = value; OnPropertyChanged(nameof(SelectedLandCoverType)); }
        }

        /// <summary>
        /// Possible land cover types.
        /// </summary>
        public IEnumerable<string> LandCoverTypesEnumerable { get; set; }

        /// <summary>
        /// Possible classifiers.
        /// </summary>
        public IEnumerable<string> ClassifiersEnumerable { get; set; }

        public ClassifierViewModel()
        {
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

            Features.CollectionChanged += (sender, args) => { MarkClassifierNotTrained(); };

            MarkClassifierNotTrained();

            ExportFeaturesCommand = new RelayCommand(ExportTrainingSet, CanExportTrainingSet);
            ImportFeatureCommand = new RelayCommand(ImportTrainingSet, CanImportTrainingSet);

            OpenImagesCommand = new RelayCommand(OpenBands, () => true);

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
                // Write CSV Path
                var csvPath = saveFileDialog.FileName;
                using (var outputStreamWriter = new StreamWriter(csvPath))
                {
                    foreach (var feature in Features.Select(f => f.ClassifiedFeatureVector))
                    {

                        outputStreamWriter.WriteLine(Bands.Count);
                        outputStreamWriter.WriteLine(Bands.Aggregate("", (a,b) => a + ";" + b.BandPath));

                        var featureString = feature.FeatureVector.BandIntensities.Aggregate(feature.Type.ToString(), (a,b) => a + ";" + b);
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

        private void OpenBands()
        {
            OpenImageDialog dialog = new OpenImageDialog();

            if (dialog.ShowDialog() == true && dialog.DialogViewModel.Bands.Count > 0)
            {
                // Initialize RGB data
                byte[] bgra = null;
                Dataset rgbDataSet = null;
                if (dialog.DialogViewModel.AddRgb)
                {
                    var firstRGBBand = dialog.DialogViewModel.Bands.First(b => b.B || b.G || b.R);
                    rgbDataSet = Gdal.Open(firstRGBBand.Path, Access.GA_ReadOnly);
                    bgra = new byte[rgbDataSet.RasterXSize * rgbDataSet.RasterYSize * 4];
                }

                var firstBand = dialog.DialogViewModel.Bands.First();
                var firstDataSet = Gdal.Open(firstBand.Path, Access.GA_ReadOnly);

                // Transformation
                double[] transform = new double[6];
                firstDataSet.GetGeoTransform(transform);
                double[,] matArray =
                {
                    {1, transform[2], transform[0]},
                    {transform[4], -1, transform[3]},
                    {0, 0, 1}
                };
                var builder = Matrix<double>.Build;
                var transformMat = builder.DenseOfArray(matArray);

                ProjectionName = firstDataSet.GetProjection();
                ScreenToWorld = transformMat;
                WorldToScreen = transformMat.Inverse();

                // Parallel band loading
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
                                Parallel.ForEach(Partitioner.Create(0, rasterBand.XSize * rasterBand.YSize), (range) =>
                                {
                                    for (int dataIndex = range.Item1; dataIndex < range.Item2; ++dataIndex)
                                    {
                                        ushort current = *(dataPtr + dataIndex);
                                        byte val = (byte)MoreMath.Clamp((current - minCutValue) / (double)(maxCutValue - minCutValue) *
                                                    byte.MaxValue, 0, byte.MaxValue - 1);

                                        bgra[dataIndex * 4 + colorOffset] = val;
                                        bgra[dataIndex * 4 + 3] = 255;
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


                            Parallel.ForEach(Partitioner.Create(0, rasterBand.XSize * rasterBand.YSize), (range) =>
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

                        // Position
                        double[] bandTransform = new double[6];
                        dataSet.GetGeoTransform(bandTransform);
                        var vecBuilder = Vector<double>.Build;
                        var upperLeft = vecBuilder.DenseOfArray(new[] { bandTransform[0], bandTransform[3], 1 });
                        var meterPerPixel = bandTransform[1];
                        var xRes = bandTransform[1];
                        var yRes = bandTransform[5];
                        var bottomRight = vecBuilder.DenseOfArray(new[] { upperLeft[0] + (rasterBand.XSize * xRes), upperLeft[1] + (rasterBand.YSize * yRes), 1 });

                        int bandNumber = dialog.DialogViewModel.SateliteType.GetBand(Path.GetFileName(bandInfo.Path));
                        var imageBandViewModel = new BandViewModel("Band " + bandNumber, bandInfo.Path, bandNumber, meterPerPixel, bandImage, upperLeft, bottomRight, true);

                        Bands.AddSorted(imageBandViewModel, Comparer<BandViewModel>.Create((a, b) => a.BandNumber - b.BandNumber));
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
                            var rgbStride = rgbDataSet.RasterXSize * 4;

                            var rgbImage = BitmapSource.Create(rgbDataSet.RasterXSize, rgbDataSet.RasterYSize, 96, 96, PixelFormats.Bgra32, null, bgra,
                                rgbStride);

                            // Transformation
                            double[] rgbTransform = new double[6];
                            rgbDataSet.GetGeoTransform(rgbTransform);
                            var vecBuilder = Vector<double>.Build;
                            var upperLeft = vecBuilder.DenseOfArray(new[] { rgbTransform[0], rgbTransform[3], 1 });
                            var meterPerPixel = rgbTransform[1];
                            var xRes = rgbTransform[1];
                            var yRes = rgbTransform[5];
                            var bottomRight = vecBuilder.DenseOfArray(new[] { upperLeft[0] + (rgbDataSet.RasterXSize * xRes), upperLeft[1] + (rgbDataSet.RasterYSize * yRes), 1 });

                            Bands.Insert(0, new BandViewModel("RGB", null, -1, meterPerPixel, new WriteableBitmap(rgbImage), upperLeft, bottomRight, false));
                        });
                    });
                }
            }
        }

        public static int ProgressFunc(double complete, IntPtr message, IntPtr data)
        {
            return 1;
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
