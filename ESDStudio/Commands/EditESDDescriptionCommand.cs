using ESDStudio.ViewModels;

namespace ESDStudio.Commands;

public class EditESDDescriptionCommand : CommandBase
{
    public EditESDDescriptionCommand(ESDViewModel esd, string newDescription)
    {
        _esd = esd;
        _oldDescription = _esd.Description;
        _newDescription = newDescription;
    }
    
    private ESDViewModel _esd;
    private string _oldDescription;
    private string _newDescription;
    
    public override void Redo()
    {
        _esd.Description = _newDescription;
        _esd.DescriptionEditCount++;
    }

    public override void Undo()
    {
        _esd.Description = _oldDescription;
        _esd.DescriptionEditCount--;
    }
}