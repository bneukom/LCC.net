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
    public class WaterFootprintViewModel : ViewModelBase
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

        public ICommand ShowWaterMaskCommand { get; set; }

        public ICommand FlattenWaterBodiesCommand { get; set; }

        public WaterFootprintViewModel()
        {
            BrowseWaterMapPathCommand = new RelayCommand(BrowseWaterMapPath, () => true);
            BrowseDigitalElevationModelPathCommand = new RelayCommand(BrowserDigitalElevationModelPath, () => true);
            ShowWaterMaskCommand = new RelayCommand(ShowWaterMap, () => !string.IsNullOrEmpty(WaterMapPath));
            FlattenWaterBodiesCommand = new RelayCommand(FlattenWaterBodies, () => !string.IsNullOrEmpty(WaterMapPath) && !string.IsNullOrEmpty(DigitalElevationModelPath));
        }

        private void FlattenWaterBodies()
        {
            try
            {
                using (Mat demMat = Imread(DigitalElevationModelPath, LoadImageType.AnyDepth | LoadImageType.Grayscale))
                {
                    var demImage = demMat.ToImage<Gray, ushort>();
                    var smoothedImage = demImage.Clone();

                    var enforcedHeightMat = new Mat(demMat.Size, DepthType.Cv8U, 1);
                    var enfocedHeightImgage = enforcedHeightMat.ToImage<Gray, byte>();

                    var waterMask = ComputeWaterMask(WaterMapPath);
                    var waterMaskImage = waterMask.ToImage<Gray, byte>();

                   

                    using (VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint())
                    using (Mat hierachy = new Mat())
                    {
                        FindContours(waterMask.Clone(), contours, hierachy, RetrType.Tree, ChainApproxMethod.ChainApproxNone);

                        for (int contourIndex = 0; contourIndex < contours.Size; contourIndex++)
                        {
                            var contour = contours[contourIndex];
                            var boundingRect = BoundingRectangle(contour);

                            List<Point> waterPoints = new List<Point>();

                            double total = 0.0;
                            for (int x = boundingRect.Left; x < boundingRect.Right; ++x)
                            {
                                for (int y = boundingRect.Top; y < boundingRect.Bottom; ++y)
                                {
                                    if (PointPolygonTest(contour, new PointF(x, y), false) > 0)
                                    {
                                        waterPoints.Add(new Point(x, y));
                                        total += demImage[y, x].Intensity;
                                    }
                                }
                            }

                            double average = total/waterPoints.Count;

                            foreach (Point waterPoint in waterPoints)
                            {

                                var demGray = demImage[waterPoint.Y, waterPoint.X].Intensity;
                                var alpha = waterMaskImage[waterPoint.Y, waterPoint.X].Intensity/255.0d;
                                var lerp = MoreMath.Lerp(demGray, average, alpha);

                                enfocedHeightImgage[waterPoint.Y, waterPoint.X] = new Gray(average / ushort.MaxValue * byte.MaxValue);
                                smoothedImage[waterPoint.Y, waterPoint.X] = new Gray(lerp);
                            }
                        }

                        Imwrite($@"C:\temp\footprint.png", waterMaskImage);
                        Imwrite($@"C:\temp\enforced.png", enfocedHeightImgage);
                        Imwrite($@"C:\temp\merged.png", smoothedImage);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private Mat ComputeWaterMask(string waterMaskPath)
        {
            using (Mat imageMat = Imread(waterMaskPath, LoadImageType.Grayscale))
            using (Mat contourMat = new Mat(imageMat.Size, DepthType.Cv8U, 1))
            {
                var contours = FindWaterContours(imageMat);

                for (int contourIndex = 0; contourIndex < contours.Size; contourIndex++)
                {
                    DrawContours(contourMat, contours, contourIndex, new MCvScalar(byte.MaxValue), -1, LineType.EightConnected);
                }

                var footprint = new Mat(contourMat.Size, DepthType.Cv8U, 1);
                var crossStructure = GetStructuringElement(ElementShape.Cross, new Size(3, 3), new Point(1, 1));
                Dilate(contourMat, footprint, crossStructure, new Point(1, 1), 5, BorderType.Default, new MCvScalar(0));


                var blurred = new Mat(contourMat.Size, DepthType.Cv8U, 1);
                GaussianBlur(footprint, blurred, new Size(15, 15), 0);

                return blurred;
            }
        }


        private void ShowWaterMap()
        {
            var waterMask = ComputeWaterMask(WaterMapPath);
            Imwrite(@"C:\temp\contours0.png", waterMask);
        }

        private VectorOfVectorOfPoint FindWaterContours(IInputOutputArray input)
        {
            VectorOfVectorOfPoint waterContours = new VectorOfVectorOfPoint();
            using (VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint())
            using (Mat hierachy = new Mat())
            {
                FindContours(input, contours, hierachy, RetrType.Tree, ChainApproxMethod.ChainApproxNone);

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
