using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using LandscapeClassifier.Model;
using LandscapeClassifier.ViewModel.MainWindow.Classification;
using MathNet.Numerics.LinearAlgebra;

namespace LandscapeClassifier.ViewModel.MainWindow.Prediction
{
    public class PredictionViewModel : ViewModelBase
    {
        private LandcoverType _mousePredictionType;

        /// <summary>
        /// Predict all.
        /// </summary>
        public ICommand PredictAllCommand { set; get; }

        /// <summary>
        /// Export predictions.
        /// </summary>
        public ICommand ExportPredictionsCommand { set; get; }

        /// <summary>
        /// Band to display.
        /// </summary>
        public BandViewModel VisibleBand { set; get; }

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

        public PredictionViewModel()
        {
            ScreenToWorld = Matrix<double>.Build.DenseIdentity(3);
            WorldToScreen = ScreenToWorld.Inverse();

            PredictAllCommand = new RelayCommand(PredictAll, CanPredictAll);
            ExportPredictionsCommand = new RelayCommand(ExportPredictions, CanExportPredictions);
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

        private bool CanPredictAll() => true;
    }
}
