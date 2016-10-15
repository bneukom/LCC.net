using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Emgu.CV;
using Emgu.CV.ML;
using Emgu.CV.ML.MlEnum;
using LandscapeClassifier.Extensions;
using LandscapeClassifier.Model;
using LandscapeClassifier.ViewModel;

namespace LandscapeClassifier.Classifier
{
    public class KNearestNeighbourClassifier : ILandCoverClassifier
    {
        private KNearest _classifier;

        private const int FeaturesPerVector = 5;

        public void Train(List<ClassifiedFeatureVector> samples)
        {
            _classifier = new KNearest();

            Matrix<float> trainData = new Matrix<float>(samples.Count, FeaturesPerVector);
            Matrix<int> trainClasses = new Matrix<int>(samples.Count, 1);

            _classifier.DefaultK = 5;

            for (var featureIndex = 0; featureIndex < samples.Count; ++featureIndex)
            {
                var classifiedFeature = samples[featureIndex];

                trainClasses[featureIndex, 0] = (int)classifiedFeature.Type;

                trainData[featureIndex, 0] = classifiedFeature.FeatureVector.Altitude;
                trainData[featureIndex, 1] = classifiedFeature.FeatureVector.Color.GetLuminance();
                trainData[featureIndex, 2] = classifiedFeature.FeatureVector.AverageNeighbourhoodColor.GetLuminance();
                trainData[featureIndex, 3] = classifiedFeature.FeatureVector.Aspect;
                trainData[featureIndex, 4] = classifiedFeature.FeatureVector.Slope;
            }

            using (TrainData data = new TrainData(trainData, DataLayoutType.RowSample, trainClasses))
            {
                _classifier.Train(data);
            }
        }

        public LandcoverType Predict(FeatureVector feature)
        {
            Matrix<float> sampleMat = new Matrix<float>(1, FeaturesPerVector)
            {
                Data =
                {
                    [0, 0] = feature.Altitude,
                    [0, 1] = feature.Color.GetLuminance(),
                    [0, 2] = feature.AverageNeighbourhoodColor.GetLuminance(),
                    [0, 3] = feature.Aspect,
                    [0, 4] = feature.Slope
                }
            };


            int result = (int)_classifier.Predict(sampleMat, null);
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
                    var featureIndex = y * dimensionX + x;
                    sampleMat.Data[featureIndex, 0] = feature.Altitude;
                    sampleMat.Data[featureIndex, 1] = feature.Color.GetLuminance();
                    sampleMat.Data[featureIndex, 2] = feature.AverageNeighbourhoodColor.GetLuminance();
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
                var prediction = (int)_classifier.Predict(sampleMat.GetRow(row));
                var y = row / dimensionX;
                var x = row % dimensionX;
                var landCoverType = (LandcoverType)prediction;
                var color = landCoverType.GetColor();

                pixelData[row * 4 + 0] = color.B;
                pixelData[row * 4 + 1] = color.G;
                pixelData[row * 4 + 2] = color.R;
                pixelData[row * 4 + 3] = color.A;
            }

            var predictionBitmapSource = BitmapSource.Create(dimensionX, dimensionY, dpi, dpi, PixelFormats.Bgra32,
                null, pixelData, stride);

            return predictionBitmapSource;
        }
    }
}
