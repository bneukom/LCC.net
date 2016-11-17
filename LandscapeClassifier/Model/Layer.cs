using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LandscapeClassifier.Model
{
    // TODO use for LayerViewModel
    public class Layer
    {
        public readonly string ProjectionName;
        public readonly int BandIndex;

        public Layer(string projectionName, int bandIndex)
        {
            ProjectionName = projectionName;
            BandIndex = bandIndex;
        }
    }
}
