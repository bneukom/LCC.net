using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;
using Accord.Collections;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using GalaSoft.MvvmLight.Ioc;
using LandscapeClassifier.Extensions;
using LandscapeClassifier.Model;
using LandscapeClassifier.View.Export;
using LandscapeClassifier.ViewModel.MainWindow;
using LandscapeClassifier.ViewModel.MainWindow.Classification;
using OSGeo.GDAL;

namespace LandscapeClassifier.ViewModel.Dialogs
{
    public class ExportPredictionDialogViewModel : ViewModelBase
    {
        private bool _exportHeightmap;
        private ExportLandCoverTypeViewModel _selectedLayer;
        private string _exportPath;
        private bool _isExportPathSet;
        private bool _exportAsProbabilities;
        private double _minAcceptanceProbability;
        private bool _scaleToUnrealLandscape;
        private object _canExportAsProbabilities;

        public string ExportPath
        {
            get { return _exportPath; }
            set
            {
                _exportPath = value;
                IsExportPathSet = !string.IsNullOrWhiteSpace(value);
                RaisePropertyChanged();
            }
        }

        public bool ScaleToUnrealLandscape
        {
            get { return _scaleToUnrealLandscape; }
            set { _scaleToUnrealLandscape = value; RaisePropertyChanged(); }
        }

        public object CanExportAsProbabilities
        {
            get { return _canExportAsProbabilities; }
            set { _canExportAsProbabilities = value; RaisePropertyChanged(); }
        }

        public double MinAcceptanceProbability
        {
            get { return _minAcceptanceProbability; }
            set { _minAcceptanceProbability = value; RaisePropertyChanged(); }
        }

        public bool ExportAsProbabilities
        {
            get { return _exportAsProbabilities; }
            set { _exportAsProbabilities = value; RaisePropertyChanged(); }
        }

        public bool ExportHeightmap
        {
            get { return _exportHeightmap; }
            set { _exportHeightmap = value; RaisePropertyChanged(); }
        }

        public bool IsExportPathSet
        {
            get { return _isExportPathSet; }
            set { _isExportPathSet = value; RaisePropertyChanged(); }
        }

        public ObservableCollection<LayerViewModel> Layers { get; } = new ObservableCollection<LayerViewModel>();

        public ExportLandCoverTypeViewModel SelectedLayer
        {
            get { return _selectedLayer; }
            set { _selectedLayer = value; RaisePropertyChanged(); }
        }

        public ObservableCollection<ExportLandCoverTypeViewModel> ExportLandCoverLayers { get; set; } = new ObservableCollection<ExportLandCoverTypeViewModel>();
        public ObservableCollection<ExportLayerViewModel> ExportLayers { get; set; } = new ObservableCollection<ExportLayerViewModel>();

        public ICommand AddLayerCommand { get; set; }
        public ICommand RemoveLayerCommand { get; set; }
        public ICommand BrowseExportPathCommand { get; set; }
        public ICommand BrowseHeightmapLayerCommand { get; set; }

        public static ObservableCollection<System.Windows.Media.PixelFormat> PixelFormats { get; set; } = new ObservableCollection<System.Windows.Media.PixelFormat>();

        static ExportPredictionDialogViewModel()
        {
            PixelFormats.Add(System.Windows.Media.PixelFormats.Gray16);
            PixelFormats.Add(System.Windows.Media.PixelFormats.Pbgra32);
        }

        public ExportPredictionDialogViewModel()
        {
            AddLayerCommand = new RelayCommand(AddLayer);
            RemoveLayerCommand = new RelayCommand(RemoveLayer, CanRemoveLayer);
            BrowseExportPathCommand = new RelayCommand(BrowseExportPath);
        }

        private void BrowseExportPath()
        {
            // Create an instance of the open file dialog box.
            var openFileDialog = new FolderBrowserDialog();

            // Call the ShowDialog method to show the dialog box.
            var userClickedOk = openFileDialog.ShowDialog();

            // Process input if the user clicked OK.
            if (userClickedOk == DialogResult.OK)
            {
                ExportPath = openFileDialog.SelectedPath;
            }
        }

        private bool CanRemoveLayer()
        {
            return SelectedLayer != null;
        }

        private void RemoveLayer()
        {
            ExportLandCoverLayers.Remove(SelectedLayer);
        }

        private void AddLayer()
        {
            int missingLayer = MissingLayerIndex(ExportLandCoverLayers);
            ExportLandCoverLayers.Add(new ExportLandCoverTypeViewModel("layer" + missingLayer + ".png", ExportLandCoverLayers));
        }

        public static int MissingLayerIndex(ObservableCollection<ExportLandCoverTypeViewModel> layers)
        {
            var layerNumbers = layers.Where(l =>
               Regex.IsMatch(l.Name, "layer[0-9]+.png")).
               Select(l => int.Parse(Regex.Match(l.Name, "layer([0-9]+).png").Groups[1].Value)).ToList();

            return layerNumbers.Count == 0 ? 0 : Enumerable.Range(layerNumbers.Min(), layerNumbers.Count + 1).Except(layerNumbers).FirstOrDefault();
        }

        public void Initialize(bool canExportAsProbabilities, List<LayerViewModel> layers)
        {
            ExportHeightmap = false;
            Layers.Clear();
            Layers.AddRange(layers);
            CanExportAsProbabilities = canExportAsProbabilities;
            ExportLayers.Clear();
            ExportLayers.AddRange(layers.Select(l => new ExportLayerViewModel(l)));

            ExportLandCoverLayers.Clear();

            var types = MainWindowViewModel.Default.LandcoverTypes.Values.ToList();
            for (int i = 0; i < types.Count; ++i)
            {
                var type = types[i];
                if (type == LandcoverTypeViewModel.None) continue;

                var layer = new ExportLandCoverTypeViewModel(type + ".png", ExportLandCoverLayers);

                layer.ExportedLandCoverTypes[i] = true;
                ExportLandCoverLayers.Add(layer);
            }
        }
    }

    public class ExportLayerViewModel : ViewModelBase
    {
        private bool _export;
        private bool _isHeightmap;
        private System.Windows.Media.PixelFormat _format;

        public System.Windows.Media.PixelFormat Format
        {
            get { return _format; }
            set { _format = value; RaisePropertyChanged(); }
        }

        public bool Export
        {
            get { return _export; }
            set { _export = value; RaisePropertyChanged(); }
        }

        public LayerViewModel Layer { get; }

        public bool IsHeightmap
        {
            get { return _isHeightmap; }
            set { _isHeightmap = value; RaisePropertyChanged(); }
        }


        public ExportLayerViewModel(LayerViewModel layer)
        {
            Layer = layer;
        }
    }

    public class ExportLandCoverTypeViewModel : ViewModelBase
    {
        private readonly ObservableCollection<ExportLandCoverTypeViewModel> _existingLayers;

        private string _name;
        private bool[] _exportedLandCoverTypes;

        public string Name
        {
            get { return _name; }
            set { _name = value; RaisePropertyChanged(); }
        }

        public ObservableCollection<bool> ExportedLandCoverTypes { get; }

        public int[] SelectedTypeIndices
        {
            get { return ExportedLandCoverTypes.Where(f => f).Select((b, i) => i).ToArray(); }
        }

        public bool[] LandCoverTypes => ExportedLandCoverTypes.ToArray();

        public ExportLandCoverTypeViewModel(string name, ObservableCollection<ExportLandCoverTypeViewModel> existingLayers)
        {
            _existingLayers = existingLayers;
            Name = name;
            ExportedLandCoverTypes = new ObservableCollection<bool>(Enumerable.Repeat(false, MainWindowViewModel.Default.LandcoverTypes.Values.Count()));
            
            PropertyChanged += OnPropertyChanged;
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            if (propertyChangedEventArgs.PropertyName != nameof(Name))
            {
                string newName = "";

                var types = MainWindowViewModel.Default.LandcoverTypes.Values.ToList();
                for (int i = 0; i < types.Count; ++i)
                {
                    if (ExportedLandCoverTypes[i])
                    {
                        newName += types[i].Name;
                    }
                }

                if (newName.Length == 0)
                {
                    int missingLayer = ExportPredictionDialogViewModel.MissingLayerIndex(_existingLayers);
                    newName = "layer" + missingLayer;
                }

                newName += ".png";
                Name = newName;
            }
        }
    }
}
