using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Accord.Statistics.Analysis;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using LandscapeClassifier.Extensions;
using LandscapeClassifier.Model;
using LandscapeClassifier.Model.Classification;
using LandscapeClassifier.Util;
using LandscapeClassifier.View.Export;
using LandscapeClassifier.ViewModel.MainWindow.Classification;
using MahApps.Metro.Controls;
using MathNet.Numerics.LinearAlgebra;
using Microsoft.Win32;
using OSGeo.GDAL;

namespace LandscapeClassifier.ViewModel.MainWindow.Prediction
{
    public class PredictionViewModel : ViewModelBase
    {
        private readonly int[] _sectionSizes = { 7, 15, 31, 63, 127, 255 };
        private readonly int[] _numSections = { 1, 2 };

        private readonly MainWindowViewModel _mainWindowViewModel;
        private double _acceptanceProbabilty;
        private int[][] _classification;
        private BitmapSource _classificationOverlay;

        private bool _isAllPredicted;

        private Neighborhood _majorityFilterNeighborhood;
        private double _mousePredictionProbability;

        private LandcoverType _mousePredictionType;

        private bool _notBlocking = true;
        private double _overlayOpacity = 0.5d;
        private int _predictionHeight;
        private double _predictionProgress;

        private int _predictionWidth;
        private double[][][] _probabilities;

        private Visibility _progressVisibility = Visibility.Hidden;

        /// <summary>
        ///     Conversion from screen to world coordinates.
        /// </summary>
        public Matrix<double> ScreenToWorld;

        /// <summary>
        ///     Conversion from world to screen coordinates.
        /// </summary>
        public Matrix<double> WorldToScreen;

        private bool _predictProbabilities;


        public PredictionViewModel(MainWindowViewModel mainWindowViewModel)
        {
            _mainWindowViewModel = mainWindowViewModel;

            // TODO nono
            ScreenToWorld = Matrix<double>.Build.DenseOfArray(new[,] { { 1, 0, 300000.0 }, { 0, -1, 5090220 }, { 0, 0, 1 } });
            WorldToScreen = ScreenToWorld.Inverse();

            PredictAllCommand = new RelayCommand(PredictAll, CanPredictAll);
            ExportPredictionsCommand = new RelayCommand(ExportPredictionsAsync, CanExportPredictions);
            ApplyMajorityFilterCommand = new RelayCommand(ApplyMajorityFilter, CanApplyMajorityFilter);
            AssessAccuracyCommand = new RelayCommand(AssessAccuracy);
        }


        /// <summary>
        ///     Predict all.
        /// </summary>
        public ICommand PredictAllCommand { set; get; }

        /// <summary>
        ///     Export predictions.
        /// </summary>
        public ICommand ExportPredictionsCommand { set; get; }

        /// <summary>
        ///     Executes the majority filter on the classification.
        /// </summary>
        public ICommand ApplyMajorityFilterCommand { get; set; }

        /// <summary>
        ///     Assess the accuracy of the algorithm.
        /// </summary>
        public ICommand AssessAccuracyCommand { get; set; }

        /// <summary>
        ///     Classification bitmap.
        /// </summary>
        public BitmapSource ClassificationOverlay
        {
            set
            {
                _classificationOverlay = value;
                RaisePropertyChanged();
            }
            get { return _classificationOverlay; }
        }

        /// <summary>
        ///     Landcover type at mouse position.
        /// </summary>
        public LandcoverType MousePredictionType
        {
            set
            {
                _mousePredictionType = value;
                RaisePropertyChanged();
            }
            get { return _mousePredictionType; }
        }

        /// <summary>
        ///     Opacity overlay.
        /// </summary>
        public double OverlayOpacity
        {
            get { return _overlayOpacity; }
            set
            {
                _overlayOpacity = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        ///     Acceptance probabilty of the underlying machine learning algorithm for a certain land cover type.
        /// </summary>
        public double AcceptanceProbabilty
        {
            get { return _acceptanceProbabilty; }
            set
            {
                _acceptanceProbabilty = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        ///     True if all pixels have been predicted.
        /// </summary>
        public bool IsAllPredicted
        {
            get { return _isAllPredicted; }
            set
            {
                _isAllPredicted = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        ///     Probabilty of landcover type at mouse position.
        /// </summary>
        public double MousePredictionProbability
        {
            get { return _mousePredictionProbability; }
            set
            {
                _mousePredictionProbability = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        ///     Is currently predicting
        /// </summary>
        public bool NotBlocking
        {
            get { return _notBlocking; }
            set
            {
                _notBlocking = value;
                RaisePropertyChanged();
            }
        }


        public bool PredictProbabilities
        {
            get { return _predictProbabilities; }
            set
            {
                _predictProbabilities = value;
                RaisePropertyChanged();
            }
        }


        public Visibility ProgressVisibility
        {
            get { return _progressVisibility; }
            set
            {
                _progressVisibility = value;
                RaisePropertyChanged();
            }
        }

        public double PredictionProgress
        {
            get { return _predictionProgress; }
            set
            {
                _predictionProgress = value;
                RaisePropertyChanged();
            }
        }

        public Neighborhood MajorityFilterNeighborhood
        {
            get { return _majorityFilterNeighborhood; }
            set
            {
                _majorityFilterNeighborhood = value;
                RaisePropertyChanged();
            }
        }

        public Vector<double> PredictionUpperLeftWorld { get; set; }

        public Vector<double> PredictionBottomRightWorld { get; set; }

        private bool CanExportPredictions() => NotBlocking && IsAllPredicted;
        private bool CanPredictAll() => NotBlocking;
        private bool CanApplyMajorityFilter() => NotBlocking && IsAllPredicted;

        private void AssessAccuracy()
        {

        }

        private void ApplyMajorityFilter()
        {
            NotBlocking = false;


            var filtered = new int[_predictionHeight][];


            var majorityFilter = Task.Factory.StartNew(() => Parallel.For(0, _predictionHeight, y =>
            {
                var line = new int[_predictionWidth];
                var neighbors = new int[Enum.GetValues(typeof(LandcoverType)).Length - 1];

                for (var x = 0; x < _predictionWidth; ++x)
                {
                    if (y > 0 && x > 0 && y < _predictionHeight - 1 && x < _predictionWidth - 1)
                    {
                        for (var i = 0; i < neighbors.Length; ++i) neighbors[i] = 0;

                        neighbors[_classification[y - 1][x - 1]]++;
                        neighbors[_classification[y - 1][x + 0]]++;
                        neighbors[_classification[y - 1][x + 1]]++;
                        neighbors[_classification[y + 0][x - 1]]++;
                        neighbors[_classification[y + 0][x + 0]]++;
                        neighbors[_classification[y + 0][x + 1]]++;
                        neighbors[_classification[y + 1][x - 1]]++;
                        neighbors[_classification[y + 1][x + 0]]++;
                        neighbors[_classification[y + 1][x + 1]]++;

                        var biggest = neighbors[0];
                        var biggestIndex = 0;
                        for (var index = 0; index < neighbors.Length; index++)
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

                var stride = _predictionWidth * 4;
                var size = _predictionHeight * stride;
                var imageData = new byte[size];

                Parallel.For(0, _predictionWidth, x =>
                {
                    for (var y = 0; y < _predictionHeight; ++y)
                    {
                        var index = 4 * y * _predictionWidth + 4 * x;
                        var type = (LandcoverType)_classification[y][x];
                        var color = type.GetColor();
                        imageData[index + 0] = color.B;
                        imageData[index + 1] = color.G;
                        imageData[index + 2] = color.R;
                        imageData[index + 3] = color.A;
                    }
                });

                Application.Current.Invoke(() =>
                {
                    ClassificationOverlay = BitmapSource.Create(_predictionWidth, _predictionHeight, 96, 96,
                        PixelFormats.Bgra32, null, imageData, stride);
                    NotBlocking = true;
                    ProgressVisibility = Visibility.Hidden;
                });
            });
        }

        private async void ExportPredictionsAsync()
        {
            var dialog = new ExportPredicitonDialog(PredictProbabilities, _mainWindowViewModel.Layers.ToList());

            if (dialog.ShowDialog() == true)
            {
                var exportPath = dialog.DialogViewModel.ExportPath;
                var layers = dialog.DialogViewModel.ExportLandCoverLayers;

                var width = _classificationOverlay.PixelWidth;
                var stride = width;
                var height = _classificationOverlay.PixelHeight;

                var exportTasks = new List<Task>();

                NotBlocking = false;

                var scaleToUnrealLandscape = dialog.DialogViewModel.ScaleToUnrealLandscape;

                var layerTasks = new List<Task<Tuple<int, ushort[]>>>();

                // Create landcovertype layers
                for (var layerIndex = 0; layerIndex < layers.Count; ++layerIndex)
                {
                    var layer = layers[layerIndex];
                    var types = layer.LandCoverTypes;
                    var selectedIndices = layer.SelectedTypeIndices;
                    var constLayerIndex = layerIndex;

                    layerTasks.Add(dialog.DialogViewModel.ExportAsProbabilities
                        ? LoadLayerProbabilisticAsync(dialog, width, stride, height, selectedIndices, constLayerIndex)
                        : LoadLayerAsync(width, stride, height, types, constLayerIndex));
                }

                // TODO what if does not exist?
                var heightMapLayer = dialog.DialogViewModel.ExportLayers.First(l => l.IsHeightmap).Layer;

                int exportWidth, exportHeight, imageWidth, imageHeight;
                CalculateHeightmapExportDimensions(scaleToUnrealLandscape, heightMapLayer, out exportWidth, out exportHeight, out imageWidth, out imageHeight);

                // Export layers
                foreach (var exportLayer in dialog.DialogViewModel.ExportLayers)
                {
                    if (exportLayer.Export)
                    {
                        var layer = exportLayer.Layer;

                        var upperLeftImage = layer.WorldToImage * PredictionUpperLeftWorld;
                        var bottomRightImage = layer.WorldToImage * PredictionBottomRightWorld;
                        var upperLeftX = (int)upperLeftImage[0];
                        var upperLeftY = (int)upperLeftImage[1];
                        var bottomLeftX = (int)bottomRightImage[0];
                        var bottomLeftY = (int)bottomRightImage[1];

                        int predictionWidth = bottomLeftX - upperLeftX;
                        int predictionHeight = bottomLeftY - upperLeftY;

                        var layerImage = layer.BandImage;

                        // TODO possible loss of precision
                        var cropped = layerImage.Crop(upperLeftX, upperLeftY, predictionWidth, predictionHeight);
                        // var scaled = cropped.Scale(exportWidth, exportHeight);
                        var resized = cropped.Resize(imageWidth, imageHeight);
                        var formatChanged = resized.ConvertFormat(exportLayer.Format);

                        BitmapFrame.Create(cropped).SaveAsPng(Path.Combine(exportPath, layer.Name + "_cropped.png"));
                        // BitmapFrame.Create(scaled).SaveAsPng(Path.Combine(exportPath, layer.Name + "_scaled.png"));
                        BitmapFrame.Create(resized).SaveAsPng(Path.Combine(exportPath, layer.Name + "_resized.png"));
                        BitmapFrame.Create(formatChanged).SaveAsPng(Path.Combine(exportPath, layer.Name + ".png"));
                    }
                }

                // Export prediction layers
                var layersData = await Task.WhenAll(layerTasks);
                var outputLayers = layersData.OrderBy(t => t.Item1).Select(t => t.Item2).ToList();

                for (var layerIndex = 0; layerIndex < outputLayers.Count; ++layerIndex)
                {
                    var layer = layers[layerIndex];
                    var data = outputLayers[layerIndex];


                    var bitmap = BitmapSource.Create(width, height, 96, 96, PixelFormats.Gray16, null, data, width * 2);

                    // Save layer as Gray16
                    if (scaleToUnrealLandscape)
                    {
                        var scaled = bitmap.Scale(exportWidth, exportHeight);
                        var resized = scaled.Resize(imageWidth, imageHeight);
                        var formatChanged = resized.ConvertFormat(PixelFormats.Gray16);
                        BitmapFrame.Create(formatChanged).SaveAsPng(Path.Combine(exportPath, layer.Name));
                    }
                    else
                    {
                        BitmapFrame.Create(bitmap).SaveAsPng(Path.Combine(exportPath, layer.Name));
                    }
                }


                // Finish
                Task.WaitAll(exportTasks.ToArray());

                NotBlocking = true;
            }
        }

        private void CalculateHeightmapExportDimensions(bool scaleToUnrealLandscape, LayerViewModel heightMapLayer, out int exportWidth, out int exportHeight, out int imageWidth, out int imageHeight)
        {
            var upperLeftImage = heightMapLayer.WorldToImage * PredictionUpperLeftWorld;
            var bottomRightImage = heightMapLayer.WorldToImage * PredictionBottomRightWorld;
            var upperLeftX = (int)upperLeftImage[0];
            var upperLeftY = (int)upperLeftImage[1];
            var bottomLeftX = (int)bottomRightImage[0];
            var bottomLeftY = (int)bottomRightImage[1];

            int predictionWidth = bottomLeftX - upperLeftX;
            int predictionHeight = bottomLeftY - upperLeftY;
            imageWidth = predictionWidth;
            imageHeight = predictionHeight;

            if (scaleToUnrealLandscape)
            {
                var outputSize = ComputeUnrealLandscapeSize(new Point2D(predictionWidth, predictionHeight));
                imageWidth = outputSize.X;
                imageHeight = outputSize.Y;
            }

            exportWidth = Math.Min(predictionWidth, imageWidth);
            exportHeight = Math.Min(predictionHeight, imageHeight);
        }

        private async Task<Tuple<int, ushort[]>> LoadLayerProbabilisticAsync(ExportPredicitonDialog dialog, int width, int stride, int height, int[] selectedIndices, int constLayerIndex)
        {
            return await Task.Run(() =>
            {
                var layerData = new ushort[stride * height];
                Parallel.ForEach(Partitioner.Create(0, height), range =>
                {
                    for (var y = range.Item1; y < range.Item2; ++y)
                    {
                        for (var x = 0; x < width; ++x)
                        {
                            double totalProbability = 0;
                            foreach (int selectedIndex in selectedIndices)
                            {
                                var probability = _probabilities[y][x][selectedIndex];
                                if (probability >= dialog.DialogViewModel.MinAcceptanceProbability)
                                    totalProbability += probability;
                            }

                            totalProbability = Math.Min(totalProbability, 1);

                            var dataIndex = y * stride + x;
                            layerData[dataIndex] = (ushort)(totalProbability * ushort.MaxValue);
                        }
                    }
                });

                return new Tuple<int, ushort[]>(constLayerIndex, layerData);
            });
        }

        private async Task<Tuple<int, ushort[]>> LoadLayerAsync(int width, int stride, int height, bool[] types, int constLayerIndex)
        {
            return await Task.Run(() =>
            {
                var layerData = new ushort[stride * height];
                Parallel.ForEach(Partitioner.Create(0, height), range =>
                {
                    for (var y = range.Item1; y < range.Item2; ++y)
                    {
                        for (var x = 0; x < width; ++x)
                        {
                            var prediction = (LandcoverType)_classification[y][x];

                            var color = types[(int)prediction] ? ushort.MaxValue : (ushort)0;

                            var dataIndex = y * stride + x;
                            layerData[dataIndex] = color;
                        }
                    }
                });

                return new Tuple<int, ushort[]>(constLayerIndex, layerData);
            });
        }
        /// <summary>
        /// Based on https://github.com/EpicGames/UnrealEngine/blob/55c9f3ba0010e2e483d49a4cd378f36a46601fad/Engine/Source/Editor/LandscapeEditor/Private/LandscapeEditorDetailCustomization_NewLandscape.cpp#L1193
        /// </summary>
        private Point2D ComputeUnrealLandscapeSize(Point2D input)
        {
            int width = input.X;
            int height = input.Y;

            if (width > 0 && height > 0)
            {
                int componentsX;
                int componentsY;

                // Try to find a section size and number of sections that exactly matches the dimensions of the heightfield
                for (int sectionSizesIndex = _sectionSizes.Length - 1; sectionSizesIndex >= 0; sectionSizesIndex--)
                {
                    for (int numSectionsIndex = _numSections.Length - 1; numSectionsIndex >= 0; numSectionsIndex--)
                    {
                        int ss = _sectionSizes[sectionSizesIndex];
                        int ns = _numSections[numSectionsIndex];

                        if (((width - 1) % (ss * ns)) == 0 && ((width - 1) / (ss * ns)) <= 32 &&
                            ((height - 1) % (ss * ns)) == 0 && ((height - 1) / (ss * ns)) <= 32)
                        {
                            componentsX = (width - 1) / (ss * ns);
                            componentsY = (height - 1) / (ss * ns);
                            return new Point2D(componentsX * 255 + 1, componentsY * 255 + 1);
                        }
                    }
                }

                const int currentSectionSize = 63;
                const int currentNumSections = 1;

                for (int sectionSizesIdx = 0; sectionSizesIdx < _sectionSizes.Length; sectionSizesIdx++)
                {
                    if (_sectionSizes[sectionSizesIdx] < currentSectionSize)
                    {
                        continue;
                    }

                    componentsX = (int)Math.Ceiling((double)(width - 1) / _sectionSizes[sectionSizesIdx] * currentNumSections);
                    componentsY = (int)Math.Ceiling((double)(height - 1) / _sectionSizes[sectionSizesIdx] * currentNumSections);

                    if (componentsX <= 32 && componentsY <= 32)
                    {
                        return new Point2D(componentsX * 255 + 1, componentsY * 255 + 1);
                    }
                }

                // if the heightmap is very large, fall back to using the largest values we support
                int maxSectionSize = _sectionSizes[_sectionSizes.Length - 1];
                int maxNumSubSections = _numSections[_numSections.Length - 1];
                componentsX = (int)Math.Ceiling((double)(width - 1) / maxSectionSize * maxNumSubSections);
                componentsY = (int)Math.Ceiling((double)(height - 1) / maxSectionSize * maxNumSubSections);

                return new Point2D(componentsX * 255 + 1, componentsY * 255 + 1);
            }

            return new Point2D(0, 0);
        }


        private void PredictAll()
        {
            IsAllPredicted = false;
            NotBlocking = false;
            ProgressVisibility = Visibility.Visible;
            PredictionProgress = 0.0;

            var featureBands = _mainWindowViewModel.Layers.Where(f => f.UseFeature).ToList();
            var numFeatures = featureBands.Count;

            var bounds = new List<Rect>();
            foreach (var layerViewModel in featureBands)
            {
                var screenUpperLeft = WorldToScreen * layerViewModel.UpperLeftWorld;
                var screenBottomRight = WorldToScreen * layerViewModel.BottomRightWorld;
                var x = (float)screenUpperLeft[0];
                var y = (float)screenUpperLeft[1];
                var width = (float)(screenBottomRight[0] - screenUpperLeft[0]);
                var height = (float)(screenBottomRight[1] - screenUpperLeft[1]);

                bounds.Add(new Rect(x, y, width, height));
            }

            var intersection = bounds.Aggregate(Rect.Intersect);

            var upperLeftWorld = ScreenToWorld *
                                 Vector<double>.Build.DenseOfArray(new[] { intersection.X, intersection.Y, 1.0 });
            var bottomRightWorld = ScreenToWorld *
                                   Vector<double>.Build.DenseOfArray(new[]
                                   {intersection.X + intersection.Width, intersection.Y + intersection.Height, 1.0});

            PredictionUpperLeftWorld = upperLeftWorld;
            PredictionBottomRightWorld = bottomRightWorld;

            var scaleBand = featureBands.OrderBy(b => b.ScaleX).First();

            _predictionWidth = (int)(scaleBand.WorldToImage * bottomRightWorld)[0];
            _predictionHeight = (int)(scaleBand.WorldToImage * bottomRightWorld)[1];

            var data = featureBands.Select(b => b.BandImage.BackBuffer).ToArray();
            _classification = new int[_predictionHeight][];

            if (PredictProbabilities) _probabilities = new double[_predictionHeight][][];

            Task predict =
                Task.Factory.StartNew(() => Parallel.ForEach(Partitioner.Create(0, _predictionHeight), range =>
                {
                    var features = new double[_predictionWidth][];

                    for (var i = 0; i < _predictionWidth; ++i)
                    {
                        features[i] = new double[numFeatures];
                    }

                    for (var line = range.Item1; line < range.Item2; ++line)
                    {
                        for (var bandIndex = 0; bandIndex < featureBands.Count; ++bandIndex)
                        {
                            var band = featureBands[bandIndex];

                            var transform = band.WorldToImage * scaleBand.ImageToWorld;

                            unsafe
                            {
                                if (featureBands[bandIndex].Format == PixelFormats.Gray16)
                                {
                                    var dataPtr = (ushort*)data[bandIndex].ToPointer();
                                    for (var x = 0; x < _predictionWidth; ++x)
                                    {
                                        var pixelPosition = transform * Vector<double>.Build.DenseOfArray(new[] { x, line, 1.0 });
                                        var pixelX = (int)pixelPosition[0];
                                        var pixelY = (int)pixelPosition[1];

                                        var pixelValue = *(dataPtr + pixelY * band.ImagePixelWidth + pixelX);
                                        features[x][bandIndex] = (double)pixelValue / ushort.MaxValue;
                                    }
                                }
                                else if (featureBands[bandIndex].Format == PixelFormats.Gray32Float)
                                {
                                    var dataPtr = (float*)data[bandIndex].ToPointer();
                                    for (var x = 0; x < _predictionWidth; ++x)
                                    {
                                        var pixelPosition = transform *
                                                            Vector<double>.Build.DenseOfArray(new[] { x, line, 1.0 });
                                        var pixelX = (int)pixelPosition[0];
                                        var pixelY = (int)pixelPosition[1];

                                        var pixelValue = *(dataPtr + pixelY * band.ImagePixelWidth + pixelX);
                                        features[x][bandIndex] = pixelValue;
                                    }
                                }
                            }
                        }


                        _classification[line] = _mainWindowViewModel.ClassifierViewModel.CurrentClassifierViewModel.Predict(features);

                        if (PredictProbabilities) _probabilities[line] = _mainWindowViewModel.ClassifierViewModel.CurrentClassifierViewModel.Probabilities(features);

                        lock (this)
                        {
                            PredictionProgress += 100.0 / _predictionHeight;
                        }
                    }
                }));

            predict.ContinueWith(t =>
            {
                var stride = _predictionWidth * 4;
                var size = _predictionHeight * stride;
                var imageData = new byte[size];

                Parallel.For(0, _predictionWidth, x =>
                {
                    for (var y = 0; y < _predictionHeight; ++y)
                    {
                        var index = 4 * y * _predictionWidth + 4 * x;
                        var type = (LandcoverType)_classification[y][x];
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

    struct LoadedImageData
    {
        public readonly Task<IntPtr> ImageDataTask;
        public readonly int ImageExportWidth;
        public readonly int ImageExportHeight;
        public readonly int ImageWidth;
        public readonly int ImageHeight;
        public readonly int Stride;

        public LoadedImageData(Task<IntPtr> imageDataTask, int imageExportWidth, int imageExportHeight, int imageWidth, int imageHeight, int stride)
        {
            ImageDataTask = imageDataTask;
            ImageExportWidth = imageExportWidth;
            ImageExportHeight = imageExportHeight;
            ImageWidth = imageWidth;
            ImageHeight = imageHeight;
            Stride = stride;
        }
    }

    struct Point2D
    {
        public int X;
        public int Y;

        public Point2D(int x, int y)
        {
            X = x;
            Y = y;
        }
    }
}