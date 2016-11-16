using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using LandscapeClassifier.Model;
using LandscapeClassifier.Util;
using LandscapeClassifier.View.Open;
using LandscapeClassifier.ViewModel;
using LandscapeClassifier.ViewModel.MainWindow;
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
        private int _rowIndex = -1;
        private MainWindowViewModel _viewModel;
        public delegate Point GetPosition(IInputElement element);

        public MainWindow()
        {
            InitializeComponent();

            //LayersGrid.PreviewMouseLeftButtonDown += productsDataGrid_PreviewMouseLeftButtonDown;
            //LayersGrid.Drop += productsDataGrid_Drop;

            _viewModel = (MainWindowViewModel)DataContext;
        }

        void productsDataGrid_Drop(object sender, DragEventArgs e)
        {
            if (_rowIndex < 0)
                return;
            int index = this.GetCurrentRowIndex(e.GetPosition);
            if (index < 0)
                return;
            if (index == _rowIndex)
                return;
            if (index == LayersGrid.Items.Count)
                return;

            var changed = _viewModel.Layers[_rowIndex];
            _viewModel.Layers.RemoveAt(_rowIndex);
            _viewModel.Layers.Insert(index, changed);
        }
        void productsDataGrid_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _rowIndex = GetCurrentRowIndex(e.GetPosition);
            Console.WriteLine(_rowIndex);
            if (_rowIndex < 0)
                return;
            LayersGrid.SelectedIndex = _rowIndex;
            var selectedLayer = _viewModel.Layers[_rowIndex];
            if (selectedLayer == null)
                return;
            DragDropEffects dragdropeffects = DragDropEffects.Move;
            if (DragDrop.DoDragDrop(LayersGrid, selectedLayer, dragdropeffects) != DragDropEffects.None)
            {
                LayersGrid.SelectedItem = selectedLayer;
            }
        }
        private bool GetMouseTargetRow(Visual theTarget, GetPosition position)
        {
            Rect rect = VisualTreeHelper.GetDescendantBounds(theTarget);
            Point point = position((IInputElement)theTarget);
            return rect.Contains(point);
        }
        private DataGridRow GetRowItem(int index)
        {
            if (LayersGrid.ItemContainerGenerator.Status
                    != GeneratorStatus.ContainersGenerated)
                return null;
            return LayersGrid.ItemContainerGenerator.ContainerFromIndex(index)
                                                            as DataGridRow;
        }
        private int GetCurrentRowIndex(GetPosition pos)
        {
            int curIndex = -1;
            for (int i = 0; i < LayersGrid.Items.Count; i++)
            {
                DataGridRow itm = GetRowItem(i);
                if (GetMouseTargetRow(itm, pos))
                {
                    curIndex = i;
                    break;
                }
            }
            return curIndex;
        }
    }
}