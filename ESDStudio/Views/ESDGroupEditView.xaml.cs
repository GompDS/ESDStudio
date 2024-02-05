using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace ESDStudio.Views;

public partial class ESDGroupEditView : Window
{
    public ESDGroupEditView()
    {
        InitializeComponent();
        ESDIDBox.MaxLength = Project.Current.Game.IdLength;
    }
    
    private void AddESDIdEntry_OnPreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        e.Handled = Regex.IsMatch( e.Text,"^[^0-9]*$");
    }
}