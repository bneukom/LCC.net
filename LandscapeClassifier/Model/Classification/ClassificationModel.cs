using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LandscapeClassifier.Model.Classification
{
    public class ClassificationModel
    {
        public readonly List<Band> Bands;
        public readonly List<ClassifiedFeatureVector> ClassifiedFeatureVectors;

        public ClassificationModel(List<Band> bands, List<ClassifiedFeatureVector> classifiedFeatureVectors)
        {
            Bands = bands;
            ClassifiedFeatureVectors = classifiedFeatureVectors;
        }
    }
}
