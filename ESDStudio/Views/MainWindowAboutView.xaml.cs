using System;
using System.Reflection;
using System.Windows;

namespace ESDStudio.Views;

public partial class MainWindowAboutView : Window
{
    public MainWindowAboutView()
    {
        InitializeComponent();
        DataContext = this;
        Version? v = Assembly.GetExecutingAssembly().GetName().Version;
        if (v == null)
        {
            AppVersion = "(Unknown Version)";
        }
        else
        {
            AppVersion = $"v{v.Major}.{v.Minor}.{v.Build}";
        }
    }

    public string AppVersion { get; }
}