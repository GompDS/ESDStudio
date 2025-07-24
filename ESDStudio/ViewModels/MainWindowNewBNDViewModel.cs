using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Org.BouncyCastle.Bcpg.OpenPgp;

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
                UpdateDescriptionBasedOnID();
            }
        }
    }
    
    private string _blockAEntry = "0";
    public string BlockAEntry
    {
        get => _blockAEntry;
        set
        {
            if (_blockAEntry != value)
            {
                _blockAEntry = value;
                OnPropertyChanged();
                UpdateDescriptionBasedOnID();
            }
        }
    }
    
    private string _blockBEntry = "0";
    public string BlockBEntry
    {
        get => _blockBEntry;
        set
        {
            if (_blockBEntry != value)
            {
                _blockBEntry = value;
                OnPropertyChanged();
                UpdateDescriptionBasedOnID();
            }
        }
    }
    
    private string _blockCEntry = "0";
    public string BlockCEntry
    {
        get => _blockCEntry;
        set
        {
            if (_blockCEntry != value)
            {
                _blockCEntry = value;
                OnPropertyChanged();
                UpdateDescriptionBasedOnID();
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

    private void UpdateDescriptionBasedOnID()
    {
        int mapId = int.Parse(_mapIdEntry);
        int blockA = int.Parse(_blockAEntry);
        int blockB = int.Parse(_blockBEntry);
        int blockC = int.Parse(_blockCEntry);
        _mapDescriptions.TryGetValue($"m{mapId:D2}_{blockA:D2}_{blockB:D2}_{blockC:D2}", out string? mapName);
        if (mapName != null)
        {
            DescriptionEntry = mapName;
        }
        else
        {
            DescriptionEntry = "";
        }
    }

    protected override void Confirm()
    {
        MapIdEntry = MapIdEntry.Replace(" ", "");
        BlockAEntry = BlockAEntry.Replace(" ", "");
        BlockBEntry = BlockBEntry.Replace(" ", "");
        BlockCEntry = BlockCEntry.Replace(" ", "");
        if (MapIdEntry.Length == 0)
        {
            MessageBoxResult result = ShowErrorMessageBox("Map ID must be at least 1 digit.");
            if (result == MessageBoxResult.OK) return;
        }
        if (BlockAEntry.Length == 0 || BlockBEntry.Length == 0 || BlockCEntry.Length == 0)
        {
            MessageBoxResult result = ShowErrorMessageBox("Block ID must be at least 1 digit.");
            if (result == MessageBoxResult.OK) return;
        }

        int mapId = int.Parse(MapIdEntry);
        int blockA = int.Parse(BlockAEntry);
        int blockB = int.Parse(BlockBEntry);
        int blockC = int.Parse(BlockCEntry);
        if (_bndNames.Any(x => x.Equals($"m{mapId:D2}_{blockA:D2}_{blockB:D2}_{blockC:D2}")))
        {
            MessageBoxResult result = ShowErrorMessageBox("A BND with this map and block ID already exists.");
            if (result == MessageBoxResult.OK) return;
        }
        DialogResult = true;
    }
}