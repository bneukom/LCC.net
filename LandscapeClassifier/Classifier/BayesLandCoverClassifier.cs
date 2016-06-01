using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.ML;
using Emgu.CV.ML.MlEnum;
using Emgu.CV.Structure;
using LandscapeClassifier.Extensions;
using LandscapeClassifier.Model;
using LandscapeClassifier.ViewModel;

namespace LandscapeClassifier.Classifier
{
    public class BayesLandCoverClassifier : ILandCoverClassifier
    {
        
        private NormalBayesClassifier classifier;

        private const int FeaturesPerVector = 5;

        public BayesLandCoverClassifier()
        {
            
        }

        // TODO http://www.emgu.com/wiki/index.php/Normal_Bayes_Classifier_in_CSharp
        // TODO http://www.bytefish.de/pdf/machinelearning.pdf
        public void Train(List<ClassifiedFeatureVector> samples)
        {
            classifier = new NormalBayesClassifier();
            Matrix<float> trainData = new Matrix<float>(samples.Count, FeaturesPerVector);
            Matrix<int> trainClasses = new Matrix<int>(samples.Count, 1);


            for (var featureIndex = 0; featureIndex < samples.Count; ++ featureIndex)
            {
                var classifiedFeature = samples[featureIndex];

                trainClasses[featureIndex, 0] = (int) classifiedFeature.Type;

                trainData[featureIndex, 0] = classifiedFeature.FeatureVector.Altitude;
                trainData[featureIndex, 1] = classifiedFeature.FeatureVector.Color.GetARGB();
                trainData[featureIndex, 2] = classifiedFeature.FeatureVector.AverageNeighbourhoodColor.GetARGB();
                trainData[featureIndex, 3] = classifiedFeature.FeatureVector.Aspect;
                trainData[featureIndex, 4] = classifiedFeature.FeatureVector.Slope;
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
                    [0, 1] = feature.Color.GetARGB(),
                    [0, 2] = feature.AverageNeighbourhoodColor.GetARGB(),
                    [0, 3] = feature.Aspect,
                    [0, 4] = feature.Slope
                }
            };


            int result = (int)classifier.Predict(sampleMat, null);
            return (LandcoverType)result;
        }

        public BitmapSource Predict(FeatureVector[,] features)
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
                    sampleMat.Data[featureIndex, 1] = (feature.Color.A << 24) | (feature.Color.R << 16) |
                                                      (feature.Color.G << 8) | feature.Color.B;
                    sampleMat.Data[featureIndex, 2] = (feature.AverageNeighbourhoodColor.A << 24) | (feature.AverageNeighbourhoodColor.R << 16) |
                                                      (feature.AverageNeighbourhoodColor.G << 8) | feature.AverageNeighbourhoodColor.B;
                    sampleMat.Data[featureIndex, 3] = feature.Aspect;
                    sampleMat.Data[featureIndex, 4] = feature.Slope;
                }
            }


            var dpi = 96d;
            var width = dimensionX;
            var height = dimensionY;

            var stride = width * 4; // 4 bytes per pixel
            var pixelData = new byte[stride * height];

            for (var row = 0; row < sampleMat.Rows; row++)
            {
                var prediction = (int)classifier.Predict(sampleMat.GetRow(row));
                var y = row / dimensionX;
                var x = row % dimensionX;
                var landCoverType = (LandcoverType) prediction;
                var color = landCoverType.GetColor();

                pixelData[row * 4 + 0] = color.B;
                pixelData[row * 4 + 1] = color.G;
                pixelData[row * 4 + 2] = color.R;
                pixelData[row * 4 + 3] = color.A;
            }

            var predictionBitmapSource = BitmapSource.Create(dimensionX, dimensionY, dpi, dpi,  PixelFormats.Bgra32,
                null, pixelData, stride);

            return predictionBitmapSource;
        }
    }
}
