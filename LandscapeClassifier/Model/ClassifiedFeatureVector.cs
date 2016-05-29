using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LandscapeClassifier.Model
{
    public class ClassifiedFeatureVector
    {
        public LandcoverType Type { get; set; }
        public FeatureVector FeatureVector { get; set; }

        public ClassifiedFeatureVector(LandcoverType type, FeatureVector featureVector)
        {
            Type = type;
            FeatureVector = featureVector;
        }
    }
}
