using System.Linq;
using ESDStudio.ViewModels;

namespace ESDStudio.Commands;

public class EditBNDDescriptionCommand : CommandBase
{
    public EditBNDDescriptionCommand(BNDViewModel bnd, string newDescription)
    {
        _bnd = bnd;
        _oldDescription = _bnd.Description;
        _newDescription = newDescription;
    }
    
    private BNDViewModel _bnd;
    private string _oldDescription;
    private string _newDescription;
    
    public override void Redo()
    {
        _bnd.Description = _newDescription;
        if (Project.Current.MapDescriptions.Keys.Contains(_bnd.Name))
        {
            Project.Current.Game.MapDescriptions.TryGetValue(_bnd.Name, out string? defaultDescription);
            if ((_bnd.Description.Length > 0 || Project.Current.Game.MapDescriptions.Keys.Contains(_bnd.Name))
                && _bnd.Description != defaultDescription)
            {
                Project.Current.MapDescriptions[_bnd.Name] = _bnd.Description;
            }
            else
            {
                Project.Current.MapDescriptions.Remove(_bnd.Name);
            }

            _bnd.DescriptionEditCount++;
        }
        else
        {
            Project.Current.Game.MapDescriptions.TryGetValue(_bnd.Name, out string? defaultDescription);
            if (_bnd.Description != defaultDescription)
            {
                Project.Current.MapDescriptions.Add(_bnd.Name, _bnd.Description);
                _bnd.DescriptionEditCount++;
            }
        }
    }

    public override void Undo()
    {
        _bnd.Description = _oldDescription;
        if (Project.Current.MapDescriptions.Keys.Contains(_bnd.Name))
        {
            Project.Current.Game.MapDescriptions.TryGetValue(_bnd.Name, out string? defaultDescription);
            if (_oldDescription.Length > 0 || Project.Current.Game.MapDescriptions.Keys.Contains(_bnd.Name)
                && _oldDescription != defaultDescription)
            {
                Project.Current.MapDescriptions[_bnd.Name] = _oldDescription;
            }
            else
            {
                Project.Current.MapDescriptions.Remove(_bnd.Name);
            }

            _bnd.DescriptionEditCount--;
        }
        /*else
        {
            Project.Current.Game.MapDescriptions.TryGetValue(_bnd.Name, out string? defaultDescription);
            if (_oldDescription != defaultDescription)
            {
                Project.Current.MapDescriptions.Add(_bnd.Name, _oldDescription);
                _bnd.DescriptionEditCount++;
            }
        }*/
    }
}