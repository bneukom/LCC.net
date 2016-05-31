using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using GalaSoft.MvvmLight;
using LandscapeClassifier.Model;

namespace LandscapeClassifier.ViewModel
{
    public class ClassifiedFeatureVectorViewModel : ViewModelBase
    {

        public ClassifiedFeatureVector ClassifiedFeatureVector { get; }

        /// <summary>
        /// 
        /// </summary>
        public SolidColorBrush TypeColorBrush =>  new SolidColorBrush(ClassifiedFeatureVector.FeatureVector.Color);

        /// <summary>
        /// 
        /// </summary>
        public string TypeText => ClassifiedFeatureVector.Type.ToString();

        /// <summary>
        /// 
        /// </summary>
        public string FeatureText => 
            ClassifiedFeatureVector.FeatureVector.Altitude + " " + 
            ClassifiedFeatureVector.FeatureVector.Slope + " " +
            ClassifiedFeatureVector.FeatureVector.Aspect;

        public ClassifiedFeatureVectorViewModel(ClassifiedFeatureVector classifiedFeatureVector)
        {
            ClassifiedFeatureVector = classifiedFeatureVector;

        }

    }
}
