using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using GalaSoft.MvvmLight.CommandWpf;
using LandscapeClassifier.Annotations;
using LandscapeClassifier.Classifier;
using LandscapeClassifier.Extensions;
using LandscapeClassifier.Model;
using LandscapeClassifier.Util;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace LandscapeClassifier.ViewModel
{
    public class ViewModel : INotifyPropertyChanged
    {
        private AscFile _ascFile;
        private WorldFile _worldFile;
        private BitmapImage _bitmapImage;
        private byte[] _imageData;
        private ClassifiedFeatureVector _selectedFeatureVector;
        private ILandCoverClassifier _classifier;
        private bool _isTrained;

        /// <summary>
        /// Export command.
        /// </summary>
        public ICommand ExportCommand { set; get; }

        /// <summary>
        /// Exit command.
        /// </summary>
        public ICommand ExitCommand { set; get; }

        /// <summary>
        /// Remove selected command.
        /// </summary>
        public ICommand RemoveSelectedFeatureVectorCommand { set; get; }

        /// <summary>
        /// Train command.
        /// </summary>
        public ICommand TrainCommand { set; get; }

        /// <summary>
        /// True if the classifier has been trained.
        /// </summary>
        public bool IsTrained
        {
            get { return _isTrained; }
            set
            {
                if (value != _isTrained)
                {
                    _isTrained = value;
                    NotifyPropertyChanged("IsTrained");
                }
            }
        }

        /// <summary>
        /// The currently selected feature vector.
        /// </summary>
        public ClassifiedFeatureVector SelectedFeatureVector
        {
            get { return _selectedFeatureVector;  }
            set
            {
                if (value != _selectedFeatureVector)
                {
                    _selectedFeatureVector = value;
                    NotifyPropertyChanged("SelectedFeatureVector");
                }
            }
        }

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
        public ObservableCollection<ClassifiedFeatureVector> Features { get; set; }

        /// <summary>
        /// Possible land cover types.
        /// </summary>
        public IEnumerable<string> LandCoverTypesEnumerable { get; set; }

        /// <summary>
        /// The current land cover type.
        /// </summary>
        public LandcoverType SelectedLandCoverType { get; set; }

        /// <summary>
        /// Possible modes.
        /// </summary>
        public IEnumerable<string> ModeTypesEnumerable { get; set; }

        /// <summary>
        /// The current mode.
        /// </summary>
        public Mode Mode { get; set; }



        public ViewModel()
        {
            _classifier = new BayesLandCoverClassifier();

            LandCoverTypesEnumerable = Enum.GetNames(typeof(LandcoverType));
            ModeTypesEnumerable = Enum.GetNames(typeof(Mode));

            Features = new ObservableCollection<ClassifiedFeatureVector>();

            ExitCommand = new RelayCommand(() => Application.Current.Shutdown(), () => true);
            ExportCommand = new RelayCommand(Export, CanExport);
            RemoveSelectedFeatureVectorCommand = new RelayCommand(RemoveSelectedFeature, CanRemoveSelectedFeature);
            TrainCommand = new RelayCommand(Train, CanTrain);
        }

        /// <summary>
        /// Predicts the land cover type with the given feature vector.
        /// </summary>
        /// <param name="feature"></param>
        /// <returns></returns>
        public LandcoverType Predict(FeatureVector feature)
        {
            return _classifier.Predict(feature);
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

            // @TODO http://stackoverflow.com/questions/3745824/loading-image-into-imagesource-incorrect-width-and-height
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

        /// <summary>
        /// Returns the slope at the given position or 0 if not available.
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public AspectSlope GetSlopeAndAspectAt(Point position)
        {
            var left = (_ascFile.Xllcorner - _worldFile.X) / _worldFile.PixelSizeX;

            var topWorldCoordinates = _ascFile.Yllcorner +
                                      _ascFile.Cellsize * _ascFile.Nrows;

            var topScreenCoordinates = (topWorldCoordinates - _worldFile.Y) /
                                       _worldFile.PixelSizeY;

            int positionX = (int) (position.X - left);
            int positionY = (int) (position.Y - topScreenCoordinates);

            if (positionX - 1 < 0 || positionX + 1 >= _ascFile.Ncols || positionY - 1 < 0 ||
                positionY + 1 >= _ascFile.Nrows)
            {
                return new AspectSlope(0, 0);
            }

            float Z1 = _ascFile.Data[positionY - 1, positionX - 1];
            float Z2 = _ascFile.Data[positionY - 1, positionX];
            float Z3 = _ascFile.Data[positionY - 1, positionX + 1];

            float Z4 = _ascFile.Data[positionY, positionX - 1];
            float Z5 = _ascFile.Data[positionY, positionX];
            float Z6 = _ascFile.Data[positionY, positionX + 1];

            float Z7 = _ascFile.Data[positionY + 1, positionX - 1];
            float Z8 = _ascFile.Data[positionY + 1, positionX];
            float Z9 = _ascFile.Data[positionY + 1, positionX + 1];


            float b = (Z3 + 2*Z6 + Z9 - Z1 - 2*Z4 - Z7)/(8*_ascFile.Cellsize);
            float c = (Z1 + 2 * Z2 + Z3 - Z7 - 2 * Z8 - Z9) / (8 * _ascFile.Cellsize);

            float slope = (float)Math.Atan(Math.Sqrt(b*b + c*c));
            double aspect;

            if (MoreMath.AlmostZero(c))
            {
                aspect = 0;
            }
            else
            {
                aspect = (float)Math.Atan(b / c);

                if (c > 0)
                {
                    aspect += Math.PI;
                }
                else if (c < 0 && b > 0)
                {
                    aspect += (2 * Math.PI);
                }
            }

            return new AspectSlope(slope, (float)aspect);
        }

        private void Export()
        {
            var chooseFolderDialog = new CommonOpenFileDialog
            {
                Title = "Choose Training Data Folder",
                IsFolderPicker = true,
                AddToMostRecentlyUsedList = false,
                AllowNonFileSystemItems = false,
                EnsureFileExists = true,
                EnsurePathExists = true,
                EnsureReadOnly = false,
                EnsureValidNames = true,
                Multiselect = false,
                ShowPlacesList = true
            };

            if (chooseFolderDialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                var folder = chooseFolderDialog.FileName;

                // Write CSV Path
                var csvPath = Path.Combine(folder, Path.ChangeExtension(_worldFile.FileName, ".csv"));
                using (var outputStreamWriter = new StreamWriter(csvPath))
                {
                    foreach (var feature in Features)
                    {
                        outputStreamWriter.WriteLine(feature.Type + ";" + feature.FeatureVector.Altitude + ";" + feature.FeatureVector.Aspect + ";" + feature.FeatureVector.Slope + ";" + feature.FeatureVector.Luma);
                    }
                }

                // Copy ASC file
                // File.Copy();

            }
        }

        private void Train()
        {
            _classifier.Train(Features.ToList());
            IsTrained = true;
        }

        private bool CanTrain()
        {
            return Features.Count > 0 && Features.GroupBy(f => f.Type).Count() >= 2;
        }

        private bool CanExport()
        {
            return _ascFile != null && _bitmapImage != null;
        }

        private void RemoveSelectedFeature()
        {
            Features.Remove(SelectedFeatureVector);
        }

        private bool CanRemoveSelectedFeature()
        {
            return SelectedFeatureVector != null;
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