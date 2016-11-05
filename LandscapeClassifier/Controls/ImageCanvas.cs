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
using LandscapeClassifier.ViewModel;
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

            ImageBandViewModel viewModel = (ImageBandViewModel) DataContext;
            viewModel.MouseScreenPoisition = new Point(0, 0);
            viewModel.MouseWorldPoisition = new Point(0, 0);
        }

        private void OnMouseUp(object sender, MouseButtonEventArgs mouseButtonEventArgs)
        {
            _drag = false;
            _lastMousePosition = null;
        }

        private void OnMouseDown(object sender, MouseButtonEventArgs mouseButtonEventArgs)
        {
            _drag = true;
            _lastMousePosition = mouseButtonEventArgs.GetPosition(this);
        }

        private void OnMove(object sender, MouseEventArgs args)
        {
            ImageBandViewModel viewModel = (ImageBandViewModel) DataContext;
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
                var viewToWorld = viewModel.Band.ScreenToWorld * _scaleMat.Inverse() * _screenToViewMat.Inverse();
                var posVec = _vecBuilder.DenseOfArray(new[] {position.X, position.Y, 1});
                var transformed = viewToWorld*posVec;

                viewModel.MouseScreenPoisition = position;
                viewModel.MouseWorldPoisition = new Point(transformed[0], transformed[1]);
            }
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            ImageBandViewModel viewModel = (ImageBandViewModel) DataContext;

            /*
            var src = new SpatialReference(viewModel.Band.ProjectionName);
            var dst = new SpatialReference("+proj=utm +zone=32 +datum=WGS84 +units=m +no_defs");

            CoordinateTransformation transformation = new CoordinateTransformation(src,dst);
            double[] ret = new double[3];
            transformation.TransformPoint(ret, viewModel.Band.UpperLeft[0], viewModel.Band.UpperLeft[1], 1);
            */

            var worldToScreen = _scaleMat * viewModel.Band.WorldToScreen;

            var upperLeft = new Point(viewModel.Band.UpperLeft[0], viewModel.Band.UpperLeft[1]);
            var bottomRight = new Point(viewModel.Band.BottomRight[0], viewModel.Band.BottomRight[1]);

            var worldToView =  _screenToViewMat * worldToScreen;

            // Background
            dc.DrawRectangle(Brushes.White, null, new Rect(0, 0, ActualWidth, ActualHeight));

            Matrix mat = new Matrix(
                worldToView[0,0], worldToView[0,1], 
                worldToView[1,0], worldToView[1,1], 
                worldToView[0,2], worldToView[1,2]);

            MatrixTransform matTransform = new MatrixTransform(mat);

            dc.PushTransform(matTransform);
            dc.DrawImage(viewModel.BandImage,new Rect(upperLeft,bottomRight));
            dc.Pop();
        }
    }
}