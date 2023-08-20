using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using ICSharpCode.AvalonEdit.Document;
using SoulsFormats;

namespace ESDStudio.Models;

public class BNDModel
{
    public string Name { get; set; }
    public string Description { get; set; }
    public int MapId { get; }
    public int BlockId { get; }
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
        BlockId = int.Parse(Name[4..6]);
        
        ESDModels = new ObservableCollection<ESDModel>();

        BND4 BND = BND4.Read(BNDPath);
        foreach (BinderFile BNDFile in BND.Files.OrderBy(x => x.Name))
        {
            string esdName = Path.GetFileNameWithoutExtension(BNDFile.Name);
            string fileName = $"{Project.Current.BaseDirectory}\\{Name}\\{esdName}.py";
            ESDModel newESDModel;
            if (File.Exists(fileName))
            {
                newESDModel = new(esdName, File.ReadAllText(fileName), this);
            }
            else
            {
                newESDModel = new(esdName, this);
            }
            ESDModels.Add(newESDModel);
        }

        LastSavedESDCount = ESDModels.Count;
    }
    
    public BNDModel(int mapId, int blockId, string description, GameInfo gameInfo)
    {
        MapId = mapId;
        BlockId = blockId;
        Name = $"m{MapId:D2}_{BlockId:D2}_00_00";
        Description = description;
        ESDModels = new ObservableCollection<ESDModel>();
        LastSavedESDCount = 0;
    }
}