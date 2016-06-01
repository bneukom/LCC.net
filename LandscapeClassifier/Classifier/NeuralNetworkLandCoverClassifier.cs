using System;
using System.Collections.Generic;
using System.Windows.Media.Imaging;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.ML;
using Emgu.CV.ML.MlEnum;
using Emgu.CV.Structure;
using LandscapeClassifier.Extensions;
using LandscapeClassifier.Model;

namespace LandscapeClassifier.Classifier
{
    // TODO see https://github.com/arnaudgelas/OpenCVExamples/blob/master/NeuralNetwork/NeuralNetwork.cpp
    public class NeuralNetworkLandCoverClassifier : ILandCoverClassifier
    {
        private ANN_MLP mlp;

        private const int FeaturesPerVector = 5;
        private readonly int NumClasses;

        public NeuralNetworkLandCoverClassifier()
        {
            NumClasses = Enum.GetValues(typeof(LandcoverType)).Length;
        }

        public void Train(List<ClassifiedFeatureVector> samples)
        {
            mlp = new ANN_MLP();
            var criteria = new MCvTermCriteria
            {
                Epsilon = 0.000001f,
                MaxIter = 100,
                Type = TermCritType.Iter | TermCritType.Eps
            };

            var layerSizes = new Matrix<float>(4, 1, 1)
            {
                [0, 0] = FeaturesPerVector,
                [1, 0] = 10,
                [2, 0] = 15,
                [3, 0] = NumClasses
            };


            mlp.TermCriteria = criteria;
            mlp.BackpropMomentumScale = 0.05f;
            mlp.BackpropWeightScale = 0.05f;
            mlp.SetTrainMethod(ANN_MLP.AnnMlpTrainMethod.Backprop);
            mlp.SetLayerSizes(layerSizes);

            mlp.SetActivationFunction(ANN_MLP.AnnMlpActivationFunction.SigmoidSym);

            Matrix<float> trainData = new Matrix<float>(samples.Count, FeaturesPerVector);
            Matrix<float> trainClasses = new Matrix<float>(samples.Count, NumClasses);

            for (var featureIndex = 0; featureIndex < samples.Count; ++featureIndex)
            {
                var classifiedFeature = samples[featureIndex];

                trainClasses[featureIndex, (int)classifiedFeature.Type] = 1;

                trainData[featureIndex, 0] = classifiedFeature.FeatureVector.Altitude;
                trainData[featureIndex, 1] = classifiedFeature.FeatureVector.Color.GetARGB();
                trainData[featureIndex, 2] = classifiedFeature.FeatureVector.AverageNeighbourhoodColor.GetARGB();
                trainData[featureIndex, 3] = classifiedFeature.FeatureVector.Aspect;
                trainData[featureIndex, 4] = classifiedFeature.FeatureVector.Slope;
            }

            try
            {
                using (TrainData data = new TrainData(trainData, DataLayoutType.RowSample, trainClasses))
                {
                    mlp.Train(data);

                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
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

            Matrix<float> responseMatrix = new Matrix<float>(1, NumClasses);

            mlp.Predict(sampleMat, responseMatrix);
            Console.WriteLine(responseMatrix);

            return LandcoverType.Grass;
        }

        public BitmapSource Predict(FeatureVector[,] features)
        {
            throw new NotImplementedException();
        }
    }
}