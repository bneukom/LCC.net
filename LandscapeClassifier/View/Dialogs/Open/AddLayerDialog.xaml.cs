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
    public partial class AddLayerDialog : MetroWindow
    {
        public AddLayerDialogViewModel DialogViewModel { get; private set; }

        public AddLayerDialog()
        {
            InitializeComponent();

            DialogViewModel = (AddLayerDialogViewModel)DataContext;
        }


        public bool? ShowAddBandsDialog()
        {
            AddRemoveBandsStackPanel.Visibility = Visibility.Visible;
            DialogViewModel.Reset();
            return ShowDialog();
        }

        private void OkClick(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }


        private void CancelClick(object sender, RoutedEventArgs e)
        {
        }

        // TODO Command
        private void AddLayerClick(object sender, RoutedEventArgs e)
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
                    DialogViewModel.Layers.Add(new LayerInfo(openFileDialog.FileNames[pathIndex], false));
                }
            }
        }

        private void RemoveLayerClick(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }
    }
}