using System.Collections.Generic;
using System.IO;
using System.Linq;
using ESDStudio.ViewModels;
using SoulsFormats;

namespace ESDStudio.Commands.Main;

public class SaveBNDCommand : CommandBase
{
    public SaveBNDCommand(BNDViewModel bnd)
    {
        _bnd = bnd;
        _bndDescriptionEditCount = _bnd.DescriptionEditCount;
        _esdEditCounts = _bnd.ESDViewModels.Select(x => x.ESDEditCount).ToList();
        _esdDescriptionEditCounts = _bnd.ESDViewModels.Select(x => x.DescriptionEditCount).ToList();
        _lastSavedESDCount = _bnd.LastSavedESDCount;
    }
    
    private BNDViewModel _bnd;
    private int _bndDescriptionEditCount;
    private List<int> _esdEditCounts;
    private List<int> _esdDescriptionEditCounts;
    private int _lastSavedESDCount;

    public override void Redo()
    {
        if (_bnd.IsDescriptionEdited)
        {
            _bnd.DescriptionEditCount = 0;
        
            ProjectUtils.WriteMapDescriptions(Project.Current.MapDescriptions, "MapDescriptions",
                Project.Current.BaseDirectory + @"\MapDescriptions.toml");
        }
        
        foreach (ESDViewModel esd in _bnd.ESDViewModels.Where(x => x.IsESDEdited))
        {
            esd.SaveCommand.Execute(null);
        }
        
        IBinder bnd = _bnd.GetTalkBND(Project.Current.ModDirectory, Project.Current.GameDirectory, out string BNDPath);
        if (!File.Exists(BNDPath + ".bak"))
        {
            string writeBakPath = $"{Project.Current.ModDirectory}\\{Project.Current.Game.TalkPath}\\{_bnd.Name}.talkesdbnd";
            if (Project.Current.Game.Compression is not DCX.NoCompressionInfo)
            {
                writeBakPath += ".dcx";
            }

            writeBakPath += ".bak";
            if (Project.Current.Game.BNDVersion == GameInfo.BNDType.BND3)
            {
                ((BND3)bnd).Write(writeBakPath);
            }
            else
            {
                ((BND4)bnd).Write(writeBakPath);
            }
        }
        bnd.Files = bnd.Files.Where(x => 
            _bnd.ESDViewModels.Any(y => y.Id == Path.GetFileNameWithoutExtension(x.Name))).ToList();
        
        string writePath = $"{Project.Current.ModDirectory}\\{Project.Current.Game.TalkPath}\\{_bnd.Name}.talkesdbnd";
        if (Project.Current.Game.Compression is not DCX.NoCompressionInfo)
        {
            writePath += ".dcx";
        }
        if (Project.Current.Game.BNDVersion == GameInfo.BNDType.BND3)
        {
            ((BND3)bnd).Write(writePath);
        }
        else
        {
            ((BND4)bnd).Write(writePath);
        }
        _bnd.LastSavedESDCount = _bnd.ESDViewModels.Count;
        _bnd.UpdateIsBNDEdited();
    }

    public override void Undo()
    {
        _bnd.DescriptionEditCount = _bndDescriptionEditCount;
        for (int i = 0; i < _bnd.ESDViewModels.Count; i++)
        {
            _bnd.ESDViewModels[i].ESDEditCount = _esdEditCounts[i];
            _bnd.ESDViewModels[i].DescriptionEditCount = _esdDescriptionEditCounts[i];
        }

        //_bnd.LastSavedESDCount = _lastSavedESDCount;
        _bnd.UpdateIsBNDEdited();
    }
}