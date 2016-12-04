using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OSGeo.GDAL;

namespace LandscapeClassifier.Util
{
    public class GdalUtil
    {
        public static void WritePng(IntPtr data, int width, int height, string path)
        {
            Driver png = Gdal.GetDriverByName("PNG");
            Driver mem = Gdal.GetDriverByName("MEM");
            using (Dataset memDataSet = mem.Create("", width, height, 1, DataType.GDT_UInt16, new string[0]))
            {
                memDataSet.WriteRaster(0, 0, width, height, data,
                    width, height, DataType.GDT_UInt16, 1, new[] { 1 }, 0, 0, 0);

                Dataset pngDataSet = png.CreateCopy(path, memDataSet, 1, new string[0], null, null);
                pngDataSet.Dispose();
            }
        }

    }
}
