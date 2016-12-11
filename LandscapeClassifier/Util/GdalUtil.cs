using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using OSGeo.GDAL;

namespace LandscapeClassifier.Util
{
    public class GdalUtil
    {
        public static void WritePng(IntPtr data, int dataWidth, int dataHeight, int outputWidth, int outputHeight, string path)
        {
            Driver png = Gdal.GetDriverByName("PNG");
            Driver mem = Gdal.GetDriverByName("MEM");
            using (Dataset memDataSet = mem.Create("", outputWidth, outputHeight, 1, DataType.GDT_UInt16, new string[0]))
            {
                memDataSet.WriteRaster(0, 0, outputWidth, outputHeight, data, dataWidth, dataHeight, DataType.GDT_UInt16, 1, new[] { 1 }, 0, 0, 0);
                Dataset pngDataSet = png.CreateCopy(path, memDataSet, 1, new string[0], null, null);
                pngDataSet.Dispose();
            }
        }

        public static void WritePng(IntPtr data, int width, int height, string path)
        {
            WritePng(data, width, height, width, height, path);
        }

        public static void WritePng(ushort[] data, int width, int height, string path)
        {
            short[] temp = new short[data.Length];
            Buffer.BlockCopy(data, 0, temp, 0, temp.Length*2);

            IntPtr ptr = Marshal.AllocHGlobal(data.Length * 2);
            Marshal.Copy(temp, 0, ptr, data.Length);

            WritePng(ptr, width, height, path);

            Marshal.FreeHGlobal(ptr);
        }

    }
}
