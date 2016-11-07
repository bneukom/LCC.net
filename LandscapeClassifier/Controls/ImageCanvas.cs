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

            ClassifierViewModel viewModel = (ClassifierViewModel) DataContext;

            if (viewModel != null)
            {
                viewModel.MouseScreenPoisition = new Point(0, 0);
                viewModel.MouseWorldPoisition = new Point(0, 0);
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
                ClassifierViewModel viewModel = (ClassifierViewModel)DataContext;

                var mousePosition = mouseButtonEventArgs.GetPosition(this);
                Vector<double> mouseVec = _vecBuilder.DenseOfArray(new[] { mousePosition.X, mousePosition.Y, 1.0f});

                var transform = _scaleMat.Inverse()*_screenToViewMat.Inverse();


                // TODO translate

                ushort[] bandPixels = new ushort[viewModel.Bands.Count];
                int[] bandNumbers = new int[viewModel.Bands.Count];

                for (int bandIndex = 0; bandIndex < viewModel.Bands.Count; ++bandIndex)
                {
                    var band = viewModel.Bands[bandIndex];
                    ushort bandPixelValue = band.BandImage.GetUshortPixelValue((int)mousePosition.X, (int)mousePosition.Y);

                    bandPixels[bandIndex] = bandPixelValue;
                    bandNumbers[bandIndex] = band.BandNumber;
                }

                var classifiedFeatureVector = new ClassifiedFeatureVector(LandcoverType.Grass, new FeatureVector(bandPixels, bandNumbers));
                viewModel.Features.Add(new ClassifiedFeatureVectorViewModel(classifiedFeatureVector));
            }
        }

        private void OnMove(object sender, MouseEventArgs args)
        {
            ClassifierViewModel viewModel = (ClassifierViewModel) DataContext;
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
                // TODO divide by pixel resolution!
                var viewToWorld = viewModel.ScreenToWorld * _scaleMat.Inverse() * _screenToViewMat.Inverse();
                var screenToView = _scaleMat.Inverse() * _screenToViewMat.Inverse();
                var posVec = _vecBuilder.DenseOfArray(new[] {position.X, position.Y, 1});

                var mouseWorld = viewToWorld*posVec;
                var mouseView = screenToView*posVec;

                viewModel.MouseScreenPoisition = new Point((int)mouseView[0], (int)mouseView[1]);
                viewModel.MouseWorldPoisition = new Point(mouseWorld[0], mouseWorld[1]);
            }
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            // Background
            dc.DrawRectangle(Brushes.White, null, new Rect(0, 0, ActualWidth, ActualHeight));

            // Draw bands
            ClassifierViewModel viewModel = (ClassifierViewModel)DataContext;

            if (viewModel == null) return;

            foreach (var band in viewModel.Bands)
            { 
                if (!band.IsVisible) continue;
                var worldToScreen = _scaleMat * viewModel.WorldToScreen;

                var upperLeft = new Point(band.UpperLeft[0], band.UpperLeft[1]);
                var bottomRight = new Point(band.BottomRight[0], band.BottomRight[1]);

                var worldToView =  _screenToViewMat * worldToScreen;

                Matrix mat = new Matrix(
                    worldToView[0,0], worldToView[0,1], 
                    worldToView[1,0], worldToView[1,1], 
                    worldToView[0,2], worldToView[1,2]);

                MatrixTransform matTransform = new MatrixTransform(mat);

                dc.PushTransform(matTransform);
                dc.DrawImage(band.BandImage,new Rect(upperLeft,bottomRight));
                dc.Pop();
            }
        }
    }
}