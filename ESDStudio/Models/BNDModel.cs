using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using ESDLang.EzSemble;
using ICSharpCode.AvalonEdit.Document;
using SoulsFormats;

namespace ESDStudio.Models;

public class BNDModel
{
    public string Name { get; set; }
    public string Description { get; set; }
    public int MapId { get; }
    public int BlockA { get; }
    public int BlockB { get; }
    public int BlockC { get; }
    public ObservableCollection<ESDModel> ESDModels { get; }
    public bool IsDescriptionEdited => DescriptionEditCount > 0;
    public int DescriptionEditCount { get; set; }
    public bool IsContentsEdited
    {
        get
        {
            return ESDModels.Any(x => x.IsESDEdited || x.IsDescriptionEdited);
        }
    }
    
    public int LastSavedESDCount { get; set; }

    public BNDModel(string BNDPath, GameInfo gameInfo)
    {
        Name = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(BNDPath));
        Project.Current.MapDescriptions.TryGetValue(Name, out string? projectDescription);
        if (projectDescription != null)
        {
            Description = projectDescription;
        }
        else
        {
            gameInfo.MapDescriptions.TryGetValue(Name, out string? defaultDescription);
            if (defaultDescription != null)
            {
                Description = defaultDescription;
            }
            else
            {
                Description = "";
            }
        }

        MapId = int.Parse(Name[1..3]);
        BlockA = int.Parse(Name[4..6]);
        BlockB = int.Parse(Name[7..9]);
        BlockC = int.Parse(Name[10..12]);
        
        ESDModels = new ObservableCollection<ESDModel>();

        IBinder BND;
        if (Project.Current.Game.BNDVersion == GameInfo.BNDType.BND3)
        {
            BND = BND3.Read(BNDPath);
        }
        else
        {
            BND = BND4.Read(BNDPath);
        }
        foreach (BinderFile BNDFile in BND.Files.OrderBy(x => x.Name))
        {
            ESD esd = ESD.Read(BNDFile.Bytes);
            //EzSembleContext context = EzSembleContext.LoadFromXml();
            //EzSembleContext.EzSembleMethodInfo info = GetCommandInfo()//GetCommandInfo(6, 2147483643);
            string esdName = Path.GetFileNameWithoutExtension(BNDFile.Name);
            //string fileName = $"{Project.Current.BaseDirectory}\\{Name}\\{esdName}.py";
            ESDModel newESDModel;
            /*if (File.Exists(fileName))
            {
                newESDModel = new(esdName, File.ReadAllText(fileName), this);
            }
            else
            {
                newESDModel = new(esdName, this);
            }*/
            newESDModel = new(esdName, this);
            ESDModels.Add(newESDModel);
        }

        LastSavedESDCount = ESDModels.Count;
    }
    
    public BNDModel(int mapId, int blockA, int blockB, int blockC, string description, GameInfo gameInfo)
    {
        MapId = mapId;
        BlockA = blockA;
        BlockB = blockB;
        BlockC = blockC;
        Name = $"m{MapId:D2}_{BlockA:D2}_{BlockB:D2}_{BlockC:D2}";
        Description = description;
        ESDModels = new ObservableCollection<ESDModel>();
        LastSavedESDCount = 0;
    }
}