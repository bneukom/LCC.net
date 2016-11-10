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
        public static int GetBand(this SatelliteType satelliteType, string fileName)
        {
            int bandNumber = -1;

            switch (satelliteType)
            {
                case SatelliteType.Sentinel2:
                    var bandNumberString = fileName.Substring(fileName.Length - 6, 2);
                    bandNumber = int.Parse(bandNumberString);
                    break;
            }

            return bandNumber;
        }
    }
}