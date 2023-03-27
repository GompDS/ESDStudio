using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;

namespace ESDStudio.ViewModels;

public class EditDescriptionViewModel : DialogViewModelBase
{
    public EditDescriptionViewModel(string currentDescription)
    {
        _newDescriptionEntry = currentDescription;
    }
    
    private string _newDescriptionEntry;
    public string NewDescriptionEntry
    {
        get => _newDescriptionEntry;
        set
        {
            if (_newDescriptionEntry != value)
            {
                _newDescriptionEntry = value;
                OnPropertyChanged();
            }
        }
    }

}