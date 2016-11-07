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
            unsafe
            {
                ushort* dataPtr = (ushort*)writeableBitmap.BackBuffer.ToPointer();
                return (ushort) (dataPtr + writeableBitmap.BackBufferStride * y + x * sizeof(ushort));
            }
        }
    }
}
