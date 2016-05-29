using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.ML;
using Emgu.CV.ML.MlEnum;
using Emgu.CV.Structure;
using LandscapeClassifier.Model;

namespace LandscapeClassifier.Classifier
{
    public class BayesLandCoverClassifier : ILandCoverClassifier
    {
        
        private NormalBayesClassifier classifier;

        private const int NumFeatures = 6;

        public BayesLandCoverClassifier()
        {
            classifier = new NormalBayesClassifier();
        }

        // TODO http://www.emgu.com/wiki/index.php/Normal_Bayes_Classifier_in_CSharp
        public void Train(List<ClassifiedFeatureVector> samples)
        {

            Matrix<float> trainData = new Matrix<float>(samples.Count, NumFeatures);
            Matrix<int> trainClasses = new Matrix<int>(samples.Count, 1);

            trainData.Data[0, 0] = 3;

            for (var featureIndex = 0; featureIndex < samples.Count; ++ featureIndex)
            {
                var classifiedFeature = samples[featureIndex];

                trainClasses[featureIndex, 0] = (int) classifiedFeature.Type;

                trainData[featureIndex, 0] = classifiedFeature.FeatureVector.Altitude;
                trainData[featureIndex, 1] = classifiedFeature.FeatureVector.Color.R;
                trainData[featureIndex, 2] = classifiedFeature.FeatureVector.Color.G;
                trainData[featureIndex, 3] = classifiedFeature.FeatureVector.Color.B;
                trainData[featureIndex, 4] = classifiedFeature.FeatureVector.Aspect;
                trainData[featureIndex, 5] = classifiedFeature.FeatureVector.Slope;
            }

            using (TrainData data = new TrainData(trainData, DataLayoutType.RowSample, trainClasses))
            {
                classifier.Train(data);
            }
            
        }

        public LandcoverType Predict(FeatureVector feature)
        {
            Matrix<float> sampleMat = new Matrix<float>(1, NumFeatures)
            {
                Data =
                {
                    [0, 0] = feature.Altitude,
                    [0, 1] = feature.Color.R,
                    [0, 2] = feature.Color.G,
                    [0, 3] = feature.Color.B,
                    [0, 4] = feature.Aspect,
                    [0, 5] = feature.Slope
                }
            };


            int result = (int)classifier.Predict(sampleMat, null);
            return (LandcoverType)result;
        }
    }
}
