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
    public partial class FillDemHolesDialog : MetroWindow
    {
        public DemHolesFootprintViewModel DialogViewModel { get; private set; }

        public FillDemHolesDialog()
        {
            InitializeComponent();

            DialogViewModel = (DemHolesFootprintViewModel)DataContext;
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