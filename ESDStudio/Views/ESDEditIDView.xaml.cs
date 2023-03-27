using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace ESDStudio.Views;

public partial class ESDEditIDView : Window
{
    public ESDEditIDView()
    {
        InitializeComponent();
    }
    
    private void NewIdEntry_OnPreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        e.Handled = Regex.IsMatch( e.Text,"^[^0-9]*$");
    }
}