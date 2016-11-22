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
using LandscapeClassifier.Model;
using LandscapeClassifier.View.Open;
using LandscapeClassifier.ViewModel.MainWindow.Classification;
using MathNet.Numerics.LinearAlgebra;
using ZedGraph;
using ExportPredicitonDialog = LandscapeClassifier.View.Export.ExportPredicitonDialog;
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

        public ICommand ApplyMajorityFilterCommand { get; set; }

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

        public Vector<double> PredictionUpperLeft { get; set; }

        public Vector<double> PredictionBottomRight { get; set; }




        public PredictionViewModel(MainWindowViewModel mainWindowViewModel)
        {
            _mainWindowViewModel = mainWindowViewModel;

            // TODO nono
            ScreenToWorld = Matrix<double>.Build.DenseOfArray(new[,] { { 1, 0, 300000.0 }, { 0, -1, 5090220 }, { 0, 0, 1 } });
            WorldToScreen = ScreenToWorld.Inverse();

            PredictAllCommand = new RelayCommand(PredictAll, CanPredictAll);
            ExportPredictionsCommand = new RelayCommand(ExportPredictions, CanExportPredictions);
            ApplyMajorityFilterCommand = new RelayCommand(ApplyMajorityFilter, CanApplyMajorityFilter);
        }

        private bool CanExportPredictions() => NotBlocking && IsAllPredicted;
        private bool CanPredictAll() => NotBlocking;
        private bool CanApplyMajorityFilter() => NotBlocking && IsAllPredicted;

        private void ApplyMajorityFilter()
        {
        }

        private void ExportPredictions()
        {
            ExportPredicitonDialog dialog = new ExportPredicitonDialog();
            if (dialog.ShowDialog() == true)
            {

                var layers = dialog.DialogViewModel.ExportLayers;

                var width = _classificationOverlay.PixelWidth;
                var stride = width * 4;
                var height = _classificationOverlay.PixelHeight;
                var layerData = layers.Select(l => new byte[stride * height]).ToList();

                NotBlocking = false;

                List<Task> createLayerTasks = new List<Task>();
                for (int layerIndex = 0; layerIndex < layerData.Count; ++layerIndex)
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

                Task.WhenAll(createLayerTasks).ContinueWith(t =>
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

                    NotBlocking = true;
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

            PredictionUpperLeft = upperLeftWorld;
            PredictionBottomRight = bottomRightWorld;

            var scaleBand = featureBands.OrderBy(b => b.ScaleX).First();

            int predictionWidth = (int)(scaleBand.WorldToImage * bottomRightWorld)[0];
            int predictionHeight = (int)(scaleBand.WorldToImage * bottomRightWorld)[1];

            IntPtr[] data = featureBands.Select(b => b.BandImage.BackBuffer).ToArray();
            _classification = new int[predictionHeight][];

            Task predict = Task.Factory.StartNew(() => Parallel.ForEach(Partitioner.Create(0, predictionHeight), range =>
            {
                double[][] features = new double[predictionWidth][];

                for (int i = 0; i < predictionWidth; ++i)
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
                                for (int x = 0; x < predictionWidth; ++x)
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
                                for (int x = 0; x < predictionWidth; ++x)
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
                    _classification[line] = _mainWindowViewModel.ClassifierViewModel.CurrentClassifier.Predict(features);

                    lock (this)
                    {
                        PredictionProgress += 100.0/predictionHeight;
                    }
                }

            }));

            predict.ContinueWith(t =>
            {
                int stride = predictionWidth * 4;
                int size = predictionHeight * stride;
                byte[] imageData = new byte[size];

                Parallel.For(0, predictionWidth, x =>
                {
                    for (int y = 0; y < predictionHeight; ++y)
                    {
                        int index = 4 * y * predictionWidth + 4 * x;
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
                    ClassificationOverlay = BitmapSource.Create(predictionWidth, predictionHeight, 96, 96, PixelFormats.Bgra32, null, imageData, stride);
                    IsAllPredicted = true;
                    NotBlocking = true;
                    ProgressVisibility = Visibility.Hidden;
                });
            });
        }


    }
}
