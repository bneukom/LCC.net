using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Ioc;
using LandscapeClassifier.Extensions;
using LandscapeClassifier.Model;
using LandscapeClassifier.Model.Classification;

namespace LandscapeClassifier.ViewModel.MainWindow.Classification
{
    public class ClassifiedFeatureVectorViewModel : ViewModelBase
    {
        private LandcoverTypeViewModel _featureTypeViewModel;

        /// <summary>
        /// The feature vector model.
        /// </summary>
        public ClassifiedFeatureVector ClassifiedFeatureVector { get; }


        /// <summary>
        /// The class of this feature.
        /// </summary>
        public LandcoverTypeViewModel FeatureTypeViewModel
        {
            get { return _featureTypeViewModel; }
            set { _featureTypeViewModel = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// Position of the feature.
        /// </summary>
        public Point Position => ClassifiedFeatureVector.Position;

        /// <summary>
        /// The values of this feature vector.
        /// </summary>
        public string FeatureValues => "(" + BandIntensitiesToFloat(ClassifiedFeatureVector.FeatureVector.BandIntensities) + ")";

        public ClassifiedFeatureVectorViewModel(ClassifiedFeatureVector classifiedFeatureVector)
        {
            ClassifiedFeatureVector = classifiedFeatureVector;
            FeatureTypeViewModel = MainWindowViewModel.Default.LandcoverTypes.Values.ToList()[ClassifiedFeatureVector.FeatureClass];
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
