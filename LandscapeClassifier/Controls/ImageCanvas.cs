using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using LandscapeClassifier.Extensions;
using LandscapeClassifier.Model;
using LandscapeClassifier.Model.Classification;
using LandscapeClassifier.ViewModel;
using LandscapeClassifier.ViewModel.BandsCanvas;
using LandscapeClassifier.ViewModel.MainWindow;
using MathNet.Numerics.LinearAlgebra;
using OSGeo.GDAL;
using OSGeo.OSR;

namespace LandscapeClassifier.Controls
{
    public class ImageCanvas : Canvas
    {
        private bool _drag;
        private Point? _lastMousePosition;

        private Matrix<double> _scaleMat;
        private Matrix<double> _screenToViewMat;

        private readonly Timer _timer;
        private readonly VectorBuilder<double> _vecBuilder = Vector<double>.Build;
        private readonly MatrixBuilder<double> _matrixBuilder = Matrix<double>.Build;

        public ImageCanvas()
        {
            _scaleMat = _matrixBuilder.DenseOfArray(new[,]
            {
                {1/50.0, 0, 0},
                {0, 1/50.0, 0},
                {0, 0, 1}
            });

            _screenToViewMat = _matrixBuilder.DenseOfArray(new[,]
            {
                {1, 0, 128.0},
                {0, 1, 128.0},
                {0, 0, 1}
            });

            ClipToBounds = true;

            // redraw timer
            _timer = new Timer((o) =>
            {
                if (Application.Current != null) Application.Current.Dispatcher.Invoke(InvalidateVisual);
                else _timer.Change(0, Timeout.Infinite);
            }, this, 1000, 33);

            MouseMove += OnMove;
            MouseDown += OnMouseDown;
            MouseUp += OnMouseUp;
            MouseLeave += OnMouseLeave;
            MouseEnter += OnMouseEnter;
            MouseWheel += OnMouseWheel;

            RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.NearestNeighbor);
        }

        private void OnMouseWheel(object sender, MouseWheelEventArgs mouseWheelEventArgs)
        {
            double scale = mouseWheelEventArgs.Delta < 0 ? 0.9 : 1.1;
            var scaleMat = _matrixBuilder.DenseOfArray(new[,]
            {
                { _scaleMat[0, 0] * scale, 0, 0},
                {0, _scaleMat[1, 1] * scale, 0},
                {0, 0, 1}
            });

            _scaleMat = scaleMat;

            Console.WriteLine(mouseWheelEventArgs.Delta);
        }

        private void OnMouseEnter(object sender, MouseEventArgs mouseEventArgs)
        {
            if (Mouse.LeftButton == MouseButtonState.Pressed)
            {
                _drag = true;
                _lastMousePosition = mouseEventArgs.GetPosition(this);
            }
        }

        private void OnMouseLeave(object sender, MouseEventArgs mouseEventArgs)
        {
            _drag = false;
            _lastMousePosition = null;

            MainWindowViewModel viewModel = (MainWindowViewModel) DataContext;

            if (viewModel != null)
            {
                viewModel.ClassifierViewModel.MouseScreenPoisition = new Point(0, 0);
                viewModel.ClassifierViewModel.MouseWorldPoisition = new Point(0, 0);
            }
        }

        private void OnMouseUp(object sender, MouseButtonEventArgs mouseButtonEventArgs)
        {
            _drag = false;
            _lastMousePosition = null;
        }

        private void OnMouseDown(object sender, MouseButtonEventArgs mouseButtonEventArgs)
        {
            if (Mouse.LeftButton == MouseButtonState.Pressed)
            {
                _drag = true;
                _lastMousePosition = mouseButtonEventArgs.GetPosition(this);
            }
            else
            {
                MainWindowViewModel viewModel = (MainWindowViewModel)DataContext;

                var mousePosition = mouseButtonEventArgs.GetPosition(this);
                Vector<double> mouseVec = _vecBuilder.DenseOfArray(new[] { mousePosition.X, mousePosition.Y, 1.0f});

                var screenToView = _scaleMat.Inverse() * _screenToViewMat.Inverse();

                var featureBands = viewModel.ClassifierViewModel.Bands.Where(b => b.IsFeature).OrderBy(b => b.BandNumber).ToList();

                ushort[] bandPixels = new ushort[featureBands.Count];

                for (int bandIndex = 0; bandIndex < featureBands.Count; ++bandIndex)
                {
                    var band = featureBands[bandIndex];
                    var bandPixelPosition = screenToView* mouseVec / band.MetersPerPixel;
                    
                    ushort bandPixelValue = band.BandImage.GetUshortPixelValue((int)bandPixelPosition[0], (int)bandPixelPosition[1]);
                    Console.WriteLine("pixel value at (" + (int)bandPixelPosition[0] + ", " + (int)bandPixelPosition[1] + "): " + bandPixelValue);

                    bandPixels[bandIndex] = bandPixelValue;
                }

                var classifiedFeatureVector = new ClassifiedFeatureVector(viewModel.ClassifierViewModel.SelectedLandCoverType, new FeatureVector(bandPixels));
                viewModel.ClassifierViewModel.Features.Add(new ClassifiedFeatureVectorViewModel(classifiedFeatureVector));
            }
        }

        private void OnMove(object sender, MouseEventArgs args)
        {
            MainWindowViewModel viewModel = (MainWindowViewModel) DataContext;
            if (viewModel == null) return;

            var position = args.GetPosition(this);

            if (_drag && _lastMousePosition.HasValue)
            {
                double deltaX = position.X - _lastMousePosition.Value.X;
                double deltaY = position.Y - _lastMousePosition.Value.Y;
                var translate = _matrixBuilder.DenseOfArray(new[,]
                {
                    {0, 0, deltaX},
                    {0, 0, deltaY},
                    {0, 0, 0}
                });

                _screenToViewMat += translate;
                _lastMousePosition = position;
            }
            else
            {
                var screenToView = _scaleMat.Inverse() * _screenToViewMat.Inverse();
                var viewToWorld = viewModel.ClassifierViewModel.ScreenToWorld * _scaleMat.Inverse() * _screenToViewMat.Inverse();
                
                var posVec = _vecBuilder.DenseOfArray(new[] {position.X, position.Y, 1});

                var mouseWorld = viewToWorld*posVec;

                viewModel.ClassifierViewModel.MouseScreenPoisition = position;
                viewModel.ClassifierViewModel.MouseWorldPoisition = new Point(mouseWorld[0], mouseWorld[1]);

                var featureBands = viewModel.ClassifierViewModel.Bands.Where(b => b.IsFeature).ToList();

                for (int bandIndex = 0; bandIndex < featureBands.Count; ++bandIndex)
                {
                    var band = featureBands[bandIndex];
                    var bandPixelPosition = screenToView * posVec / band.MetersPerPixel;

                    ushort bandPixelValue = band.BandImage.GetUshortPixelValue((int)bandPixelPosition[0], (int)bandPixelPosition[1]);

                    byte grayScale = (byte) (((float) bandPixelValue/ushort.MaxValue)*byte.MaxValue);
                    band.CurrentPositionBrush = new SolidColorBrush(Color.FromRgb(grayScale, grayScale, grayScale));
                }
            }
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            // Background
            dc.DrawRectangle(Brushes.White, null, new Rect(0, 0, ActualWidth, ActualHeight));

            // Draw bands
            MainWindowViewModel viewModel = (MainWindowViewModel)DataContext;

            if (viewModel == null) return;

            foreach (var band in viewModel.ClassifierViewModel.Bands)
            { 
                if (!band.IsVisible) continue;
                var worldToScreen = _scaleMat * viewModel.ClassifierViewModel.WorldToScreen;

                var worldToView =  _screenToViewMat * worldToScreen;

                var upperLeft = worldToView * band.UpperLeft;
                var bottomRight = worldToView * band.BottomRight;

                dc.DrawImage(band.BandImage,new Rect(new Point(upperLeft[0], upperLeft[1]), new Point(bottomRight[0], bottomRight[1])));
            }
        }
    }
}