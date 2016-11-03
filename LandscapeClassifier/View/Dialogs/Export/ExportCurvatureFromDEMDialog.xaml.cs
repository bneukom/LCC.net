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
    public partial class ExportCurvatureFromDEMDialog : MetroWindow
    {
        private AscFile _ascFile;

        private ExportCurvatureFromDEMDialog()
        {
            InitializeComponent();

           
        }

        public static bool? ShowDialog(AscFile ascFile)
        {
            ExportCurvatureFromDEMDialog dialog = new ExportCurvatureFromDEMDialog();
            dialog._ascFile = ascFile;

            var folder = Path.GetDirectoryName(ascFile.Path);
            dialog.PathTextBox.Text = folder;

            return dialog.ShowDialog();
        }

        private void OkClick(object sender, RoutedEventArgs e)
        {
            int levels = 256;

            for (int level = 1; level <= levels; level *= 2)
            {
                int stride = _ascFile.Ncols / level * 4;
                int size = _ascFile.Nrows / level * stride;

                byte[] slopePixelData = new byte[size];

                float[] neighbours = new float[8];
                float L = _ascFile.Cellsize * level;

                float minCurvature = 1000;
                float maxCurvature = 0;
                for (int y = level; y < _ascFile.Nrows - level; y += level)
                {
                    for (int x = level; x < _ascFile.Ncols - level; x += level)
                    {

                        neighbours[0] = _ascFile.Data[y - level, x - level];
                        neighbours[1] = _ascFile.Data[y - level, x];
                        neighbours[2] = _ascFile.Data[y - level, x + level];

                        neighbours[3] = _ascFile.Data[y, x - level];
                        neighbours[4] = _ascFile.Data[y, x + level];

                        neighbours[5] = _ascFile.Data[y + level, x - level];
                        neighbours[6] = _ascFile.Data[y + level, x];
                        neighbours[7] = _ascFile.Data[y + level, x + level];

                        float Z2 = neighbours[0]; // N
                        float Z4 = neighbours[2]; // W
                        float Z5 = _ascFile.Data[y, x];
                        float Z6 = neighbours[6]; // E
                        float Z8 = neighbours[4]; // S

                        float D = ((Z4 + Z6) / 2 - Z5) / (L * L);
                        float E = ((Z2 + Z8) / 2 - Z5) / (L * L);
                        float curvature = 2 * (D + E);

                        byte slopeGrayScale = (byte)MoreMath.Clamp(((curvature * 16 * level) + (255.0f / 2.0f)), 0, 255);

                        int index = (y / level) * stride + 4 * (x / level);
                        slopePixelData[index + 0] = slopeGrayScale;
                        slopePixelData[index + 1] = slopeGrayScale;
                        slopePixelData[index + 2] = slopeGrayScale;
                        slopePixelData[index + 3] = 255;

                        minCurvature = Math.Min(minCurvature, curvature);
                        maxCurvature = Math.Max(maxCurvature, curvature);
                    }
                }

                Console.WriteLine($"min curvature for level {level}: {minCurvature}");
                Console.WriteLine($"max curvature for level {level}: {maxCurvature}");

                // Write prediction image
                using (var fileStream = new FileStream(System.IO.Path.Combine(PathTextBox.Text, $"curvature{level}.png"), FileMode.Create))
                {
                    var encoder = new PngBitmapEncoder();
                    var slopeImage = BitmapSource.Create(_ascFile.Ncols / level, _ascFile.Nrows / level, 96, 96, PixelFormats.Bgra32, null, slopePixelData,
                        stride);

                    encoder.Frames.Add(BitmapFrame.Create(slopeImage));
                    encoder.Save(fileStream);
                }
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