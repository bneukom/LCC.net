using System.Collections.ObjectModel;
using System.Dynamic;
using System.Windows;
using System.Windows.Controls;

namespace Selen.Wpf.DemoApplication
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();

            dynamic dataContext = new ExpandoObject();
            dataContext.firstData = new []
            {
                new FoodItem("Bagel", 100, 140, false),
                new FoodItem("White Bread", 1874, 96, false), 
                new FoodItem("Steak", 45, 158, true), 
                new FoodItem("Hamburger", 4000, 250, false)
            };

            dataContext.secondData = new[]
            {
                new FoodItem("Water", 100, 1, false),
                new FoodItem("White Bread", 1874, 96, false), 
                new FoodItem("Orange juice", 46, 20, true), 
                new FoodItem("Hamburger", 4000, 250, false)
            };

            DataContext = dataContext;
        }

        private void ButtonAddRocketClick(object sender, RoutedEventArgs e)
        {
            new AddRocketDialog().ShowDialog();
        }
    }
}