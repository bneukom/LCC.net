using System;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace LandscapeClassifier.Extensions
{
    public static class WriteableBitmapExtensions
    {

        public static float GetFloatPixelValue(this WriteableBitmap writeableBitmap, int x, int y)
        {
            if (x < 0 || y < 0 || x >= writeableBitmap.PixelWidth || y >= writeableBitmap.PixelHeight) return 0;
            unsafe
            {
                float* dataPtr = (float*)writeableBitmap.BackBuffer.ToPointer();
                return *(dataPtr + writeableBitmap.PixelWidth * y + x);
            }
        }

        public static ushort GetUshortPixelValue(this WriteableBitmap writeableBitmap, int x, int y)
        {
            if (x < 0 || y < 0 || x >= writeableBitmap.PixelWidth || y >= writeableBitmap.PixelHeight) return 0;
            unsafe
            {
                ushort* dataPtr = (ushort*)writeableBitmap.BackBuffer.ToPointer();
                return  *(dataPtr + writeableBitmap.PixelWidth * y + x);
            }
        }


        public static ushort GetScaledToUshort(this WriteableBitmap writeableBitmap, int posX, int posY)
        {
            if (writeableBitmap.Format == PixelFormats.Gray32Float)
            {
                float bandIntensity = writeableBitmap.GetFloatPixelValue(posX, posY);
                return (ushort)(bandIntensity * ushort.MaxValue);
            }
            if (writeableBitmap.Format == PixelFormats.Gray16)
            {
                return writeableBitmap.GetUshortPixelValue(posX, posY);
            }
            throw new InvalidOperationException();
        }

        public static byte GetScaledToByte(this WriteableBitmap writeableBitmap, int posX, int posY)
        {
            if (writeableBitmap.Format == PixelFormats.Gray32Float)
            {
                float bandIntensity = writeableBitmap.GetFloatPixelValue(posX, posY);
                return (byte)(bandIntensity * byte.MaxValue);
            }
            if (writeableBitmap.Format == PixelFormats.Gray16)
            {
                ushort bandIntensity = writeableBitmap.GetUshortPixelValue(posX, posY);
                return (byte)((float)bandIntensity / ushort.MaxValue * byte.MaxValue);
            }
            throw new InvalidOperationException();
        }
    }
}
