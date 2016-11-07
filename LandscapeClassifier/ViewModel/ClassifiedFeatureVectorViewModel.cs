using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using GalaSoft.MvvmLight;
using LandscapeClassifier.Model;
using LandscapeClassifier.Model.Classification;

namespace LandscapeClassifier.ViewModel
{
    public class ClassifiedFeatureVectorViewModel : ViewModelBase
    {

        public ClassifiedFeatureVector ClassifiedFeatureVector { get; }

        /// <summary>
        /// Brush for the feature.
        /// </summary>
        public SolidColorBrush FeatureClassColorBrush =>  new SolidColorBrush(Colors.Black);

        /// <summary>
        /// The class of this feature.
        /// </summary>
        public string FeatureClass => ClassifiedFeatureVector.Type.ToString();

        public ClassifiedFeatureVectorViewModel(ClassifiedFeatureVector classifiedFeatureVector)
        {
            ClassifiedFeatureVector = classifiedFeatureVector;
        }

    }
}
