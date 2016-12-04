using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using GalaSoft.MvvmLight.Messaging;
using LandscapeClassifier.Extensions;
using LandscapeClassifier.Model;
using LandscapeClassifier.Model.Classification;
using LandscapeClassifier.Util;
using LandscapeClassifier.View.Open;
using LandscapeClassifier.ViewModel.MainWindow.Classification;
using MahApps.Metro.Controls;
using MathNet.Numerics.LinearAlgebra;
using Microsoft.Win32;
using OSGeo.GDAL;
using ZedGraph;
using ExportPredicitonDialog = LandscapeClassifier.View.Export.ExportPredicitonDialog;
using Point = System.Windows.Point;
using Rectangle = System.Windows.Shapes.Rectangle;

namespace LandscapeClassifier.ViewModel.MainWindow.Prediction
{
    public class PredictionViewModel : ViewModelBase
    {
        private readonly MainWindowViewModel _mainWindowViewModel;

        private LandcoverType _mousePredictionType;
        private double _mousePredictionProbability;

        private bool _isAllPredicted = false;
        private BitmapSource _classificationOverlay;
        private double _overlayOpacity = 0.5d;
        private double _acceptanceProbabilty;

        private int _predictionWidth;
        private int _predictionHeight;
        private int[][] _classification;

        private bool _notBlocking = true;


        private Visibility _progressVisibility = Visibility.Hidden;
        private double _predictionProgress;



        /// <summary>
        /// Predict all.
        /// </summary>
        public ICommand PredictAllCommand { set; get; }

        /// <summary>
        /// Export predictions.
        /// </summary>
        public ICommand ExportPredictionsCommand { set; get; }

        /// <summary>
        /// Executes the majority filter on the classification.
        /// </summary>
        public ICommand ApplyMajorityFilterCommand { get; set; }

        /// <summary>
        /// Assess the accuracy of the algorithm.
        /// </summary>
        public ICommand AssessAccuracyCommand { get; set; }

        /// <summary>
        /// Classification bitmap.
        /// </summary>
        public BitmapSource ClassificationOverlay
        {
            set { _classificationOverlay = value; RaisePropertyChanged(); }
            get { return _classificationOverlay; }
        }

        /// <summary>
        /// Landcover type at mouse position.
        /// </summary>
        public LandcoverType MousePredictionType
        {
            set { _mousePredictionType = value; RaisePropertyChanged(); }
            get { return _mousePredictionType; }
        }

        /// <summary>
        /// Conversion from screen to world coordinates.
        /// </summary>
        public Matrix<double> ScreenToWorld;

        /// <summary>
        /// Conversion from world to screen coordinates.
        /// </summary>
        public Matrix<double> WorldToScreen;

        private Neighborhood _majorityFilterNeighborhood;

        /// <summary>
        /// Opacity overlay.
        /// </summary>
        public double OverlayOpacity
        {
            get { return _overlayOpacity; }
            set { _overlayOpacity = value; RaisePropertyChanged(); }

        }

        /// <summary>
        /// Acceptance probabilty of the underlying machine learning algorithm for a certain land cover type.
        /// </summary>
        public double AcceptanceProbabilty
        {
            get { return _acceptanceProbabilty; }
            set { _acceptanceProbabilty = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// True if all pixels have been predicted.
        /// </summary>
        public bool IsAllPredicted
        {
            get { return _isAllPredicted; }
            set { _isAllPredicted = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// Probabilty of landcover type at mouse position.
        /// </summary>
        public double MousePredictionProbability
        {
            get { return _mousePredictionProbability; }
            set { _mousePredictionProbability = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// Is currently predicting
        /// </summary>
        public bool NotBlocking
        {
            get { return _notBlocking; }
            set { _notBlocking = value; RaisePropertyChanged(); }
        }

        public Visibility ProgressVisibility
        {
            get { return _progressVisibility; }
            set { _progressVisibility = value; RaisePropertyChanged(); }
        }

        public double PredictionProgress
        {
            get { return _predictionProgress; }
            set { _predictionProgress = value; RaisePropertyChanged(); }
        }

        public Neighborhood MajorityFilterNeighborhood
        {
            get { return _majorityFilterNeighborhood; }
            set { _majorityFilterNeighborhood = value; RaisePropertyChanged(); }
        }

        public Vector<double> PredictionUpperLeftWorld { get; set; }

        public Vector<double> PredictionBottomRightWorld { get; set; }




        public PredictionViewModel(MainWindowViewModel mainWindowViewModel)
        {
            _mainWindowViewModel = mainWindowViewModel;

            // TODO nono
            ScreenToWorld = Matrix<double>.Build.DenseOfArray(new[,] { { 1, 0, 300000.0 }, { 0, -1, 5090220 }, { 0, 0, 1 } });
            WorldToScreen = ScreenToWorld.Inverse();

            PredictAllCommand = new RelayCommand(PredictAll, CanPredictAll);
            ExportPredictionsCommand = new RelayCommand(ExportPredictions, CanExportPredictions);
            ApplyMajorityFilterCommand = new RelayCommand(ApplyMajorityFilter, CanApplyMajorityFilter);
            AssessAccuracyCommand = new RelayCommand(AssessAccuracy);
        }



        private bool CanExportPredictions() => NotBlocking && IsAllPredicted;
        private bool CanPredictAll() => NotBlocking;
        private bool CanApplyMajorityFilter() => NotBlocking && IsAllPredicted;

        private void AssessAccuracy()
        {
            var openFileDialog = new OpenFileDialog()
            {
                FilterIndex = 1,
                Title = "Choose Feature File"
            };

            var userClickedOk = openFileDialog.ShowDialog();

            if (userClickedOk == true)
            {
                using (var file = new StreamReader(openFileDialog.FileName))
                {

                    int numLayers = int.Parse(file.ReadLine());


                    for (int i = 0; i < numLayers*8; ++i)
                    {
                        file.ReadLine();
                    }

                    string featureLine;

                    int wrongPredictions = 0;
                    int totalPredictions = 0;

                    int numFeatures = Enum.GetValues(typeof(LandcoverType)).Length - 1;
                    int[,] predictionMatrix = new int[numFeatures, numFeatures];

                    while ((featureLine = file.ReadLine()) != null)
                    {
                        string[] positionTypeIntensities = featureLine.Split(' ');
                        string position = positionTypeIntensities[0];
                        string[] coordinates = position.Split(',');

                        var type = (LandcoverType) Enum.Parse(typeof(LandcoverType), positionTypeIntensities[1]);
                        var featurePosition = Vector<double>.Build.DenseOfArray(new[] {double.Parse(coordinates[0]), double.Parse(coordinates[1]), 1.0});
                        var featureBands = _mainWindowViewModel.Layers.Where(b => b.IsFeature).OrderBy(b => b.Name).ToList();

                        ushort[] bandPixels = new ushort[featureBands.Count];

                        for (int bandIndex = 0; bandIndex < featureBands.Count; ++bandIndex)
                        {
                            var band = featureBands[bandIndex];
                            var bandPixelPosition = band.WorldToImage * featurePosition;

                            ushort bandPixelValue = band.BandImage.GetScaledToUshort((int)bandPixelPosition[0], (int)bandPixelPosition[1]);

                            bandPixels[bandIndex] = bandPixelValue;
                        }

                        

                        var predictedType = _mainWindowViewModel.ClassifierViewModel.CurrentClassifierViewModel.Classifier.Predict(new FeatureVector(bandPixels));
                        if (predictedType != type) wrongPredictions++;

                        predictionMatrix[(int)type, (int)predictedType]++;


                        totalPredictions++;
                    }

                    Console.WriteLine("Total predictions: " + totalPredictions);
                    Console.WriteLine("Wrong predictions: " + wrongPredictions);
                    for (int i = 0; i < numFeatures; ++i)
                    {
                        for (int j = 0; j < numFeatures; ++j)
                        {
                            Console.Write(predictionMatrix[i,j] + ", ");
                        }
                        Console.Write(Environment.NewLine);
                    }
                }
            }
        }

        private void ApplyMajorityFilter()
        {
            NotBlocking = false;


            int[][] filtered = new int[_predictionHeight][];


            var majorityFilter = Task.Factory.StartNew(() => Parallel.For(0, _predictionHeight, y =>
            {
                int[] line = new int[_predictionWidth];
                int[] neighbors = new int[Enum.GetValues(typeof(LandcoverType)).Length - 1];

                for (int x = 0; x < _predictionWidth; ++x)
                {
                    if (y > 0 && x > 0 && y < _predictionHeight - 1 && x < _predictionWidth - 1)
                    {
                        for (int i = 0; i < neighbors.Length; ++i) neighbors[i] = 0;

                        neighbors[_classification[y - 1][x - 1]]++;
                        neighbors[_classification[y - 1][x + 0]]++;
                        neighbors[_classification[y - 1][x + 1]]++;
                        neighbors[_classification[y + 0][x - 1]]++;
                        neighbors[_classification[y + 0][x + 0]]++;
                        neighbors[_classification[y + 0][x + 1]]++;
                        neighbors[_classification[y + 1][x - 1]]++;
                        neighbors[_classification[y + 1][x + 0]]++;
                        neighbors[_classification[y + 1][x + 1]]++;

                        int biggest = neighbors[0];
                        int biggestIndex = 0;
                        for (int index = 0; index < neighbors.Length; index++)
                        {
                            if (neighbors[index] > biggest)
                            {
                                biggest = neighbors[index];
                                biggestIndex = index;
                            }
                        }

                        line[x] = biggestIndex;
                    }
                    else
                    {
                        line[x] = _classification[y][x];
                    }
                }

                filtered[y] = line;
            }));

            majorityFilter.ContinueWith(t =>
            {
                _classification = filtered;

                int stride = _predictionWidth * 4;
                int size = _predictionHeight * stride;
                byte[] imageData = new byte[size];

                Parallel.For(0, _predictionWidth, x =>
                {
                    for (int y = 0; y < _predictionHeight; ++y)
                    {
                        int index = 4 * y * _predictionWidth + 4 * x;
                        LandcoverType type = (LandcoverType)_classification[y][x];
                        var color = type.GetColor();
                        imageData[index + 0] = color.B;
                        imageData[index + 1] = color.G;
                        imageData[index + 2] = color.R;
                        imageData[index + 3] = color.A;
                    }
                });

                Application.Current.Invoke(() =>
                {
                    ClassificationOverlay = BitmapSource.Create(_predictionWidth, _predictionHeight, 96, 96, PixelFormats.Bgra32, null, imageData, stride);
                    NotBlocking = true;
                    ProgressVisibility = Visibility.Hidden;
                });
            });
        }

        private void ExportPredictions()
        {
            ExportPredicitonDialog dialog = new ExportPredicitonDialog();

            if (dialog.ShowDialog(_mainWindowViewModel.Layers.Where(l => l.Path != null).ToList()) == true)
            {
                var layers = dialog.DialogViewModel.ExportLayers;

                var width = _classificationOverlay.PixelWidth;
                var stride = width * 4;
                var height = _classificationOverlay.PixelHeight;
                var layerData = layers.Select(l => new byte[stride * height]).ToList();

                NotBlocking = false;

                // Create layers
                List<Task> createLayerTasks = new List<Task>();
                for (int layerIndex = 0; layerIndex < layers.Count; ++layerIndex)
                {
                    var layer = layers[layerIndex];
                    var types = layer.LandCoverTypes;
                    var constLayer = layerIndex;

                    createLayerTasks.Add(Task.Factory.StartNew(() => Parallel.ForEach(Partitioner.Create(0, height), (range) =>
                    {
                        for (int y = range.Item1; y < range.Item2; ++y)
                        {
                            for (int x = 0; x < width; ++x)
                            {
                                LandcoverType prediction = (LandcoverType)_classification[y][x];

                                var color = types[(int)prediction] ? (byte)0 : (byte)255;

                                int dataIndex = y * stride + x * 4;
                                layerData[constLayer][dataIndex + 0] = color;
                                layerData[constLayer][dataIndex + 1] = color;
                                layerData[constLayer][dataIndex + 2] = color;
                                layerData[constLayer][dataIndex + 3] = color;
                            }
                        }

                    })));
                }

                // Write results
                List<Task> writeResultTasks = new List<Task>();
                if (dialog.DialogViewModel.ExportHeightmap)
                {
                    var layer = dialog.DialogViewModel.HeightmapLayer;

                    var upperLeftImage = layer.WorldToImage * PredictionUpperLeftWorld;
                    var bottomRightImage = layer.WorldToImage * PredictionBottomRightWorld;
                    int upperLeftX = (int)upperLeftImage[0];
                    int upperLeftY = (int)upperLeftImage[1];
                    int bottomLeftX = (int)bottomRightImage[0];
                    int bottomLeftY = (int)bottomRightImage[1];
                    int heightmapExportWidth = bottomLeftX - upperLeftX;
                    int heightmapExportHeight = bottomLeftY - upperLeftY;

                    var dataSet = Gdal.Open(layer.Path, Access.GA_ReadOnly);
                    var rasterBand = dataSet.GetRasterBand(1);

                    var bitsPerPixel = rasterBand.DataType.ToPixelFormat().BitsPerPixel;
                    int originalStride = (rasterBand.XSize * bitsPerPixel + 7) / 8;
                    IntPtr originalPtr = Marshal.AllocHGlobal(originalStride * rasterBand.YSize);

                    rasterBand.ReadRaster(0, 0, rasterBand.XSize, rasterBand.YSize, originalPtr, rasterBand.XSize,
                        rasterBand.YSize, rasterBand.DataType, bitsPerPixel / 8, originalStride);

                    double[] minMax = new double[2];
                    rasterBand.ComputeRasterMinMax(minMax, 0);
                    double minAltitude = minMax[0];
                    double maxAltitude = minMax[1];

                    double noDataValue;
                    int hasValue;
                    rasterBand.GetNoDataValue(out noDataValue, out hasValue);

                    var targetFormat = PixelFormats.Gray16;
                    int transformedStride = (heightmapExportWidth * targetFormat.BitsPerPixel + 7) / 8;
                    var transformedPtr = Marshal.AllocHGlobal(transformedStride * heightmapExportHeight);

                    var transform = Task.Factory.StartNew(() => Parallel.ForEach(Partitioner.Create(0, heightmapExportHeight), (range) =>
                    {
                        unsafe
                        {
                            // TODO check if is in bounds
                            float* original = (float*)originalPtr.ToPointer();
                            ushort* transformed = (ushort*)transformedPtr.ToPointer();
                            for (int y = upperLeftY + range.Item1; y < upperLeftY + range.Item2; ++y)
                            {
                                for (int x = upperLeftX; x < bottomLeftX; ++x)
                                {
                                    int originalOffset = y * layer.ImagePixelWidth + x;
                                    int transformedOffset = (y - upperLeftY) * heightmapExportWidth + (x - upperLeftX);
                                    float value = *(original + originalOffset);

                                    if (Math.Abs(value - noDataValue) < 0.0001)
                                    {
                                        *(transformed + transformedOffset) = 0;
                                    }
                                    else
                                    {
                                        ushort scaled = (ushort)Math.Min((value - minAltitude) / (maxAltitude - minAltitude) * ushort.MaxValue, ushort.MaxValue);
                                        *(transformed + transformedOffset) = scaled;
                                    }
                                }
                            }
                        }
                    }));

                    var writeToDisk = transform.ContinueWith(t =>
                    {
                        GdalUtil.WritePng(transformedPtr, heightmapExportWidth, heightmapExportHeight,
                            Path.Combine(dialog.DialogViewModel.ExportPath, "heightmap.png"));
                        dataSet.Dispose();
                    });
                    writeResultTasks.Add(writeToDisk);
                }


                writeResultTasks.Add(Task.WhenAll(createLayerTasks).ContinueWith(t =>
                {
                    for (int layerIndex = 0; layerIndex < layerData.Count; ++layerIndex)
                    {
                        var layer = layers[layerIndex];
                        var data = layerData[layerIndex];
                        var bitmapImage = BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgra32, null, data, stride);

                        // write layer
                        using (var fileStream = new FileStream(Path.Combine(dialog.DialogViewModel.ExportPath, layer.Name),
                                    FileMode.Create))
                        {
                            BitmapEncoder encoder = new PngBitmapEncoder();
                            encoder.Frames.Add(BitmapFrame.Create(bitmapImage));
                            encoder.Save(fileStream);
                        }
                    }
                }));

                // Finish
                Task.WhenAll(writeResultTasks).ContinueWith(t =>
                {
                    Application.Current.Dispatcher.Invoke(() => NotBlocking = true);
                });

            }
        }

        private void PredictAll()
        {
            IsAllPredicted = false;
            NotBlocking = false;
            ProgressVisibility = Visibility.Visible;
            PredictionProgress = 0.0;

            var featureBands = _mainWindowViewModel.Layers.Where(f => f.IsFeature).ToList();
            var numFeatures = featureBands.Count;

            var bounds = new List<Rect>();
            foreach (var layerViewModel in featureBands)
            {
                var screenUpperLeft = WorldToScreen * layerViewModel.UpperLeftWorld;
                var screenBottomRight = WorldToScreen * layerViewModel.BottomRightWorld;
                float x = (float)screenUpperLeft[0];
                float y = (float)screenUpperLeft[1];
                float width = (float)(screenBottomRight[0] - screenUpperLeft[0]);
                float height = (float)(screenBottomRight[1] - screenUpperLeft[1]);

                bounds.Add(new Rect(x, y, width, height));
            }

            var intersection = bounds.Aggregate(Rect.Intersect);

            var upperLeftWorld = ScreenToWorld * Vector<double>.Build.DenseOfArray(new[] { intersection.X, intersection.Y, 1.0 });
            var bottomRightWorld = ScreenToWorld * Vector<double>.Build.DenseOfArray(new[] { intersection.X + intersection.Width, intersection.Y + intersection.Height, 1.0 });

            PredictionUpperLeftWorld = upperLeftWorld;
            PredictionBottomRightWorld = bottomRightWorld;

            var scaleBand = featureBands.OrderBy(b => b.ScaleX).First();

            _predictionWidth = (int)(scaleBand.WorldToImage * bottomRightWorld)[0];
            _predictionHeight = (int)(scaleBand.WorldToImage * bottomRightWorld)[1];

            IntPtr[] data = featureBands.Select(b => b.BandImage.BackBuffer).ToArray();
            _classification = new int[_predictionHeight][];

            Task predict = Task.Factory.StartNew(() => Parallel.ForEach(Partitioner.Create(0, _predictionHeight), range =>
            {
                double[][] features = new double[_predictionWidth][];

                for (int i = 0; i < _predictionWidth; ++i)
                {
                    features[i] = new double[numFeatures];
                }

                for (int line = range.Item1; line < range.Item2; ++line)
                {
                    for (int bandIndex = 0; bandIndex < featureBands.Count; ++bandIndex)
                    {
                        var band = featureBands[bandIndex];

                        var transform = band.WorldToImage * scaleBand.ImageToWorld;

                        unsafe
                        {
                            if (featureBands[bandIndex].Format == PixelFormats.Gray16)
                            {
                                ushort* dataPtr = (ushort*)data[bandIndex].ToPointer();
                                for (int x = 0; x < _predictionWidth; ++x)
                                {
                                    var pixelPosition = transform * Vector<double>.Build.DenseOfArray(new[] { x, line, 1.0 });
                                    int pixelX = (int)pixelPosition[0];
                                    int pixelY = (int)pixelPosition[1];

                                    var pixelValue = *(dataPtr + pixelY * band.ImagePixelWidth + pixelX);
                                    features[x][bandIndex] = (double)pixelValue / ushort.MaxValue;
                                }
                            }
                            else if (featureBands[bandIndex].Format == PixelFormats.Gray32Float)
                            {
                                float* dataPtr = (float*)data[bandIndex].ToPointer();
                                for (int x = 0; x < _predictionWidth; ++x)
                                {
                                    var pixelPosition = transform * Vector<double>.Build.DenseOfArray(new[] { x, line, 1.0 });
                                    int pixelX = (int)pixelPosition[0];
                                    int pixelY = (int)pixelPosition[1];

                                    var pixelValue = *(dataPtr + pixelY * band.ImagePixelWidth + pixelX);
                                    features[x][bandIndex] = pixelValue;
                                }
                            }
                        }
                    }
                    _classification[line] = _mainWindowViewModel.ClassifierViewModel.CurrentClassifierViewModel.Classifier.Predict(features);

                    lock (this)
                    {
                        PredictionProgress += 100.0 / _predictionHeight;
                    }
                }

            }));

            predict.ContinueWith(t =>
            {
                int stride = _predictionWidth * 4;
                int size = _predictionHeight * stride;
                byte[] imageData = new byte[size];

                Parallel.For(0, _predictionWidth, x =>
                {
                    for (int y = 0; y < _predictionHeight; ++y)
                    {
                        int index = 4 * y * _predictionWidth + 4 * x;
                        LandcoverType type = (LandcoverType)_classification[y][x];
                        var color = type.GetColor();
                        imageData[index + 0] = color.B;
                        imageData[index + 1] = color.G;
                        imageData[index + 2] = color.R;
                        imageData[index + 3] = color.A;
                    }
                });

                Application.Current.Dispatcher.Invoke(() =>
                {
                    ClassificationOverlay = BitmapSource.Create(_predictionWidth, _predictionHeight, 96, 96, PixelFormats.Bgra32, null, imageData, stride);
                    IsAllPredicted = true;
                    NotBlocking = true;
                    ProgressVisibility = Visibility.Hidden;
                });
            });
        }


    }
}
