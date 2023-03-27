using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using SoulsFormats;

namespace ESDStudio.Models;

public class BNDModel
{
    public string Name { get; set; }
    public string Description { get; set; }
    public int MapId { get; }
    public int BlockId { get; }
    public ObservableCollection<ESDModel> ESDModels { get; }

    public bool IsDescriptionEdited { get; set; }
    public bool IsContentsEdited
    {
        get
        {
            return ESDModels.Any(x => x.IsESDEdited || x.IsDescriptionEdited);
        }
    }
    
    public Dictionary<int, string> ESDDescriptionDictionary { get; }
    
    public BNDModel(string BNDPath, GameInfo gameInfo)
    {
        Name = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(BNDPath));
        gameInfo.MapDescriptions.TryGetValue(Name, out string? mapName);
        if (mapName != null)
        {
            Description = mapName;
        }
        else
        {
            Description = "";
        }
        MapId = int.Parse(Name[1..3]);
        BlockId = int.Parse(Name[4..6]);
        ESDModels = new ObservableCollection<ESDModel>();

        gameInfo.ESDDescriptions.TryGetValue($"m{MapId:D2}_{BlockId:D2}_00_00",
            out Dictionary<int, string>? mapDictionary);
        if (mapDictionary != null)
        {
            ESDDescriptionDictionary = mapDictionary;
        }
        else
        {
            ESDDescriptionDictionary = new Dictionary<int, string>();
        }
        
        BND4 BND = BND4.Read(BNDPath);
        foreach (BinderFile BNDFile in BND.Files.OrderBy(x => x.Name))
        {
            ESDModel newESDModel = new(BNDFile, this);
            ESDModels.Add(newESDModel);
        }
    }
    
    public BNDModel(int mapId, int blockId, string description, GameInfo gameInfo)
    {
        MapId = mapId;
        BlockId = blockId;
        Name = $"m{MapId:D2}_{BlockId:D2}_00_00";
        Description = description;
        ESDModels = new ObservableCollection<ESDModel>();

        gameInfo.ESDDescriptions.TryGetValue(Name,
            out Dictionary<int, string>? mapDictionary);
        if (mapDictionary != null)
        {
            ESDDescriptionDictionary = mapDictionary;
        }
        else
        {
            ESDDescriptionDictionary = new Dictionary<int, string>();
        }
    }
}