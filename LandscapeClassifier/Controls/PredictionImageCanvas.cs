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
        }

        private void OnMove(object sender, MouseEventArgs args)
        {
            MainWindowViewModel viewModel = (MainWindowViewModel)DataContext;
            if (viewModel == null) return;

            var position = args.GetPosition(this);

            var screenToView = _scaleMat.Inverse() * _screenToViewMat.Inverse();

            var posVec = _vecBuilder.DenseOfArray(new[] { position.X, position.Y, 1 });

            var featureBands = viewModel.Bands.Where(b => b.IsFeature).ToList();

            ushort[] bandIntensities = new ushort[featureBands.Count];
            for (int bandIndex = 0; bandIndex < featureBands.Count; ++bandIndex)
            {
                var band = featureBands[bandIndex];
                var bandPixelPosition = screenToView * posVec / band.MetersPerPixel;

                ushort bandIntensity = band.BandImage.GetUshortPixelValue((int)bandPixelPosition[0], (int)bandPixelPosition[1]);
                bandIntensities[bandIndex] = bandIntensity;
            }

            if (viewModel.ClassifierViewModel.IsTrained)
            {
                var featureVector = new FeatureVector(bandIntensities);
                var predictedType = viewModel.ClassifierViewModel.CurrentClassifier.Predict(featureVector);

                viewModel.PredictionViewModel.MousePredictionType = predictedType;
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

            DrawBand(viewModel.PredictionViewModel.VisibleBand, dc, viewModel.PredictionViewModel.WorldToScreen);
        }
    }
}
