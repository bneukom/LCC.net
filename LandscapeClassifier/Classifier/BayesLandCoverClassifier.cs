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

        private const int FeaturesPerVector = 6;

        public BayesLandCoverClassifier()
        {
            classifier = new NormalBayesClassifier();
        }

        // TODO http://www.emgu.com/wiki/index.php/Normal_Bayes_Classifier_in_CSharp
        // TODO http://www.bytefish.de/pdf/machinelearning.pdf
        public void Train(List<ClassifiedFeatureVector> samples)
        {

            Matrix<float> trainData = new Matrix<float>(samples.Count, FeaturesPerVector);
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
            Matrix<float> sampleMat = new Matrix<float>(1, FeaturesPerVector)
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

        public LandcoverType[,] Predict(FeatureVector[,] features)
        {
            var dimensionY = features.GetLength(0);
            var dimensionX = features.GetLength(1);

            Matrix<float> sampleMat = new Matrix<float>(dimensionX * dimensionY, FeaturesPerVector);
            for (var y = 0; y < dimensionY; ++y)
            {
                for (var x = 0; x < dimensionX; ++x)
                {
                    var feature = features[y, x];
                    var featureIndex = y*dimensionX + x;
                    sampleMat.Data[featureIndex, 0] = feature.Altitude;
                    sampleMat.Data[featureIndex, 1] = feature.Color.R;
                    sampleMat.Data[featureIndex, 2] = feature.Color.G;
                    sampleMat.Data[featureIndex, 3] = feature.Color.B;
                    sampleMat.Data[featureIndex, 4] = feature.Aspect;
                    sampleMat.Data[featureIndex, 5] = feature.Slope;
                }
            }

            LandcoverType[,] predictions = new LandcoverType[dimensionY, dimensionX];
            for (var row = 0; row < sampleMat.Rows; row++)
            {
                var prediction = (int)classifier.Predict(sampleMat.GetRow(row));
                var y = row / dimensionX;
                var x = row % dimensionX;
                predictions[y, x] = (LandcoverType) prediction;
            }

            return predictions;
        }
    }
}
