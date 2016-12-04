using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using LandscapeClassifier.ViewModel.Dialogs;
using MahApps.Metro.Controls;

namespace LandscapeClassifier.View.Export
{
    /// <summary>
    /// Interaction logic for CreateSlopeFromDEMDialog.xaml
    /// </summary>
    public partial class PredictionAccuracyDialog : MetroWindow
    {
        public PredictionAccuracyDialogViewModel DialogViewModel { get; private set; }

        public PredictionAccuracyDialog()
        {
            InitializeComponent();

            DialogViewModel = (PredictionAccuracyDialogViewModel)DataContext;

        }

        private void OkClick(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

    }
}