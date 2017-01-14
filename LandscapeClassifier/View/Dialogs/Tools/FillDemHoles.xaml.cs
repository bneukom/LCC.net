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
        public FlattenWaterBodiesViewModel DialogViewModel { get; private set; }

        public FlattenWaterBodiesDialog()
        {
            InitializeComponent();

            DialogViewModel = (FlattenWaterBodiesViewModel)DataContext;
        }


        private void OkClick(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }


        private void DoneClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}