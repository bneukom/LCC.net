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
        public SolidColorBrush FeatureClassColorBrush
        {
            get
            {
                switch (ClassifiedFeatureVector.Type)
                {
                    case LandcoverType.Grass:
                        return new SolidColorBrush(Colors.LightGreen);
                    case LandcoverType.Gravel:
                        return new SolidColorBrush(Colors.LightGray);
                    case LandcoverType.Rock:
                        return new SolidColorBrush(Colors.DarkGray);
                    case LandcoverType.Snow:
                        return new SolidColorBrush(Colors.White);
                    case LandcoverType.Tree:
                        return new SolidColorBrush(Colors.DarkGreen);
                    case LandcoverType.Water:
                        return new SolidColorBrush(Colors.DodgerBlue);
                    default:
                        return new SolidColorBrush(Colors.White);
                }
            }
        }

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
