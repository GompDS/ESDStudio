using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Xml;
using ESDStudio.ViewModels;
using ESDStudio.Views;
using ICSharpCode.AvalonEdit.Highlighting;

namespace ESDStudio
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            MainWindow = new MainWindow();
            MainWindow.DataContext = new MainWindowViewModel();
            MainWindow.Show();
        }
    }
}