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

        private const int FeaturesPerVector = 6;

        private Matrix<float> SampleMat = new Matrix<float>(1, FeaturesPerVector);

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
                var wpfColor = classifiedFeature.FeatureVector.AverageNeighbourhoodColor;
                var color = System.Drawing.Color.FromArgb(wpfColor.R, wpfColor.G, wpfColor.B);

                trainClasses[featureIndex, 0] = (int) classifiedFeature.Type;
                trainData[featureIndex, 0] = classifiedFeature.FeatureVector.Altitude;
                trainData[featureIndex, 1] = color.GetHue();
                trainData[featureIndex, 2] = color.GetSaturation();
                trainData[featureIndex, 3] = color.GetBrightness();
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
            var wpfColor = feature.AverageNeighbourhoodColor;
            var color = System.Drawing.Color.FromArgb(wpfColor.R, wpfColor.G, wpfColor.B);

            SampleMat.Data[0, 0] = feature.Altitude;
            SampleMat.Data[0, 1] = color.GetHue();
            SampleMat.Data[0, 2] = color.GetSaturation();
            SampleMat.Data[0, 3] = color.GetBrightness();
            SampleMat.Data[0, 4] = feature.Aspect;
            SampleMat.Data[0, 5] = feature.Slope;


            int result = (int)classifier.Predict(SampleMat, null);
            return (LandcoverType)result;
        }

        public BitmapSource Predict(FeatureVector[,] features)
        {
            var dimensionY = features.GetLength(0);
            var dimensionX = features.GetLength(1);

            Matrix<float> sampleMat = new Matrix<float>(dimensionX * dimensionY, FeaturesPerVector);
            Parallel.For(0, dimensionY, y => 
            { 
                for (var x = 0; x < dimensionX; ++x)
                {
                    var feature = features[y, x];
                    var featureIndex = y*dimensionX + x;
                    var wpfColor = feature.AverageNeighbourhoodColor;
                    var color = System.Drawing.Color.FromArgb(wpfColor.R, wpfColor.G, wpfColor.B);

                    sampleMat.Data[featureIndex, 0] = feature.Altitude;
                    sampleMat.Data[featureIndex, 1] = color.GetHue();
                    sampleMat.Data[featureIndex, 2] = color.GetSaturation();
                    sampleMat.Data[featureIndex, 3] = color.GetBrightness();
                    sampleMat.Data[featureIndex, 4] = feature.Aspect;
                    sampleMat.Data[featureIndex, 5] = feature.Slope;
                }
            });

            Matrix<int> results = new Matrix<int>(sampleMat.Rows, 1);


            var dpi = 96d;
            var width = dimensionX;
            var height = dimensionY;

            var stride = width * 4; // 4 bytes per pixel
            var pixelData = new byte[stride * height];

            classifier.Predict(sampleMat, results);

            Parallel.For(0, results.Rows, row =>
            {
                var prediction = (int)results[row, 0];
                var landCoverType = (LandcoverType)prediction;
                var color = landCoverType.GetColor();

                pixelData[row * 4 + 0] = color.B;
                pixelData[row * 4 + 1] = color.G;
                pixelData[row * 4 + 2] = color.R;
                pixelData[row * 4 + 3] = color.A;
            });

            var predictionBitmapSource = BitmapSource.Create(dimensionX, dimensionY, dpi, dpi,  PixelFormats.Bgra32,
                null, pixelData, stride);

            return predictionBitmapSource;
        }

    }
}
