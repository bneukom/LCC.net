using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LandscapeClassifier.Model.Classification
{
    public class ClassificationModel
    {
        public readonly string Projection;
        public readonly List<int> Bands;
        public readonly List<ClassifiedFeatureVector> ClassifiedFeatureVectors;

        public ClassificationModel(string projection, List<int> bands, List<ClassifiedFeatureVector> classifiedFeatureVectors)
        {
            Projection = projection;
            Bands = bands;
            ClassifiedFeatureVectors = classifiedFeatureVectors;
        }
    }
}
