using System.Collections.Generic;
using System.DirectoryServices.ActiveDirectory;
using System.IO;
using System.Linq;
using System.Windows.Forms;
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
    public Dictionary<string, Dictionary<int, string>> ESDDescriptions = new();
    public static Project Current = new();
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
}