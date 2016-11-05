using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using LandscapeClassifier.Controls;
using LandscapeClassifier.Model;
using LandscapeClassifier.Util;
using LandscapeClassifier.View.Open;
using LandscapeClassifier.ViewModel;
using MahApps.Metro.Controls;
using Microsoft.Win32;
using OSGeo.GDAL;

namespace LandscapeClassifier.View
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        private readonly ViewModel.MainWindowViewModel _mainWindowViewModel;

        // Dragging points shared for training and prediction scroll viewers
        private Point? _lastCenterPositionOnTarget;
        private Point? _lastDragPoint;
        private Point? _lastMousePositionOnTarget;

        public MainWindow()
        {
            InitializeComponent();
            _mainWindowViewModel = (ViewModel.MainWindowViewModel) DataContext;

            /*
            // Workaround for https://social.msdn.microsoft.com/Forums/vstudio/en-US/bdf2f91e-469a-4931-b5aa-35c5ce58591f/tabcontrol-how-to-get-own-scrollviewer-position-on-each-tab?forum=wpf
            // visual tree is the same for all tabs and casues rendering issues with layout transforms.
            _mainWindowViewModel.ImageBandViewModels.CollectionChanged += (sender, args) =>
            {
                if (args.NewItems != null)
                {
                    foreach (ImageBandViewModel item in args.NewItems)
                    {
                        TabItem tab = new TabItem {Header = item.Title};
                        var content = new ImageControl {DataContext = item};
                        tab.Content = content;
                        BandsTabs.Items.Add(tab);
                    }
                }
                
            };
            */
        }

        private void OpenDEM_Click(object sender, RoutedEventArgs e)
        {
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

        private void BandTabsOnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var source = (TabControl) e.Source;
            //source.Dispatcher.Invoke(DispatcherPriority.Render, EmptyDelegate);
            //source.InvalidateVisual();
        }

        private static readonly Action EmptyDelegate = delegate () { };
    }
}