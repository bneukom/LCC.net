using System.Collections.Generic;
using System.Windows;
using LandscapeClassifier.Extensions;
using LandscapeClassifier.ViewModel.Dialogs;
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

        public ExportPredicitonDialog(bool canExportAsProbabilities, bool canExportRgb, List<LayerViewModel> layerPaths)
        {
            InitializeComponent();

            DialogViewModel = (ExportPredictionDialogViewModel)DataContext;
            DialogViewModel.Initialize(canExportAsProbabilities, canExportRgb, layerPaths);
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