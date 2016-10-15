using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LandscapeClassifier.Classifier
{
    public static class ClassifierExtensions
    {
        public static ILandCoverClassifier CreateClassifier(this Classifier classifier)
        {
            switch (classifier)
            {
                case Classifier.Bayes:
                    return new BayesLandCoverClassifier();
                case Classifier.ANN:
                    return new NeuralNetworkLandCoverClassifier();
                case Classifier.NearestNeighbour:
                    return new NearestNeighbourClassifier();
                case Classifier.KNearestNeighbour:
                    return new KNearestNeighbourClassifier();
                case Classifier.DecisionTrees:
                    return new DecisionTreeClassifier();
                default:
                    throw new ArgumentOutOfRangeException(nameof(classifier), classifier, null);
            }
        }
    }
}