using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using OSGeo.GDAL;

namespace LandscapeClassifier.Extensions
{
    public static class DataTypeExtensions
    {
        public static PixelFormat ToPixelFormat(this DataType dataType)
        {
            switch (dataType)
            {
                case DataType.GDT_Int16:
                    return PixelFormats.Gray16;
                case DataType.GDT_UInt16:
                    return PixelFormats.Gray16;
                case DataType.GDT_Float32:
                    return PixelFormats.Gray32Float;
                default:
                    throw new ArgumentOutOfRangeException(nameof(dataType), dataType, null);
            }
        }
    }
}
