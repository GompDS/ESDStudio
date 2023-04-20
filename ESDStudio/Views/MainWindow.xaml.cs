using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml;
using ESDStudio.UserControls;
using ESDStudio.ViewModels;
using ICSharpCode.AvalonEdit.Highlighting;
using Microsoft.Win32;
using SoulsFormats;

namespace ESDStudio.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            // Load our custom highlighting definition
            IHighlightingDefinition customHighlighting; 
            using (Stream? s = typeof(MainWindow).Assembly.GetManifestResourceStream("ESDStudio.ESDLang.xshd")) 
            {
                if (s == null)
                {
                    throw new InvalidOperationException("Could not find embedded resource"); 
                }
                using (XmlReader reader = new XmlTextReader(s)) 
                { 
                    customHighlighting = ICSharpCode.AvalonEdit.Highlighting.Xshd. 
                        HighlightingLoader.Load(reader, HighlightingManager.Instance); 
                } 
            } 
            // and register it in the HighlightingManager 
            HighlightingManager.Instance.RegisterHighlighting("ESDLang", new string[] { ".esdlang" }, customHighlighting);
            
            InitializeComponent();
        }

        private void TreeView_OnSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            TreeView tree = (TreeView)sender;
            ((MainWindowViewModel)DataContext).SelectedTreeItem = tree.SelectedItem;
        }

        private void Click_RecentProject(object sender, RoutedEventArgs e)
        {
            MenuItem item = (MenuItem)sender;
            ((MainWindowViewModel)DataContext).SelectedRecentProject = item.Header;
            ((MainWindowViewModel)DataContext).OpenRecentProjectCommand.Execute(null);
        }

        private void DoubleClick_ESDTreeItem(object sender, MouseButtonEventArgs e)
        {
            ESDViewModel ESDToOpen = (ESDViewModel)((TreeViewItem)sender).Header;
            ((MainWindowViewModel)DataContext).OpenNewTab(ESDToOpen);
        }

        private void CurrentTabChanged(object sender, SelectionChangedEventArgs e)
        {
            DetailPanel.ClearDisplay();
            MainWindowViewModel viewModel = (MainWindowViewModel)DataContext;
            TabControl tabControl = (TabControl)sender;
            TabItem tabItem = (TabItem)tabControl.ItemContainerGenerator.ContainerFromItem(tabControl.SelectedItem);
            if (tabItem == null) return;
            ESDView esdView;
            if (tabItem.Content == viewModel.CurrentTab)
            {
                esdView = new ESDView(viewModel.CurrentTab, DetailPanel);
                tabItem.Content = esdView;
            }
            else
            {
                esdView = (ESDView)tabItem.Content;
            }
        }
    }
}