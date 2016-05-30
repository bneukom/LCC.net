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
        // Dragging points shared for training and prediction scroll viewers
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

                // @TODO transformation matrix
                var left = (_viewModel.AscFile.Xllcorner - _viewModel.WorldFile.X) / _viewModel.WorldFile.PixelSizeX;

                var topWorldCoordinates = _viewModel.AscFile.Yllcorner +
                                          _viewModel.AscFile.Cellsize * _viewModel.AscFile.Nrows;

                var topScreenCoordinates = (topWorldCoordinates - _viewModel.WorldFile.Y) /
                                           _viewModel.WorldFile.PixelSizeY;

                var width = _viewModel.AscFile.Ncols;
                var height = _viewModel.AscFile.Nrows;

                _viewModel.ExcludeGeometryRect = new Rect(new Point(left, topScreenCoordinates), new Size(width, height));
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
            if (_viewModel.WorldFile != null)
            {
                var position = e.GetPosition(TrainingOrthoImage);
                PixelPosition.Content = "(" + (int)position.X + ", " + (int)position.Y + ")";

                // Position in LV95 coordinate system
                var lv95X = (int)(position.X * _viewModel.WorldFile.PixelSizeX + _viewModel.WorldFile.X);
                var lv95Y = (int)(position.Y * _viewModel.WorldFile.PixelSizeY + _viewModel.WorldFile.Y);
                LV95Position.Content = "(" + lv95X + ", " + lv95Y + ")";

                // Color
                var color = _viewModel.GetColorAt(position);
                ColorLabel.Content = color;
                ColorLabel.Background = new SolidColorBrush(color);

                // AverageNeighbourhoodColor
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
                }
            }

            Drag(e, scrollViewer);
        }

 

        private void PredictionImageScrollViewer_MouseMove(object sender, MouseEventArgs e)
        {
            var scrollViewer = (ScrollViewer)sender;

            // Prediction at position
            if (_viewModel.WorldFile != null && _viewModel.AscFile != null && _viewModel.IsTrained)
            {
                var position = e.GetPosition(PredictionOrthoImage);

                var altitude = _viewModel.GetAscDataAt(position);
                var averageNeighborhoodColor = _viewModel.GetAverageNeighborhoodColor(position);
                var color = _viewModel.GetColorAt(position);
                var aspectSlope = _viewModel.GetSlopeAndAspectAt(position);
                var prediciton = _viewModel.Predict(new FeatureVector(altitude, color, averageNeighborhoodColor, aspectSlope.Aspect, aspectSlope.Slope));
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
            if (_viewModel.WorldFile != null && _viewModel.AscFile != null)
            {
                var position = e.GetPosition(TrainingOrthoImage);

                var color = _viewModel.GetColorAt(position);
                var averageNeighborhoodColor = _viewModel.GetAverageNeighborhoodColor(position);
                var altitude = _viewModel.GetAscDataAt(position);
                var landCoverType = _viewModel.SelectedLandCoverType;
                var slopeAspect = _viewModel.GetSlopeAndAspectAt(position);

                _viewModel.Features.Add(new ClassifiedFeatureVector(landCoverType, new FeatureVector(altitude, color, averageNeighborhoodColor, slopeAspect.Aspect, slopeAspect.Slope)));
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