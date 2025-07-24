using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media;
using ESDLang.Doc;
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

    public ESDDocumentation.MethodDoc? parentFunction { get; set; }
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
            ESDDocumentation.MethodDoc? funcDef = Project.Current.Game.TalkMethods
                .FirstOrDefault(x => x.Name != null && x.Name == Text);
            if (funcDef != null)
            {
                TextEditor funcDescription = new()
                {
                    Style = Application.Current.FindResource("ReadOnlyEditor") as Style
                };
                funcDescription.Text += "(";
                int argCount = 0;
                foreach (ESDDocumentation.ArgDoc argDef in funcDef.Args)
                {
                    if (argDef.Optional)
                    {
                        funcDescription.Text += "*";
                    }

                    if (argDef.Enum != null)
                    {
                        funcDescription.Text += $"{argDef.Type}<{argDef.EnumName}> {argDef.Name}";
                    }
                    else
                    {
                        funcDescription.Text += $"{argDef.Type} {argDef.Name}";
                    }

                    if (argCount < funcDef.Args.Count - 1)
                    {
                        funcDescription.Text += ", ";
                    }

                    argCount++;
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

    public double Priority { get; set; }

    public void Complete(TextArea textArea, ISegment completionSegment,
        EventArgs insertionRequestEventArgs)
    {
        TextSegment seg = new TextSegment();
        seg.Length = LengthToRemove;
        seg.StartOffset = InsertionOffset;
        seg.EndOffset = seg.StartOffset;
        textArea.Document.Remove(InsertionOffset, LengthToRemove);
        ESDDocumentation.MethodDoc? funcDef = Project.Current.Game.TalkMethods.FirstOrDefault(x => x.Name != null && x.Name == Text);
        if (funcDef != null)
        {
            Text += "()";
        }
        textArea.Document.Replace(seg, Text);
        if (funcDef == null) return;
        if (funcDef.Args.Count > 0)
        {
            textArea.Caret.Offset -= 1;
        }
    }
}