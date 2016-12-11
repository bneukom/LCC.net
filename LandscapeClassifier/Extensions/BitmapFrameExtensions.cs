using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace LandscapeClassifier.Extensions
{
    public static class BitmapFrameExtensions
    {
        public static void SaveAsPng(this BitmapFrame frame, string filePath)
        {
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                BitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(frame);
                encoder.Save(fileStream);
            }
        }
    }
}
