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

            var types = Enum.GetValues(typeof(LandcoverType));
            foreach (LandcoverType type in types)
            {
                if (type == LandcoverType.None) continue;

                var layer = new ExportLandCoverTypeViewModel(type + ".png", ExportLandCoverLayers);

                if (type == LandcoverType.Grass) layer.Grass = true;
                if (type == LandcoverType.Gravel) layer.Gravel = true;
                if (type == LandcoverType.Rock) layer.Rock = true;
                if (type == LandcoverType.Snow) layer.Snow = true;
                if (type == LandcoverType.Tree) layer.Tree = true;
                if (type == LandcoverType.Water) layer.Water = true;
                if (type == LandcoverType.Agriculture) layer.Agriculture = true;
                if (type == LandcoverType.Settlement) layer.Settlement = true;

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
