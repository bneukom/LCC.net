using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using MahApps.Metro.Controls;

namespace LandscapeClassifier.View.Dialogs
{
    /// <summary>
    /// Interaction logic for ExceptionHandlerDialog.xaml
    /// </summary>
    public partial class ExceptionHandlerDialog : MetroWindow
    {
        public ExceptionHandlerDialog()
        {
            InitializeComponent();
        }

        public void ShowDialog(Exception e)
        {
            ExceptionTextBox.Text = e.ToString();
            ShowDialog();
        }

        private void QuitClick(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void ContinueClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
