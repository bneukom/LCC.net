using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using LandscapeClassifier.Extensions;
using LandscapeClassifier.Model;

namespace LandscapeClassifier.Classifier
{
    public class NearestNeighbourClassifier : ILandCoverClassifier
    {
        private List<ClassifiedFeatureVector> _samples;
        private float _altitudeNormalization;

        public void Train(List<ClassifiedFeatureVector> samples)
        {
            this._samples = samples;

            _altitudeNormalization = samples.Max(s => s.FeatureVector.Altitude);
        }

        public LandcoverType Predict(FeatureVector feature)
        {

            float minDistance = float.MaxValue;
            LandcoverType bestType = LandcoverType.Grass;



            var alt = feature.Altitude / _altitudeNormalization;
            var asp = feature.Aspect / (2 * Math.PI);
            var slp = feature.Slope / (2 * Math.PI);
            var lum = feature.Color.GetLuminance() / 255f;
            var nlum = feature.AverageNeighbourhoodColor.GetLuminance() / 255f;

            foreach (var classifiedFeatureVector in _samples)
            {
                var dalt = alt - classifiedFeatureVector.FeatureVector.Altitude / _altitudeNormalization;
                var dasp = asp - classifiedFeatureVector.FeatureVector.Aspect / (2 *Math.PI);
                var dslp = slp - classifiedFeatureVector.FeatureVector.Slope / (2 * Math.PI);
                var dlum = lum - classifiedFeatureVector.FeatureVector.Color.GetLuminance() / 255f;
                var dnlum = nlum - classifiedFeatureVector.FeatureVector.AverageNeighbourhoodColor.GetLuminance() / 255f;

                var distance = (float)Math.Sqrt(dalt*dalt + dasp*dasp + dslp*dslp + dlum*dlum + dnlum*dnlum);

                if (distance < minDistance)
                {
                    minDistance = distance;
                    bestType = classifiedFeatureVector.Type;
                }
            }

            return bestType;
        }

        public BitmapSource Predict(FeatureVector[,] features)
        {
            throw new NotImplementedException();
        }

    }
}
