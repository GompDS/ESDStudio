using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Tomlyn;
using Tomlyn.Model;

namespace ESDStudio;

public class GameInfo
{
    public enum Game
    {
        DarkSoulsIII
    }

    public Game Type;
    public string Name = "Unknown";
    public string FilePathStart = "";
    public Dictionary<string, string> MapDescriptions = new();
    public Dictionary<string, Dictionary<int, string>> ESDDescriptions = new();

    public GameInfo(string text)
    {
        if (text.EndsWith("DarkSoulsIII.exe", StringComparison.OrdinalIgnoreCase) ||
            text.Equals("ds3", StringComparison.OrdinalIgnoreCase))
        {
            Type = Game.DarkSoulsIII;
            Name = "ds3";
            FilePathStart = @"N:\FDP\data\INTERROOT_win64";
        }
        ReadDefaultMapDescriptions();
        ReadDefaultESDDescriptions();
    }

    private void ReadDefaultMapDescriptions()
    {
        string tomlPath = AppDomain.CurrentDomain.BaseDirectory + $"DefaultDescriptions\\DefaultMapDescriptions_{Name}.toml";
        MapDescriptions = ProjectUtils.ReadMapDescriptions(tomlPath);
    }

    private void ReadDefaultESDDescriptions()
    {
        string tomlPath = AppDomain.CurrentDomain.BaseDirectory + $"DefaultDescriptions\\DefaultESDDescriptions_{Name}.toml";
        ESDDescriptions = ProjectUtils.ReadESDDescriptions(tomlPath);
    }

    public override string ToString()
    {
        return Name;
    }
}