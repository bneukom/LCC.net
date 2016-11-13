using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using GalaSoft.MvvmLight;
using LandscapeClassifier.Model;
using Xceed.Wpf.Toolkit.PropertyGrid.Converters;

namespace LandscapeClassifier.ViewModel.MainWindow.Classification
{
    public class FeatureByTypeViewModel : ViewModelBase
    {
        public ObservableCollection<ClassifiedFeatureVectorViewModel> Features { get; set; }
        private LandcoverType _type;

        public LandcoverType Type
        {
            get { return _type; }
            set { _type = value; RaisePropertyChanged(); }
        }

        public int NumberOfFeatures => Features.Count;

        public FeatureByTypeViewModel(LandcoverType type)
        {
            Type = type;
            Features = new ObservableCollection<ClassifiedFeatureVectorViewModel>();
        }
    }

    public class FeaturesViewModel : ViewModelBase
    {
        public ObservableCollection<FeatureByTypeViewModel> FeaturesByType { get; set; }

        public List<ClassifiedFeatureVectorViewModel> AllFeaturesView => FeaturesByType.SelectMany(l => l.Features).ToList();

        public static readonly string FeatureProperty = "Feature";

        public FeaturesViewModel()
        {
            FeaturesByType = new ObservableCollection<FeatureByTypeViewModel>();

            foreach (LandcoverType landCoverType in Enum.GetValues(typeof(LandcoverType)))
            {
                FeaturesByType.Add(new FeatureByTypeViewModel(landCoverType));
            }
        }

        public ObservableCollection<ClassifiedFeatureVectorViewModel> GetFeaturesByType(LandcoverType type)
        {
            return FeaturesByType.First(f => f.Type == type).Features;
        }

        public bool HasFeatures()
        {
            return FeaturesByType.Any(f => f.Features.Count > 0);
        }

        public void RemoveAllFeatures()
        {
            foreach (var featureByType in FeaturesByType)
            {
                featureByType.Features.Clear();
            }
        }

        public void AddFeature(ClassifiedFeatureVectorViewModel classifiedFeatureVector)
        {
            FeaturesByType.First(f => f.Type == classifiedFeatureVector.FeatureType).Features.Add(classifiedFeatureVector);

            RaisePropertyChanged(FeatureProperty);
        }

        public void RemoveFeature(ClassifiedFeatureVectorViewModel classifiedFeatureVector)
        {
            FeaturesByType.First(f => f.Type == classifiedFeatureVector.FeatureType).Features.Remove(classifiedFeatureVector);

            RaisePropertyChanged(FeatureProperty);
        }
    }
}
