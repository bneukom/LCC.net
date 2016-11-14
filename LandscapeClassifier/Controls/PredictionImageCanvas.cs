using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using LandscapeClassifier.Extensions;
using LandscapeClassifier.Model;
using LandscapeClassifier.Model.Classification;
using LandscapeClassifier.Util;
using LandscapeClassifier.ViewModel.MainWindow;
using LandscapeClassifier.ViewModel.MainWindow.Prediction;
using MathNet.Numerics.LinearAlgebra;

namespace LandscapeClassifier.Controls
{
    public class PredictionImageCanvas : ImageCanvasBase
    {
        private readonly VectorBuilder<double> _vecBuilder = Vector<double>.Build;

        public PredictionImageCanvas()
        {
            MouseMove += OnMove;
            MouseLeave += OnMouseLeave;
        }

        private void OnMouseLeave(object sender, MouseEventArgs mouseEventArgs)
        {
            MainWindowViewModel viewModel = (MainWindowViewModel)DataContext;
            if (viewModel == null) return;

            viewModel.PredictionViewModel.MousePredictionType = LandcoverType.None;
            viewModel.PredictionViewModel.MousePredictionProbability = 0.0;
        }

        private void OnMove(object sender, MouseEventArgs args)
        {
            MainWindowViewModel viewModel = (MainWindowViewModel)DataContext;
            if (viewModel == null) return;

            var position = args.GetPosition(this);

            var screenToView = _scaleMat.Inverse() * _screenToViewMat.Inverse();

            var posVec = _vecBuilder.DenseOfArray(new[] { position.X, position.Y, 1 });

            var featureBands = viewModel.Layers.Where(b => b.IsFeature).ToList();

            ushort[] bandIntensities = new ushort[featureBands.Count];
            for (int bandIndex = 0; bandIndex < featureBands.Count; ++bandIndex)
            {
                var band = featureBands[bandIndex];
                var bandScaleVec = _vecBuilder.DenseOfArray(new[] { band.ScaleX, band.ScaleY, 1 });
                var bandPixelPosition = screenToView * posVec / bandScaleVec;



                ushort bandIntensity = band.BandImage.GetUshortPixelValue((int)bandPixelPosition[0], (int)bandPixelPosition[1]);
                bandIntensities[bandIndex] = bandIntensity;
            }

            if (viewModel.ClassifierViewModel.IsTrained)
            {
                var featureVector = new FeatureVector(bandIntensities);
                var predictedType = viewModel.ClassifierViewModel.CurrentClassifier.Predict(featureVector);
                var predictionProbabilty = viewModel.ClassifierViewModel.CurrentClassifier.PredictionProbabilty(featureVector);

                viewModel.PredictionViewModel.MousePredictionType = predictedType;
                viewModel.PredictionViewModel.MousePredictionProbability = predictionProbabilty;
            }
        }

        private void DrawClassification(DrawingContext dc, Vector<double> bandUpperLeft, Vector<double> bandBottomRight)
        {
            MainWindowViewModel viewModel = (MainWindowViewModel)DataContext;

            var worldToScreenScaled = _scaleMat * viewModel.PredictionViewModel.WorldToScreen;

            var worldToView = _screenToViewMat * worldToScreenScaled;

            var upperLeft = worldToView * bandUpperLeft;
            var bottomRight = worldToView * bandBottomRight;

            dc.PushOpacity(viewModel.PredictionViewModel.OverlayOpacity);
            dc.DrawImage(viewModel.PredictionViewModel.ClassificationOverlay, new Rect(new Point(upperLeft[0], upperLeft[1]), new Point(bottomRight[0], bottomRight[1])));
            dc.Pop();
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            // Background
            dc.DrawRectangle(Brushes.White, null, new Rect(0, 0, ActualWidth, ActualHeight));

            // Draw bands
            MainWindowViewModel viewModel = (MainWindowViewModel)DataContext;

            if (viewModel == null) return;

            DrawBand(viewModel.PredictionViewModel.VisibleLayer, dc, viewModel.PredictionViewModel.WorldToScreen);

            if (viewModel.PredictionViewModel.ClassificationOverlay != null)
            {
                var bandUpperLeft = viewModel.PredictionViewModel.VisibleLayer.UpperLeft;
                var bandBottomRight = viewModel.PredictionViewModel.VisibleLayer.BottomRight;
                DrawClassification(dc, bandUpperLeft, bandBottomRight);
            }

        }
    }
}
