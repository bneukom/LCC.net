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
using MahApps.Metro.Controls;
using Microsoft.Win32;

namespace LandscapeClassifier.View
{
    /// <summary>
    /// Interaction logic for CreateSlopeFromDEMDialog.xaml
    /// </summary>
    public partial class CreateSlopeFromDEMDialog : MetroWindow
    {
        public CreateSlopeFromDEMDialog()
        {
            InitializeComponent();
        }

        private void BrowseClick(object sender, RoutedEventArgs e)
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
                var path = openFileDialog.FileName;
                PathTextBox.Text = path;
            }
        }

        private void OkClick(object sender, RoutedEventArgs e)
        {
            BitmapImage image = new BitmapImage(new Uri(PathTextBox.Text));
            int width = (int)image.Width;
            int height = (int)image.Height;
            int stride = image.PixelWidth * 4;
            int size = image.PixelHeight * stride;
            byte[] heightPixels = new byte[size];
            image.CopyPixels(heightPixels, stride, 0);

            float D = float.Parse(PixelDimension.Text);
            float min = float.Parse(MinAltitude.Text);
            float max = float.Parse(MaxAltitude.Text);

            byte[] slopePixelData = new byte[size];

            double minSlope = 1000;
            double maxSlope = 0;
            for (int y = 1; y < image.Height - 1; ++y)
            {
                for (int x = 1; x < image.Width - 1; ++x)
                {
                    float z1 = grayscaleToAltitude(heightPixels[(y - 1) * stride + 4 * (x - 1)], min, max);
                    float z2 = grayscaleToAltitude(heightPixels[(y - 1) * stride + 4 * (x + 0)], min, max);
                    float z3 = grayscaleToAltitude(heightPixels[(y - 1) * stride + 4 * (x + 1)], min, max);
                    float z4 = grayscaleToAltitude(heightPixels[(y + 0) * stride + 4 * (x - 1)], min, max);
                    float z6 = grayscaleToAltitude(heightPixels[(y + 0) * stride + 4 * (x + 1)], min, max);
                    float z7 = grayscaleToAltitude(heightPixels[(y + 1) * stride + 4 * (x - 1)], min, max);
                    float z8 = grayscaleToAltitude(heightPixels[(y + 1) * stride + 4 * (x + 0)], min, max);
                    float z9 = grayscaleToAltitude(heightPixels[(y + 1) * stride + 4 * (x + 1)], min, max);

                    float b = (z3 + 2*z6 + z9 - z1 - 2*z4 - z7)/(8*D);
                    float c = (z1 + 2*z2 + z3 - z7 - 2*z8 - z9)/(8*D);

                    double slope = Math.Atan(Math.Sqrt(b*b + c*c));

                    if (slope > 0.1 && slope < 0.2)
                    {
                        Console.WriteLine("Test");
                    }
                    byte slopeGrayScale = (byte)(slope / (Math.PI/2) * 255);
                    byte slopeGrayScale2 = (byte) (Math.Sqrt(slope/(Math.PI/2))*255);

                    int index = y * stride + 4 * x;
                    slopePixelData[index + 0] = slopeGrayScale;
                    slopePixelData[index + 1] = slopeGrayScale;
                    slopePixelData[index + 2] = slopeGrayScale;
                    slopePixelData[index + 3] = 255;

                    minSlope = Math.Min(minSlope, slope);
                    maxSlope = Math.Max(maxSlope, slope);
                }
            }

            var folder = Path.GetDirectoryName(PathTextBox.Text);

            // Write prediction image
            using (var fileStream = new FileStream(System.IO.Path.Combine(folder, "slope.png"), FileMode.Create))
            {
                var encoder = new PngBitmapEncoder();
                var slopeImage = BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgra32, null, slopePixelData, stride);

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
