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
    public class DemHolesFootprintViewModel : ViewModelBase
    {
        private string _digitalElevationModelPath;
        private double _heightThreshold = 200;
        private bool _isInputSet;
        private FillHoleMode _fillHoleMode = FillHoleMode.Linear;

        private float _cutOff = 0.99999f;

        public string DigitalElevationModelPath
        {
            get { return _digitalElevationModelPath; }
            set { _digitalElevationModelPath = value; RaisePropertyChanged(); }
        }

        public double HeightThreshold
        {
            get { return _heightThreshold; }
            set { _heightThreshold = value; RaisePropertyChanged(); }
        }

        public bool IsInputSet
        {
            get { return _isInputSet; }
            set { _isInputSet = value; RaisePropertyChanged(); }
        }

        public float CutOff
        {
            get { return _cutOff; }
            set { _cutOff = value; RaisePropertyChanged(); }
        }


        public ICommand BrowseDigitalElevationModelPathCommand { get; set; }

        public ICommand FillHolesCommand { get; set; }


        public DemHolesFootprintViewModel()
        {
            BrowseDigitalElevationModelPathCommand = new RelayCommand(BrowserDigitalElevationModelPath, () => true);
            FillHolesCommand = new RelayCommand(FillHoleAsync, () => !string.IsNullOrEmpty(DigitalElevationModelPath));
        }

        private void FillHoleAsync()
        {
            Task.Factory.StartNew(() =>
            {
                using (VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint())
                using (Mat demMat = Imread(DigitalElevationModelPath, LoadImageType.AnyDepth | LoadImageType.Grayscale))
                {
                    var enforcedHeightMat = new Mat(demMat.Size, DepthType.Cv8U, 1);
                    var enforcedHeightImage = enforcedHeightMat.ToImage<Gray, byte>();

                    var demImage = demMat.ToImage<Gray, float>();
                    var demByte = demImage.Convert<Gray, byte>();

                    var sobelX = demByte.Sobel(1, 0, 3);
                    var sobelY = demByte.Sobel(0, 1, 3);

                    var thresholdSobelX = ThresholdSobel(demMat, sobelX);
                    var thresholdSobelY = ThresholdSobel(demMat, sobelY);

                    var combined = thresholdSobelX + thresholdSobelY;

                    var closeStructure = GetStructuringElement(ElementShape.Ellipse, new Size(30, 30), new Point(15, 15));
                    var crossStructure = GetStructuringElement(ElementShape.Cross, new Size(3, 3), new Point(1, 1));

                    MorphologyEx(combined, combined, MorphOp.Close, closeStructure, new Point(15, 15), 1, BorderType.Default, new MCvScalar(0));

                    var voidAreas = new Image<Gray, byte>(combined.Size);
                    Dilate(combined, voidAreas, crossStructure, new Point(1, 1), 3, BorderType.Default, new MCvScalar(0));

                    Imwrite($@"C:\temp\footprint_holes.png", voidAreas);

                    var mask = new Matrix<byte>(voidAreas.Rows, voidAreas.Cols, voidAreas.NumberOfChannels);
                    voidAreas.CopyTo(mask);

                    FindContours(voidAreas, contours, null, RetrType.Tree, ChainApproxMethod.ChainApproxNone);

                    var fixedImage = demMat.ToImage<Gray, ushort>();
                    switch (_fillHoleMode)
                    {
                        case FillHoleMode.MovingWindowAverage:
                            for (int contourIndex = 0; contourIndex < contours.Size; contourIndex++)
                            {
                                var contour = contours[contourIndex];
                                var boundingRectangle = BoundingRectangle(contour);

                                for (int y = boundingRectangle.Top; y < boundingRectangle.Bottom; ++y)
                                {
                                    for (int x = boundingRectangle.Left; x < boundingRectangle.Right; ++x)
                                    {
                                        if (mask[y, x] != 0)
                                        {
                                            double average = 0;
                                            int count = 0;
                                            for (int w = -1; w <= 1; ++w)
                                            {
                                                for (int u = -1; u <= 1; ++u)
                                                {
                                                    if (mask[y + w, x + u] != 255)
                                                    {
                                                        average += fixedImage[y + w, x + u].Intensity;
                                                        count++;
                                                    }
                                                }
                                            }
                                            average /= count;
                                            fixedImage[y, x] = new Gray(average);
                                            mask[y, x] = 0;
                                        }
                                    }
                                }
                            }

                            Imwrite($@"C:\temp\holeFixed.png", fixedImage);
                            break;
                        case FillHoleMode.Linear:

                            for (int contourIndex = 0; contourIndex < contours.Size; contourIndex++)
                            {
                                var contour = contours[contourIndex];
                                var boundingRectangle = BoundingRectangle(contour);

                                for (int y = boundingRectangle.Top; y < boundingRectangle.Bottom; ++y)
                                {
                                    // scanline
                                    for (int x = boundingRectangle.Left; x < boundingRectangle.Right; ++x)
                                    {
                                        if (mask[y, x] == 255)
                                        {
                                            double left = demImage[y, x - 1].Intensity;
                                            int trackLeft = x;
                                            int trackRight = x;
                                            while (mask[y, trackRight] == 255) trackRight++;
                                            double right = demImage[y, trackRight + 1].Intensity;
                                            int width = trackRight - x;

                                            for (int x2 = trackLeft; x2 < trackRight; ++x2)
                                            {
                                                double lerp = MoreMath.Lerp(left, right, (float)(x2 - trackLeft) / width);
                                                fixedImage[y, x2] = new Gray(lerp);
                                                enforcedHeightImage[y, x2] = new Gray(lerp/ushort.MaxValue*byte.MaxValue);
                                            }

                                            x += width;
                                        }

                                    }
                                }
                            }
                            Imwrite($@"C:\temp\combined_holes.png", fixedImage);
                            Imwrite($@"C:\temp\enforced_height_holes.png", enforcedHeightImage);
                            break;
                    }
                }
            });
        }

        private Image<Gray, byte> ThresholdSobel(Mat demMat, Image<Gray, float> sobel)
        {
            var grayScale = sobel.Convert<Gray, byte>();
            Normalize(grayScale, grayScale, 0, 255, NormType.MinMax);

            DenseHistogram hist = CalculateHist(grayScale);
            int cutOffHigh = CalculateCutOff(demMat, hist, _cutOff);
            int cutOffLow = CalculateCutOff(demMat, hist, 1 - _cutOff);

            var sobelThresholdedHigh = grayScale.ThresholdBinary(new Gray(cutOffHigh), new Gray(255));
            var sobelThresholdedLow = grayScale.ThresholdBinary(new Gray(cutOffLow), new Gray(255)).Not();

            var sobelCombined = sobelThresholdedHigh + sobelThresholdedLow;
            return sobelCombined;
        }

        private static DenseHistogram CalculateHist(Image<Gray, byte> grayscaleSobelX)
        {
            double[] min, max;
            Point[] minLocs, maxLocs;
            grayscaleSobelX.MinMax(out min, out max, out minLocs, out maxLocs);

            int range = (int)(Math.Abs(min[0]) + Math.Abs(max[0]));
            DenseHistogram hist = new DenseHistogram(range, new RangeF((float)min[0], (float)max[0]));

            hist.Calculate(new[] { grayscaleSobelX }, true, null);
            return hist;
        }

        private int CalculateCutOff(Mat demMat, DenseHistogram hist, float cutOff)
        {
            var bins = hist.GetBinValues();
            float total = 0;
            float totalPixels = demMat.Width * demMat.Height;
            int cutOffHigh = 0;
            for (int bin = 0; bin < bins.Length; ++bin)
            {
                total += bins[bin];
                if (total >= totalPixels * cutOff)
                {
                    cutOffHigh = bin;
                    break;
                }
            }

            return cutOffHigh;
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

        private enum FillState
        {
            Outside, BorderLeft, Inside
        }
    }

    public enum FillHoleMode
    {
        Linear, MovingWindowAverage
    }
}
