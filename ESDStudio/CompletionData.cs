using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media;
using ESDStudio.UserControls;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Highlighting;
using Application = System.Windows.Application;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using Orientation = System.Windows.Controls.Orientation;

namespace ESDStudio;

/// Implements AvalonEdit ICompletionData interface to provide the entries in the
/// completion drop down.
public class CompletionData : ICompletionData
{
    public CompletionData(string completionText)
    {
        Text = completionText;
        Priority = 1.0;
        //InsertionOffset = insertionOffset;
        //LengthToRemove = enteredText.Length;
        //ParameterIndex = parameterIndex;
    }

    public FunctionDefinition? parentFunction { get; set; }
    public int ParameterIndex { get; set; }
    public int InsertionOffset { get; set; }
    public int LengthToRemove { get; set; }

    public ImageSource? Image 
    {
        get { return null; }
    }

    public string Text { get; private set; }
    
    public object Content 
    {
        get
        {
            return new CompletionDataContent(Text);
        }
    }

    public object? Description 
    {
        get
        {
            FunctionDefinition? funcDef = XmlData.FunctionDefinitions.FirstOrDefault(x => x.Name == Text);
            if (funcDef != null)
            {
                TextEditor funcDescription = new()
                {
                    Style = Application.Current.FindResource("ReadOnlyEditor") as Style
                };
                funcDescription.Text += "(";
                int parameterCount = 0;
                foreach (FunctionParameter parameter in funcDef.Parameters)
                {
                    if (parameter.IsOptional)
                    {
                        funcDescription.Text += "*";
                    }

                    if (parameter.IsEnum)
                    {
                        funcDescription.Text += $"{parameter.Type}<{parameter.EnumType}> {parameter.Name}";
                    }
                    else
                    {
                        funcDescription.Text += $"{parameter.Type} {parameter.Name}";
                    }

                    if (parameterCount < funcDef.Parameters.Count - 1)
                    {
                        funcDescription.Text += ", ";
                    }

                    parameterCount++;
                }
                funcDescription.Text += "):";
                if (funcDef.ReturnValue != null)
                {
                    funcDescription.Text += funcDef.ReturnValue.Type;
                }
                else
                {
                    funcDescription.Text += "void";
                }
                return funcDescription;
            }

            return null;
        }
    }

    public double Priority { get; }

    public void Complete(TextArea textArea, ISegment completionSegment,
        EventArgs insertionRequestEventArgs)
    {
        TextSegment seg = new TextSegment();
        seg.Length = LengthToRemove;
        seg.StartOffset = InsertionOffset;
        seg.EndOffset = seg.StartOffset;
        textArea.Document.Remove(InsertionOffset, LengthToRemove);
        FunctionDefinition? funcDef = XmlData.FunctionDefinitions.FirstOrDefault(x => x.Name == Text);
        if (funcDef != null)
        {
            Text += "()";
        }
        textArea.Document.Replace(seg, Text);
        if (funcDef == null) return;
        if (funcDef.Parameters.Count > 0)
        {
            textArea.Caret.Offset -= 1;
        }
    }
}