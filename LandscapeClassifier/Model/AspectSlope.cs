using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LandscapeClassifier.Model
{
    public struct AspectSlope
    {
        public readonly float Slope;
        public readonly float Aspect;

        public AspectSlope(float slope, float aspect)
        {
            Slope = slope;
            Aspect = aspect;
        }
    }
}
