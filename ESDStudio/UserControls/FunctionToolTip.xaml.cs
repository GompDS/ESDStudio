using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ESDLang.Doc;
using SoulsFormats;
using Label = System.Windows.Controls.Label;
using UserControl = System.Windows.Controls.UserControl;

namespace ESDStudio.UserControls;

public partial class FunctionToolTip : UserControl
{
    public FunctionToolTip(ESDDocumentation.MethodDoc funcDef, int parameterIndex = -1)
    {
        InitializeComponent();

        if (parameterIndex == -1)
        {
            if (funcDef.ReturnValue != null)
            {
                textEditor.Text = $"{funcDef.ReturnValue.Type}";
                if (funcDef.ReturnValue.Enum != null)
                {
                    textEditor.Text += $"<{funcDef.ReturnValue.EnumName}>";
                }
            }
            else
            {
                textEditor.Text = "void";
            }
            textEditor.Text += " ";
            textEditor.Text += $"{funcDef.Name}";
            textEditor.Text += "(";
        }
        int argCount = 0;
        int indent = textEditor.Text.Length;
        int maxLineLength = 100;
        int currentLineLength = indent;
        int textLengthBeforeAdditions = textEditor.Text.Length;
        foreach (ESDDocumentation.ArgDoc arg in funcDef.Args)
        {
            
            if (currentLineLength > maxLineLength)
            {
                textEditor.Text += "\n";
                for (int i = 0; i < indent; i++)
                {
                    textEditor.Text += " ";
                }
                
                currentLineLength = indent;
            }

            if (parameterIndex == argCount)
            {
                textEditor.Text += "\u2B9A";
            }

            if (arg.Optional)
            {
                textEditor.Text += "*";
            }

            if (arg.Enum != null)
            {
                textEditor.Text += $"{arg.Type}<{arg.EnumName}> {arg.Name}";
            }
            else
            {
                textEditor.Text += $"{arg.Type} {arg.Name}";
            }

            if (argCount < funcDef.Args.Count - 1)
            {
                textEditor.Text += ", ";
            }

            currentLineLength += Math.Abs(textEditor.Text.Length - textLengthBeforeAdditions);
            textLengthBeforeAdditions = textEditor.Text.Length;
            argCount++;
        }

        if (parameterIndex == -1)
        {
            textEditor.Text += ")";
        }
        /*if (funcDef.ReturnValue != null)
        {
            textEditor.Text += "\nreturns ";
            textEditor.Text += $"{funcDef.ReturnValue.Type}";
            if (funcDef.ReturnValue.IsEnum)
            {
                textEditor.Text += $"<{funcDef.ReturnValue.EnumType}>";
            }
        }*/
    }
}