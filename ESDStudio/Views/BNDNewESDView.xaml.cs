using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace ESDStudio.Views;

public partial class BNDNewESDView : Window
{
    public BNDNewESDView()
    {
        InitializeComponent();
        EntryBox.MaxLength = Project.Current.Game.IdLength;
    }
    
    private void NewIdEntry_OnPreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        e.Handled = Regex.IsMatch( e.Text,"^[^0-9]*$");
    }
}