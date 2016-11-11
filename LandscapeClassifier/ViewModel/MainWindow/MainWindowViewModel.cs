using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using LandscapeClassifier.Annotations;
using LandscapeClassifier.Extensions;
using LandscapeClassifier.Model;
using LandscapeClassifier.Model.Classification;
using LandscapeClassifier.Util;
using LandscapeClassifier.View;
using LandscapeClassifier.View.Open;
using LandscapeClassifier.ViewModel.MainWindow.Classification;
using LandscapeClassifier.ViewModel.MainWindow.Prediction;
using MathNet.Numerics.LinearAlgebra;
using OSGeo.GDAL;

namespace LandscapeClassifier.ViewModel.MainWindow
{
    public class MainWindowViewModel : ViewModelBase
    {
        public ClassifierViewModel ClassifierViewModel { get; set; }
        public PredictionViewModel PredictionViewModel { get; set; }


        private BitmapSource _predictedLandcoverImage;
        private double _overlayOpacity = 0.5d;

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
        /// Exit command.
        /// </summary>
        public ICommand ExitCommand { set; get; }

        /// <summary>
        /// Open image bands.
        /// </summary>
        public ICommand OpenSatelliteBandsCommand { set; get; }

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



        public MainWindowViewModel()
        {
            GdalConfiguration.ConfigureGdal();

            ClassifierViewModel = new ClassifierViewModel(this);
            PredictionViewModel = new PredictionViewModel();

            ExitCommand = new RelayCommand(() => Application.Current.Shutdown(), () => true);

            // ExportSlopeImageCommand = new RelayCommand(() => ExportSlopeFromDEMDialog.ShowDialog(_ascFile), () => AscFile != null);
            // ExportCurvatureImageCommand = new RelayCommand(() => ExportCurvatureFromDEMDialog.ShowDialog(_ascFile), () => AscFile != null);
            // ExportSmoothedHeightImageCommand = new RelayCommand(() => ExportSmoothedDEMDialog.ShowDialog(_ascFile), () => AscFile != null);
            CreateSlopeFromDEM = new RelayCommand(() => new CreateSlopeFromDEMDialog().ShowDialog(), () => true);
            OpenSatelliteBandsCommand = new RelayCommand(OpenBands, () => true);
        }


        /// <summary>
        /// Predicts the land cover type with the given feature vector.
        /// </summary>
        /// <param name="feature"></param>
        /// <returns></returns>
        public LandcoverType Predict(FeatureVector feature)
        {
            // return _currentClassifier.Predict(feature);
            return LandcoverType.Grass;
        }

        private void OpenBands()
        {
            AddBandsDialog dialog = new AddBandsDialog();

            if (dialog.ShowAddBandsDialog() == true && dialog.DialogViewModel.Bands.Count > 0)
            {
                AddBands(dialog.DialogViewModel);
            }
        }

        // TODO use Messanger Service https://msdn.microsoft.com/en-us/magazine/jj694937.aspx
        /// <summary>
        /// Adds the bands from the bands view model.
        /// </summary>
        /// <param name="viewModel"></param>
        public void AddBands(AddBandsDialogViewModel viewModel)
        {
            // Store bands loading information
            ClassifierViewModel.SatelliteType = viewModel.SatelliteType;
            ClassifierViewModel.BandsContrastEnhancement = viewModel.RgbContrastEnhancement;
            ClassifierViewModel.AreBandsUnscaled = !viewModel.BandContrastEnhancement;

            // Initialize RGB data
            byte[] bgra = null;
            Dataset rgbDataSet = null;
            if (viewModel.AddRgb)
            {
                var firstRGBBand = viewModel.Bands.First(b => b.B || b.G || b.R);
                rgbDataSet = Gdal.Open(firstRGBBand.Path, Access.GA_ReadOnly);
                bgra = new byte[rgbDataSet.RasterXSize * rgbDataSet.RasterYSize * 4];
            }

            var firstBand = viewModel.Bands.First();
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

            ClassifierViewModel.ProjectionName = firstDataSet.GetProjection();
            ClassifierViewModel.ScreenToWorld = transformMat;
            ClassifierViewModel.WorldToScreen = transformMat.Inverse();
            PredictionViewModel.ScreenToWorld = transformMat;
            PredictionViewModel.WorldToScreen = transformMat.Inverse();

            // Parallel band loading
            Task loadImages = Task.Factory.StartNew(() => Parallel.ForEach(viewModel.Bands, (bandInfo, _, bandIndex) =>
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
                if (viewModel.AddRgb)
                {
                    // Apply RGB contrast enhancement
                    if (viewModel.RgbContrastEnhancement && (bandInfo.B || bandInfo.G || bandInfo.R))
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
                if (viewModel.BandContrastEnhancement)
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

                    int bandNumber = viewModel.SatelliteType.GetBand(Path.GetFileName(bandInfo.Path));
                    var imageBandViewModel = new BandViewModel("Band " + bandNumber, bandInfo.Path, bandNumber, meterPerPixel, bandImage, upperLeft, bottomRight, minCutValue, maxCutValue, true, ClassifierViewModel.Features.Count == 0);

                    ClassifierViewModel.Bands.AddSorted(imageBandViewModel, Comparer<BandViewModel>.Create((a, b) => a.BandNumber - b.BandNumber));
                });

                Marshal.FreeHGlobal(data);
            }));

            // Load rgb image
            if (viewModel.AddRgb)
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

                        ClassifierViewModel.Bands.Insert(0, new BandViewModel("RGB", null, -1, meterPerPixel, new WriteableBitmap(rgbImage), upperLeft, bottomRight, 0, 0, false, ClassifierViewModel.Features.Count == 0));
                    });
                });

                // Set image for the prediction view.
                loadImages.ContinueWith(t => 
                    Application.Current.Dispatcher.Invoke(() => 
                    PredictionViewModel.VisibleBand = ClassifierViewModel.Bands[0])
                    );

            }

            
        }


        private static int ProgressFunc(double complete, IntPtr message, IntPtr data)
        {
            return 1;
        }

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

        #endregion
    }
}
