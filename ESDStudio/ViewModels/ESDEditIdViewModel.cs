using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;

namespace ESDStudio.ViewModels;

public class EditESDIdViewModel : DialogViewModelBase
{
    public EditESDIdViewModel(string currentId, List<string> localESDIds)
    {
        _newIdEntry = currentId.ToString();
        LocalESDIds = localESDIds;
    }
    
    private string _newIdEntry;
    public string NewIdEntry
    {
        get => _newIdEntry;
        set
        {
            if (_newIdEntry != value)
            {
                _newIdEntry = value;
                OnPropertyChanged();
            }
        }
    }

    public List<string> LocalESDIds;

    protected override void Confirm()
    {
        NewIdEntry = NewIdEntry.Replace(" ", "");
        if (string.IsNullOrEmpty(NewIdEntry) || NewIdEntry.Length < 7)
        {
            MessageBoxResult result = ShowErrorMessageBox("ESD Name must be at least 7 characters long.");
            if (result == MessageBoxResult.OK) return;
        }
        if (LocalESDIds.Contains(NewIdEntry))
        {
            MessageBoxResult result = ShowErrorMessageBox("Another ESD with the same ID already exists in this map.");
            if (result == MessageBoxResult.OK) return;
        }
        DialogResult = true;
    }

}