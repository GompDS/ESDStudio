using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using SoulsFormats;

namespace ESDStudio.Models;

public class BNDModel
{
    public string Name { get; }
    public string Description { get; }
    public int MapId { get; }
    public int BlockId { get; }
    public ObservableCollection<ESDModel> ESDModels { get; }

    public bool IsEdited
    {
        get
        {
            return ESDModels.Any(x => x.IsEdited);
        }
    }
    
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

        BND4 BND = BND4.Read(BNDPath);
        foreach (BinderFile BNDFile in BND.Files.OrderBy(x => x.Name))
        {
            ESDModel newESDModel = new(BNDFile, this, gameInfo);
            ESDModels.Add(newESDModel);
        }
    }
}