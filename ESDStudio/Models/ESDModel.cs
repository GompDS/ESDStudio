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
                if (ParentBNDModel.ESDDescriptionDictionary.Keys.Any(x => x == Id))
                {
                    if (_description.Length > 0)
                    {
                        if (ParentBNDModel.ESDDescriptionDictionary[Id] != _description)
                        {
                            ParentBNDModel.ESDDescriptionDictionary[Id] = _description;
                            IsDescriptionEdited = true;
                        }
                    }
                    else
                    {
                        ParentBNDModel.ESDDescriptionDictionary.Remove(Id);
                    }
                }
                else if (_description.Length > 0)
                {
                    ParentBNDModel.ESDDescriptionDictionary.Add(Id, _description);
                    IsDescriptionEdited = true;
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
                ParentBNDModel.ESDDescriptionDictionary.TryGetValue(value, out string? description);
                if (description != null)
                {
                    Description = description;
                }
                else
                {
                    Description = "";
                }
            }
        }
    }
    public TextDocument Code { get; }
    public BNDModel ParentBNDModel { get; }
    
    public bool IsDescriptionEdited { get; set; }
    public bool IsESDEdited { get; set; }
    public bool IsDecompiled { get; set; }

    public ESDModel(BinderFile BNDFile, BNDModel parent)
    {
        ParentBNDModel = parent;
        string esdName = Path.GetFileNameWithoutExtension(BNDFile.Name);
        Id = int.Parse(esdName[4..7]);
        Code = new TextDocument();
        IsDescriptionEdited = false;
        IsESDEdited = false;
        IsDecompiled = false;
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