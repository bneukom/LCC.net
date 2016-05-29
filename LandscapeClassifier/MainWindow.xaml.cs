using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using LandscapeClassifier.Model;
using LandscapeClassifier.Util;
using MahApps.Metro.Controls;
using Microsoft.Win32;
using Xceed.Wpf.Toolkit.PropertyGrid;

namespace LandscapeClassifier
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        private Point? _lastCenterPositionOnTarget;
        private Point? _lastDragPoint;
        private Point? _lastMousePositionOnTarget;

        private readonly ViewModel.ViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = (ViewModel.ViewModel) DataContext;
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

                _viewModel.AscFile = AscFile.FromFile(openFileDialog.FileName);

                var left = (_viewModel.AscFile.Xllcorner - _viewModel.WorldFile.X) / _viewModel.WorldFile.PixelSizeX;

                var topWorldCoordinates = _viewModel.AscFile.Yllcorner +
                                          _viewModel.AscFile.Cellsize * _viewModel.AscFile.Nrows;

                var topScreenCoordinates = (topWorldCoordinates - _viewModel.WorldFile.Y) /
                                           _viewModel.WorldFile.PixelSizeY;

                var width = _viewModel.AscFile.Ncols;
                var height = _viewModel.AscFile.Nrows;

                ExcludeGeometry.Rect = new Rect(new Point(left, topScreenCoordinates), new Size(width, height));
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

                _viewModel.OrthoImage = new BitmapImage(new Uri(openFileDialog.FileName));

                var worldFilePath = directoryName + "\\" + Path.GetFileNameWithoutExtension(openFileDialog.FileName) +
                                    ".tfw";

                _viewModel.WorldFile = WorldFile.FromFile(worldFilePath);
                OpenDEM.IsEnabled = true;
            }
        }

        private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            _lastMousePositionOnTarget = Mouse.GetPosition(grid);

            var centerOfViewport = new Point(ImageScrollViewer.ViewportWidth / 2, ImageScrollViewer.ViewportHeight / 2);
            _lastCenterPositionOnTarget = ImageScrollViewer.TranslatePoint(centerOfViewport, grid);

            var position = e.GetPosition(grid);
            var transform = MatrixTransform;
            var matrix = transform.Matrix;
            var scale = e.Delta >= 0 ? 1.1 : 1.0/1.1;
            matrix.ScaleAtPrepend(scale, scale, 5000, 5000);
            transform.Matrix = matrix;

            e.Handled = true;
        }

        private void ImageScrollViewer_MouseMove(object sender, MouseEventArgs e)
        {
            // Update info label
            if (_viewModel.WorldFile != null)
            {
                var position = e.GetPosition(OrthoImage);
                PixelPosition.Content = "(" + (int)position.X + ", " + (int)position.Y + ")";

                // Position in LV95 coordinate system
                var lv95X = (int) (position.X * _viewModel.WorldFile.PixelSizeX + _viewModel.WorldFile.X);
                var lv95Y = (int) (position.Y * _viewModel.WorldFile.PixelSizeY + _viewModel.WorldFile.Y);
                LV95Position.Content = "(" + lv95X + ", " + lv95Y + ")";

                // Color
                var color = _viewModel.GetColorAt(position);
                ColorLabel.Content = color;
                ColorLabel.Background = new SolidColorBrush(color);

                // Luma
                var luma = _viewModel.GetLuminance(position);
                LumaLabel.Content = (int)luma;

                if (_viewModel.AscFile != null)
                {
                    // Altitude
                    var altitude = _viewModel.GetAscDataAt(position);
                    AltitudeLabel.Content = (altitude != _viewModel.AscFile.NoDataValue) ? altitude + "m" : "???";

                    // Slope and Aspect
                    var aspectSlope = _viewModel.GetSlopeAndAspectAt(position);
                    AspectLabel.Content = Math.Round(MoreMath.ToDegrees(aspectSlope.Aspect), 2) + "°";
                    SlopeLabel.Content = Math.Round(MoreMath.ToDegrees(aspectSlope.Slope), 2) + "°";

                    // TODO prediction test
                    if (_viewModel.IsTrained)
                    {
                        Console.WriteLine(_viewModel.Predict(new FeatureVector(altitude, luma, color, aspectSlope.Aspect, aspectSlope.Slope)));
                    }
                }
            }


            // Drag
            if (_lastDragPoint.HasValue)
            {
                var posNow = e.GetPosition(ImageScrollViewer);

                var dX = posNow.X - _lastDragPoint.Value.X;
                var dY = posNow.Y - _lastDragPoint.Value.Y;

                _lastDragPoint = posNow;

                ImageScrollViewer.Cursor = Cursors.SizeAll;

                ImageScrollViewer.ScrollToHorizontalOffset(ImageScrollViewer.HorizontalOffset - dX);
                ImageScrollViewer.ScrollToVerticalOffset(ImageScrollViewer.VerticalOffset - dY);
            }
        }

        private void ImageScrollViewer_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var mousePos = e.GetPosition(ImageScrollViewer);
            if (mousePos.X <= ImageScrollViewer.ViewportWidth && mousePos.Y < ImageScrollViewer.ViewportHeight) //make sure we still can use the scrollbars
            {
                
                _lastDragPoint = mousePos;
                Mouse.Capture(ImageScrollViewer);
            }
        }

        private void ImageScrollViewer_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {

            ImageScrollViewer.Cursor = Cursors.Arrow;
            ImageScrollViewer.ReleaseMouseCapture();

            _lastDragPoint = null;
        }

        private void ImageScrollViewer_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_viewModel.WorldFile != null && _viewModel.AscFile != null)
            {
                var position = e.GetPosition(OrthoImage);

                var color = _viewModel.GetColorAt(position);
                var luma = _viewModel.GetLuminance(position);
                var altitude = _viewModel.GetAscDataAt(position);
                var landCoverType = _viewModel.SelectedLandCoverType;
                var slopeAspect = _viewModel.GetSlopeAndAspectAt(position);

                _viewModel.Features.Add(new ClassifiedFeatureVector(landCoverType, new FeatureVector(altitude, luma, color, slopeAspect.Aspect, slopeAspect.Slope)));
            }
        }

        private void ImageScrollViewer_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {

        }


    }
}