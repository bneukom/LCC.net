using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LandscapeClassifier.Model.Classification.Options;

namespace LandscapeClassifier.Classifier
{
    public static class ClassifierExtensions
    {
        public static ILandCoverClassifier<T> CreateClassifier<T>(this Classifier classifier)
        {
            switch (classifier)
            {
                case Classifier.DecisionTrees:
                    return (dynamic)new DecisionTreeClassifier();
                case Classifier.Bayes:
                    return (dynamic)new BayesClassifier();
                case Classifier.SVM:
                    return (dynamic)new SvmClassifier();
                default:
                    throw new ArgumentOutOfRangeException(nameof(classifier), classifier, null);
            }
        }
    }
}