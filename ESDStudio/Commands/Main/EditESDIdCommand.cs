using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using ESDStudio.ViewModels;

namespace ESDStudio.Commands.Main;

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
            _esd.Decompile();
        }
        _esd.Code.Text = _esd.Code.Text.ReplaceMatches(@"(?<=t)" + Regex.Escape(_esd.Id.ToString(new string('0', Project.Current.Game.IdLength))),
            _newId.ToString(new string('0', Project.Current.Game.IdLength)), true, false);
        _esd.Id = _newId;
        _esd.ParentViewModel.ESDViewModels = new ObservableCollection<ESDViewModel>(
            _esd.ParentViewModel.ESDViewModels.OrderBy(x => x.Id));
    }

    public override void Undo()
    {
        if (_wasDecompiled)
        {
            _esd.Code.Text = _esd.Code.Text.ReplaceMatches(@"(?<=t)" + Regex.Escape(_esd.Id.ToString(new string('0', Project.Current.Game.IdLength))),
                _oldId.ToString(new string('0', Project.Current.Game.IdLength)), true, false);
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