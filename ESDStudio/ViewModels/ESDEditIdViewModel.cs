using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;

namespace ESDStudio.ViewModels;

public class EditESDIdViewModel : DialogViewModelBase
{
    public EditESDIdViewModel(int currentId, List<int> localESDIds)
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

    public List<int> LocalESDIds;

    protected override void Confirm()
    {
        NewIdEntry = NewIdEntry.Replace(" ", "");
        if (LocalESDIds.Contains(int.Parse(NewIdEntry)))
        {
            MessageBoxResult result = ShowErrorMessageBox("Another ESD with the same ID already exists in this map.");
            if (result == MessageBoxResult.OK) return;
        }
        DialogResult = true;
    }

}