using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;

namespace ESDStudio.ViewModels;

public class BNDNewESDViewModel : DialogViewModelBase
{
    public BNDNewESDViewModel(BNDViewModel bnd)
    {
        _bnd = bnd;
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
                ProjectData.GetESDDescription(int.Parse(value), _bnd.Name, out string? projectDescription);
                if (projectDescription != null)
                {
                    DescriptionEntry = projectDescription;
                }
                else
                {
                    ProjectData.Game.GetESDDescription(int.Parse(value), _bnd.Name, out string? defaultDescription);
                    if (defaultDescription != null)
                    {
                        DescriptionEntry = defaultDescription;
                    }
                    else
                    {
                        DescriptionEntry = "";
                    }
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

    private BNDViewModel _bnd;

    protected override void Confirm()
    {
        IdEntry = IdEntry.Replace(" ", "");

        if (IdEntry.Length == 0)
        {
            MessageBoxResult result = ShowErrorMessageBox("ID must be at least 1 digit.");
            if (result == MessageBoxResult.OK) return;
        }
        
        if (_bnd.ESDViewModels.Select(x => x.Id).Contains(int.Parse(IdEntry)))
        {
            MessageBoxResult result = ShowErrorMessageBox("Another ESD with the same ID already exists in this map.");
            if (result == MessageBoxResult.OK) return;
        }
        DialogResult = true;
    }

}