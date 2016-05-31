using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.ML;
using Emgu.CV.Structure;
using LandscapeClassifier.Model;

namespace LandscapeClassifier.Classifier
{
    public class NeuralNetworkLandCoverClassifier : ILandCoverClassifier
    {
        private ANN_MLP mlp;

        public void Train(List<ClassifiedFeatureVector> samples)
        {
            mlp = new ANN_MLP();
            var criteria = new MCvTermCriteria
            {
                Epsilon = 0.00001f,
                MaxIter = 100,
                Type = TermCritType.Iter | TermCritType.Eps
            };

            var layerSizes = new Matrix<float>(4, 1, 1);

            mlp.TermCriteria = criteria;
            mlp.BackpropMomentumScale = 0.05f;
            mlp.BackpropWeightScale = 0.05f;
            mlp.SetTrainMethod(ANN_MLP.AnnMlpTrainMethod.Backprop);
            mlp.SetLayerSizes(layerSizes);
        }

        public LandcoverType Predict(FeatureVector feature)
        {
            throw new NotImplementedException();
        }

        public BitmapSource Predict(FeatureVector[,] features)
        {
            throw new NotImplementedException();
        }
    }
}
