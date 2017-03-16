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
using System.Windows.Media.Imaging;
using LandscapeClassifier.Extensions;
using LandscapeClassifier.Model;
using LandscapeClassifier.Model.Classification;
using LandscapeClassifier.Util;
using LandscapeClassifier.ViewModel;
using LandscapeClassifier.ViewModel.MainWindow;
using LandscapeClassifier.ViewModel.MainWindow.Classification;
using MathNet.Numerics.LinearAlgebra;
using OSGeo.GDAL;
using OSGeo.OSR;

namespace LandscapeClassifier.Controls
{
    public class ClassificationImageCanvas : ImageCanvasBase
    {

        private readonly VectorBuilder<double> _vecBuilder = Vector<double>.Build;

        public ClassificationImageCanvas()
        {
            MouseMove += OnMove;
            MouseDown += OnMouseDown;
            MouseLeave += OnMouseLeave;

        }

        private void OnMouseLeave(object sender, MouseEventArgs mouseEventArgs)
        {
            MainWindowViewModel viewModel = (MainWindowViewModel)DataContext;

            if (viewModel != null)
            {
                viewModel.ClassifierViewModel.MouseScreenPoisition = new Point(0, 0);
                viewModel.ClassifierViewModel.MouseWorldPoisition = new Point(0, 0);
            }
        }


        private void OnMouseDown(object sender, MouseButtonEventArgs mouseButtonEventArgs)
        {
            if (Mouse.RightButton == MouseButtonState.Pressed)
            {
                MainWindowViewModel viewModel = (MainWindowViewModel)DataContext;

                var mousePosition = mouseButtonEventArgs.GetPosition(this);
                Vector<double> mouseVec = _vecBuilder.DenseOfArray(new[] { mousePosition.X, mousePosition.Y, 1.0f });

                var featureBands = viewModel.Layers.Where(b => b.UseFeature).OrderBy(b => b.Name).ToList();

                ushort[] bandPixels = new ushort[featureBands.Count];

                var viewToToWorld = viewModel.ClassifierViewModel.ScreenToWorld* _scaleMat.Inverse() * _screenToViewMat.Inverse();

                for (int bandIndex = 0; bandIndex < featureBands.Count; ++bandIndex)
                {
                    var band = featureBands[bandIndex];
                    var viewToPixelMat = band.WorldToImage * viewToToWorld;
                    var bandPixelPosition = viewToPixelMat * mouseVec;

                    ushort bandPixelValue = band.BandImage.GetScaledToUshort((int)bandPixelPosition[0], (int)bandPixelPosition[1]);

                    bandPixels[bandIndex] = bandPixelValue;
                }

                var worldMousePosition = viewToToWorld*mouseVec;

                var classifiedFeatureVector = new ClassifiedFeatureVector(viewModel.SelectedLandCoverTypeViewModel, new FeatureVector(bandPixels), new Point(worldMousePosition[0], worldMousePosition[1]));
                viewModel.ClassifierViewModel.FeaturesViewModel.AddFeature(new ClassifiedFeatureVectorViewModel(classifiedFeatureVector));
            }
        }

        private void OnMove(object sender, MouseEventArgs args)
        {
            MainWindowViewModel viewModel = (MainWindowViewModel)DataContext;
            if (viewModel == null) return;

            var position = args.GetPosition(this);
            var posVec = _vecBuilder.DenseOfArray(new[] { position.X, position.Y, 1 });

            {
                var viewToWorld = viewModel.ClassifierViewModel.ScreenToWorld * _scaleMat.Inverse() * _screenToViewMat.Inverse();

                var mouseWorld = viewToWorld * posVec;

                viewModel.ClassifierViewModel.MouseScreenPoisition = position;
                viewModel.ClassifierViewModel.MouseWorldPoisition = new Point(mouseWorld[0], mouseWorld[1]);
            }

            var featureBands = viewModel.Layers.Where(b => b.UseFeature).ToList();

            foreach (var band in featureBands)
            {
                var viewToPixelMat = band.WorldToImage * viewModel.ClassifierViewModel.ScreenToWorld * _scaleMat.Inverse() * _screenToViewMat.Inverse();

                  var bandPixelPosition = viewToPixelMat * posVec;

                byte grayScale = band.BandImage.GetScaledToByte((int) bandPixelPosition[0], (int) bandPixelPosition[1]);

                //if (viewModel.ClassifierViewModel.PreviewBandIntensityScale)
                //    bandIntensity = (ushort)MoreMath.Clamp((bandIntensity - band.MaxCutScale) / (double)(band.MaxCutScale - band.MinCutScale) * ushort.MaxValue, 0, ushort.MaxValue - 1);

                // Console.WriteLine($"pos ({bandPixelPosition[0]}, {bandPixelPosition[1]}) band {band.BandNumber}: {bandIntensity}, {grayScale}");
                band.CurrentPositionBrush = new SolidColorBrush(Color.FromRgb(grayScale, grayScale, grayScale));
            }

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
            foreach (var band in viewModel.Layers)
            {
                if (!band.IsVisible) continue;
                DrawBand(dc, band, viewModel.ClassifierViewModel.WorldToScreen);
            }
        }
    }
}