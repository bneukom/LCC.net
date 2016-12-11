using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using GalaSoft.MvvmLight.Messaging;
using LandscapeClassifier.Annotations;
using LandscapeClassifier.Extensions;
using LandscapeClassifier.Model;
using LandscapeClassifier.Model.Classification;
using LandscapeClassifier.Util;
using LandscapeClassifier.View;
using LandscapeClassifier.View.Open;
using LandscapeClassifier.View.Tools;
using LandscapeClassifier.ViewModel.Dialogs;
using LandscapeClassifier.ViewModel.MainWindow.Classification;
using LandscapeClassifier.ViewModel.MainWindow.Prediction;
using MathNet.Numerics.LinearAlgebra;
using OSGeo.GDAL;
using LayerViewModel = LandscapeClassifier.ViewModel.MainWindow.Classification.LayerViewModel;

namespace LandscapeClassifier.ViewModel.MainWindow
{
    public class MainWindowViewModel : ViewModelBase
    {
        public ClassifierViewModel ClassifierViewModel { get; set; }
        public PredictionViewModel PredictionViewModel { get; set; }

        private bool _windowEnabled = true;
        private LayerViewModel _selectedLayer;
        private ObservableCollection<LayerViewModel> _featureLayerView;

        /// <summary>
        /// The bands of the image.
        /// </summary>
        public ObservableCollection<LayerViewModel> Layers { get; set; }

        /// <summary>
        /// Feature Layer View
        /// </summary>
        public ObservableCollection<LayerViewModel> FeatureLayerView
        {
            get { return _featureLayerView; }
            set { _featureLayerView = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// Selected layer.
        /// </summary>
        public LayerViewModel SelectedLayer
        {
            get { return _selectedLayer; }
            set { _selectedLayer = value; RaisePropertyChanged(); }
        }

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
        /// Create slope texture from a height map.
        /// </summary>
        public ICommand CreateSlopeFromHeightmapCommand { set; get; }

        /// <summary>
        /// Create tiled heightmap images.
        /// </summary>
        public ICommand CreateTiledHeightmapCommand { get; set; }

        /// <summary>
        /// Exit command.
        /// </summary>
        public ICommand ExitCommand { set; get; }

        /// <summary>
        /// Open image bands.
        /// </summary>
        public ICommand AddLayersCommand { set; get; }

        /// <summary>
        /// Move selected layer down.
        /// </summary>
        public ICommand MoveLayerDownCommand { set; get; }

        /// <summary>
        /// Move selected layer down.
        /// </summary>
        public ICommand MoveLayerUpCommand { set; get; }

        /// <summary>
        /// Block the main window.
        /// </summary>
        public bool WindowEnabled
        {
            get { return _windowEnabled; }
            set { _windowEnabled = value; RaisePropertyChanged(); }
        }

        public MainWindowViewModel()
        {
            GdalConfiguration.ConfigureGdal();
            ExitCommand = new RelayCommand(() => Application.Current.Shutdown(), () => true);

            CreateSlopeFromHeightmapCommand = new RelayCommand(() => new CreateSlopeFromHeightmapDialog().ShowDialog(), () => true);
            CreateTiledHeightmapCommand = new RelayCommand(() => new CreateTiledHeightmapDialog().ShowDialog(), () => true);
            AddLayersCommand = new RelayCommand(AddBands, () => PredictionViewModel.NotBlocking);

            MoveLayerDownCommand = new RelayCommand(MoveLayerDown, CanMoveDown);
            MoveLayerUpCommand = new RelayCommand(MoveLayerUp, CanMoveUp);

            Layers = new ObservableCollection<LayerViewModel>();
            Layers.CollectionChanged += (sender, args) =>
            {
                FeatureLayerView = new ObservableCollection<LayerViewModel>(Layers.Where(l => l.IsFeature).ToList());
            };

            ClassifierViewModel = new ClassifierViewModel(this);
            PredictionViewModel = new PredictionViewModel(this);
        }

        private bool CanMoveUp()
        {
            return SelectedLayer != null && Layers.IndexOf(SelectedLayer) != 0 && !ClassifierViewModel.FeaturesViewModel.HasFeatures();
        }

        private bool CanMoveDown()
        {
            return SelectedLayer != null && Layers.IndexOf(SelectedLayer) != Layers.Count - 1 && !ClassifierViewModel.FeaturesViewModel.HasFeatures();
        }

        private void MoveLayerUp()
        {
            int current = Layers.IndexOf(SelectedLayer);
            if (current == 0) return;
            Layers.Move(current, current - 1);
        }

        private void MoveLayerDown()
        {
            int current = Layers.IndexOf(SelectedLayer);
            if (current == Layers.Count) return;
            Layers.Move(current, current + 1);
        }


        private void AddBands()
        {
            AddLayersDialog dialog = new AddLayersDialog();

            if (dialog.ShowAddBandsDialog() == true && dialog.DialogViewModel.Layers.Count > 0)
            {
                AddBands(dialog.DialogViewModel);
            }
        }

        /// <summary>
        /// Adds the bands from the bands view model.
        /// </summary>
        /// <param name="viewModel"></param>
        public void AddBands(AddLayersDialogViewModel viewModel)
        {
            try
            {
                WindowEnabled = false;

                // Initialize RGB data
                byte[] bgra = null;
                Dataset rgbDataSet = null;
                if (viewModel.AddRgb)
                {
                    var firstRgbBand = viewModel.Layers.First(b => b.B || b.G || b.R);
                    rgbDataSet = Gdal.Open(firstRgbBand.Path, Access.GA_ReadOnly);
                    bgra = new byte[rgbDataSet.RasterXSize * rgbDataSet.RasterYSize * 4];
                }

                var firstBand = viewModel.Layers.First();
                var firstDataSet = Gdal.Open(firstBand.Path, Access.GA_ReadOnly);

                // Parallel band loading
                Task loadBands = Task.Factory.StartNew(() => Parallel.ForEach(viewModel.Layers, (currentLayer, _, bandIndex) =>
                    {
                        // Load raster
                        var dataSet = Gdal.Open(currentLayer.Path, Access.GA_ReadOnly);
                        var rasterBand = dataSet.GetRasterBand(1);

                        var bitsPerPixel = rasterBand.DataType.ToPixelFormat().BitsPerPixel;
                        int stride = (rasterBand.XSize * bitsPerPixel + 7) / 8;
                        IntPtr data = Marshal.AllocHGlobal(stride * rasterBand.YSize);

                        rasterBand.ReadRaster(0, 0, rasterBand.XSize, rasterBand.YSize, data, rasterBand.XSize,
                            rasterBand.YSize, rasterBand.DataType, bitsPerPixel / 8, stride);

                        // Cutoff
                        int minCutValue, maxCutValue;
                        CalculateMinMaxCut(rasterBand, currentLayer.MinCutOff, currentLayer.MaxCutOff, out minCutValue, out maxCutValue);

                        // Apply RGB contrast enhancement
                        if (viewModel.AddRgb && viewModel.RgbContrastEnhancement && (currentLayer.B || currentLayer.G || currentLayer.R))
                        {
                            int colorOffset = currentLayer.B ? 0 : currentLayer.G ? 1 : currentLayer.R ? 2 : -1;
                            unsafe
                            {
                                // TODO what if layer is not of type uint?
                                ushort* dataPtr = (ushort*)data.ToPointer();
                                Parallel.ForEach(Partitioner.Create(0, rasterBand.XSize * rasterBand.YSize),
                                    (range) =>
                                    {
                                        for (int dataIndex = range.Item1; dataIndex < range.Item2; ++dataIndex)
                                        {
                                            ushort current = *(dataPtr + dataIndex);
                                            byte val = (byte)MoreMath.Clamp((current - minCutValue) / (double)(maxCutValue - minCutValue) * byte.MaxValue, 0, byte.MaxValue - 1);

                                            bgra[dataIndex * 4 + colorOffset] = val;
                                            bgra[dataIndex * 4 + 3] = 255;
                                        }
                                    });
                            }
                        }

                        // Apply band contrast enhancement
                        if (currentLayer.ContrastEnhancement)
                        {
                            data = ApplyContrastEnhancement(rasterBand, data, minCutValue, maxCutValue);
                        }


                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            var imageBandViewModel = CreateLayerViewModel(dataSet, rasterBand, stride, data, rasterBand.DataType.ToPixelFormat(), currentLayer);

                            Layers.AddSorted(imageBandViewModel,
                                Comparer<LayerViewModel>.Create((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal)));

                            Marshal.FreeHGlobal(data);
                        });


                    }));

                // Load rgb image
                if (viewModel.AddRgb)
                {
                    var addRgb = loadBands.ContinueWith(t =>
                    {
                        // Create RGB image
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            var rgbStride = rgbDataSet.RasterXSize * 4;

                            var rgbImage = BitmapSource.Create(rgbDataSet.RasterXSize, rgbDataSet.RasterYSize, 96, 96, PixelFormats.Bgra32, null, bgra, rgbStride);

                            // Transformation
                            double[] rgbTransform = new double[6];
                            rgbDataSet.GetGeoTransform(rgbTransform);
                            var vecBuilder = Vector<double>.Build;
                            var upperLeft = vecBuilder.DenseOfArray(new[] { rgbTransform[0], rgbTransform[3], 1 });
                            var xRes = rgbTransform[1];
                            var yRes = rgbTransform[5];
                            var bottomRight =
                                vecBuilder.DenseOfArray(new[]
                                {
                                    upperLeft[0] + (rgbDataSet.RasterXSize*xRes),
                                    upperLeft[1] + (rgbDataSet.RasterYSize*yRes), 1
                                });


                            double[,] matArray =
                            {
                                {rgbTransform[1], rgbTransform[2], rgbTransform[0]},
                                {rgbTransform[4], rgbTransform[5], rgbTransform[3]},
                                {0, 0, 1}
                            };

                            var builder = Matrix<double>.Build;
                            var transformMat = builder.DenseOfArray(matArray);

                            Layers.Insert(0,
                                new LayerViewModel("RGB", SatelliteType.None, null, viewModel.RgbContrastEnhancement, xRes, yRes, new WriteableBitmap(rgbImage),
                                    transformMat, upperLeft, bottomRight, 0, 0, false, false, false, false,
                                    ClassifierViewModel.FeaturesViewModel.HasFeatures()));
                        });
                    });

                    // Send message
                    addRgb.ContinueWith(t =>
                    {
                        // TODO use projection to get correct transformation?
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

                        var message = new BandsLoadedMessage
                        {
                            RgbContrastEnhancement = viewModel.RgbContrastEnhancement,
                            ProjectionName = firstDataSet.GetProjection(),
                            ScreenToWorld = transformMat,
                        };

                        Messenger.Default.Send(message);
                        WindowEnabled = true;
                    });
                }
                else
                {
                    WindowEnabled = true;
                }
            }
            catch
            {
                WindowEnabled = true;
            }
        }

        private LayerViewModel CreateLayerViewModel(Dataset dataSet, OSGeo.GDAL.Band rasterBand, int stride, IntPtr data, PixelFormat format, CreateLayerViewModel layer)
        {
            WriteableBitmap bandImage = new WriteableBitmap(rasterBand.XSize, rasterBand.YSize, 96, 96, format, null);
            bandImage.Lock();

            unsafe
            {
                Buffer.MemoryCopy(data.ToPointer(), bandImage.BackBuffer.ToPointer(),
                    stride * rasterBand.YSize,
                    stride * rasterBand.YSize);
            }

            bandImage.AddDirtyRect(new Int32Rect(0, 0, rasterBand.XSize, rasterBand.YSize));
            bandImage.Unlock();

            // Position
            double[] bandTransform = new double[6];
            dataSet.GetGeoTransform(bandTransform);
            var vecBuilder = Vector<double>.Build;
            var upperLeft = vecBuilder.DenseOfArray(new[] { bandTransform[0], bandTransform[3], 1 });
            var xRes = bandTransform[1];
            var yRes = bandTransform[5];
            var bottomRight = vecBuilder.DenseOfArray(new[] { upperLeft[0] + (rasterBand.XSize * xRes), upperLeft[1] + (rasterBand.YSize * yRes), 1 });
            double[,] matArray =
            {
                {bandTransform[1], bandTransform[2], bandTransform[0]},
                {bandTransform[4], bandTransform[5], bandTransform[3]},
                {0, 0, 1}
            };

            var builder = Matrix<double>.Build;
            var transformMat = builder.DenseOfArray(matArray);

            var imageBandViewModel = new LayerViewModel(
                layer.GetName(), layer.SatelliteType, layer.Path, layer.ContrastEnhancement, xRes, yRes, bandImage, transformMat, upperLeft,
                bottomRight, layer.MinCutOff, layer.MaxCutOff, layer.R, layer.G, layer.B, true,
                ClassifierViewModel.FeaturesViewModel.HasFeatures());

            return imageBandViewModel;
        }

        private static void CalculateMinMaxCut(OSGeo.GDAL.Band rasterBand, double minPercentage, double maxPercentage, out int minCutValue, out int maxCutValue)
        {
            double[] minMax = new double[2];
            rasterBand.ComputeRasterMinMax(minMax, 0);
            int min = (int)minMax[0];
            int max = (int)minMax[1];
            int[] histogram = new int[max - min];
            rasterBand.GetHistogram(min, max, max - min, histogram, 1, 0, ProgressFunc, "");

            int totalPixels = 0;
            for (int bucket = 0; bucket < histogram.Length; ++bucket)
            {
                totalPixels += histogram[bucket];
            }

            double minCut = totalPixels * minPercentage;
            minCutValue = int.MaxValue;
            bool minCutSet = false;

            double maxCut = totalPixels * maxPercentage;
            maxCutValue = 0;
            bool maxCutSet = false;

            int pixelCount = 0;
            for (int bucket = 0; bucket < histogram.Length; ++bucket)
            {
                pixelCount += histogram[bucket];
                if (pixelCount >= minCut && !minCutSet)
                {
                    minCutValue = bucket + min;
                    minCutSet = true;
                }
                if (pixelCount >= maxCut && !maxCutSet)
                {
                    maxCutValue = bucket + min;
                    maxCutSet = true;
                }
            }
        }

        private static unsafe IntPtr ApplyContrastEnhancement(OSGeo.GDAL.Band rasterBand, IntPtr data, int minCutValue, int maxCutValue)
        {
            Action<int> action;

            // Select appropriate datatype
            if (rasterBand.DataType == DataType.GDT_UInt16)
            {
                action = (index) =>
                {
                    ushort* dataPtr = (ushort*)data.ToPointer();
                    ushort current = *(dataPtr + index);
                    *(dataPtr + index) = (ushort)MoreMath.Clamp((current - minCutValue) / (double)(maxCutValue - minCutValue) * ushort.MaxValue, 0, ushort.MaxValue);
                };
            }
            else if (rasterBand.DataType == DataType.GDT_Float32)
            {
                action = (index) =>
                {
                    float* dataPtr = (float*)data.ToPointer();
                    float current = *(dataPtr + index);

                    *(dataPtr + index) = (float)MoreMath.Clamp((current - minCutValue) / (double)(maxCutValue - minCutValue) * 1.0f, 0.0f, 1.0f);
                };
            }
            else
            {
                throw new InvalidOperationException();
            }

            // Execute contrast enhancement
            Parallel.ForEach(Partitioner.Create(0, rasterBand.XSize * rasterBand.YSize), (range) =>
                {
                    for (int dataIndex = range.Item1; dataIndex < range.Item2; ++dataIndex)
                    {
                        action(dataIndex);
                    }
                });

            return data;
        }

        private static int ProgressFunc(double complete, IntPtr message, IntPtr data)
        {
            return 1;
        }

    }

    public class BandsLoadedMessage
    {
        public bool RgbContrastEnhancement { get; set; }
        public string ProjectionName { get; set; }
        public Matrix<double> ScreenToWorld { get; set; }
    }
}
