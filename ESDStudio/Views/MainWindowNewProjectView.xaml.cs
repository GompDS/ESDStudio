using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ESDStudio.ViewModels;

namespace ESDStudio.Views;

public partial class MainWindowNewProjectView : Window
{
    public MainWindowNewProjectView()
    {
        InitializeComponent();
    }

    private void ProjectName_OnPreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        e.Handled = Regex.IsMatch( e.Text,"^[^a-zA-Z0-9_-]*$");
    }
}