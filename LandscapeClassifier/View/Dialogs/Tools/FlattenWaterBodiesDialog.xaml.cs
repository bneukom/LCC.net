using System.Collections.Generic;
using System.Windows;
using LandscapeClassifier.ViewModel.Dialogs;
using LandscapeClassifier.ViewModel.MainWindow.Classification;
using MahApps.Metro.Controls;

namespace LandscapeClassifier.View.Tools
{
    /// <summary>
    /// Interaction logic for CreateSlopeFromDEMDialog.xaml
    /// </summary>
    public partial class FlattenWaterBodiesDialog : MetroWindow
    {
        public ExportPredictionDialogViewModel DialogViewModel { get; private set; }

        public FlattenWaterBodiesDialog(bool canExportAsProbabilities, List<LayerViewModel> layers)
        {
            InitializeComponent();

            DialogViewModel = (ExportPredictionDialogViewModel)DataContext;
            DialogViewModel.Initialize(canExportAsProbabilities, layers);
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