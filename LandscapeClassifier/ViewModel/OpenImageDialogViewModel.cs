using LandscapeClassifier.Annotations;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace LandscapeClassifier.ViewModel
{
    public class OpenImageDialogViewModel : INotifyPropertyChanged
    {
        private bool _addRgb = true;
        private bool _rgbContrastEnhancement = true;
        private bool _bandContrastEnhancement = false;

        /// <summary>
        /// Bands
        /// </summary>
        public ObservableCollection<BandInfo> Bands { get; set; }

        /// <summary>
        /// Whether to use contrast enhancement or not.
        /// </summary>
        public bool RgbContrastEnhancement
        {
            get { return _rgbContrastEnhancement; }
            set { _rgbContrastEnhancement = value; OnPropertyChanged(nameof(RgbContrastEnhancement)); }
        }

        /// <summary>
        /// 
        /// </summary>
        public bool AddRgb
        {
            get { return _addRgb; }
            set { _addRgb = value; OnPropertyChanged(nameof(AddRgb)); }
        }

        /// <summary>
        /// 
        /// </summary>
        public bool BandContrastEnhancement
        {
            get { return _bandContrastEnhancement; }
            set { _bandContrastEnhancement = value; OnPropertyChanged(nameof(BandContrastEnhancement)); }
        }

        public OpenImageDialogViewModel()
        {
            Bands = new ObservableCollection<BandInfo>();

            Bands.CollectionChanged += (sender, args) =>
            {
                // Remove change listener on old items
                if (args.OldItems != null)
                {
                    foreach (var oldItem in args.OldItems)
                    {
                        BandInfo bandInfo = (BandInfo) oldItem;
                        bandInfo.PropertyChanged -= BandOnPropertyChanged;
                    }
                }

                // Add new listeners
                if (args.NewItems != null)
                { 
                    foreach (var newItem in args.NewItems)
                    {
                        BandInfo bandInfo = (BandInfo) newItem;
                        bandInfo.PropertyChanged += BandOnPropertyChanged;
                    }
                }
            };
        }

        private void BandOnPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            foreach (BandInfo band in Bands)
            {
                band.PropertyChanged -= BandOnPropertyChanged;

                if (band == sender)
                {
                    band.R = propertyChangedEventArgs.PropertyName == nameof(BandInfo.R);
                    band.G = propertyChangedEventArgs.PropertyName == nameof(BandInfo.G);
                    band.B = propertyChangedEventArgs.PropertyName == nameof(BandInfo.B);
                }
                else
                { 
                    switch (propertyChangedEventArgs.PropertyName)
                    {
                        case nameof(BandInfo.R):
                            band.R = false;
                            break;
                        case nameof(BandInfo.G):
                            band.G = false;
                            break;
                        case nameof(BandInfo.B):
                            band.B = false;
                            break;
                    }
                }
                band.PropertyChanged += BandOnPropertyChanged;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class BandInfo : INotifyPropertyChanged
    {
        public string Path { get; set; }

        private bool _r;
        public bool R
        {
            get { return _r; }
            set { _r = value; OnPropertyChanged(nameof(R)); }
        }

        private bool _g;
        public bool G
        {
            get { return _g; }
            set { _g = value; OnPropertyChanged(nameof(G)); }
        }

        private bool _b;
        public bool B
        {
            get { return _b; }
            set { _b = value; OnPropertyChanged(nameof(B)); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public BandInfo(string path, bool r, bool g, bool b)
        {
            Path = path;
            R = r;
            G = g;
            B = b;
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


    }
}
