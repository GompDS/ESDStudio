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

    public void SetFunctionToDisplay(FunctionDefinition funcDef)
    {
        textEditor.Text = $"{funcDef.Name}()\n";
        foreach (FunctionParameter parameter in funcDef.Parameters)
        {
            textEditor.Text += $"  {parameter.Type} {parameter.Name}\n";
            if (parameter.IsEnum)
            {
                foreach (Tuple<int, string> enumValue in XmlData.EnumTemplates[parameter.EnumType])
                {
                    textEditor.Text += $"      {enumValue.Item1}: {parameter.EnumType}.{enumValue.Item2}\n";
                }
            }
        }
        
        if (funcDef.ReturnValue != null)
        {
            textEditor.Text += "\nreturns ";
            textEditor.Text += $"{funcDef.ReturnValue.Type}";
            if (funcDef.ReturnValue.IsEnum)
            {
                textEditor.Text += $" {funcDef.ReturnValue.Name}\n";
                foreach (Tuple<int, string> enumValue in XmlData.EnumTemplates[funcDef.ReturnValue.EnumType])
                {
                    textEditor.Text += $"            {enumValue.Item1}: {funcDef.ReturnValue.EnumType}.{enumValue.Item2}\n";
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