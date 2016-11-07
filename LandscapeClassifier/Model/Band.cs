using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LandscapeClassifier.Model
{
    public class Band
    {
        public readonly string ProjectionName;
        public readonly int BandIndex;

        public Band(string projectionName, int bandIndex)
        {
            ProjectionName = projectionName;
            this.BandIndex = bandIndex;
        }
    }
}
