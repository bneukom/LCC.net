using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using LandscapeClassifier.ViewModel;
using MathNet.Numerics.LinearAlgebra;
using OSGeo.GDAL;
using OSGeo.OSR;

namespace LandscapeClassifier.Controls
{
    public class ImageCanvas : Canvas
    {
        private int _scale;
        private readonly Matrix<double> _scaleMat;
        private readonly Timer _timer;
        private readonly VectorBuilder<double> _vecBuilder = Vector<double>.Build;
        private readonly MatrixBuilder<double> _matrixBuilder = Matrix<double>.Build;

        public ImageCanvas()
        {
            _scaleMat = Matrix<double>.Build.DenseOfArray(new[,]
            {
                {1/128.0, 0, 0},
                {0, 1/128.0, 0},
                {0, 0, 1}
            });


            ClipToBounds = true;

            // redraw timer
            _timer = new Timer((o) =>
            {
                if (Application.Current != null) Application.Current.Dispatcher.Invoke(InvalidateVisual);
                else _timer.Change(0, Timeout.Infinite);
                
            }, this, 1000, 33);

            MouseMove += (sender, args) =>
            {
                ImageBandViewModel viewModel = (ImageBandViewModel)DataContext;
                var position = args.GetPosition(this);
                var transformMat = viewModel.Band.Transform;
                var posVec = _vecBuilder.DenseOfArray(new double[] { position.X, position.Y, 1});
                var transformed = transformMat * _scaleMat.Inverse() * posVec;
                Console.WriteLine(transformed.ToString());
            };
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            ImageBandViewModel viewModel = (ImageBandViewModel)DataContext;

            /*
            var src = new SpatialReference(viewModel.Band.ProjectionName);
            var dst = new SpatialReference("+proj=utm +zone=32 +datum=WGS84 +units=m +no_defs");

            CoordinateTransformation transformation = new CoordinateTransformation(src,dst);
            double[] ret = new double[3];
            transformation.TransformPoint(ret, viewModel.Band.UpperLeft[0], viewModel.Band.UpperLeft[1], 1);
            */

            var transformMat = viewModel.Band.InverseTransform;
           

            var topLeftScreen = _scaleMat * transformMat * viewModel.Band.UpperLeft ;
            var bottomRightScreen = _scaleMat * transformMat * viewModel.Band.BottomRight;

            var translate = _matrixBuilder.DenseOfArray(new[,]
            {
                {1, 0, (bottomRightScreen[0] - topLeftScreen[0]) / 2},
                {0, 1, (bottomRightScreen[1] - topLeftScreen[1]) / 2},
                {0, 0, 1}
            });

            var scaledTopLeft = translate.Inverse() * _scaleMat * translate * topLeftScreen;
            var scaledBottomRight = translate.Inverse() * _scaleMat * translate * bottomRightScreen;


            dc.DrawImage(viewModel.BandImage, new Rect(topLeftScreen[0], topLeftScreen[1], bottomRightScreen[0], bottomRightScreen[1]));
        }
    }
}
