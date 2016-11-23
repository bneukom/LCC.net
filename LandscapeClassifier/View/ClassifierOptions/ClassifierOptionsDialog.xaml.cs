using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using LandscapeClassifier.ViewModel.MainWindow.Classification.Algorithms;
using MahApps.Metro.Controls;

namespace LandscapeClassifier.View.ClassifierOptions
{
    /// <summary>
    /// Interaction logic for CreateSlopeFromDEMDialog.xaml
    /// </summary>
    public partial class ClassifierOptionsDialog : MetroWindow
    {

        public ClassifierOptionsDialog()
        {
            InitializeComponent();
        }

        public new void Show()
        {
            DataGrid.Children.Clear();

            var properties = DataContext.GetType().GetProperties().Where(p => p.CustomAttributes.Any(a => a.AttributeType == typeof(OptionAttribute)));

            var propertyInfos = properties as IList<PropertyInfo> ?? properties.ToList();

            foreach (var propertyInfo in propertyInfos)
                DataGrid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });

            for (int propertyIndex = 0; propertyIndex < propertyInfos.Count; propertyIndex++)
            {
                var propertyInfo = propertyInfos[propertyIndex];

                Label label = new Label {Content = propertyInfo.Name + ":"};
                TextBox valueTextBox = new TextBox();

                label.Margin = new Thickness(5);
                valueTextBox.Margin = new Thickness(5);

                Binding binding = new Binding {Path = new PropertyPath(propertyInfo.Name)};
                BindingOperations.SetBinding(valueTextBox, TextBox.TextProperty, binding);

                Grid.SetRow(label, propertyIndex);
                Grid.SetRow(valueTextBox, propertyIndex);
                Grid.SetColumn(label, 0);
                Grid.SetColumn(valueTextBox, 1);

                DataGrid.Children.Add(label);
                DataGrid.Children.Add(valueTextBox);
            }

            base.Show();
        }

        private void OkClick(object sender, RoutedEventArgs e)
        {

        }

    }
}