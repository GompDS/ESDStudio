﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using ESDStudio.ViewModels;
using ICSharpCode.AvalonEdit.Document;
using SoulsFormats;

namespace ESDStudio.Models;

public class ESDModel
{
    public string Name
    {
        get
        {
            return "t" + Id.ToString(new string('0', Project.Current.Game.IdLength));
        }
    }

    

    private string _description = "";
    public string Description
    {
        get
        {
            return _description;
        }
        set
        {
            if (_description.Equals(value) == false)
            {
                _description = value;
                if (Project.Current.ESDDescriptionsContainsId(Id, ParentBNDModel.Name))
                {
                    if (_description.Length > 0 || Project.Current.Game.ESDDescriptionsContainsId(Id, ParentBNDModel.Name))
                    {
                        Project.Current.ESDDescriptions[ParentBNDModel.Name][Id] = _description;
                    }
                    else
                    {
                        Project.Current.ESDDescriptions[ParentBNDModel.Name].Remove(Id);
                        if (Project.Current.ESDDescriptions[ParentBNDModel.Name].Keys.Count == 0)
                        {
                            Project.Current.ESDDescriptions.Remove(ParentBNDModel.Name);
                        }
                    }
                    //IsDescriptionEdited = true;
                }
                else
                {
                    Project.Current.Game.GetESDDescription(Id, ParentBNDModel.Name, out string? defaultDescription);
                    if (_description != defaultDescription)
                    {
                        if (Project.Current.ESDDescriptions.Keys.Contains(ParentBNDModel.Name) == false)
                        {
                            Project.Current.ESDDescriptions.Add(ParentBNDModel.Name, new Dictionary<int, string>());
                        }
                        Project.Current.ESDDescriptions[ParentBNDModel.Name].Add(Id, _description);
                        //IsDescriptionEdited = true;
                    }
                }
            }
        }
    }

    private int _id = -1;
    public int Id
    {
        get
        {
            return _id;
        }
        set
        {
            if (_id != value)
            {
                _id = value;
                
                Project.Current.GetESDDescription(value, ParentBNDModel.Name, out string? projectDescription);
                if (projectDescription != null)
                {
                    Description = projectDescription;
                }
                else
                {
                    Project.Current.Game.GetESDDescription(value, ParentBNDModel.Name, out string? defaultDescription);
                    if (defaultDescription != null)
                    {
                        Description = defaultDescription;
                    }
                    else
                    {
                        Description = "";
                    }
                }
            }
        }
    }
    public TextDocument Code { get; }
    public BNDModel ParentBNDModel { get; }
    public bool IsDescriptionEdited => DescriptionEditCount > 0;
    public int DescriptionEditCount { get; set; }
    public bool IsESDEdited => ESDEditCount > 0;
    public int ESDEditCount { get; set; }
    public bool IsDecompiled { get; set; }

    public ESDModel(string esdName, BNDModel parent)
    {
        ParentBNDModel = parent;
        Id = int.Parse(esdName.Substring(1, Project.Current.Game.IdLength));
        Code = new TextDocument();
        IsDecompiled = false;
    }
    
    public ESDModel(string esdName, string codeText, BNDModel parent)
    {
        ParentBNDModel = parent;
        Id = int.Parse(esdName.Substring(1, Project.Current.Game.IdLength));
        Code = new TextDocument(codeText);
        foreach (FunctionDefinition funcDef in Project.Current.Game.FunctionDefinitions.
                     Where(x => x.Parameters.Any(y => y.IsEnum || y.Type == "bool") ||
                                x.ReturnValue is { Type: "enum" or "bool" }))
        {
            Code.Text = funcDef.MakeNumberValuesDescriptive(Code.Text);
        }
        Code.Text = Code.Text.ReplaceMatches("    ", "\t", false, false);
        IsDecompiled = true;
    }
    
    public ESDModel(int id, string description, BNDModel parent)
    {
        ParentBNDModel = parent;
        Code = new TextDocument($"# -*- coding: utf-8 -*-\r\ndef t{id.ToString(new string('0', Project.Current.Game.IdLength))}_1():\r\n" +
                                "    \"\"\"State 0,1\"\"\"\r\n    Quit()\r\n\r\n");
        ESDEditCount++;
        IsDecompiled = true;
        Id = id;
        Description = description;
    }
    
    public ESDModel(ESDModel CopiedESD, BNDModel parent)
    {
        ParentBNDModel = parent;
        Code = new TextDocument();
        ESDEditCount++;
        IsDecompiled = false;
        Id = CopiedESD.Id;
        Description = CopiedESD.Description;
    }
}