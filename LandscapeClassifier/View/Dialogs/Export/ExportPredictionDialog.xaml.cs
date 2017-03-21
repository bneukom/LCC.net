using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using LandscapeClassifier.Extensions;
using LandscapeClassifier.ViewModel.Dialogs;
using LandscapeClassifier.ViewModel.MainWindow;
using LandscapeClassifier.ViewModel.MainWindow.Classification;
using MahApps.Metro.Controls;

namespace LandscapeClassifier.View.Export
{
    /// <summary>
    /// Interaction logic for CreateSlopeFromDEMDialog.xaml
    /// </summary>
    public partial class ExportPredicitonDialog : MetroWindow
    {
        public ExportPredictionDialogViewModel DialogViewModel { get; private set; }

        public ExportPredicitonDialog(bool canExportAsProbabilities, List<LayerViewModel> layers)
        {
            InitializeComponent();

            DialogViewModel = (ExportPredictionDialogViewModel)DataContext;
            DialogViewModel.Initialize(canExportAsProbabilities, layers);

            var landcoverTypes = MainWindowViewModel.Default.LandcoverTypes.Values.ToList();

            for (int i = 0; i < landcoverTypes.Count; i++)
            {
                Binding binding = new Binding($"ExportedLandCoverTypes[{i}]");
                DataGridCheckBoxColumn column = new DataGridCheckBoxColumn();
                binding.Mode = BindingMode.TwoWay;
                binding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
                binding.ValidatesOnDataErrors = true;
                column.Binding = binding;
                column.CanUserSort = false;
                column.Header = landcoverTypes[i].Name;
                column.Width = DataGridLength.Auto;
                column.ElementStyle = FindResource("MetroDataGridCheckBox") as Style;
                column.EditingElementStyle = FindResource("MetroDataGridCheckBox") as Style;
                ExportLandcoverTypesGrid.Columns.Add(column);
            }
        }

        public bool? ShowDialog(List<LayerViewModel> layerPaths)
        {
            return ShowDialog();
        }

        private void OkClick(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }


        private void CancelClick(object sender, RoutedEventArgs e)
        {
        }
    }
}