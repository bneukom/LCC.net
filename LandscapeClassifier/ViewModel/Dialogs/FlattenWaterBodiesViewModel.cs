using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using LandscapeClassifier.Util;
using LandscapeClassifier.ViewModel.MainWindow.Classification;
using Microsoft.Win32;
using static Emgu.CV.CvInvoke;

namespace LandscapeClassifier.ViewModel.Dialogs
{
    public class FlattenWaterBodiesViewModel : ViewModelBase
    {
        private string _digitalElevationModelPath;
        private string _waterMapPath;
        private double _areaThreshold = 2000;
        private bool _isInputSet;
        private double _formfactorThreshold = 0.025;
        private SmoothMode _smoothMode;
        private int _smoothingIterations = 4;

        public string DigitalElevationModelPath
        {
            get { return _digitalElevationModelPath; }
            set { _digitalElevationModelPath = value; RaisePropertyChanged(); }
        }

        public string WaterMapPath
        {
            get { return _waterMapPath; }
            set { _waterMapPath = value; RaisePropertyChanged(); }
        }

        public double AreaThreshold
        {
            get { return _areaThreshold; }
            set { _areaThreshold = value; RaisePropertyChanged(); }
        }

        public double FormfactorThreshold
        {
            get { return _formfactorThreshold; }
            set { _formfactorThreshold = value; RaisePropertyChanged(); }
        }

        public bool IsInputSet
        {
            get { return _isInputSet; }
            set { _isInputSet = value; RaisePropertyChanged(); }
        }

        public SmoothMode SmoothMode
        {
            get { return _smoothMode; }
            set { _smoothMode = value; RaisePropertyChanged(); }
        }

        public int SmoothingIterations
        {
            get { return _smoothingIterations; }
            set { _smoothingIterations = value; RaisePropertyChanged(); }
        }

        public ICommand BrowseDigitalElevationModelPathCommand { get; set; }

        public ICommand BrowseWaterMapPathCommand { get; set; }

        public ICommand ShowWaterMapCommand { get; set; }

        public ICommand FlattenWaterBodiesCommand { get; set; }

        public FlattenWaterBodiesViewModel()
        {
            BrowseWaterMapPathCommand = new RelayCommand(BrowseWaterMapPath, () => true);
            BrowseDigitalElevationModelPathCommand = new RelayCommand(BrowserDigitalElevationModelPath, () => true);
            ShowWaterMapCommand = new RelayCommand(ShowWaterMap, () => !string.IsNullOrEmpty(WaterMapPath));
            FlattenWaterBodiesCommand = new RelayCommand(FlattenWaterBodies, () => !string.IsNullOrEmpty(WaterMapPath) && !string.IsNullOrEmpty(DigitalElevationModelPath));
        }

        private void FlattenWaterBodies()
        {
            using (Mat demMat = Imread(DigitalElevationModelPath, LoadImageType.AnyDepth | LoadImageType.Grayscale))
            using (Mat waterBodiesMat = Imread(WaterMapPath, LoadImageType.Grayscale))
            {
                var demImage = demMat.ToImage<Gray, ushort>();
                var smoothedImage = demImage.Clone();

                var contours = FindContours(waterBodiesMat);

                demImage[0, 0] = new Gray(0);
                for (int contourIndex = 0; contourIndex < contours.Size; contourIndex++)
                {
                    var contour = contours[contourIndex];
                    var boundingRect = BoundingRectangle(contour);

                    List<PointF> waterPoints = new List<PointF>();

                    double total = 0.0;
                    for (int x = boundingRect.Left; x < boundingRect.Right; ++x)
                    {
                        for (int y = boundingRect.Top; y < boundingRect.Bottom; ++y)
                        {
                            if (PointPolygonTest(contour, new PointF(x, y), false) > 0)
                            {
                                waterPoints.Add(new PointF(x, y));
                                total += demImage[y, x].Intensity;
                            }
                        }
                    }

                    double average = total / waterPoints.Count;

                    switch (SmoothMode)
                    {
                        case SmoothMode.Relaxation:
                            // Smooth
                            for (int iteration = 0; iteration < SmoothingIterations; ++iteration)
                            {
                                Parallel.ForEach(waterPoints, waterPoint =>
                                {
                                    int y = (int)waterPoint.Y;
                                    int x = (int)waterPoint.X;

                                    // TODO lerp to average?
                                    double height = MoreMath.Lerp(demImage[y, x].Intensity, average, 0.1);
                                    double smoothedHeight = height;

                                    // http://paulbourke.net/geometry/polygonmesh/
                                    smoothedHeight += (MoreMath.Lerp(demImage[y - 1, x - 1].Intensity, average, 0.1) - height) / 8;
                                    smoothedHeight += (MoreMath.Lerp(demImage[y - 1, x].Intensity, average, 0.1) - height) / 8;
                                    smoothedHeight += (MoreMath.Lerp(demImage[y - 1, x + 1].Intensity, average, 0.1) - height) / 8;

                                    smoothedHeight += (MoreMath.Lerp(demImage[y, x - 1].Intensity, average, 0.1) - height) / 8;
                                    smoothedHeight += (MoreMath.Lerp(demImage[y, x + 1].Intensity, average, 0.1) - height) / 8;

                                    smoothedHeight += (MoreMath.Lerp(demImage[y + 1, x - 1].Intensity, average, 0.1) - height) / 8;
                                    smoothedHeight += (MoreMath.Lerp(demImage[y + 1, x].Intensity, average, 0.1) - height) / 8;
                                    smoothedHeight += (MoreMath.Lerp(demImage[y + 1, x + 1].Intensity, average, 0.1) - height) / 8;

                                    smoothedImage[y, x] = new Gray(smoothedHeight);
                                });

                                demImage = smoothedImage.Clone();
                            }
                            break;
                        case SmoothMode.Average:
                            foreach (PointF waterPoint in waterPoints)
                            {
                                smoothedImage[(int)waterPoint.Y, (int)waterPoint.X] = new Gray(average);
                            }
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }


                    Imwrite($@"C:\temp\flatten_dem_{contourIndex}.png", smoothedImage);
                }
            }
        }


        private void ShowWaterMap()
        {
            using (Mat imageMat = Imread(WaterMapPath, LoadImageType.Grayscale))
            using (Mat contourMat = new Mat(imageMat.Size, DepthType.Cv32F, 3))
            {
                var contours = FindContours(imageMat);

                for (int contourIndex = 0; contourIndex < contours.Size; contourIndex++)
                {
                    DrawContours(contourMat, contours, contourIndex, new MCvScalar(200, 0, 0), -1, LineType.EightConnected);
                }

                Mat resized = new Mat();
                Resize(contourMat, resized, new Size(1900, 1000), 0D, 0D, Inter.Linear);
                Imshow("Contours", resized);

                Imwrite(@"C:\temp\contours.png", contourMat);
            }
        }

        private VectorOfVectorOfPoint FindContours(IInputOutputArray input)
        {
            VectorOfVectorOfPoint waterContours = new VectorOfVectorOfPoint();
            using (VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint())
            using (Mat hierachy = new Mat())
            {
                CvInvoke.FindContours(input, contours, hierachy, RetrType.Tree, ChainApproxMethod.ChainApproxNone);

                for (int contourIndex = 0; contourIndex < contours.Size; contourIndex++)
                {
                    double area = ContourArea(contours[contourIndex], false);

                    double perimeter = ArcLength(contours[contourIndex], true);

                    double formfactor = 4 * Math.PI * area / (perimeter * perimeter);

                    if (area > AreaThreshold && formfactor > FormfactorThreshold)
                    {
                        var moments = Moments(contours[contourIndex], true);
                        double x = moments.M20 + moments.M02;
                        double y = 4 * Math.Pow(moments.M11, 2) + Math.Pow((moments.M20 - moments.M02), 2);
                        double elongation = (x + Math.Pow(y, 0.5)) / (x - Math.Pow(y, 0.5));

                        Console.WriteLine($"formfactor: {formfactor} elongation: {elongation}");

                        waterContours.Push(contours[contourIndex]);
                    }
                }
            }

            return waterContours;
        }


        private void BrowserDigitalElevationModelPath()
        {
            var openFileDialog = new OpenFileDialog()
            {
                Filter = "PNG Files (.png)|*.png",
                FilterIndex = 1,
            };

            var userClickedOk = openFileDialog.ShowDialog();

            // Process input if the user clicked OK.
            if (userClickedOk == true)
            {
                DigitalElevationModelPath = openFileDialog.FileName;
            }
        }

        private void BrowseWaterMapPath()
        {
            var openFileDialog = new OpenFileDialog()
            {
                Filter = "PNG Files (.png)|*.png",
                FilterIndex = 1,
            };

            var userClickedOk = openFileDialog.ShowDialog();

            // Process input if the user clicked OK.
            if (userClickedOk == true)
            {
                WaterMapPath = openFileDialog.FileName;
            }
        }
    }

    public enum SmoothMode
    {
        Relaxation,
        Average
    }
}
