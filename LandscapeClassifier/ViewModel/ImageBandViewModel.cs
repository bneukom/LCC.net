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
using LandscapeClassifier.Model;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;

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
        private bool _isActive;
        private Point _mouseScreenPoisition;
        private Point _mouseWorldPoisition;

        /// <summary>
        /// The projection of the band image.
        /// </summary>
        public readonly string ProjectionName;

        /// <summary>
        /// Conversion from screen to world coordinates.
        /// </summary>
        public readonly Matrix<double> ScreenToWorld;

        /// <summary>
        /// Conversion from world to screen coordinates.
        /// </summary>
        public readonly Matrix<double> WorldToScreen;

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

        /// <summary>
        /// Mouse screen position.
        /// </summary>
        public Point MouseScreenPoisition
        {
            get { return _mouseScreenPoisition;  }
            set { _mouseScreenPoisition = value; OnPropertyChanged(nameof(MouseScreenPoisition)); }
        }

        /// <summary>
        /// Mouse world position.
        /// </summary>
        public Point MouseWorldPoisition
        {
            get { return _mouseWorldPoisition; }
            set { _mouseWorldPoisition = value; OnPropertyChanged(nameof(MouseWorldPoisition)); }
        }

        public ImageBandViewModel(string title, BitmapSource bandImage, string projectionName, Matrix<double> screenToWorld, Vector<double> upperLeft, Vector<double> bottomRight)
        {
            BandImage = bandImage;
            ProjectionName = projectionName;
            ScreenToWorld = screenToWorld;
            UpperLeft = upperLeft;
            BottomRight = bottomRight;

            WorldToScreen = ScreenToWorld.Inverse();
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
