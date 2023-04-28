﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Tomlyn;
using Tomlyn.Model;

namespace ESDStudio;

public static class ProjectUtils
{
    public static Dictionary<string, string> ReadMapDescriptions(string path)
    {
        Dictionary<string, string> mapDescriptions = new();
        string tomlPath = path;
        TomlTable tomlModel = Toml.ToModel(File.ReadAllText(tomlPath), tomlPath);
        tomlModel.TryGetValue("maps", out object obj);
        TomlTable maps = (TomlTable) obj;
        foreach (string mapName in maps.Keys)
        {
            mapDescriptions.Add(mapName, (string) maps[mapName]);
        }

        return mapDescriptions;
    }
    
    public static Dictionary<string, Dictionary<int, string>> ReadESDDescriptions(string path)
    {
        Dictionary<string, Dictionary<int, string>> esdDescriptions = new();
        string tomlPath = path;
        TomlTable tomlModel = Toml.ToModel(File.ReadAllText(tomlPath), tomlPath);
        tomlModel.TryGetValue("maps", out object obj);
        TomlTable maps = (TomlTable) obj;
        foreach (string mapName in maps.Keys)
        {
            TomlTable map = (TomlTable) maps[mapName];
            Dictionary<int, string> esdDictionary = new();
            foreach (int id in map.Keys.Select(int.Parse))
            {
                esdDictionary.Add(id, (string) map[id.ToString()]);
            }
            esdDescriptions.Add(mapName, esdDictionary);
        }

        return esdDescriptions;
    }

    public static void WriteMapDescriptions(Dictionary<string, string> mapDescriptions, string title, string path)
    {
        TomlTable tomlModel = new() { { "title", title } };
        TomlTable maps = new();
        foreach (string mapName in mapDescriptions.Keys)
        {
            maps.Add(mapName, mapDescriptions[mapName]);
        }
        tomlModel.Add("maps", maps);
        File.WriteAllText(path, Toml.FromModel(tomlModel));
    }
    
    public static void WriteESDDescriptions(Dictionary<string, Dictionary<int, string>> esdDescriptions, string title, string path)
    {
        TomlTable tomlModel = new() { { "title", title } };
        TomlTable maps = new();
        foreach (string mapName in esdDescriptions.Keys)
        {
            TomlTable map = new();
            foreach (int esdId in esdDescriptions[mapName].Keys)
            {
                map.Add(esdId.ToString(), esdDescriptions[mapName][esdId]);
            }

            maps.Add(mapName, map);
        }
        tomlModel.Add("maps", maps);
        File.WriteAllText(path, Toml.FromModel(tomlModel));
    }
}