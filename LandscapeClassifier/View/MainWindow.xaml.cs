using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using LandscapeClassifier.Model;
using LandscapeClassifier.Util;
using LandscapeClassifier.View.Open;
using LandscapeClassifier.ViewModel;
using MahApps.Metro.Controls;
using Microsoft.Win32;
using OSGeo.GDAL;

namespace LandscapeClassifier.View
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        private readonly ViewModel.MainWindowViewModel _mainWindowViewModel;

        public MainWindow()
        {
            InitializeComponent();
            _mainWindowViewModel = (ViewModel.MainWindowViewModel) DataContext;

        }

        private void OpenDEM_Click(object sender, RoutedEventArgs e)
        {
        }



    }
}