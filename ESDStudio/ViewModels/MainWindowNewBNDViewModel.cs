using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace ESDStudio.ViewModels;

public class MainWindowNewBNDViewModel : DialogViewModelBase
{
    public MainWindowNewBNDViewModel(GameInfo game, List<string> BNDNames)
    {
        _mapDescriptions = game.MapDescriptions;
        _bndNames = BNDNames;
    }

    private Dictionary<string, string> _mapDescriptions;
    private List<string> _bndNames;
    
    private string _mapIdEntry = "10";
    public string MapIdEntry
    {
        get => _mapIdEntry;
        set
        {
            if (_mapIdEntry != value)
            {
                _mapIdEntry = value;
                OnPropertyChanged();
                int mapId = int.Parse(_mapIdEntry);
                int blockId = int.Parse(_blockIdEntry);
                _mapDescriptions.TryGetValue($"m{mapId:D2}_{blockId:D2}_00_00", out string? mapName);
                if (mapName != null)
                {
                    DescriptionEntry = mapName;
                }
                else
                {
                    DescriptionEntry = "";
                }
            }
        }
    }
    
    private string _blockIdEntry = "0";
    public string BlockIdEntry
    {
        get => _blockIdEntry;
        set
        {
            if (_blockIdEntry != value)
            {
                _blockIdEntry = value;
                OnPropertyChanged();
                int mapId = int.Parse(_mapIdEntry);
                int blockId = int.Parse(_blockIdEntry);
                _mapDescriptions.TryGetValue($"m{mapId:D2}_{blockId:D2}_00_00", out string? mapName);
                if (mapName != null)
                {
                    DescriptionEntry = mapName;
                }
                else
                {
                    DescriptionEntry = "";
                }
            }
        }
    }
    
    private string _descriptionEntry = "";
    public string DescriptionEntry
    {
        get => _descriptionEntry;
        set
        {
            if (_descriptionEntry != value)
            {
                _descriptionEntry = value;
                OnPropertyChanged();
            }
        }
    }

    protected override void Confirm()
    {
        MapIdEntry = MapIdEntry.Replace(" ", "");
        BlockIdEntry = BlockIdEntry.Replace(" ", "");
        if (MapIdEntry.Length == 0)
        {
            MessageBoxResult result = ShowErrorMessageBox("Map ID must be at least 1 digit.");
            if (result == MessageBoxResult.OK) return;
        }
        if (BlockIdEntry.Length == 0)
        {
            MessageBoxResult result = ShowErrorMessageBox("Block ID must be at least 1 digit.");
            if (result == MessageBoxResult.OK) return;
        }

        int mapId = int.Parse(MapIdEntry);
        int blockId = int.Parse(BlockIdEntry);
        if (_bndNames.Any(x => x.Equals($"m{mapId:D2}_{blockId:D2}_00_00")))
        {
            MessageBoxResult result = ShowErrorMessageBox("A BND with this map and block ID already exists.");
            if (result == MessageBoxResult.OK) return;
        }
        DialogResult = true;
    }
}