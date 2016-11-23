using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LandscapeClassifier.Model.Classification
{

    public class ClassificationModel
    {

        public List<ClassifiedFeatureVector> ClassifiedFeatureVectors { get; }
        public List<string> Bands { get; }
        public string Projection { get; }

        public ClassificationModel(string projection, List<string> bands, List<ClassifiedFeatureVector> classifiedFeatureVectors)
        {
            Projection = projection;
            Bands = bands;
            ClassifiedFeatureVectors = classifiedFeatureVectors;
        }
    }
}
