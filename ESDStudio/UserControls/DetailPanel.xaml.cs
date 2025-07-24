using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml;
using ESDLang.Doc;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Highlighting;

namespace ESDStudio.UserControls;

public partial class DetailPanel : UserControl
{
    public DetailPanel()
    {
        InitializeComponent();
    }

    public void SetFunctionToDisplay(ESDDocumentation.MethodDoc funcDef, int argIndex)
    {
        textEditor.Text = $"{funcDef.Name}()\n";
        foreach (ESDDocumentation.ArgDoc arg in funcDef.Args)
        {
            if (funcDef.Args.Count > argIndex && argIndex > -1)
            {
                if (funcDef.Args[argIndex] == arg)
                {
                    textEditor.Text += "\u2B9A";
                }
                else
                {
                    textEditor.Text += " ";
                }
            }
            else
            {
                textEditor.Text += " ";
            }

            if (arg.Optional)
            {
                textEditor.Text += "*";
            }
            else
            {
                textEditor.Text += " ";
            }
            textEditor.Text += $"{arg.Type} {arg.Name}";
            if (arg.Enum != null)
            {
                foreach (KeyValuePair<int, string> enumValue in arg.Enum.Names)
                {
                    textEditor.Text += $"\n      {enumValue.Key}: {arg.EnumName}.{enumValue.Value}";
                }
            }

            if (arg.Comment != null)
            {
                textEditor.Text += $" # {arg.Comment}";
            }

            textEditor.Text += "\n";
        }
        
        if (funcDef.ReturnValue != null)
        {
            textEditor.Text += "\nreturns ";
            textEditor.Text += $"{funcDef.ReturnValue.Type}";
            if (funcDef.ReturnValue.Enum != null)
            {
                textEditor.Text += $" {funcDef.ReturnValue.Name}\n";
                foreach (ESDDocumentation.EnumEntryDoc enumValue in funcDef.ReturnValue.Enum.Entries)
                {
                    textEditor.Text += $"            {enumValue.Value}: {funcDef.ReturnValue.EnumName}.{enumValue.Name}\n";
                }
            }
            else
            {
                textEditor.Text += $" {funcDef.ReturnValue.Name}";
            }
        }
    }

    public void ClearDisplay()
    {
        textEditor.Text = "";
    }
}