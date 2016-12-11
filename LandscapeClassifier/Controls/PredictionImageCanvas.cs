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
using Brushes = System.Windows.Media.Brushes;
using Point = System.Windows.Point;

namespace LandscapeClassifier.Controls
{
    public class PredictionImageCanvas : ImageCanvasBase
    {
        private readonly VectorBuilder<double> _vecBuilder = Vector<double>.Build;
        private readonly Pen _predictionBorderAreaPen;

        public PredictionImageCanvas()
        {
            MouseMove += OnMove;
            MouseLeave += OnMouseLeave;

            _predictionBorderAreaPen = new Pen(Brushes.DarkGreen, 5.0) {DashStyle = DashStyles.Dot};
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
            var posVec = _vecBuilder.DenseOfArray(new[] { position.X, position.Y, 1 });

            var featureBands = viewModel.Layers.Where(b => b.IsFeature).ToList();

            ushort[] bandIntensities = new ushort[featureBands.Count];
            for (int bandIndex = 0; bandIndex < featureBands.Count; ++bandIndex)
            {
                var band = featureBands[bandIndex];
                var viewToPixelMat = band.WorldToImage * viewModel.ClassifierViewModel.ScreenToWorld * _scaleMat.Inverse() * _screenToViewMat.Inverse();

                var bandPixelPosition = viewToPixelMat * posVec;

                ushort bandIntensity = band.BandImage.GetScaledToUshort((int)bandPixelPosition[0], (int)bandPixelPosition[1]);
                bandIntensities[bandIndex] = bandIntensity;
            }

            if (viewModel.ClassifierViewModel.IsTrained)
            {
                var featureVector = new FeatureVector(bandIntensities);
                var predictedType = viewModel.ClassifierViewModel.CurrentClassifierViewModel.Classifier.Predict(featureVector);
                var predictionProbabilty = viewModel.ClassifierViewModel.CurrentClassifierViewModel.Classifier.Probabilty(featureVector, (int)LandcoverType.Snow);

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
            dc.DrawRectangle(Brushes.Gray, null, new Rect(0, 0, ActualWidth, ActualHeight));

            // Draw bands
            MainWindowViewModel viewModel = (MainWindowViewModel)DataContext;
            if (viewModel == null) return;

            // White background if there are visible bands
            if (viewModel.Layers.Any(b => b.IsVisible)) dc.DrawRectangle(Brushes.White, null, new Rect(0, 0, ActualWidth, ActualHeight));

            var predictionViewModel = viewModel.PredictionViewModel;
            foreach (var band in viewModel.Layers)
            {
                if (!band.IsVisible) continue;
                DrawBand(dc, band, predictionViewModel.WorldToScreen);
            }

            if (predictionViewModel.PredictionUpperLeftWorld != null &&
                predictionViewModel.PredictionBottomRightWorld != null)
            {
                dc.PushOpacity(0.5);
                DrawProjectedRect(dc, predictionViewModel.PredictionUpperLeftWorld, predictionViewModel.PredictionBottomRightWorld, _predictionBorderAreaPen, viewModel.ClassifierViewModel.WorldToScreen);
                dc.Pop();
            }

            if (predictionViewModel.ClassificationOverlay != null)
            {
                dc.PushOpacity(viewModel.PredictionViewModel.OverlayOpacity);
                var bandUpperLeft = viewModel.PredictionViewModel.PredictionUpperLeftWorld;
                var bandBottomRight = viewModel.PredictionViewModel.PredictionBottomRightWorld;
                DrawClassification(dc, bandUpperLeft, bandBottomRight);
                dc.Pop();
            }

        }
    }
}
