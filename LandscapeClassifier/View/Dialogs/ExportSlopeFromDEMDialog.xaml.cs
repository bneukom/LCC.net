using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using LandscapeClassifier.Model;
using LandscapeClassifier.Util;
using MahApps.Metro.Controls;
using Microsoft.Win32;

namespace LandscapeClassifier.View
{
    /// <summary>
    /// Interaction logic for CreateSlopeFromDEMDialog.xaml
    /// </summary>
    public partial class ExportSlopeFromDEMDialog : MetroWindow
    {
        private AscFile _ascFile;

        private ExportSlopeFromDEMDialog()
        {
            InitializeComponent();
        }

        public static bool? ShowDialog(AscFile ascFile)
        {
            ExportSlopeFromDEMDialog dialog = new ExportSlopeFromDEMDialog();
            dialog._ascFile = ascFile;
            return dialog.ShowDialog();
        }

        private void OkClick(object sender, RoutedEventArgs e)
        {
            int stride = _ascFile.Ncols * 4;
            int size = _ascFile.Nrows * stride;

            float min = float.Parse(MinAltitude.Text);
            float max = float.Parse(MaxAltitude.Text);

            byte[] slopePixelData = new byte[size];

            double minSlope = 1000;
            double maxSlope = 0;
            for (int y = 1; y < _ascFile.Nrows - 1; ++y)
            {
                for (int x = 1; x < _ascFile.Ncols - 1; ++x)
                {
                    if (x == 2907 && y == 2122)
                    {
                        Console.WriteLine("Test");
                    }

                    float Z1 = _ascFile.Data[y - 1, x - 1];
                    float Z2 = _ascFile.Data[y - 1, x];
                    float Z3 = _ascFile.Data[y - 1, x + 1];

                    float Z4 = _ascFile.Data[y, x - 1];
                    float Z5 = _ascFile.Data[y, x];
                    float Z6 = _ascFile.Data[y, x + 1];

                    float Z7 = _ascFile.Data[y + 1, x - 1];
                    float Z8 = _ascFile.Data[y + 1, x];
                    float Z9 = _ascFile.Data[y + 1, x + 1];


                    float b = (Z3 + 2 * Z6 + Z9 - Z1 - 2 * Z4 - Z7) / (8 * _ascFile.Cellsize);
                    float c = (Z1 + 2 * Z2 + Z3 - Z7 - 2 * Z8 - Z9) / (8 * _ascFile.Cellsize);

                    float slope = (float)Math.Atan(Math.Sqrt(b * b + c * c));

                    if (MoreMath.AlmostEquals(slope, Math.PI / 2, 0.005))
                    {
                        slope = 0;
                    }

                    byte slopeGrayScale = (byte)(slope / (Math.PI / 2) * 255);

                    int index = y * stride + 4 * x;
                    slopePixelData[index + 0] = slopeGrayScale;
                    slopePixelData[index + 1] = slopeGrayScale;
                    slopePixelData[index + 2] = slopeGrayScale;
                    slopePixelData[index + 3] = 255;

                    minSlope = Math.Min(minSlope, slope);
                    maxSlope = Math.Max(maxSlope, slope);
                }
            }

            var folder = Path.GetDirectoryName(_ascFile.Path);

            // Write prediction image
            using (var fileStream = new FileStream(System.IO.Path.Combine(folder, "slope.png"), FileMode.Create))
            {
                var encoder = new PngBitmapEncoder();
                var slopeImage = BitmapSource.Create(_ascFile.Ncols, _ascFile.Nrows, 96, 96, PixelFormats.Bgra32, null, slopePixelData,
                    stride);

                encoder.Frames.Add(BitmapFrame.Create(slopeImage));
                encoder.Save(fileStream);
            }

        }

        private float grayscaleToAltitude(byte value, float minAltitude, float maxAltitude)
        {
            return (value/255.0f)*(maxAltitude - minAltitude) + minAltitude;
        }

        private void CancelClick(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }
    }
}