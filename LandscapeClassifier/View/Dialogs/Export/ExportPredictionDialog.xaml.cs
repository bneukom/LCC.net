using System.Collections.Generic;
using System.Windows;
using LandscapeClassifier.ViewModel.Dialogs;
using MahApps.Metro.Controls;

namespace LandscapeClassifier.View.Export
{
    /// <summary>
    /// Interaction logic for CreateSlopeFromDEMDialog.xaml
    /// </summary>
    public partial class ExportPredicitonDialog : MetroWindow
    {
        public ExportPredictionDialogViewModel DialogViewModel { get; private set; }

        public ExportPredicitonDialog()
        {
            InitializeComponent();

            DialogViewModel = (ExportPredictionDialogViewModel)DataContext;
            DialogViewModel.Reset();
        }

        public bool? ShowDialog(List<string> layerPaths)
        {
            DialogViewModel.ExistingLayerPaths = layerPaths;
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