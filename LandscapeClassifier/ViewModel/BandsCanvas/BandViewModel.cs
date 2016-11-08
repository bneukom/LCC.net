using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media.Imaging;
using LandscapeClassifier.Annotations;
using MathNet.Numerics.LinearAlgebra;

namespace LandscapeClassifier.ViewModel.BandsCanvas
{
    public class BandViewModel : INotifyPropertyChanged
    {
        private WriteableBitmap _bandImage;

        private int _bitmapImagePixelWidth;
        private int _bitmapImagePixelHeight;
        private bool _isVisible;
        private bool _isFeature = true;

        /// <summary>
        /// Index of the band.
        /// </summary>
        public int BandNumber { get; }

        /// <summary>
        /// Meters per pixel of the band.
        /// </summary>
        public double MetersPerPixel { get; }

        /// <summary>
        /// Title of the tab.
        /// </summary>
        public string BandName { get; set; }

        /// <summary>
        /// Band upper left in world coordinates.
        /// </summary>
        public readonly Vector<double> UpperLeft;

        /// <summary>
        /// Band bottom right in world coordinates.
        /// </summary>
        public readonly Vector<double> BottomRight;

        /// <summary>
        /// Pixel width of the image.
        /// </summary>
        public double ImagePixelWidth => _bitmapImagePixelWidth;

        /// <summary>
        /// Pixel height of the image.
        /// </summary>
        public double ImagePixelHeight => _bitmapImagePixelHeight;

        /// <summary>
        /// The band image.
        /// </summary>
        public WriteableBitmap BandImage
        {
            get { return _bandImage; }
            set
            {
                if (value != _bandImage)
                {
                    _bandImage = value;
                    _bitmapImagePixelWidth = _bandImage.PixelWidth;
                    _bitmapImagePixelHeight = _bandImage.PixelHeight;

                    OnPropertyChanged(nameof(BandImage));
                }
            }
        }


        /// <summary>
        /// Whether this band is active or not.
        /// </summary>
        public bool IsVisible {
            get { return _isVisible;}
            set { _isVisible = value; OnPropertyChanged(nameof(IsVisible)); }
        }

        /// <summary>
        /// Whether this band is used as a feature in the classification or not.
        /// </summary>
        public bool IsFeature
        {
            get { return _isFeature; }
            set { _isFeature = value; OnPropertyChanged(nameof(IsFeature)); }
        }

        public BandViewModel(string bandName, int bandNumber, double metersPerPixel, WriteableBitmap bandImage, Vector<double> upperLeft, Vector<double> bottomRight, bool isFeature = true)
        {
            BandNumber = bandNumber;
            BandImage = bandImage;
            UpperLeft = upperLeft;
            BottomRight = bottomRight;

            BandName = bandName;
            MetersPerPixel = metersPerPixel;

            IsFeature = isFeature;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));        
        }
    }
}
