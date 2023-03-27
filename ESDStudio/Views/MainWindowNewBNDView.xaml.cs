using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace ESDStudio.Views;

public partial class MainWindowNewBNDView : Window
{
    public MainWindowNewBNDView()
    {
        InitializeComponent();
    }

    private void TextBox_OnPreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        e.Handled = Regex.IsMatch( e.Text,"^[^0-9]*$");
    }
}