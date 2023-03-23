using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using SoulsFormats;

namespace ESDStudio.Models;

public class ESDModel
{
    public string Name { get; }
    public string Description { get; }
    public int Id { get; }
    public string Code { get; }
    public BNDModel ParentBNDModel { get; }
    public bool IsEdited { get; }
    public bool IsDecompiled { get; }

    public ESDModel(BinderFile BNDFile, BNDModel parent, GameInfo gameInfo)
    {
        Name = Path.GetFileNameWithoutExtension(BNDFile.Name);
        Id = int.Parse(Name[4..7]);
        Description = "";
        gameInfo.ESDDescriptions.TryGetValue($"m{parent.MapId:D2}_{parent.BlockId:D2}_00_00",
            out Dictionary<int, string>? mapDictionary);
        if (mapDictionary != null)
        {
            mapDictionary.TryGetValue(Id, out string? description);
            if (description != null)
            {
                Description = description;
            }
        }
        Code = "";
        ParentBNDModel = parent;
        IsEdited = false;
        IsDecompiled = false;
    }
}