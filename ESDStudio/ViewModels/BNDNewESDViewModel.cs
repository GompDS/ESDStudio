using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;

namespace ESDStudio.ViewModels;

public class BNDNewESDViewModel : DialogViewModelBase
{
    public BNDNewESDViewModel(Dictionary<int,string> descriptionDictionary, List<int> localESDIds)
    {
        _localESDIds = localESDIds;
        _descriptionDictionary = descriptionDictionary;
    }
    
    private string _idEntry = "";
    public string IdEntry
    {
        get => _idEntry;
        set
        {
            if (_idEntry != value)
            {
                _idEntry = value;
                OnPropertyChanged();
                _descriptionDictionary.TryGetValue(int.Parse(value), out string? description);
                if (description != null)
                {
                    DescriptionEntry = description;
                }
                else
                {
                    DescriptionEntry = "";
                }
            }
        }
    }
    
    private string _descriptionEntry = "";
    public string DescriptionEntry
    {
        get => _descriptionEntry;
        set
        {
            if (_descriptionEntry != value)
            {
                _descriptionEntry = value;
                OnPropertyChanged();
            }
        }
    }

    private List<int> _localESDIds;
    private Dictionary<int, string> _descriptionDictionary;

    protected override void Confirm()
    {
        IdEntry = IdEntry.Replace(" ", "");

        if (IdEntry.Length == 0)
        {
            MessageBoxResult result = ShowErrorMessageBox("ID must be at least 1 digit.");
            if (result == MessageBoxResult.OK) return;
        }
        
        if (_localESDIds.Contains(int.Parse(IdEntry)))
        {
            MessageBoxResult result = ShowErrorMessageBox("Another ESD with the same ID already exists in this map.");
            if (result == MessageBoxResult.OK) return;
        }
        DialogResult = true;
    }

}