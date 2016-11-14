using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LandscapeClassifier.Model
{
    public enum SatelliteType
    {
        Sentinel2
    }

    public static class SatelliteTypeExtensions
    {
        public static string GetBand(this SatelliteType satelliteType, string fileName)
        {

            switch (satelliteType)
            {
                case SatelliteType.Sentinel2:
                    return fileName.Substring(fileName.Length - 6, 2);
            }

            return null;
        }

        public static bool isRedBand(this SatelliteType satelliteType, string band)
        {
            return false;
        }
    }
}