using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using GalaSoft.MvvmLight;
using LandscapeClassifier.Annotations;
using LandscapeClassifier.Model;

namespace LandscapeClassifier.ViewModel.Dialogs
{
    public class AddLayersDialogViewModel : INotifyPropertyChanged
    {
        private bool _addRgb = true;
        private bool _missingBandsImport;
        private bool _rgbContrastEnhancement = true;
        private bool _allRgbChannelsAdded;

        public AddLayersDialogViewModel()
        {
            Layers = new ObservableCollection<CreateLayerViewModel>();

            Layers.CollectionChanged += (sender, args) =>
            {
                // Remove change listener on old items
                if (args.OldItems != null)
                {
                    foreach (var oldItem in args.OldItems)
                    {
                        var bandInfo = (CreateLayerViewModel) oldItem;
                        bandInfo.PropertyChanged -= BandOnPropertyChanged;
                    }
                }

                // Add new listeners
                if (args.NewItems != null)
                {
                    foreach (var newItem in args.NewItems)
                    {
                        var bandInfo = (CreateLayerViewModel) newItem;
                        bandInfo.PropertyChanged += BandOnPropertyChanged;
                    }
                }

                AllRgbChannelsAdded = Layers.Any(b => b.R) && Layers.Any(b => b.G) && Layers.Any(b => b.B);
            };
        }


        /// <summary>
        ///     Layers
        /// </summary>
        public ObservableCollection<CreateLayerViewModel> Layers { get; set; }

        /// <summary>
        ///     Wheter all rgb channels are added or not
        /// </summary>
        public bool AllRgbChannelsAdded
        {
            get { return _allRgbChannelsAdded; }
            set { _allRgbChannelsAdded = value; OnPropertyChanged(nameof(AllRgbChannelsAdded)); }
        }

        /// <summary>
        ///     Whether to use contrast enhancement or not.
        /// </summary>
        public bool RgbContrastEnhancement
        {
            get { return _rgbContrastEnhancement; }
            set
            {
                _rgbContrastEnhancement = value;
                OnPropertyChanged(nameof(RgbContrastEnhancement));
            }
        }

        /// <summary>
        /// </summary>
        public bool AddRgb
        {
            get { return _addRgb && AllRgbChannelsAdded; }
            set
            {
                _addRgb = value;
                OnPropertyChanged(nameof(AddRgb));
            }
        }

        /// <summary>
        ///     Import missing bands.
        /// </summary>
        public bool MissingBandsImport
        {
            get { return _missingBandsImport; }
            set
            {
                _missingBandsImport = value;
                OnPropertyChanged(nameof(MissingBandsImport));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void BandOnPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            if (propertyChangedEventArgs.PropertyName != nameof(CreateLayerViewModel.R) &&
                propertyChangedEventArgs.PropertyName != nameof(CreateLayerViewModel.G) &&
                propertyChangedEventArgs.PropertyName != nameof(CreateLayerViewModel.B)) return;

            foreach (var band in Layers)
            {
                band.PropertyChanged -= BandOnPropertyChanged;

                if (band == sender)
                {
                    band.R = propertyChangedEventArgs.PropertyName == nameof(CreateLayerViewModel.R);
                    band.G = propertyChangedEventArgs.PropertyName == nameof(CreateLayerViewModel.G);
                    band.B = propertyChangedEventArgs.PropertyName == nameof(CreateLayerViewModel.B);
                }
                else
                {
                    switch (propertyChangedEventArgs.PropertyName)
                    {
                        case nameof(CreateLayerViewModel.R):
                            band.R = false;
                            break;
                        case nameof(CreateLayerViewModel.G):
                            band.G = false;
                            break;
                        case nameof(CreateLayerViewModel.B):
                            band.B = false;
                            break;
                    }
                }
                band.PropertyChanged += BandOnPropertyChanged;
            }

            AllRgbChannelsAdded = Layers.Any(b => b.R) && Layers.Any(b => b.G) && Layers.Any(b => b.B);
        }

        /// <summary>
        ///     Resets this viewmodel to its default values.
        /// </summary>
        public void Reset()
        {
            Layers.Clear();
            AddRgb = false;
            RgbContrastEnhancement = false;
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class CreateLayerViewModel : ViewModelBase
    {
        private bool _b;
        private bool _g;
        private bool _r;

        private SatelliteType _satelliteType;
        private bool _contrastEnhancement;
        private double _minCutOff;
        private double _maxCutOff;

        public string Path { get; }

        public bool R
        {
            get { return _r; }
            set
            {
                _r = value;
                RaisePropertyChanged();
            }
        }

        public bool G
        {
            get { return _g; }
            set
            {
                _g = value;
                RaisePropertyChanged();
            }
        }


        public bool B
        {
            get { return _b; }
            set
            {
                _b = value;
                RaisePropertyChanged();
            }
        }

        public double MinCutOff
        {
            get { return _minCutOff; }
            set { _minCutOff = value; RaisePropertyChanged(); }
        }

        public double MaxCutOff
        {
            get { return _maxCutOff; }
            set { _maxCutOff = value; RaisePropertyChanged(); }
        }

        public SatelliteType SatelliteType
        {
            get { return _satelliteType; }
            set
            {
                _satelliteType = value;
                RaisePropertyChanged();
            }
        }

        public bool ContrastEnhancement
        {
            get { return _contrastEnhancement; }
            set
            {
                _contrastEnhancement = value;
                RaisePropertyChanged();
            }
        }

        public CreateLayerViewModel(string path)
        {
            Path = path;
            ContrastEnhancement = true;

            SatelliteType = GetSatelliteType(path);
            if (SatelliteType != SatelliteType.None)
            {
                var fileWithoutExtension = System.IO.Path.GetFileNameWithoutExtension(path);
                R = SatelliteType.IsRedBand(fileWithoutExtension);
                B = SatelliteType.IsBlueBand(fileWithoutExtension);
                G = SatelliteType.IsGreenBand(fileWithoutExtension);
                MinCutOff = 0.02;
                MaxCutOff = 0.98;
            }
            else
            {
                MinCutOff = 0;
                MaxCutOff = 1;
            }
        }

        public CreateLayerViewModel(string path, bool contrastEnhancement, SatelliteType satelliteType, bool r, bool g,
            bool b, double minCutOff, double maxCutOff)
        {
            Path = path;
            ContrastEnhancement = contrastEnhancement;
            SatelliteType = satelliteType;
            R = r;
            G = g;
            B = b;
            MinCutOff = minCutOff;
            MaxCutOff = maxCutOff;
        }

        private static SatelliteType GetSatelliteType(string path)
        {
            var satelliteTypes = Enum.GetValues(typeof(SatelliteType));
            foreach (SatelliteType satelliteType in satelliteTypes)
            {
                if (satelliteType.MatchesFile(System.IO.Path.GetFileNameWithoutExtension(path)))
                {
                    return satelliteType;
                }
            }

            return SatelliteType.None;
        }

        public string GetName()
        {
            var fileName = System.IO.Path.GetFileNameWithoutExtension(Path);
            if (SatelliteType != SatelliteType.None)
            {
                return SatelliteType.GetBand(fileName);
            }
            else
            {
                return fileName;
            }
        }
    }
}