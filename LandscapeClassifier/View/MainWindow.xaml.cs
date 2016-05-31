using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using LandscapeClassifier.Model;
using LandscapeClassifier.Util;
using LandscapeClassifier.ViewModel;
using MahApps.Metro.Controls;
using Microsoft.Win32;

namespace LandscapeClassifier.View
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        // Dragging points shared for training and prediction scroll viewers
        private Point? _lastCenterPositionOnTarget;
        private Point? _lastDragPoint;
        private Point? _lastMousePositionOnTarget;

        private readonly ViewModel.MainWindowViewModel _mainWindowViewModel;

        public MainWindow()
        {
            InitializeComponent();
            _mainWindowViewModel = (ViewModel.MainWindowViewModel) DataContext;
        }

        private void OpenDEM_Click(object sender, RoutedEventArgs e)
        {
            // Create an instance of the open file dialog box.
            var openFileDialog = new OpenFileDialog
            {
                Filter = "ASC Files (.asc)|*.asc",
                FilterIndex = 1,
                Multiselect = true
            };

            // Call the ShowDialog method to show the dialog box.
            var userClickedOk = openFileDialog.ShowDialog();

            // Process input if the user clicked OK.
            if (userClickedOk == true)
            {
                DEMPath.Content = openFileDialog.FileName;

                _mainWindowViewModel.AscFile = AscFile.FromFile(openFileDialog.FileName);

                // @TODO transformation matrix
                var left = (_mainWindowViewModel.AscFile.Xllcorner - _mainWindowViewModel.WorldFile.X) / _mainWindowViewModel.WorldFile.PixelSizeX;

                var topWorldCoordinates = _mainWindowViewModel.AscFile.Yllcorner +
                                          _mainWindowViewModel.AscFile.Cellsize * _mainWindowViewModel.AscFile.Nrows;

                var topScreenCoordinates = (topWorldCoordinates - _mainWindowViewModel.WorldFile.Y) /
                                           _mainWindowViewModel.WorldFile.PixelSizeY;

                var width = _mainWindowViewModel.AscFile.Ncols;
                var height = _mainWindowViewModel.AscFile.Nrows;

                _mainWindowViewModel.ViewportRect = new Rect(new Point(left, topScreenCoordinates), new Size(width, height));
            }
        }


        private void OpenOrthoImage_Click(object sender, RoutedEventArgs e)
        {
            // Create an instance of the open file dialog box.
            var openFileDialog = new OpenFileDialog
            {
                Filter = "TIF Files (.tif)|*.tif",
                FilterIndex = 1,
                Multiselect = true
            };

            // Call the ShowDialog method to show the dialog box.
            var userClickedOk = openFileDialog.ShowDialog();

            // Process input if the user clicked OK.
            if (userClickedOk == true)
            {
                var directoryName = Path.GetDirectoryName(openFileDialog.FileName);
                OrthoImagePath.Content = openFileDialog.FileName;

                // var sourceImage = new BitmapImage(new Uri(openFileDialog.FileName));

                var sourceImage = new BitmapImage();
                using (var stream = File.OpenRead(openFileDialog.FileName))
                {
                    sourceImage.BeginInit();
                    sourceImage.CacheOption = BitmapCacheOption.OnLoad;
                    sourceImage.StreamSource = stream;
                    sourceImage.EndInit();
                }

                var dpi = 96d;
                var width = sourceImage.PixelWidth;
                var height = sourceImage.PixelHeight;

                var stride = width * 4; // 4 bytes per pixel
                byte[] pixelData = new byte[stride * height];
                sourceImage.CopyPixels(pixelData, stride, 0);

                var scaledImage = BitmapSource.Create(width, height, dpi, dpi, PixelFormats.Bgra32, null, pixelData, stride);
                
                // _mainWindowViewModel.OrthoImage = new BitmapImage(new Uri(openFileDialog.FileName));
                _mainWindowViewModel.OrthoImage = scaledImage;

                var worldFilePath = directoryName + "\\" + Path.GetFileNameWithoutExtension(openFileDialog.FileName) +
                                    ".tfw";

                _mainWindowViewModel.WorldFile = WorldFile.FromFile(worldFilePath);
                OpenDEM.IsEnabled = true;

            }
        }

        private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var scrollViewer =  (ScrollViewer) sender;
            _lastMousePositionOnTarget = Mouse.GetPosition(scrollViewer);

            var centerOfViewport = new Point(scrollViewer.ViewportWidth / 2, scrollViewer.ViewportHeight / 2);
            _lastCenterPositionOnTarget = scrollViewer.TranslatePoint(centerOfViewport, scrollViewer);

            var position = e.GetPosition(scrollViewer);
            var transform = MatrixTransform; // TODO get transform from Image
            var matrix = transform.Matrix;
            var scale = e.Delta >= 0 ? 1.1 : 1.0/1.1;
           
            matrix.Scale(scale, scale);
            transform.Matrix = matrix;


            e.Handled = true;
        }

        private void TrainingImageScrollViewer_MouseMove(object sender, MouseEventArgs e)
        {
            var scrollViewer = (ScrollViewer)sender;

            // Update info label
            if (_mainWindowViewModel.WorldFile != null)
            {
                var position = e.GetPosition(TrainingOrthoImage);
                PixelPosition.Content = "(" + (int)position.X + ", " + (int)position.Y + ")";

                // Position in LV95 coordinate system
                var lv95X = (int)(position.X * _mainWindowViewModel.WorldFile.PixelSizeX + _mainWindowViewModel.WorldFile.X);
                var lv95Y = (int)(position.Y * _mainWindowViewModel.WorldFile.PixelSizeY + _mainWindowViewModel.WorldFile.Y);
                LV95Position.Content = "(" + lv95X + ", " + lv95Y + ")";

                // Color
                var color = _mainWindowViewModel.GetColorAt(position);
                ColorLabel.Content = color;
                ColorLabel.Background = new SolidColorBrush(color);

                // AverageNeighbourhoodColor
                var luma = _mainWindowViewModel.GetLuminance(position);
                LumaLabel.Content = (int)luma;

                if (_mainWindowViewModel.AscFile != null)
                {
                    // Altitude
                    var altitude = _mainWindowViewModel.GetAscDataAt(position);
                    AltitudeLabel.Content = (altitude != _mainWindowViewModel.AscFile.NoDataValue) ? altitude + "m" : "???";

                    // Slope and Aspect
                    var aspectSlope = _mainWindowViewModel.GetSlopeAndAspectAt(position);
                    AspectLabel.Content = Math.Round(MoreMath.ToDegrees(aspectSlope.Aspect), 2) + "°";
                    SlopeLabel.Content = Math.Round(MoreMath.ToDegrees(aspectSlope.Slope), 2) + "°";
                }
            }

            Drag(e, scrollViewer);
        }

 

        private void PredictionImageScrollViewer_MouseMove(object sender, MouseEventArgs e)
        {
            var scrollViewer = (ScrollViewer)sender;

            // Prediction at position
            if (_mainWindowViewModel.WorldFile != null && _mainWindowViewModel.AscFile != null && _mainWindowViewModel.IsTrained)
            {
                var position = e.GetPosition(PredictionOrthoImage);

                var altitude = _mainWindowViewModel.GetAscDataAt(position);
                var averageNeighborhoodColor = _mainWindowViewModel.GetAverageNeighborhoodColor(position);
                var color = _mainWindowViewModel.GetColorAt(position);
                var aspectSlope = _mainWindowViewModel.GetSlopeAndAspectAt(position);
                var prediciton = _mainWindowViewModel.Predict(new FeatureVector(altitude, color, averageNeighborhoodColor, aspectSlope.Aspect, aspectSlope.Slope));
                LandcoverPredictionLabel.Content = prediciton;
            }

            Drag(e, scrollViewer);
        }

        private void Drag(MouseEventArgs e, ScrollViewer scrollViewer)
        {
            // Drag
            if (_lastDragPoint.HasValue)
            {
                var posNow = e.GetPosition(scrollViewer);

                var dX = posNow.X - _lastDragPoint.Value.X;
                var dY = posNow.Y - _lastDragPoint.Value.Y;

                _lastDragPoint = posNow;

                scrollViewer.Cursor = Cursors.SizeAll;

                scrollViewer.ScrollToHorizontalOffset(scrollViewer.HorizontalOffset - dX);
                scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - dY);
            }
        }

        private void ImageScrollViewer_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var scrollViewer = (ScrollViewer)sender;

            var mousePos = e.GetPosition(scrollViewer);
            if (mousePos.X <= scrollViewer.ViewportWidth && mousePos.Y < scrollViewer.ViewportHeight) //make sure we still can use the scrollbars
            {
                _lastDragPoint = mousePos;
                Mouse.Capture(scrollViewer);
            }
        }

        private void ImageScrollViewer_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var scrollViewer = (ScrollViewer)sender;

            scrollViewer.Cursor = Cursors.Arrow;
            scrollViewer.ReleaseMouseCapture();

            _lastDragPoint = null;
        }

        private void TrainingImageScrollViewer_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            // Add feature
            if (_mainWindowViewModel.WorldFile != null && _mainWindowViewModel.AscFile != null)
            {
                var position = e.GetPosition(TrainingOrthoImage);

                var color = _mainWindowViewModel.GetColorAt(position);
                var averageNeighborhoodColor = _mainWindowViewModel.GetAverageNeighborhoodColor(position);
                var altitude = _mainWindowViewModel.GetAscDataAt(position);
                var landCoverType = _mainWindowViewModel.SelectedLandCoverType;
                var slopeAspect = _mainWindowViewModel.GetSlopeAndAspectAt(position);

                _mainWindowViewModel.Features.Add(new ClassifiedFeatureVectorViewModel(new ClassifiedFeatureVector(landCoverType, new FeatureVector(altitude, color, averageNeighborhoodColor, slopeAspect.Aspect, slopeAspect.Slope))));
            }
        }

        private void PredictionImageScrollViewer_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
        }

        private void ImageScrollViewer_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {

        }
    }
}