using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using LandscapeClassifier.Extensions;
using LandscapeClassifier.Util;
using Microsoft.Win32;
using OSGeo.GDAL;

namespace LandscapeClassifier.ViewModel.Dialogs
{
    public class CreateTiledHeightmapDialogViewModel : ViewModelBase
    {
        private string _inputPath;
        private string _outputPath;
        private double _minAltitude;
        private double _maxAltitude;
        private double _scaleX;
        private double _scaleY;
        private bool _inputPathSet;
        private double _noDataValue;
        private Visibility _loadingOverlayVisibility = Visibility.Collapsed;
        private int _tileWidth = 501;
        private int _tileHeight = 501;
        private bool _applyTiling;

        private static float Epsilon = 0.00001f;

        IntPtr _transformedPtr;

        public string InputPath
        {
            get { return _inputPath; }
            set
            {
                _inputPath = value;

                // Load raster
                using (var dataSet = Gdal.Open(_inputPath, Access.GA_ReadOnly))
                {
                    var rasterBand = dataSet.GetRasterBand(1);

                    double[] minMax = new double[2];
                    rasterBand.ComputeRasterMinMax(minMax, 0);
                    MinAltitude = minMax[0];
                    MaxAltitude = minMax[1];

                    double noDataValue;
                    int hasValue;
                    rasterBand.GetNoDataValue(out noDataValue, out hasValue);
                    if (hasValue == 1)
                        NoDataValue = noDataValue;

                    // Output path
                    var folder = Path.GetDirectoryName(_inputPath);
                    OutputPath = folder;

                    double[] transform = new double[6];
                    dataSet.GetGeoTransform(transform);

                    ScaleX = transform[1];
                    ScaleY = transform[5];
                }

                InputPathSet = !string.IsNullOrWhiteSpace(InputPath);

                RaisePropertyChanged();
            }
        }

        public int TileWidth
        {
            get { return _tileWidth; }
            set { _tileWidth = value; RaisePropertyChanged(); }
        }

        public int TileHeight
        {
            get { return _tileHeight; }
            set { _tileHeight = value; RaisePropertyChanged(); }
        }


        public bool InputPathSet
        {
            get { return _inputPathSet; }
            set
            {
                _inputPathSet = value;
                RaisePropertyChanged();

            }
        }

        public string OutputPath
        {
            get { return _outputPath; }
            set { _outputPath = value; RaisePropertyChanged(); }
        }

        public double MinAltitude
        {
            get { return _minAltitude; }
            set { _minAltitude = value; RaisePropertyChanged(); }
        }

        public double MaxAltitude
        {
            get { return _maxAltitude; }
            set { _maxAltitude = value; RaisePropertyChanged(); }
        }

        public double ScaleX
        {
            get { return _scaleX; }
            set { _scaleX = value; RaisePropertyChanged(); }
        }

        public double ScaleY
        {
            get { return _scaleY; }
            set { _scaleY = value; RaisePropertyChanged(); }
        }

        public double NoDataValue
        {
            get { return _noDataValue; }
            set { _noDataValue = value; RaisePropertyChanged(); }
        }

        public Visibility LoadingOverlayVisibility
        {
            get { return _loadingOverlayVisibility; }
            set { _loadingOverlayVisibility = value; RaisePropertyChanged(); }
        }

        public bool ApplyTiling
        {
            get { return _applyTiling; }
            set { _applyTiling = value; RaisePropertyChanged(); }
        }

        public ICommand BrowseInputPathCommand { get; set; }
        public ICommand BrowseOutputPathCommand { get; set; }
        public ICommand CreateHeightmapCommand { get; set; }

        public CreateTiledHeightmapDialogViewModel()
        {
            BrowseInputPathCommand = new RelayCommand(BrowseInputPath);
            BrowseOutputPathCommand = new RelayCommand(BrowseOutputPath);
            CreateHeightmapCommand = new RelayCommand(CreateHeightmap);
        }

        private unsafe void CreateHeightmap()
        {
            LoadingOverlayVisibility = Visibility.Visible;

            PixelFormat targetFormat;

            float* original;
            ushort* transformed;

            int rasterXSize;
            int rasterYSize;

            // Load raster
            using (var inputDataSet = Gdal.Open(_inputPath, Access.GA_ReadOnly))
            {
                var rasterBand = inputDataSet.GetRasterBand(1);

                rasterXSize = rasterBand.XSize;
                rasterYSize = rasterBand.YSize;

                var bitsPerPixel = rasterBand.DataType.ToPixelFormat().BitsPerPixel;
                int originalStride = (rasterBand.XSize * bitsPerPixel + 7) / 8;
                IntPtr originaPtr = Marshal.AllocHGlobal(originalStride * rasterBand.YSize);

                rasterBand.ReadRaster(0, 0, rasterBand.XSize, rasterBand.YSize, originaPtr, rasterBand.XSize,
                    rasterBand.YSize, rasterBand.DataType, bitsPerPixel / 8, originalStride);

                // Convert
                targetFormat = PixelFormats.Gray16;
                int transformedStride = (rasterBand.XSize * targetFormat.BitsPerPixel + 7) / 8;
                _transformedPtr = Marshal.AllocHGlobal(transformedStride * rasterBand.YSize);

                original = (float*)originaPtr.ToPointer();
                transformed = (ushort*)_transformedPtr.ToPointer();
            }
            var transformTask =
                Task.Factory.StartNew(() => Parallel.For(0, rasterXSize * rasterYSize, offset =>
                  {
                      float value = *(original + offset);

                      if (Math.Abs(value - NoDataValue) < Epsilon)
                      {
                          *(transformed + offset) = 0;
                      }
                      else
                      {
                          ushort scaled = (ushort)Math.Min((value - MinAltitude) / (MaxAltitude - MinAltitude) * ushort.MaxValue, ushort.MaxValue);
                          *(transformed + offset) = scaled;
                      }
                  }));

            if (ApplyTiling)
            {
                transformTask.ContinueWith(t =>
                {
                    int numTilesX = (int)Math.Ceiling((double)rasterXSize / TileWidth);
                    int numTilesY = (int)Math.Ceiling((double)rasterYSize / TileHeight);

                    int tileStride = (TileWidth * targetFormat.BitsPerPixel + 7) / 8;

                    //var task = Task.Run(() => Parallel.For(0, numTilesY, tileY =>
                    for (int tileY = 0; tileY < numTilesY; ++tileY)
                    {
                        for (int tileX = 0; tileX < numTilesX; ++tileX)
                        {
                            IntPtr tilePtr = Marshal.AllocHGlobal(tileStride * TileHeight);
                            ushort* tile = (ushort*)tilePtr.ToPointer();

                            int tileWidth = Math.Min(TileWidth, rasterXSize - tileX * TileWidth);
                            int tileHeight = Math.Min(TileHeight, rasterYSize - tileY * TileHeight);
                            int tileOffsetX = tileX * TileWidth;
                            int tileOffsetY = tileY * TileHeight;
                            for (int y = 0; y < tileHeight; ++y)
                            {
                                for (int x = 0; x < tileWidth; ++x)
                                {
                                    ushort value = *(transformed + (tileOffsetY + y) * rasterXSize + (x + tileOffsetX));

                                    *(tile + y * TileWidth + x) = value;
                                }
                            }

                            GdalUtil.WritePng(tilePtr, TileWidth, TileHeight, Path.Combine(OutputPath, $"heightmap_x{tileX}_y{tileY}.png"));

                            Marshal.FreeHGlobal(tilePtr);
                        }
                    }


                    Application.Current.Dispatcher.Invoke(() => LoadingOverlayVisibility = Visibility.Collapsed);
                });
            }
            else
            {
                transformTask.ContinueWith(t =>
                {
                    GdalUtil.WritePng(_transformedPtr, rasterXSize, rasterYSize, Path.Combine(OutputPath, "heightmap.png"));

                    Application.Current.Dispatcher.Invoke(() => LoadingOverlayVisibility = Visibility.Collapsed);
                });
            }

        }

        private void BrowseInputPath()
        {
            var openFileDialog = new OpenFileDialog()
            {
                Filter = "TIF Files (.tif)|*.tif|PNG Files (.png)|*.png",
                FilterIndex = 1,
            };

            var userClickedOk = openFileDialog.ShowDialog();

            // Process input if the user clicked OK.
            if (userClickedOk == true)
            {
                InputPath = openFileDialog.FileName;
            }
        }


        private void BrowseOutputPath()
        {
        }
    }
}
