using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using LandscapeClassifier.Model;
using LandscapeClassifier.ViewModel;
using MahApps.Metro.Controls;
using Microsoft.Win32;

namespace LandscapeClassifier.View.Open
{
    /// <summary>
    /// Interaction logic for CreateSlopeFromDEMDialog.xaml
    /// </summary>
    public partial class OpenImageDialog : MetroWindow
    {
        public OpenImageDialogViewModel DialogViewModel { get; private set; }

        public OpenImageDialog()
        {
            InitializeComponent();

            DialogViewModel = (OpenImageDialogViewModel) DataContext;
            DialogViewModel.Bands.Clear();
        }

        private void OkClick(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }


        private void CancelClick(object sender, RoutedEventArgs e)
        {
        }

        private void AddBandsClick(object sender, RoutedEventArgs e)
        {
            // Create an instance of the open file dialog box.
            var openFileDialog = new OpenFileDialog
            {
                FilterIndex = 1,
                Multiselect = true
            };

            // Call the ShowDialog method to show the dialog box.
            var userClickedOk = openFileDialog.ShowDialog();

            // Process input if the user clicked OK.
            if (userClickedOk == true)
            {
                for (int pathIndex = 0; pathIndex < openFileDialog.FileNames.Length; ++pathIndex)
                {
                    DialogViewModel.Bands.Add(new BandInfo(openFileDialog.FileNames[pathIndex], pathIndex == 0, pathIndex == 1, pathIndex == 2));
                }
            }
        }
    }
}