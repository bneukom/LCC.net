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
using LandscapeClassifier.ViewModel.MainWindow.Classification;
using MathNet.Numerics.LinearAlgebra;

namespace LandscapeClassifier.Controls
{
    public class ImageCanvasBase : Canvas
    {
        protected Matrix<double> _scaleMat;
        protected Matrix<double> _screenToViewMat;
        private readonly Timer _timer;
        private bool _isMouseInside = true;
        private bool _drag;
        private Point? _lastMousePosition;

        protected readonly MatrixBuilder<double> _matrixBuilder = Matrix<double>.Build;

        public ImageCanvasBase()
        {
            ClipToBounds = true;

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


            // redraw timer
            _timer = new Timer((o) =>
            {
                if (!_isMouseInside) return;

                if (Application.Current != null)
                {
                    Application.Current.Dispatcher.Invoke(InvalidateVisual);
                }
                else
                {
                    _timer.Change(0, Timeout.Infinite);
                }
            }, this, 1000, 33);

            MouseEnter += OnEnter;
            MouseLeave += OnLeave;
            MouseUp += OnMouseUp;
            MouseDown += OnMouseDown;
            MouseMove += OnMove;
            MouseWheel += OnMouseWheel;
        }

        private void OnMove(object sender, MouseEventArgs args)
        {
            _isMouseInside = true;

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
        }

        private void OnMouseDown(object sender, MouseButtonEventArgs mouseButtonEventArgs)
        {
            if (Mouse.LeftButton == MouseButtonState.Pressed)
            {
                _drag = true;
                _lastMousePosition = mouseButtonEventArgs.GetPosition(this);
            }
        }

        private void OnMouseUp(object sender, MouseButtonEventArgs mouseButtonEventArgs)
        {
            _drag = false;
            _lastMousePosition = null;
        }

        private void OnLeave(object sender, MouseEventArgs args)
        {
            _isMouseInside = false;

            _drag = false;
            _lastMousePosition = null;
        }

        private void OnEnter(object sender, MouseEventArgs args)
        {
            _isMouseInside = true;

            if (Mouse.LeftButton == MouseButtonState.Pressed)
            {
                _drag = true;
                _lastMousePosition = args.GetPosition(this);
            }
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
        }

        protected void DrawBand(BandViewModel band, DrawingContext dc, Matrix<double> worldToScreen)
        {
            var worldToScreenScaled = _scaleMat * worldToScreen;

            var worldToView = _screenToViewMat * worldToScreenScaled;

            var upperLeft = worldToView * band.UpperLeft;
            var bottomRight = worldToView * band.BottomRight;

            dc.DrawImage(band.BandImage, new Rect(new Point(upperLeft[0], upperLeft[1]), new Point(bottomRight[0], bottomRight[1])));
        }
    }
}
