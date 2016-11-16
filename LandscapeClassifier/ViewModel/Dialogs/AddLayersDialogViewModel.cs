using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
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

        public AddLayersDialogViewModel()
        {
            Bands = new ObservableCollection<BandInfoViewModel>();

            Bands.CollectionChanged += (sender, args) =>
            {
                // Remove change listener on old items
                if (args.OldItems != null)
                {
                    foreach (var oldItem in args.OldItems)
                    {
                        var bandInfo = (BandInfoViewModel) oldItem;
                        bandInfo.PropertyChanged -= BandOnPropertyChanged;
                    }
                }

                // Add new listeners
                if (args.NewItems != null)
                {
                    foreach (var newItem in args.NewItems)
                    {
                        var bandInfo = (BandInfoViewModel) newItem;
                        bandInfo.PropertyChanged += BandOnPropertyChanged;
                    }
                }
            };
        }


        /// <summary>
        ///     Bands
        /// </summary>
        public ObservableCollection<BandInfoViewModel> Bands { get; set; }

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
            get { return _addRgb; }
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
            foreach (var band in Bands)
            {
                band.PropertyChanged -= BandOnPropertyChanged;

                if (band == sender)
                {
                    band.R = propertyChangedEventArgs.PropertyName == nameof(BandInfoViewModel.R);
                    band.G = propertyChangedEventArgs.PropertyName == nameof(BandInfoViewModel.G);
                    band.B = propertyChangedEventArgs.PropertyName == nameof(BandInfoViewModel.B);
                }
                else
                {
                    switch (propertyChangedEventArgs.PropertyName)
                    {
                        case nameof(BandInfoViewModel.R):
                            band.R = false;
                            break;
                        case nameof(BandInfoViewModel.G):
                            band.G = false;
                            break;
                        case nameof(BandInfoViewModel.B):
                            band.B = false;
                            break;
                    }
                }
                band.PropertyChanged += BandOnPropertyChanged;
            }
        }

        /// <summary>
        ///     Resets this viewmodel to its default values.
        /// </summary>
        public void Reset()
        {
            Bands.Clear();
            AddRgb = true;
            RgbContrastEnhancement = true;
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class BandInfoViewModel : ViewModelBase
    {
        private bool _b;
        private bool _g;
        private bool _r;

        private SatelliteType _satelliteType;
        private bool _contrastEnhancement;
        private int _minCutOff;
        private int _maxCutOff;

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

        public int MinCutOff
        {
            get { return _minCutOff; }
            set { _minCutOff = value; RaisePropertyChanged(); }
        }

        public int MaxCutOff
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

        public BandInfoViewModel(string path)
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
                MinCutOff = 2;
                MaxCutOff = 98;
            }
            else
            {
                MinCutOff = 0;
                MaxCutOff = 100;
            }
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