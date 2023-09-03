using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using SoulsFormats;
using Tomlyn;
using Tomlyn.Model;

namespace ESDStudio;

public class GameInfo
{
    public enum Game
    {
        DarkSouls,
        DarkSoulsRemastered,
        Bloodborne,
        DarkSoulsIII,
        Sekiro,
        EldenRing
    }
    
    public enum BNDType
    {
        BND3,
        BND4
    }

    public Game Type;
    public string Name = "Unknown";
    public string FilePathStart = "";
    public string TalkPath = "";
    public DCX.Type Compression = DCX.Type.None;
    public int IdLength = 0;
    public BNDType BNDVersion;
    public string Timestamp = "";
    public Dictionary<string, string> MapDescriptions = new();
    public Dictionary<string, Dictionary<int, string>> ESDDescriptions = new();
    public List<FunctionDefinition> FunctionDefinitions = new();
    public Dictionary<string, List<Tuple<int, string>>> EnumTemplates = new();
    public string IconPath { get; }

    public GameInfo(string text)
    {
        bool validGame = true;
        if (text.EndsWith("DARKSOULS.exe", StringComparison.OrdinalIgnoreCase) ||
            text.Equals("ds1", StringComparison.OrdinalIgnoreCase))
        {
            Type = Game.DarkSouls;
            Name = "ds1";
            FilePathStart = @"N:\FRPG\data\INTERROOT_win32";
            TalkPath = @"script\talk";
            Compression = DCX.Type.None;
            IdLength = 6;
            BNDVersion = BNDType.BND3;
            Timestamp = "07D7R6";
        }
        else if (text.EndsWith("DarkSoulsRemastered.exe", StringComparison.OrdinalIgnoreCase) ||
                 text.Equals("ds1r", StringComparison.OrdinalIgnoreCase))
        {
            Type = Game.DarkSoulsRemastered;
            Name = "ds1r";
            FilePathStart = @"N:\FRPG\data\INTERROOT_x64";
            TalkPath = @"script\talk";
            Compression = DCX.Type.DCX_DFLT_10000_24_9;
            IdLength = 6;
            BNDVersion = BNDType.BND3;
            Timestamp = "07D7R6";
        }
        else if (text.EndsWith("eboot.bin", StringComparison.OrdinalIgnoreCase) ||
            text.Equals("bb", StringComparison.OrdinalIgnoreCase))
        {
            Type = Game.Bloodborne;
            Name = "bb";
            FilePathStart = @"N:\SPRJ\data\INTERROOT_ps4";
            TalkPath = @"script\talk";
            Compression = DCX.Type.DCX_DFLT_10000_44_9;
            IdLength = 6;
            BNDVersion = BNDType.BND4;
            Timestamp = "07D7R6";
        }
        else if (text.EndsWith("DarkSoulsIII.exe", StringComparison.OrdinalIgnoreCase) ||
            text.Equals("ds3", StringComparison.OrdinalIgnoreCase))
        {
            Type = Game.DarkSoulsIII;
            Name = "ds3";
            FilePathStart = @"N:\FDP\data\INTERROOT_win64";
            TalkPath = @"script\talk";
            Compression = DCX.Type.DCX_DFLT_10000_44_9;
            IdLength = 6;
            BNDVersion = BNDType.BND4;
            Timestamp = "07D7R6";
        }
        else if (text.EndsWith("sekiro.exe", StringComparison.OrdinalIgnoreCase) ||
                 text.Equals("sdt", StringComparison.OrdinalIgnoreCase))
        {
            Type = Game.Sekiro;
            Name = "sdt";
            FilePathStart = @"N:\NTC\data\Target\INTERROOT_win64";
            TalkPath = @"script\talk";
            Compression = DCX.Type.DCX_KRAK;
            IdLength = 6;
            BNDVersion = BNDType.BND4;
            Timestamp = "07D7R6";
        }
        else if (text.EndsWith("eldenring.exe", StringComparison.OrdinalIgnoreCase) ||
                    text.Equals("er", StringComparison.OrdinalIgnoreCase))
        {
            Type = Game.EldenRing;
            Name = "er";
            FilePathStart = @"N:\GR\data\INTERROOT_win64";
            TalkPath = @"script\talk";
            Compression = DCX.Type.DCX_KRAK;
            IdLength = 9;
            BNDVersion = BNDType.BND4;
            Timestamp = "07D7R6";
        }
        else
        {
            validGame = false;
        }
        IconPath = $"{AppDomain.CurrentDomain.BaseDirectory}Content\\Images\\{Name}.jpg";
        if (validGame)
        {
            ReadFunctionDefXml();
            ReadDefaultMapDescriptions();
            ReadDefaultESDDescriptions();
        }
    }

    public static bool IsValidExecutable(string exePath)
    {
        return exePath.EndsWith("DARKSOULS.exe", StringComparison.OrdinalIgnoreCase) ||
               exePath.EndsWith("DarkSoulsRemastered.exe", StringComparison.OrdinalIgnoreCase) ||
               exePath.EndsWith("eboot.bin", StringComparison.OrdinalIgnoreCase) ||
               exePath.EndsWith("DarkSoulsIII.exe", StringComparison.OrdinalIgnoreCase) ||
               exePath.EndsWith("sekiro.exe", StringComparison.OrdinalIgnoreCase) ||
               exePath.EndsWith("eldenring.exe", StringComparison.OrdinalIgnoreCase);
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

    public bool ESDDescriptionsContainsId(int esdId, string mapName)
    {
        if (ESDDescriptions.Keys.Any(x => x == mapName))
        {
            if (ESDDescriptions[mapName].Keys.Any(x => x == esdId))
            {
                return true;
            }
        }

        return false;
    }

    public void GetESDDescription(int esdId, string mapName, out string? description)
    {
        description = null;
        ESDDescriptions.TryGetValue(mapName, out Dictionary<int, string>? esds);
        if (esds != null)
        {
            esds.TryGetValue(esdId, out description);
        }
    }
    
    public void ReadEnumTemplatesXml()
    {
        string cwd = AppDomain.CurrentDomain.BaseDirectory;
        XElement document = XElement.Load($"{cwd}\\Resources\\EnumTemplates_{Name}.xml");
        foreach (XElement e in document.Elements())
        {
            XAttribute? key = e.Attribute("key");
            if (key == null) continue;
            List<Tuple<int, string>> enumValues = new();
            foreach (XElement value in e.Elements())
            {
                XAttribute? index = value.Attribute("index");
                XAttribute? name = value.Attribute("name");
                if (index == null || name == null) continue;
                enumValues.Add(new Tuple<int, string>(int.Parse(index.Value), name.Value));
            }
            EnumTemplates.Add(key.Value, enumValues);
        }
        Console.WriteLine();
    }

    public void ReadFunctionDefXml()
    {
        ReadEnumTemplatesXml();
        string cwd = AppDomain.CurrentDomain.BaseDirectory;
        XElement element = XElement.Load($"{cwd}\\Resources\\FunctionDefinitions_{Name}.xml");
        foreach (XElement e in element.Elements())
        {
            XAttribute? funcName = e.Attribute("name");
            if (funcName == null) continue;
            FunctionDefinition funcDef = new(funcName.Value);
            XElement? parameterGroup = e.Element("Parameters");
            if (parameterGroup != null)
            {
                foreach (XElement parameter in parameterGroup.Elements())
                {
                    string type = "";
                    XAttribute? typeAttribute = parameter.Attribute("type");
                    if (typeAttribute != null) type = typeAttribute.Value;
                    string name = "";
                    XAttribute? nameAttribute = parameter.Attribute("name");
                    if (nameAttribute != null) name = nameAttribute.Value;
                    string enumType = "";
                    XAttribute? enumAttribute = parameter.Attribute("enum");
                    if (enumAttribute != null) enumType = enumAttribute.Value;
                    string comment = "";
                    XAttribute? commentAttribute = parameter.Attribute("comment");
                    if (commentAttribute != null) comment = commentAttribute.Value;
                    bool optional = false;
                    XAttribute? optionalAttribute = parameter.Attribute("optional");
                    if (optionalAttribute != null) optional = bool.Parse(optionalAttribute.Value);
                    funcDef.AddParameter(type, name, enumType, comment, optional);
                }
            }

            XElement? returnValue = e.Element("ReturnValue");
            if (returnValue != null)
            {
                string type = "";
                XAttribute? typeAttribute = returnValue.Attribute("type");
                if (typeAttribute != null) type = typeAttribute.Value;
                string name = "";
                XAttribute? nameAttribute = returnValue.Attribute("name");
                if (nameAttribute != null) name = nameAttribute.Value;
                string enumType = "";
                XAttribute? enumAttribute = returnValue.Attribute("enum");
                if (enumAttribute != null) enumType = enumAttribute.Value;
                string comment = "";
                XAttribute? commentAttribute = returnValue.Attribute("comment");
                if (commentAttribute != null) comment = commentAttribute.Value;
                funcDef.SetReturnValue(type, name, enumType, comment);
            }
            FunctionDefinitions.Add(funcDef);
        }
        Console.WriteLine();
    }
}