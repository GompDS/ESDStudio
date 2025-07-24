using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ESDStudio.Views;

public partial class ESDEditIDView : Window
{
    public ESDEditIDView()
    {
        InitializeComponent();
        //EntryBox.MaxLength = Project.Current.Game.IdLength;
    }
    
    private void NewIdEntry_OnPreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        e.Handled = string.IsNullOrEmpty(e.Text) || !e.Text.Any(x => char.IsLetter(x) || char.IsDigit(x) || x == '_');;
    }
}