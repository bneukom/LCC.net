using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using LandscapeClassifier.ViewModel.MainWindow.Classification;
using MathNet.Numerics.LinearAlgebra;
using ZedGraph;

namespace LandscapeClassifier.ViewModel.MainWindow.Prediction
{
    public class PredictionViewModel : ViewModelBase
    {
        private readonly MainWindowViewModel _mainWindowViewModel;

        private LandcoverType _mousePredictionType;
        private double _mousePredictionProbability;

        private bool _isAllPredicted;
        private BitmapSource _classificationOverlay;
        private double _overlayOpacity = 0.5d;
        private double _acceptanceProbabilty;
        

        /// <summary>
        /// Predict all.
        /// </summary>
        public ICommand PredictAllCommand { set; get; }

        /// <summary>
        /// Export predictions.
        /// </summary>
        public ICommand ExportPredictionsCommand { set; get; }

        /// <summary>
        /// Export grayscale by landcover type.
        /// </summary>
        public ICommand ExportByLandcoverTypeCommand { get; set; }

        /// <summary>
        /// Band to display.
        /// </summary>
        public LayerViewModel VisibleLayer { set; get; }

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

        public PredictionViewModel(MainWindowViewModel mainWindowViewModel)
        {
            _mainWindowViewModel = mainWindowViewModel;

            ScreenToWorld = Matrix<double>.Build.DenseIdentity(3);
            WorldToScreen = ScreenToWorld.Inverse();

            PredictAllCommand = new RelayCommand(PredictAll, CanPredictAll);
            ExportPredictionsCommand = new RelayCommand(ExportPredictions, CanExportPredictions);
            ExportByLandcoverTypeCommand = new RelayCommand(ExportPredictionsByLandcoverType, CanExportPredictions);

            Messenger.Default.Register<BandsLoadedMessage>(this, m =>
            {
                ScreenToWorld = m.ScreenToWorld;
                WorldToScreen = m.ScreenToWorld.Inverse();
                VisibleLayer = _mainWindowViewModel.Layers.FirstOrDefault(f => f.IsRgb) ??
                           _mainWindowViewModel.Layers.First();
            });
        }

        private void ExportPredictionsByLandcoverType()
        {
            throw new NotImplementedException();
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
            var firstBand = _mainWindowViewModel.Layers.FirstOrDefault(f => f.IsRgb) ??
                            _mainWindowViewModel.Layers.First();

            var numFeatures = _mainWindowViewModel.Layers.Count(f => f.IsFeature);

            var firstBandUpperLeftScreen = WorldToScreen * firstBand.UpperLeft;
            var firstBandBottomRightScreen = WorldToScreen * firstBand.BottomRight;

            int pixelWidth = (int)((firstBandBottomRightScreen[0] - firstBandUpperLeftScreen[0]) / firstBand.MetersPerPixel);
            int pixelHeight = (int)((firstBandBottomRightScreen[1] - firstBandUpperLeftScreen[1]) / firstBand.MetersPerPixel);

            double firstBandScale = firstBand.MetersPerPixel;

            var featureBands = _mainWindowViewModel.Layers.Where(f => f.IsFeature).ToList();

            IntPtr[] data = featureBands.Select(b => b.BandImage.BackBuffer).ToArray();

            int[][] result = new int[pixelHeight][];

            // TODO iterate over world coordinates (scale with global world to screen)?
            Task predict = Task.Factory.StartNew(() => Parallel.ForEach(Partitioner.Create(0, pixelHeight), range =>
            {
                double[][] features = new double[pixelWidth][];

                for (int i = 0; i < pixelWidth; ++i)
                {
                    features[i] = new double[numFeatures];
                }

                for (int line = range.Item1; line < range.Item2; ++line)
                {
                    for (int bandIndex = 0; bandIndex < featureBands.Count; ++bandIndex)
                    {
                        var band = featureBands[bandIndex];

                        var inverseTransform = band.Transform.Inverse();
                        var bandUpperLeftScreen = inverseTransform * band.UpperLeft;
                        var bandBottomRightScreen = inverseTransform * band.BottomRight;
                        var left = (int)bandUpperLeftScreen[0];
                        var right = (int)bandBottomRightScreen[0];

                        var width = right - left;
                        int bandLine = (int)(line / band.MetersPerPixel * firstBandScale);

                        unsafe
                        {
                            ushort* dataPtr = (ushort*)data[bandIndex].ToPointer();

                            for (int i = 0; i < pixelWidth; ++i)
                            {
                                var indexX = (int)(i / band.MetersPerPixel * firstBandScale);
                                var pixelValue = *(dataPtr + bandLine * width + indexX);
                                features[i][bandIndex] = (double)pixelValue / ushort.MaxValue;
                            }
                        }
                    }


                    result[line] = _mainWindowViewModel.ClassifierViewModel.CurrentClassifier.Predict(features);
                }
            }));

            predict.ContinueWith(t =>
            {
                int stride = pixelWidth * 4;
                int size = pixelHeight * stride;
                byte[] imageData = new byte[size];

                // TODO add majority filter tool
                /*
                int[,] majorityFiltered = new int[pixelHeight, pixelWidth];
                int numTypes = Enum.GetValues(typeof(LandcoverType)).Length - 1;

                int[] occurence = new int[numTypes];
                for (int x = 0; x < pixelWidth; ++x)
                {
                    for (int y = 0; y < pixelHeight; ++y)
                    {
                        for (int i = 0; i < numTypes; ++i) occurence[i] = 0;

                        occurence[result[y][x]]++;
                        if (y - 1 >= 0) occurence[result[y - 1][x]]++;
                        if (x - 1 >= 0) occurence[result[y][x - 1]]++;
                        if (y + 1 < pixelHeight) occurence[result[y + 1][x]]++;
                        if (x + 1 < pixelWidth) occurence[result[y][x + 1]]++;

                        majorityFiltered[y, x] = Array.IndexOf(occurence, occurence.Max());
                    }
                }
                */

                Parallel.For(0, pixelWidth, x =>
                {
                    for (int y = 0; y < pixelHeight; ++y)
                    {
                        int index = 4 * y * pixelWidth + 4 * x;
                        LandcoverType type = (LandcoverType)result[y][x];
                        var color = type.GetColor();
                        imageData[index + 0] = color.B;
                        imageData[index + 1] = color.G;
                        imageData[index + 2] = color.R;
                        imageData[index + 3] = color.A;
                    }
                });

                Application.Current.Dispatcher.Invoke(() =>
                {
                    ClassificationOverlay = BitmapSource.Create(pixelWidth, pixelHeight, 96, 96, PixelFormats.Bgra32, null, imageData, stride);
                    IsAllPredicted = true;
                });
            });
        }

        private bool CanPredictAll() => true;
    }
}
