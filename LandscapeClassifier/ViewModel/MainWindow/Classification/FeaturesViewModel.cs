using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using GalaSoft.MvvmLight;
using LandscapeClassifier.Extensions;
using LandscapeClassifier.Model;
using Xceed.Wpf.Toolkit.PropertyGrid.Converters;

namespace LandscapeClassifier.ViewModel.MainWindow.Classification
{
    public class FeatureByTypeViewModel : ViewModelBase
    {
        public ObservableCollection<ClassifiedFeatureVectorViewModel> Features { get; set; }
        private LandcoverTypeViewModel _landCoverType;

        public LandcoverTypeViewModel LandCoverType
        {
            get { return _landCoverType; }
            set { _landCoverType = value; RaisePropertyChanged(); }
        }

        public int NumberOfFeatures => Features.Count;

        public FeatureByTypeViewModel(LandcoverTypeViewModel landCoverType)
        {
            LandCoverType = landCoverType;
            Features = new ObservableCollection<ClassifiedFeatureVectorViewModel>();
        }
    }

    public class FeaturesViewModel : ViewModelBase
    {
        public ObservableCollection<FeatureByTypeViewModel> FeaturesByType { get; set; }

        public List<ClassifiedFeatureVectorViewModel> AllFeaturesView => FeaturesByType.SelectMany(l => l.Features).ToList();

        public static readonly string FeatureProperty = "Feature";

        public FeaturesViewModel(MainWindowViewModel mainWindowViewModel)
        {
            // Initialize with initial land cover types
            FeaturesByType = new ObservableCollection<FeatureByTypeViewModel>();
            foreach (LandcoverTypeViewModel landCoverType in mainWindowViewModel.LandcoverTypes.Values)
            {
                FeaturesByType.Add(new FeatureByTypeViewModel(landCoverType));
            }

            // Check if landcover types has been newly set
            var idComparer = new LandCoverTypeIdComparer();
            mainWindowViewModel.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == nameof(MainWindowViewModel.LandcoverTypes))
                {
                    foreach (LandcoverTypeViewModel landcoverTypeViewModel in mainWindowViewModel.LandcoverTypes.Values)
                    {
                        // Update land cover type
                        var featureByType = FeaturesByType.FirstOrDefault(f => f.LandCoverType.Id == landcoverTypeViewModel.Id);
                        if (featureByType != null) featureByType.LandCoverType = landcoverTypeViewModel;
                    }

                    // Remove deleted
                    var remove = FeaturesByType.Select(f => f.LandCoverType).Except(mainWindowViewModel.LandcoverTypes.Values, idComparer);
                    FeaturesByType.RemoveAll(f => remove.Contains(f.LandCoverType, idComparer));

                    // Add new
                    var add = mainWindowViewModel.LandcoverTypes.Values.Except(FeaturesByType.Select(f => f.LandCoverType), idComparer);
                    FeaturesByType.AddRange(add.Select(f => new FeatureByTypeViewModel(f)));
                }
            };
        }

        public ObservableCollection<ClassifiedFeatureVectorViewModel> GetFeaturesByType(LandcoverTypeViewModel typeViewModel)
        {
            return FeaturesByType.First(f => f.LandCoverType == typeViewModel).Features;
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
            FeaturesByType.First(f => f.LandCoverType == classifiedFeatureVector.FeatureTypeViewModel).Features.Add(classifiedFeatureVector);

            RaisePropertyChanged(FeatureProperty);
        }

        public void RemoveFeature(ClassifiedFeatureVectorViewModel classifiedFeatureVector)
        {
            FeaturesByType.First(f => f.LandCoverType == classifiedFeatureVector.FeatureTypeViewModel).Features.Remove(classifiedFeatureVector);

            RaisePropertyChanged(FeatureProperty);
        }

        class LandCoverTypeIdComparer : IEqualityComparer<LandcoverTypeViewModel>
        {
            public bool Equals(LandcoverTypeViewModel x, LandcoverTypeViewModel y)
            {
                return x.Id == y.Id;
            }

            public int GetHashCode(LandcoverTypeViewModel obj)
            {
                return obj.Id;
            }
        }
    }
}
