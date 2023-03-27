using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace ESDStudio.ViewModels;

public class ViewModelBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected MessageBoxResult ShowErrorMessageBox(string errorDescription)
    {
        return MessageBox.Show(
            errorDescription, 
            "Error",
            MessageBoxButton.OK,
            MessageBoxImage.Error,
            MessageBoxResult.OK);
    }
    
    protected MessageBoxResult ShowConfirmationMessageBox(string confirmDescription)
    {
        return MessageBox.Show(
            confirmDescription, 
            "Confirmation",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question,
            MessageBoxResult.OK);
    }
}