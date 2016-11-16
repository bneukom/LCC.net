using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LandscapeClassifier.Model
{
    public enum SatelliteType
    {
        Sentinel2, None
    }

    public static class SatelliteTypeExtensions
    {
        public static bool IsRedBand(this SatelliteType satelliteType, string fileName)
        {
            switch (satelliteType)
            {
                case SatelliteType.Sentinel2:
                    return fileName.Substring(fileName.Length - 2, 2) == "04";
                case SatelliteType.None:
                    return false;
            }
            return false;
        }


        public static bool IsGreenBand(this SatelliteType satelliteType, string fileName)
        {
            switch (satelliteType)
            {
                case SatelliteType.Sentinel2:
                    return fileName.Substring(fileName.Length - 2, 2) == "03";
                case SatelliteType.None:
                    return false;
            }
            return false;
        }

        public static bool IsBlueBand(this SatelliteType satelliteType, string fileName)
        {
            switch (satelliteType)
            {
                case SatelliteType.Sentinel2:
                    return fileName.Substring(fileName.Length - 2, 2) == "02";
                case SatelliteType.None:
                    return false;
            }
            return false;
        }


        public static string GetBand(this SatelliteType satelliteType, string fileName)
        {

            switch (satelliteType)
            {
                case SatelliteType.Sentinel2:
                    return fileName.Substring(fileName.Length - 2, 2);
                case SatelliteType.None:
                    return null;
            }

            return null;
        }

        public static bool MatchesFile(this SatelliteType satelliteType, string fileName)
        {
            string withoutExtension = Path.GetFileNameWithoutExtension(fileName);

            switch (satelliteType)
            {
                case SatelliteType.Sentinel2:
                    return Regex.IsMatch(withoutExtension, "[0-9A-Z]{3}_[A-Z]{4}_[0-9A-Z_]{16}[0-9]{8}T[0-9]{6}_[0-9A-Z]{7}_[0-9A-Z]{6}_[0-9A-Z]{3}");
                case SatelliteType.None:
                    return false;
                default:
                    throw new ArgumentOutOfRangeException(nameof(satelliteType), satelliteType, null);
            }
        }
    }
}