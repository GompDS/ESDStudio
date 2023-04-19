using System.Windows;

namespace ESDStudio.Views;

public partial class MainWindowFindView : Window
{
    public MainWindowFindView()
    {
        InitializeComponent();
        FindEntryBox.Focus();
    }

    private void Close_OnClick(object sender, RoutedEventArgs e)
    {
        Close();
    }
}