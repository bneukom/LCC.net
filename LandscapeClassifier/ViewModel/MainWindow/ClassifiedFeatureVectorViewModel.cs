using System;
using System.Windows.Media;
using GalaSoft.MvvmLight;
using LandscapeClassifier.Extensions;
using LandscapeClassifier.Model.Classification;

namespace LandscapeClassifier.ViewModel.MainWindow
{
    public class ClassifiedFeatureVectorViewModel : ViewModelBase
    {

        public ClassifiedFeatureVector ClassifiedFeatureVector { get; }

        /// <summary>
        /// Brush for the feature.
        /// </summary>
        public Brush FeatureClassColorBrush => ClassifiedFeatureVector.Type.GetDefaultBrush();

        /// <summary>
        /// The class of this feature.
        /// </summary>
        public string FeatureClass => ClassifiedFeatureVector.Type.ToString();

        /// <summary>
        /// The values of this feature vector.
        /// </summary>
        public string FeatureValues => "(" + BandIntensitiesToFloat(ClassifiedFeatureVector.FeatureVector.BandIntensities) + ")";

        public ClassifiedFeatureVectorViewModel(ClassifiedFeatureVector classifiedFeatureVector)
        {
            ClassifiedFeatureVector = classifiedFeatureVector;
        }

        private static string BandIntensitiesToFloat(ushort[] intensities)
        {
            string result = "";
            for (int i = 0; i < intensities.Length; ++i)
            {
                ushort shortValue = intensities[i];
                double doubleValue = Math.Round((double)shortValue / ushort.MaxValue, 2);
                result += doubleValue.ToString();
                if (i < intensities.Length - 1)
                {
                    result += ", ";
                }
            }
            return result;
        }
    }
}
