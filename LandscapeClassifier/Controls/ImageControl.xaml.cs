using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace LandscapeClassifier.Controls
{
    /// <summary>
    /// Interaction logic for ImageControl.xaml
    /// </summary>
    public partial class ImageControl : UserControl
    {
        // Dragging points shared for training and prediction scroll viewers
        private Point? _lastCenterPositionOnTarget;
        private Point? _lastDragPoint;
        private Point? _lastMousePositionOnTarget;


        public ImageControl()
        {
            InitializeComponent();
            var mat = Matrix.Identity;
            mat.Scale(0.5,0.5);
            // MatrixTransform.Matrix = mat;
        }

        private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            /*
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
            */
        }

        private void TrainingImageScrollViewer_MouseMove(object sender, MouseEventArgs e)
        {
            var scrollViewer = (ScrollViewer)sender;

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
            if (mousePos.X <= scrollViewer.ViewportWidth && mousePos.Y < scrollViewer.ViewportHeight)
            //make sure we still can use the scrollbars
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
            /*
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
            */
        }

        private void ImageScrollViewer_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
        }
    }


}
