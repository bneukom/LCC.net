using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using LandscapeClassifier.Model;
using MahApps.Metro.Controls;

namespace LandscapeClassifier.View.Export
{
    /// <summary>
    /// Interaction logic for CreateSlopeFromDEMDialog.xaml
    /// </summary>
    public partial class ExportSmoothedDEMDialog : MetroWindow
    {
        private AscFile _ascFile;

        private ExportSmoothedDEMDialog()
        {
            InitializeComponent();
        }

        public static bool? ShowDialog(AscFile ascFile)
        {
            ExportSmoothedDEMDialog dialog = new ExportSmoothedDEMDialog();
            dialog._ascFile = ascFile;

            var folder = Path.GetDirectoryName(ascFile.Path);
            dialog.PathTextBox.Text = folder;

            return dialog.ShowDialog();
        }

        private void OkClick(object sender, RoutedEventArgs e)
        {
            float[,] smoothedHeightMap = new float[_ascFile.Nrows, _ascFile.Ncols];
            float[,] smoothIterationResult = new float[_ascFile.Nrows, _ascFile.Ncols];
            float[,] smoothDelta = new float[_ascFile.Nrows, _ascFile.Ncols];

            float L = _ascFile.Cellsize;

            // Initialize
            for (int y = 0; y < _ascFile.Nrows; y++)
            {
                for (int x = 0; x < _ascFile.Ncols; x++)
                {
                    var height = _ascFile.Data[y, x];
                    smoothedHeightMap[y,x] = height;
                    smoothIterationResult[y, x] = height;
                }
            }

            // Smooth
            for (int iteration = 0; iteration < IterationsSlider.Value; ++iteration)
            {
                Parallel.For(1, _ascFile.Nrows - 1, y =>
                {
                    float[] neighbours = new float[8];

                    for (int x = 1; x < _ascFile.Ncols - 1; x++)
                    {
                        neighbours[0] = smoothedHeightMap[y - 1, x - 1];
                        neighbours[1] = smoothedHeightMap[y - 1, x];
                        neighbours[2] = smoothedHeightMap[y - 1, x + 1];

                        neighbours[3] = smoothedHeightMap[y, x - 1];
                        neighbours[4] = smoothedHeightMap[y, x + 1];

                        neighbours[5] = smoothedHeightMap[y + 1, x - 1];
                        neighbours[6] = smoothedHeightMap[y + 1, x];
                        neighbours[7] = smoothedHeightMap[y + 1, x + 1];

                        // http://paulbourke.net/geometry/polygonmesh/
                        float height = smoothedHeightMap[y, x];
                        float smoothedHeight = height;
                        for (int i = 0; i < 8; ++i)
                        {
                            smoothedHeight += (neighbours[i] - height) / 8;
                        }

                        
                        smoothIterationResult[y, x] = smoothedHeight;
                    }
                });

                Array.Copy(smoothIterationResult, smoothedHeightMap, _ascFile.Nrows * _ascFile.Ncols);
            }

            // Calculate delta image
            Parallel.For(1, _ascFile.Nrows - 1, y =>
            {
                for (int x = 0; x < _ascFile.Ncols; x++)
                {
                    var height = smoothedHeightMap[y, x];
                    smoothDelta[y, x] = _ascFile.Data[y, x] - height;
                }
            });

            Task.Run(() =>
            {
                float[,] floatOriginalImageData = new float[_ascFile.Nrows, _ascFile.Ncols];
                Array.Copy(_ascFile.Data, floatOriginalImageData, _ascFile.Data.Length);
                writeGrayscale16Image(floatOriginalImageData, "heightmap");
            });

            Task.Run(() => writeGrayscale16Image(smoothedHeightMap, "heightmapSmoothed"));

            Task.Run(() => writeGrayscale16Image(smoothDelta, "heightmapDelta"));

            Task.WaitAll();
        }

        private void writeGrayscale16Image(float[,] data, string fileName)
        {
            int height = data.GetLength(0);
            int width = data.GetLength(1);
            float min = (float)1e6;
            float max = (float)-1e6;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var val = data[y, x];
                    min = Math.Min(min, val);
                    max = Math.Max(max, val);
                }
            }

            int stride = (width * 16 + 7) / 8;
            int size = height * width;

            ushort[] pixelData = new ushort[size];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var val = data[y, x];
                    int index = y*width + 1*x;

                    ushort smoothedHeightGrayscale = floatToUshort(val, min, max);
                    pixelData[index + 0] = smoothedHeightGrayscale;
                }
            }

            Dispatcher.Invoke(() =>
            {
                using (
                    var fileStream = new FileStream(System.IO.Path.Combine(PathTextBox.Text, fileName + ".png"),
                    FileMode.Create))
                {
                    var encoder = new PngBitmapEncoder();
                    var image = BitmapSource.Create(width, height, 96, 96, PixelFormats.Gray16, null,
                        pixelData,
                        stride);

                    encoder.Frames.Add(BitmapFrame.Create(image));
                    encoder.Save(fileStream);
                }

                File.WriteAllLines(System.IO.Path.Combine(PathTextBox.Text, fileName + ".aux"), new string[] { $"min: {min}", $"max: {max}" });

            });
       }


        private ushort floatToUshort(float value, float min, float max)
        {
            return (ushort) ((value - min)/(max - min)* ushort.MaxValue);
        }

        private void CancelClick(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        static T[,] Cast2D<T>(object[,] input)
        {
            int rows = input.GetLength(0);
            int columns = input.GetLength(1);
            T[,] ret = new T[rows, columns];
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < columns; j++)
                {
                    ret[i, j] = (T)input[i, j];
                }
            }
            return ret;
        }
    }
}