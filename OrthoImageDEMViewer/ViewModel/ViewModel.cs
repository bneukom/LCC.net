using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using LandscapeClassifier.Annotations;
using LandscapeClassifier.Model;

namespace LandscapeClassifier.ViewModel
{
    public class ViewModel : INotifyPropertyChanged
    {
        private AscFile _ascFile;
        private WorldFile _worldFile;
        private BitmapImage _bitmapImage;
        private byte[] _imageData;

        /// <summary>
        /// World file to the ortho image.
        /// </summary>
        public WorldFile WorldFile
        {
            get { return _worldFile; }
            set
            {
                if (value != _worldFile)
                {
                    _worldFile = value;
                    NotifyPropertyChanged("WorldFile");
                }
            }
        }

        /// <summary>
        /// ASC file which represents the DEM.
        /// </summary>
        public AscFile AscFile
        {
            get { return _ascFile; }
            set
            {
                if (value != _ascFile)
                {
                    _ascFile = value;
                    NotifyPropertyChanged("AscFile");
                }
            }
        }

        /// <summary>
        /// The OrthoImage map.
        /// </summary>
        public BitmapImage OrthoImage
        {
            get { return _bitmapImage; }
            set
            {
                if (value != _bitmapImage)
                {
                    _bitmapImage = value;

                    var stride = (int) _bitmapImage.PixelWidth*(_bitmapImage.Format.BitsPerPixel/8);
                    _imageData = new byte[(int) _bitmapImage.PixelHeight*stride];

                    _bitmapImage.CopyPixels(_imageData, stride, 0);

                    NotifyPropertyChanged("OrthoImage");
                }
            }
        }

        /// <summary>
        /// Feature vectors.
        /// </summary>
        public ObservableCollection<FeatureVector> Features { get; set; }

        /// <summary>
        /// Possible land cover types.
        /// </summary>
        public IEnumerable<string> LandCoverTypesEnumerable { get; set; }

        /// <summary>
        /// The current land cover type.
        /// </summary>
        public LandcoverType SelectedLandCoverType { get; set; }

        public ViewModel()
        {
            LandCoverTypesEnumerable = Enum.GetNames(typeof(LandcoverType));
            Features = new ObservableCollection<FeatureVector>();
        }

        /// <summary>
        /// Returns whether the given position is within the bounds of the ASC file.
        /// </summary>
        /// <param name="position">Pixel position in the </param>
        /// <returns></returns>
        public bool IsInAscBounds(Point position)
        {
            if (_ascFile == null) throw new InvalidOperationException();

            // Position in LV95 coordinate system
            var lv95X = (int) (position.X*WorldFile.PixelSizeX + WorldFile.X);
            var lv95Y = (int) (position.Y*WorldFile.PixelSizeY + WorldFile.Y);

            return lv95X > _ascFile.Xllcorner && lv95X < _ascFile.Xllcorner + _ascFile.Ncols*_ascFile.Cellsize &&
                   lv95Y > _ascFile.Yllcorner && lv95Y < _ascFile.Yllcorner + _ascFile.Nrows*_ascFile.Cellsize;
        }

        /// <summary>
        /// Returns the data from the ASC file at the given position.
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public short GetAscDataAt(Point position)
        {
            if (_ascFile == null || _worldFile == null) throw new InvalidOperationException();

            // Position in LV95 coordinate system
            var lv95X = (int) (position.X*_worldFile.PixelSizeX + _worldFile.X);
            var lv95Y = (int) (position.Y*_worldFile.PixelSizeY + _worldFile.Y);

            if (lv95X > _ascFile.Xllcorner && lv95X < _ascFile.Xllcorner + _ascFile.Ncols*_ascFile.Cellsize &&
                lv95Y > _ascFile.Yllcorner && lv95Y < _ascFile.Yllcorner + _ascFile.Nrows*_ascFile.Cellsize)
            {
                var indexX = (int) ((lv95X - _ascFile.Xllcorner)/_ascFile.Cellsize);
                var indexY = (int) ((lv95Y - _ascFile.Yllcorner)/_ascFile.Cellsize);
                return _ascFile.Data[_ascFile.Nrows - indexY - 1, indexX];
            }
            else
            {
                return _ascFile.NoDataValue;
            }
        }

        /// <summary>
        /// Returns the color at the given pixel position.
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public Color GetColorAt(Point position)
        {
            if (_bitmapImage == null) throw new InvalidOperationException();

            if (position.Y < 0 || position.Y < 0 || position.X >= _bitmapImage.Width ||
                position.Y >= _bitmapImage.Height)
                return Colors.Black;

            var stride = (int) _bitmapImage.PixelWidth*(_bitmapImage.Format.BitsPerPixel/8);
            var index = (int) position.Y*stride + _bitmapImage.Format.BitsPerPixel/8*(int) position.X;

            var B = _imageData[index];
            var G = _imageData[index + 1];
            var R = _imageData[index + 2];
            var A = _imageData[index + 3];

            return Color.FromArgb(A, R, G, B);
        }

        /// <summary>
        /// Returns the luminance at the given pixel position.
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public float GetLuminance(Point position)
        {
            var color = GetColorAt(position);
            return 0.2126f*color.R + 0.7152f*color.G + 0.0722f*color.B;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void NotifyPropertyChanged(string info)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(info));
        }
    }
}