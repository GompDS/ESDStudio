using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ESDStudio.Views;

public partial class CreateNewESD : Window
{
    public ESDInfo? NewESDInfo = null;
    public CreateNewESD()
    {
        InitializeComponent();
    }

    private void NewESDId_OnPreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        e.Handled = IsTextAllowed(e.Text);
    }

    private bool IsTextAllowed(string text)
    {
        Regex ESDIdRegex = new("[^0-9]+");
        return ESDIdRegex.IsMatch(text);
    }

    private void OkButton_OnClick(object sender, RoutedEventArgs e)
    {
        int ESDId = int.Parse(NewESDId.Text);
        string BNDName = (string)((ComboBoxItem)BNDChoice.SelectedItem).Content;
        bool? success = (Application.Current.MainWindow as MainWindow)?.AddNewESDToBND(ESDId, BNDName);
        if (success != true)
        {
            MessageBoxResult result = MessageBox.Show("There was an error", "Error", MessageBoxButton.OK);
        }
        DialogResult = true;
    }
    
    private void CancelButton_OnClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}