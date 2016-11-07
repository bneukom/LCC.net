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
        public readonly string Path;
        public readonly int BandIndex;

        public Band(string projectionName, string path)
        {
            ProjectionName = projectionName;
            Path = path;
        }
    }
}
