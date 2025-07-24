using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.DirectoryServices.ActiveDirectory;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using ESDLang.Doc;
using ESDStudio.ViewModels;
using Tomlyn;
using Tomlyn.Model;

namespace ESDStudio;

public class Project
{
    public string Name { get; }
    public string BaseDirectory { get; }
    public string GameDirectory { get; }
    public string ModDirectory { get; }
    public GameInfo Game { get; }
    public Dictionary<string, string> MapDescriptions = new();
    public Dictionary<string, Dictionary<string, string>> ESDDescriptions = new();
    public static Project Current;

    public static bool IsProjectLoaded = false;

    public Project(string name, string baseDir, string gameDir, string modDir, string gameName)
    {
        Name = name;
        BaseDirectory = baseDir;
        GameDirectory = gameDir;
        ModDirectory = modDir;
        Game = new GameInfo(gameName);
        if (File.Exists(BaseDirectory + @"\MapDescriptions.toml"))
        {
            MapDescriptions = ProjectUtils.ReadMapDescriptions(BaseDirectory + @"\MapDescriptions.toml");
        }
        if (File.Exists(BaseDirectory + @"\ESDDescriptions.toml"))
        {
            ESDDescriptions = ProjectUtils.ReadESDDescriptions(BaseDirectory + @"\ESDDescriptions.toml");
        }
    }

    public Project()
    {
        Name = "";
        BaseDirectory = "";
        GameDirectory = "";
        ModDirectory = "";
        Game = new GameInfo("");
    }

    public bool ESDDescriptionsContainsId(string esdId, string mapName)
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
    
    public void GetESDDescription(string esdId, string mapName, out string? description)
    {
        description = null;
        ESDDescriptions.TryGetValue(mapName, out Dictionary<string, string>? esds);
        if (esds != null)
        {
            esds.TryGetValue(esdId, out description);
        }
    }

    public static Project ReadFromToml(string tomlPath)
    {
        TomlTable model = Toml.ToModel(File.ReadAllText(tomlPath), tomlPath);
        model.TryGetValue("project_info", out object obj);
        TomlTable projectInfo = (TomlTable)obj;
        projectInfo.TryGetValue("name", out object nameObj);
        projectInfo.TryGetValue("base_directory", out object baseDirObj);
        projectInfo.TryGetValue("game_directory", out object gameDirObj);
        projectInfo.TryGetValue("mod_directory", out object modDirObj);
        projectInfo.TryGetValue("game", out object gameObj);
        Project newProject = new((string)nameObj, (string)baseDirObj, (string)gameDirObj,
            (string)modDirObj, (string)gameObj);
        return newProject;
    }

    public static void ReadESDDocs()
    {
        string cwd = AppDomain.CurrentDomain.BaseDirectory;
        //AIDoc = ESDDocumentation.DeserializeFromFile($"{cwd}\\esdtool\\dist\\ESDScriptingDocumentation_AI.json", new ESDDocumentation.DocOptions());
        //ChrDoc = ESDDocumentation.DeserializeFromFile($"{cwd}\\esdtool\\dist\\ESDScriptingDocumentation_Chr.json", new ESDDocumentation.DocOptions());
        //EventDoc = ESDDocumentation.DeserializeFromFile($"{cwd}\\esdtool\\dist\\ESDScriptingDocumentation_Event.json", new ESDDocumentation.DocOptions());
        //NoneDoc = ESDDocumentation.DeserializeFromFile($"{cwd}\\esdtool\\dist\\ESDScriptingDocumentation_None.json", new ESDDocumentation.DocOptions());
        //TalkDoc = ESDDocumentation.DeserializeFromFile($"{cwd}\\esdtool\\dist\\ESDScriptingDocumentation_Talk.json", new ESDDocumentation.DocOptions().);
    }
}