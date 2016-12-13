using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;
using Accord.Collections;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using LandscapeClassifier.Extensions;
using LandscapeClassifier.Model;
using LandscapeClassifier.View.Export;
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
        private double _minAltitude;
        private double _maxAltitude;
        private LayerViewModel _heightmapLayer;
        private bool _exportAsProbabilities;
        private double _minAcceptanceProbability;
        private bool _scaleToUnrealLandscape;
        private object _canExportAsProbabilities;
        private bool _exportRgb;
        private bool _canExportRgb;

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

        public double MinAltitude
        {
            get { return _minAltitude; }
            set { _minAltitude = value; RaisePropertyChanged(); }
        }

        public double MaxAltitude
        {
            get { return _maxAltitude; }
            set { _maxAltitude = value; RaisePropertyChanged(); }
        }


        public bool ExportRgb
        {
            get { return _exportRgb; }
            set { _exportRgb = value; RaisePropertyChanged(); }
        }

        public bool CanExportRgb
        {
            get { return _canExportRgb; }
            set { _canExportRgb = value; RaisePropertyChanged(); }
        }

        public LayerViewModel HeightmapLayer
        {
            get { return _heightmapLayer; }
            set
            {
                _heightmapLayer = value;
                RaisePropertyChanged();

                Task.Run(() =>
                {
                    if (value != null)
                    {
                        var dataSet = Gdal.Open(value.Path, Access.GA_ReadOnly);
                        var rasterBand = dataSet.GetRasterBand(1);

                        double[] minMax = new double[2];
                        rasterBand.ComputeRasterMinMax(minMax, 0);

                        MinAltitude = minMax[0];
                        MaxAltitude = minMax[1];
                    }
                });
            }
        }

        public ObservableCollection<LayerViewModel> ExistingLayers { get; } = new ObservableCollection<LayerViewModel>();

        public ExportLandCoverTypeViewModel SelectedLayer
        {
            get { return _selectedLayer; }
            set { _selectedLayer = value; RaisePropertyChanged(); }
        }

        public ObservableCollection<ExportLandCoverTypeViewModel> ExportLayers { get; set; }

        public ICommand AddLayerCommand { get; set; }
        public ICommand RemoveLayerCommand { get; set; }
        public ICommand BrowseExportPathCommand { get; set; }
        public ICommand BrowseHeightmapLayerCommand { get; set; }


        public ExportPredictionDialogViewModel()
        {
            ExportLayers = new ObservableCollection<ExportLandCoverTypeViewModel>();

            AddLayerCommand = new RelayCommand(AddLayer);
            RemoveLayerCommand = new RelayCommand(RemoveLayer, CanRemoveLayer);
            BrowseExportPathCommand = new RelayCommand(BrowseExportPath);
            BrowseHeightmapLayerCommand = new RelayCommand(BrowseHeightmapLayer, CanBrowseHeightmapLayer);
        }

        private void BrowseHeightmapLayer()
        {
            var browseLayerDialog = new BrowseLayerDialog();

            browseLayerDialog.DialogViewModel.Layers.AddRange(ExistingLayers);

            if (browseLayerDialog.ShowDialog() == true)
            {
                var selectedLayer = browseLayerDialog.DialogViewModel.SelectedLayer;
                HeightmapLayer = selectedLayer;
            }
        }

        private bool CanBrowseHeightmapLayer()
        {
            return ExportHeightmap;
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
            ExportLayers.Remove(SelectedLayer);
        }

        private void AddLayer()
        {
            int missingLayer = MissingLayerIndex(ExportLayers);
            ExportLayers.Add(new ExportLandCoverTypeViewModel("layer" + missingLayer + ".png", ExportLayers));
        }

        public static int MissingLayerIndex(ObservableCollection<ExportLandCoverTypeViewModel> layers)
        {
            var layerNumbers = layers.Where(l =>
               Regex.IsMatch(l.Name, "layer[0-9]+.png")).
               Select(l => int.Parse(Regex.Match(l.Name, "layer([0-9]+).png").Groups[1].Value)).ToList();

            return layerNumbers.Count == 0 ? 0 : Enumerable.Range(layerNumbers.Min(), layerNumbers.Count + 1).Except(layerNumbers).FirstOrDefault();
        }


        public void Initialize(bool canExportAsProbabilities, bool canExportRgb, List<LayerViewModel> layerPaths)
        {
            CanExportRgb = canExportRgb;
            ExportHeightmap = false;
            ExistingLayers.Clear();
            ExistingLayers.AddRange(layerPaths);
            CanExportAsProbabilities = canExportAsProbabilities;

            var types = Enum.GetValues(typeof(LandcoverType));
            foreach (LandcoverType type in types)
            {
                if (type == LandcoverType.None) continue;

                var layer = new ExportLandCoverTypeViewModel(type + ".png", ExportLayers);

                if (type == LandcoverType.Grass) layer.Grass = true;
                if (type == LandcoverType.Gravel) layer.Gravel = true;
                if (type == LandcoverType.Rock) layer.Rock = true;
                if (type == LandcoverType.Snow) layer.Snow = true;
                if (type == LandcoverType.Tree) layer.Tree = true;
                if (type == LandcoverType.Water) layer.Water = true;
                if (type == LandcoverType.Agriculture) layer.Agriculture = true;
                if (type == LandcoverType.Settlement) layer.Settlement = true;

                ExportLayers.Add(layer);
            }
        }
    }

    public class ExportLandCoverTypeViewModel : ViewModelBase
    {
        private readonly ObservableCollection<ExportLandCoverTypeViewModel> _existingLayers;
        private bool _grass;
        private bool _gravel;
        private bool _rock;
        private bool _snow;
        private bool _tree;
        private bool _water;
        private bool _agriculture;
        private bool _settlement;
        private string _name;

        public string Name
        {
            get { return _name; }
            set { _name = value; RaisePropertyChanged(); }
        }

        public bool Grass
        {
            get { return _grass; }
            set { _grass = value; RaisePropertyChanged(); }
        }

        public bool Gravel
        {
            get { return _gravel; }
            set { _gravel = value; RaisePropertyChanged(); }
        }

        public bool Rock
        {
            get { return _rock; }
            set { _rock = value; RaisePropertyChanged(); }
        }

        public bool Snow
        {
            get { return _snow; }
            set { _snow = value; RaisePropertyChanged(); }
        }

        public bool Tree
        {
            get { return _tree; }
            set { _tree = value; RaisePropertyChanged(); }
        }

        public bool Water
        {
            get { return _water; }
            set { _water = value; RaisePropertyChanged(); }
        }

        public bool Agriculture
        {
            get { return _agriculture; }
            set { _agriculture = value; RaisePropertyChanged(); }
        }

        public bool Settlement
        {
            get { return _settlement; }
            set { _settlement = value; RaisePropertyChanged(); }
        }

        public int[] SelectedTypeIndices
        {
            get
            {
                var selectedIndices = new List<int>();
                if (Grass) selectedIndices.Add((int)LandcoverType.Grass);
                if (Gravel) selectedIndices.Add((int)LandcoverType.Gravel);
                if (Rock) selectedIndices.Add((int)LandcoverType.Rock);
                if (Snow) selectedIndices.Add((int)LandcoverType.Snow);
                if (Tree) selectedIndices.Add((int)LandcoverType.Tree);
                if (Water) selectedIndices.Add((int)LandcoverType.Water);
                if (Agriculture) selectedIndices.Add((int)LandcoverType.Agriculture);
                if (Settlement) selectedIndices.Add((int)LandcoverType.Settlement);
                return selectedIndices.ToArray();
            }
        }

        public bool[] LandCoverTypes
        {
            get
            {
                var types = Enum.GetValues(typeof(LandcoverType));
                bool[] typesResult = new bool[types.Length - 1];
                foreach (LandcoverType type in types)
                {
                    if (type == LandcoverType.Grass) typesResult[(int)type] = Grass;
                    if (type == LandcoverType.Gravel) typesResult[(int)type] = Gravel;
                    if (type == LandcoverType.Rock) typesResult[(int)type] = Rock;
                    if (type == LandcoverType.Snow) typesResult[(int)type] = Snow;
                    if (type == LandcoverType.Tree) typesResult[(int)type] = Tree;
                    if (type == LandcoverType.Water) typesResult[(int)type] = Water;
                    if (type == LandcoverType.Agriculture) typesResult[(int)type] = Agriculture;
                    if (type == LandcoverType.Settlement) typesResult[(int)type] = Settlement;
                }
                return typesResult;
            }
        }

        public ExportLandCoverTypeViewModel(string name, ObservableCollection<ExportLandCoverTypeViewModel> existingLayers)
        {
            _existingLayers = existingLayers;
            Name = name;

            PropertyChanged += OnPropertyChanged;
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            if (propertyChangedEventArgs.PropertyName != nameof(Name))
            {
                string newName = "";
                if (Grass) newName += "Grass";
                if (Gravel) newName += "Gravel";
                if (Rock) newName += "Rock";
                if (Snow) newName += "Snow";
                if (Tree) newName += "Tree";
                if (Water) newName += "Water";
                if (Agriculture) newName += "Agriculture";
                if (Settlement) newName += "Settlement";
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
