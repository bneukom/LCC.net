using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;
using Accord.Collections;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using LandscapeClassifier.Model;

namespace LandscapeClassifier.ViewModel.Dialogs
{
    public class ExportPredictionDialogViewModel : ViewModelBase
    {
  
        private bool _exportHeightmap;
        private ExportLandCoverTypeViewModel _selectedLayer;
        private string _exportPath;
        private bool _isExportPathSet;

        public ExportLandCoverTypeViewModel SelectedLayer
        {
            get { return _selectedLayer; }
            set { _selectedLayer = value; RaisePropertyChanged(); }
        }


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

        public ObservableCollection<ExportLandCoverTypeViewModel> ExportLayers { get; set; }

        public ICommand AddLayerCommand { get; set; }
        public ICommand RemoveLayerCommand { get; set; }
        public ICommand BrowseExportPathCommand { get; set; }

        public ExportPredictionDialogViewModel()
        {
            ExportLayers = new ObservableCollection<ExportLandCoverTypeViewModel>();

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
            ExportLayers.Remove(SelectedLayer);
        }

        private void AddLayer()
        {
            var layerNumbers = ExportLayers.Where(l =>
                Regex.IsMatch(l.Name, "layer[0-9]+.png")).
                Select(l => int.Parse(Regex.Match(l.Name, "layer([0-9]+).png").Groups[1].Value)).ToList();

            if (layerNumbers.Count == 0)
            {
                ExportLayers.Add(new ExportLandCoverTypeViewModel("layer0.png"));
            }
            else
            {
                var missing = Enumerable.Range(layerNumbers.Min(), layerNumbers.Count + 1).Except(layerNumbers).FirstOrDefault();

                ExportLayers.Add(new ExportLandCoverTypeViewModel("layer" + missing + ".png"));
            }
        }


    }

    public class ExportLandCoverTypeViewModel : ViewModelBase
    {

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

        public ExportLandCoverTypeViewModel(string name)
        {
            Name = name;
        }
    }
}
