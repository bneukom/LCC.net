using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace LandscapeClassifier.Extensions
{
    public static class WriteableBitmapExtensions
    {
        public static ushort GetUshortPixelValue(this WriteableBitmap writeableBitmap, int x, int y)
        {
            if (x < 0 || y < 0 || x >= writeableBitmap.PixelWidth || y >= writeableBitmap.PixelHeight) return 0;
            unsafe
            {
                ushort* dataPtr = (ushort*)writeableBitmap.BackBuffer.ToPointer();
                return  *(dataPtr + writeableBitmap.PixelWidth * y + x);
            }
        }

    }
}
