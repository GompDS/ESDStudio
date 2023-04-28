using System.Collections.Generic;

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
}