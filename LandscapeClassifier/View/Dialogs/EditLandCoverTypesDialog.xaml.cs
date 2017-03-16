using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using Accord.Statistics.Analysis;
using LandscapeClassifier.Extensions;
using LandscapeClassifier.Model;
using LandscapeClassifier.ViewModel.Dialogs;
using MahApps.Metro.Controls;

namespace LandscapeClassifier.View
{
    /// <summary>
    /// Interaction logic for CreateSlopeFromDEMDialog.xaml
    /// </summary>
    public partial class EditLandCoverTypesDialog : MetroWindow
    {
        public EditLandcoverTypesDialogViewModel DialogViewModel { get; private set; }

        public EditLandCoverTypesDialog()
        {
            InitializeComponent();

            DialogViewModel = (EditLandcoverTypesDialogViewModel)DataContext;

        }

        private void OkClick(object sender, RoutedEventArgs e)
        {
            DataGrid.CancelEdit();
            DialogResult = true;
        }

        public bool? ShowDialog(ObservableCollection<LandcoverTypeViewModel> landcoverTypes)
        {
            DialogViewModel.Reset();
            DialogViewModel.LandcoverTypes = landcoverTypes;
            return ShowDialog();
        }

        private void CancelClick(object sender, RoutedEventArgs e)
        {
            DataGrid.CancelEdit();
            DialogResult = false;
        }
    }
}