using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using LandscapeClassifier.Converter;
using LandscapeClassifier.Extensions;
using LandscapeClassifier.View.ValidationRules;
using LandscapeClassifier.ViewModel.MainWindow;
using LandscapeClassifier.ViewModel.MainWindow.Classification;
using LandscapeClassifier.ViewModel.MainWindow.Classification.Algorithms;
using LandscapeClassifier.ViewModel.MainWindow.Classification.Algorithms.Attributes;
using MahApps.Metro.Controls;

namespace LandscapeClassifier.View
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        private int _rowIndex = -1;
        private MainWindowViewModel _viewModel;
        private ClassifierViewModelBase _currentClassifierViewModel;

        public delegate Point GetPosition(IInputElement element);

        public MainWindow()
        {
            InitializeComponent();

            //LayersGrid.PreviewMouseLeftButtonDown += productsDataGrid_PreviewMouseLeftButtonDown;
            //LayersGrid.Drop += productsDataGrid_Drop;

            _viewModel = (MainWindowViewModel)DataContext;
            _viewModel.ClassifierViewModel.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == nameof(ClassifierViewModel.SelectededClassifier))
                {
                    UpdateClassifierOptionsProperties(_viewModel.ClassifierViewModel.CurrentClassifierViewModel);
                }
            };

            UpdateClassifierOptionsProperties(_viewModel.ClassifierViewModel.CurrentClassifierViewModel);
        }

        public void UpdateClassifierOptionsProperties(ClassifierViewModelBase classifierViewModel)
        {
            if (_currentClassifierViewModel != null)
            {
                _currentClassifierViewModel.PropertyChanged -= UpdateViewPropertyChangeHandler;
            }

            _currentClassifierViewModel = classifierViewModel;

            if (classifierViewModel == null)
            {
                DataGrid.Children.Clear();
                return;
            }

            _currentClassifierViewModel = classifierViewModel;
            _currentClassifierViewModel.PropertyChanged += UpdateViewPropertyChangeHandler;

            UpdateView();

        }

        private void UpdateViewPropertyChangeHandler(object sender, PropertyChangedEventArgs args)
        {
            if (_currentClassifierViewModel.PropertyAffectsOptions(args.PropertyName))
            {
                UpdateView();
            }
        }

        private void UpdateView()
        {
            DataGrid.Children.Clear();
            DataGrid.DataContext = _currentClassifierViewModel;

            var properties = _currentClassifierViewModel.GetType().GetProperties().Where(p => p.CustomAttributes.Any(a => a.AttributeType == typeof(OptionAttribute)));

            var propertyInfos = properties as IList<PropertyInfo> ?? properties.ToList();

            for (int propertyIndex = 0; propertyIndex < propertyInfos.Count; propertyIndex++)
            {
                var propertyInfo = propertyInfos[propertyIndex];

                var visibleWhens = propertyInfo.GetCustomAttributes(typeof(VisibleWhen), true).Cast<VisibleWhen>();
                var visibleWhensList = visibleWhens as IList<VisibleWhen> ?? visibleWhens.ToList();

                bool visible = !visibleWhensList.Any();
                foreach (var visibleWhen in visibleWhensList)
                {
                    var property = _currentClassifierViewModel.GetType().GetProperty(visibleWhen.Property);
                    if (property.GetValue(_currentClassifierViewModel).Equals(visibleWhen.Value))
                    {
                        visible = true;
                        break;
                    }
                }

                if (!visible)
                {
                    continue;
                }

                DataGrid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });

                Label label = new Label { Content = propertyInfo.Name + ":" };
                Control valueControl = CreateValueControl(propertyInfo);

                label.Margin = new Thickness(5);
                valueControl.Margin = new Thickness(5);

                Grid.SetRow(label, propertyIndex);
                Grid.SetRow(valueControl, propertyIndex);
                Grid.SetColumn(label, 0);
                Grid.SetColumn(valueControl, 1);

                DataGrid.Children.Add(label);
                DataGrid.Children.Add(valueControl);
            }
        }

        private Control CreateValueControl(PropertyInfo propertyInfo)
        {
            var binding = new Binding
            {
                Path = new PropertyPath(propertyInfo.Name),
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            };
            if (propertyInfo.PropertyType.IsEnum)
            {
                var combo = new ComboBox { ItemsSource = Enum.GetValues(propertyInfo.PropertyType) };

                BindingOperations.SetBinding(combo, Selector.SelectedItemProperty, binding);
                return combo;
            }
            else if (propertyInfo.PropertyType == typeof(bool))
            {
                CheckBox checkBox = new CheckBox();
                BindingOperations.SetBinding(checkBox, CheckBox.IsCheckedProperty, binding);
                return checkBox;
            }
            else
            {

                if (propertyInfo.PropertyType == typeof(double))
                {
                    binding.ValidationRules.Add(new DoubleValidationRule());
                    binding.Converter = new DoubleToPersistantStringConverter();
                } else if (propertyInfo.PropertyType == typeof(int))
                {
                    binding.ValidationRules.Add(new IntValidationRule());
                    binding.Converter = new IntToPersistantStringConverter();
                }

                binding.ValidationRules.AddRange(propertyInfo.GetCustomAttributes(typeof(BiggerThan)).Cast<BiggerThan>().Select(b => new BiggerThanValidationRule(b.Value)));

                TextBox text = new TextBox();
                BindingOperations.SetBinding(text, TextBox.TextProperty, binding);
                return text;
            }
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