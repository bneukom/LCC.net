using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using LandscapeClassifier.Annotations;
using LandscapeClassifier.Model;
using MathNet.Numerics.LinearAlgebra;

namespace LandscapeClassifier.ViewModel.MainWindow.Classification
{
    public class LayerViewModel : INotifyPropertyChanged
    {
        private WriteableBitmap _bandImage;

        private int _bitmapImagePixelWidth;
        private int _bitmapImagePixelHeight;
        private bool _isVisible;
        private bool _isFeature = true;
        private bool _canChangeIsFeature = true;

        private Brush _currentPositionBrush;

        /// <summary>
        /// Transform from the image coordinate system to the layer coordinate system.
        /// </summary>
        public readonly Matrix<double> ImageToWorld;

        /// <summary>
        /// Transform from layer coordinate system to the image coordinate system.
        /// </summary>
        public readonly Matrix<double> WorldToImage;

        /// <summary>
        /// Layer name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Meters per pixel in dimension x of the band.
        /// </summary>
        public double ScaleX { get; }

        /// <summary>
        ///  Meters per pixel in dimension y of the band.
        /// </summary>
        public double ScaleY { get; }

        /// <summary>
        /// Current Color of the mouse position.
        /// </summary>
        public Brush CurrentPositionBrush
        {
            get { return _currentPositionBrush;  }
            set { _currentPositionBrush = value; OnPropertyChanged(nameof(CurrentPositionBrush)); }
        }

        /// <summary>
        /// Path to the file of this band.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Band upper left in world coordinates.
        /// </summary>
        public readonly Vector<double> UpperLeftWorld;

        /// <summary>
        /// Band bottom right in world coordinates.
        /// </summary>
        public readonly Vector<double> BottomRightWorld;

        /// <summary>
        /// Pixel width of the image.
        /// </summary>
        public double ImagePixelWidth => _bitmapImagePixelWidth;

        /// <summary>
        /// Pixel height of the image.
        /// </summary>
        public double ImagePixelHeight => _bitmapImagePixelHeight;

        /// <summary>
        /// Minimum histogram percentage for contrast enhancement lower bound.
        /// </summary>
        public double MinCutPercentage;

        /// <summary>
        /// Maximum histogram percentage for contrast enhancement upper bound.
        /// </summary>
        public double MaxCutPercentage;

        /// <summary>
        /// The format of this band.
        /// </summary>
        public PixelFormat Format => BandImage.Format;

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


        /// <summary>
        /// Whether this view model is enabled to be used as a feature.
        /// </summary>
        public bool CanChangeIsFeature
        {
            set {
                _canChangeIsFeature = value;
                OnPropertyChanged(nameof(CanChangeIsFeature));
            }
            get { return _canChangeIsFeature; }
        }

        /// <summary>
        /// True if this layer represents the red channel.
        /// </summary>
        public bool IsRed { get; }

        /// <summary>
        /// True if this layer represents the green channel.
        /// </summary>
        public bool IsGreen { get; }

        /// <summary>
        /// True if this layer represents the blue channel. 
        /// </summary>
        public bool IsBlue { get; }

        /// <summary>
        /// The satellite type of the layer.
        /// </summary>
        public SatelliteType SatelliteType { get; }

        /// <summary>
        /// Contrast was enhanced or not.
        /// </summary>
        public bool ContrastEnhanced { get; }

        public LayerViewModel(string name, SatelliteType satelliteType, string path, bool contrastEnhanced, double xScale, double yScale, 
            WriteableBitmap bandImage, Matrix<double> imageToWorld, Vector<double> upperLeftWorld, Vector<double> bottomRightWorld, 
            double minCutPercentage, double maxCutPercentage, bool isRed, bool isGreen, bool isBlue, bool isFeature = true, bool canChangeIsFeature = true)
        {
            Name = name;
            Path = path;
            SatelliteType = satelliteType;
            ContrastEnhanced = contrastEnhanced;

            MinCutPercentage = minCutPercentage;
            MaxCutPercentage = maxCutPercentage;

            ImageToWorld = imageToWorld;
            WorldToImage = imageToWorld.Inverse();
      
            BandImage = bandImage;
            UpperLeftWorld = upperLeftWorld;
            BottomRightWorld = bottomRightWorld;

            ScaleX = xScale;
            ScaleY = yScale;

            IsFeature = isFeature;
            CanChangeIsFeature = canChangeIsFeature;

            IsRed = isRed;
            IsGreen = isGreen;
            IsBlue = isBlue;
        }


        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));        
        }
    }
}
