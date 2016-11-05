using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LandscapeClassifier.Model
{
    public enum SateliteType
    {
        Sentinel2
    }

    public static class SateliteTypeExtensions
    {
        public static int GetBand(this SateliteType sateliteType, string fileName)
        {
            int bandNumber = -1;

            switch (sateliteType)
            {
                case SateliteType.Sentinel2:
                    var bandNumberString = fileName.Substring(fileName.Length - 6, 2);
                    bandNumber = int.Parse(bandNumberString);
                    break;
            }

            return bandNumber;
        }
    }
}