using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using ESDStudio.ViewModels;

namespace ESDStudio.Commands;

public class EditESDIdCommand : CommandBase
{
    public EditESDIdCommand(ESDViewModel esd, int newId)
    {
        _esd = esd;
        _oldId = esd.Id;
        _newId = newId;
        _wasDecompiled = esd.IsDecompiled;
    }
    
    private ESDViewModel _esd;
    private int _oldId;
    private int _newId;
    private bool _wasDecompiled;
    public override void Redo()
    {
        if (_esd.IsDecompiled == false)
        {
            _esd.Decompile(Project.Current.ModDirectory, Project.Current.GameDirectory);
        }
        _esd.Code.Text = _esd.Code.Text.ReplaceMatches(@"(?<=t[0-9]{3})" + Regex.Escape($"{_esd.Id:D3}"),
            _newId.ToString("D3"), true, false);
        _esd.Id = _newId;
        _esd.ParentViewModel.ESDViewModels = new ObservableCollection<ESDViewModel>(
            _esd.ParentViewModel.ESDViewModels.OrderBy(x => x.Id));
    }

    public override void Undo()
    {
        if (_wasDecompiled)
        {
            _esd.Code.Text = _esd.Code.Text.ReplaceMatches(@"(?<=t[0-9]{3})" + Regex.Escape($"{_esd.Id:D3}"),
                _oldId.ToString("D3"), true, false);
        }
        else
        {
            _esd.Code.Text = "";
            _esd.IsDecompiled = false;
        }
        _esd.Id = _oldId;
        _esd.ParentViewModel.ESDViewModels = new ObservableCollection<ESDViewModel>(
            _esd.ParentViewModel.ESDViewModels.OrderBy(x => x.Id));
        _esd.ESDEditCount -= 2;
    }
}