using System.Collections.Generic;
using System.Linq;

namespace ESDStudio;

public static class ProjectData
{
    public static string Name = "";
    public static string BaseDirectory = "";
    public static string GameDirectory = "";
    public static string ModDirectory = "";
    public static GameInfo? Game = null;
    public static Dictionary<string, string> MapDescriptions = new();
    public static Dictionary<string, Dictionary<int, string>> ESDDescriptions = new();
    
    public static bool ESDDescriptionsContainsId(int esdId, string mapName)
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
    public static void GetESDDescription(int esdId, string mapName, out string? description)
    {
        description = null;
        ESDDescriptions.TryGetValue(mapName, out Dictionary<int, string>? esds);
        if (esds != null)
        {
            esds.TryGetValue(esdId, out description);
        }
    }
}