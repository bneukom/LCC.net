using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using LandscapeClassifier.Extensions;
using LandscapeClassifier.ViewModel.Dialogs;
using MahApps.Metro.Controls;
using Microsoft.Win32;
using OSGeo.GDAL;
using OSGeo.OGR;
using Driver = OSGeo.GDAL.Driver;

namespace LandscapeClassifier.View.Tools
{
    /// <summary>
    /// Interaction logic for CreateSlopeFromDEMDialog.xaml
    /// </summary>
    public partial class CreateTiledHeightmapDialog : MetroWindow
    {

        private CreateTiledHeightmapDialogViewModel _viewModel;

        public CreateTiledHeightmapDialog()
        {
            InitializeComponent();

            _viewModel = (CreateTiledHeightmapDialogViewModel) DataContext;
        }


        private void DoneClick(object sender, RoutedEventArgs e)
        {
            DialogResult = true;

        }


    }
}
