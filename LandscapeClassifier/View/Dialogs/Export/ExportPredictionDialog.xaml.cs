using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using LandscapeClassifier.Extensions;
using LandscapeClassifier.Model;
using LandscapeClassifier.ViewModel;
using LandscapeClassifier.ViewModel.Dialogs;
using MahApps.Metro.Controls;
using Microsoft.Win32;
using OSGeo.GDAL;

namespace LandscapeClassifier.View.Open
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