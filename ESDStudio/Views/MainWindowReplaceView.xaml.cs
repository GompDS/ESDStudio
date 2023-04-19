using System.Windows;

namespace ESDStudio.Views;

public partial class MainWindowReplaceView : Window
{
    public MainWindowReplaceView()
    {
        InitializeComponent();
        FindEntryBox.Focus();
    }

    private void Close_OnClick(object sender, RoutedEventArgs e)
    {
        Close();
    }
}