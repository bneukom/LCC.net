using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using LandscapeClassifier.Annotations;

namespace LandscapeClassifier.ViewModel
{
    public class ImageBandViewModel : INotifyPropertyChanged
    {
        private BitmapSource _bandImage;
        private double _bitmapImageWidth;
        private double _bitmapImageHeight;
        private int _bitmapImagePixelWidth;
        private int _bitmapImagePixelHeight;
        private int _bitmapImageBitsPerPixel;
        private byte[] _imageData;
        private bool _isActive;

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
        public BitmapSource BandImage
        {
            get { return _bandImage; }
            set
            {
                if (value != _bandImage)
                {
                    _bandImage = value;
                    _bitmapImageWidth = _bandImage.Width;
                    _bitmapImageHeight = _bandImage.Height;
                    _bitmapImagePixelWidth = _bandImage.PixelWidth;
                    _bitmapImagePixelHeight = _bandImage.PixelHeight;
                    _bitmapImageBitsPerPixel = _bandImage.Format.BitsPerPixel;

                    var stride = (int)_bandImage.PixelWidth * (_bandImage.Format.BitsPerPixel / 8);
                    _imageData = new byte[(int)_bandImage.PixelHeight * stride];

                    // @TODO waste of memory!
                    _bandImage.CopyPixels(_imageData, stride, 0);

                    OnPropertyChanged(nameof(BandImage));
                }
            }
        }

        /// <summary>
        /// Title of the tab.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Whether this band is active or not.
        /// </summary>
        public bool IsActive {
            get { return _isActive;}
            set { _isActive = value; OnPropertyChanged(nameof(IsActive)); }
        }
        

        public ImageBandViewModel(string title, BitmapSource bandImage)
        {
            BandImage = bandImage;
            Title = title;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));        
        }
    }
}
