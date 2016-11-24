using System.Collections.Generic;
using System.Windows;
using LandscapeClassifier.ViewModel.Dialogs;
using MahApps.Metro.Controls;

namespace LandscapeClassifier.View.Export
{
    /// <summary>
    /// Interaction logic for CreateSlopeFromDEMDialog.xaml
    /// </summary>
    public partial class BrowseLayerDialog : MetroWindow
    {
        public BrowseLayerDialogViewModel DialogViewModel { get; private set; }

        public BrowseLayerDialog()
        {
            InitializeComponent();

            DialogViewModel = (BrowseLayerDialogViewModel)DataContext;
            DialogViewModel.Reset();
        }

        private void OkClick(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}