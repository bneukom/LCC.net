﻿using System;
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
    public partial class AddBandsDialog : MetroWindow
    {
        public AddBandsDialogViewModel DialogViewModel { get; private set; }

        public AddBandsDialog()
        {
            InitializeComponent();

            DialogViewModel = (AddBandsDialogViewModel)DataContext;
        }

        public bool? ShowImportMissingBandsDialog(List<BandInfo> bandInfo, bool contrastEnhancement, SatelliteType satelliteType)
        {
            MissingBandsInfoTextBlock.Visibility = Visibility.Visible;
            AddRemoveBandsStackPanel.Visibility = Visibility.Collapsed;
            DialogViewModel.Reset();
            bandInfo.ForEach(b => DialogViewModel.Bands.Add(b));
            DialogViewModel.BandContrastEnhancement = contrastEnhancement;
            DialogViewModel.SatelliteType = satelliteType;
            DialogViewModel.MissingBandsImport = true;
            return ShowDialog();
        }

        public bool? ShowAddBandsDialog()
        {
            MissingBandsInfoTextBlock.Visibility = Visibility.Collapsed;
            AddRemoveBandsStackPanel.Visibility = Visibility.Visible;
            DialogViewModel.Reset();
            return ShowDialog();
        }

        private void OkClick(object sender, RoutedEventArgs e)
        {
            if (DialogViewModel.AddRgb)
            {
                if (!DialogViewModel.Bands.Any(b => b.B)
                    || !DialogViewModel.Bands.Any(b => b.G)
                    || !DialogViewModel.Bands.Any(b => b.R))
                {
                    MessageBox.Show(this, "Not all RGB channels have been specified.", "RGB Channel Missing",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    DialogResult = true;
                }
            }
            else
            {
                DialogResult = true;
            }
        }


        private void CancelClick(object sender, RoutedEventArgs e)
        {
        }

        // TODO Command
        private void AddBandsClick(object sender, RoutedEventArgs e)
        {
            // Get GDAL extensions
            StringBuilder allImageExtensions = new StringBuilder();
            Dictionary<string, string> images = new Dictionary<string, string>();
            for (int driverIndex = 0; driverIndex < Gdal.GetDriverCount(); ++driverIndex)
            {
                var driver = Gdal.GetDriver(driverIndex);
                var metadata = driver.GetMetadata("");

                if (metadata.Any(m => m.StartsWith("DMD_EXTENSION")))
                {
                    string extensionMetadata = metadata.First(m => m.StartsWith("DMD_EXTENSION"));
                    string extension = "*." + extensionMetadata.Substring(extensionMetadata.IndexOf("=") + 1);
                    string describtionMetadata = metadata.First(m => m.StartsWith("DMD_LONGNAME"));
                    string describtion = describtionMetadata.Substring(describtionMetadata.IndexOf("=") + 1);
                    if (!images.ContainsKey(describtion))
                        images.Add(describtion, extension);

                    allImageExtensions.Append(";" + extension);
                }
            }
            images.Add("All Files", "*.*");

            StringBuilder filter = new StringBuilder();
            if (allImageExtensions.Length > 0)
            {
                filter.Append($"All Images|{allImageExtensions.ToString()}");
            }
            foreach (var image in images)
            {
                filter.Append($"|{image.Key}|{image.Value}");
            }

            // Create an instance of the open file dialog box.
            var openFileDialog = new OpenFileDialog
            {
                FilterIndex = 1,
                Filter = filter.ToString(),
                Multiselect = true
            };

            // Call the ShowDialog method to show the dialog box.
            var userClickedOk = openFileDialog.ShowDialog();

            // Process input if the user clicked OK.
            if (userClickedOk == true)
            {
                for (int pathIndex = 0; pathIndex < openFileDialog.FileNames.Length; ++pathIndex)
                {
                    string fileName = Path.GetFileName(openFileDialog.FileNames[pathIndex]);
                    string bandNumber = DialogViewModel.SatelliteType.GetBand(fileName);

                    DialogViewModel.Bands.Add(new BandInfo(openFileDialog.FileNames[pathIndex], bandNumber == "04", bandNumber == "03", bandNumber == "02"));
                }
            }
        }
    }
}