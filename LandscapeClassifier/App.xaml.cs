using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using LandscapeClassifier.View.Dialogs;

namespace LandscapeClassifier
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void App_OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            ExceptionHandlerDialog exceptionHandlerDialog = new ExceptionHandlerDialog();
            exceptionHandlerDialog.ShowDialog(e.Exception);
            Console.WriteLine(e.Exception);
            e.Handled = true;
        }
    }
}
