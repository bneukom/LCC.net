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
            
            RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.NearestNeighbor);
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

                var screenToView = _scaleMat.Inverse() * _screenToViewMat.Inverse();

                var featureBands = viewModel.Bands.Where(b => b.IsFeature).OrderBy(b => b.BandNumber).ToList();

                ushort[] bandPixels = new ushort[featureBands.Count];

                for (int bandIndex = 0; bandIndex < featureBands.Count; ++bandIndex)
                {
                    var band = featureBands[bandIndex];
                    var bandPixelPosition = screenToView * mouseVec / band.MetersPerPixel;

                    ushort bandPixelValue = band.BandImage.GetUshortPixelValue((int)bandPixelPosition[0], (int)bandPixelPosition[1]);

                    bandPixels[bandIndex] = bandPixelValue;
                }

                var classifiedFeatureVector = new ClassifiedFeatureVector(viewModel.ClassifierViewModel.SelectedLandCoverType, new FeatureVector(bandPixels));
                viewModel.ClassifierViewModel.FeaturesViewModel.AddFeature(new ClassifiedFeatureVectorViewModel(classifiedFeatureVector));
            }
        }

        private void OnMove(object sender, MouseEventArgs args)
        {
            MainWindowViewModel viewModel = (MainWindowViewModel)DataContext;
            if (viewModel == null) return;

            var position = args.GetPosition(this);


            var screenToView = _scaleMat.Inverse() * _screenToViewMat.Inverse();
            var viewToWorld = viewModel.ClassifierViewModel.ScreenToWorld * _scaleMat.Inverse() * _screenToViewMat.Inverse();

            var posVec = _vecBuilder.DenseOfArray(new[] { position.X, position.Y, 1 });

            var mouseWorld = viewToWorld * posVec;

            viewModel.ClassifierViewModel.MouseScreenPoisition = position;
            viewModel.ClassifierViewModel.MouseWorldPoisition = new Point(mouseWorld[0], mouseWorld[1]);

            var featureBands = viewModel.Bands.Where(b => b.IsFeature).ToList();

            foreach (var band in featureBands)
            {
                var bandPixelPosition = screenToView * posVec / band.MetersPerPixel;

                ushort bandIntensity = band.BandImage.GetUshortPixelValue((int)bandPixelPosition[0], (int)bandPixelPosition[1]);

                if (viewModel.ClassifierViewModel.PreviewBandIntensityScale)
                    bandIntensity = (ushort)MoreMath.Clamp((bandIntensity - band.MaxCutScale) / (double)(band.MaxCutScale - band.MinCutScale) * ushort.MaxValue, 0, ushort.MaxValue - 1);

                byte grayScale = (byte)((float)bandIntensity / ushort.MaxValue * byte.MaxValue);
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
            if (viewModel.Bands.Any(b => b.IsVisible)) dc.DrawRectangle(Brushes.White, null, new Rect(0, 0, ActualWidth, ActualHeight));
            foreach (var band in viewModel.Bands)
            {
                if (!band.IsVisible) continue;
                DrawBand(band, dc, viewModel.ClassifierViewModel.WorldToScreen);
            }
        }
    }
}