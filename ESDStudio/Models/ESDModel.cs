using System.Collections.Generic;
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
            return $"t{ParentBNDModel.MapId:D2}{ParentBNDModel.BlockId:D1}{Id:D3}";
        }
    }

    public string? OriginalName = null;

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
                if (ProjectData.ESDDescriptionsContainsId(Id, ParentBNDModel.Name))
                {
                    if (_description.Length > 0 || ProjectData.Game.ESDDescriptionsContainsId(Id, ParentBNDModel.Name))
                    {
                        ProjectData.ESDDescriptions[ParentBNDModel.Name][Id] = _description;
                    }
                    else
                    {
                        ProjectData.ESDDescriptions[ParentBNDModel.Name].Remove(Id);
                        if (ProjectData.ESDDescriptions[ParentBNDModel.Name].Keys.Count == 0)
                        {
                            ProjectData.ESDDescriptions.Remove(ParentBNDModel.Name);
                        }
                    }
                    IsDescriptionEdited = true;
                }
                else
                {
                    ProjectData.Game.GetESDDescription(Id, ParentBNDModel.Name, out string? defaultDescription);
                    if (_description != defaultDescription)
                    {
                        if (ProjectData.ESDDescriptions.Keys.Contains(ParentBNDModel.Name) == false)
                        {
                            ProjectData.ESDDescriptions.Add(ParentBNDModel.Name, new Dictionary<int, string>());
                        }
                        ProjectData.ESDDescriptions[ParentBNDModel.Name].Add(Id, _description);
                        IsDescriptionEdited = true;
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
                
                ProjectData.GetESDDescription(value, ParentBNDModel.Name, out string? projectDescription);
                if (projectDescription != null)
                {
                    Description = projectDescription;
                }
                else
                {
                    ProjectData.Game.GetESDDescription(value, ParentBNDModel.Name, out string? defaultDescription);
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
    
    public bool IsDescriptionEdited { get; set; }
    public bool IsESDEdited { get; set; }
    public bool IsDecompiled { get; set; }

    public ESDModel(string esdName, BNDModel parent)
    {
        ParentBNDModel = parent;
        Id = int.Parse(esdName[4..7]);
        Code = new TextDocument();
        IsDescriptionEdited = false;
        IsESDEdited = false;
        IsDecompiled = false;
    }
    
    public ESDModel(string esdName, string codeText, BNDModel parent)
    {
        ParentBNDModel = parent;
        Id = int.Parse(esdName[4..7]);
        Code = new TextDocument(codeText);
        IsDescriptionEdited = false;
        IsESDEdited = false;
        IsDecompiled = true;
    }
    
    public ESDModel(int id, string description, BNDModel parent)
    {
        ParentBNDModel = parent;
        Code = new TextDocument($"# -*- coding: utf-8 -*-\r\ndef t{parent.MapId:D2}{parent.BlockId:D1}{id:D3}_1():\r\n" +
                                "    \"\"\"State 0,1\"\"\"\r\n    Quit()\r\n\r\n");
        IsDescriptionEdited = false;
        IsESDEdited = true;
        IsDecompiled = true;
        Id = id;
        Description = description;
    }
    
    public ESDModel(ESDModel CopiedESD, BNDModel parent)
    {
        ParentBNDModel = parent;
        Code = new TextDocument();
        IsDescriptionEdited = false;
        IsESDEdited = true;
        IsDecompiled = false;
        Id = CopiedESD.Id;
        Description = CopiedESD.Description;
    }
}