using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SoulsFormats;
using Label = System.Windows.Controls.Label;
using UserControl = System.Windows.Controls.UserControl;

namespace ESDStudio.UserControls;

public partial class FunctionToolTip : UserControl
{
    public FunctionToolTip(FunctionDefinition funcDef, int parameterIndex = -1)
    {
        InitializeComponent();

        if (parameterIndex == -1)
        {
            if (funcDef.ReturnValue != null)
            {
                textEditor.Text = $"{funcDef.ReturnValue.Type}";
                if (funcDef.ReturnValue.IsEnum)
                {
                    textEditor.Text += $"<{funcDef.ReturnValue.EnumType}>";
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
        int parameterCount = 0;
        int indent = textEditor.Text.Length;
        int maxLineLength = 100;
        int currentLineLength = indent;
        int textLengthBeforeAdditions = textEditor.Text.Length;
        foreach (FunctionParameter parameter in funcDef.Parameters)
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

            if (parameterIndex == parameterCount)
            {
                textEditor.Text += "\u2B9A";
            }

            if (parameter.IsOptional)
            {
                textEditor.Text += "*";
            }

            if (parameter.IsEnum)
            {
                textEditor.Text += $"{parameter.Type}<{parameter.EnumType}> {parameter.Name}";
            }
            else
            {
                textEditor.Text += $"{parameter.Type} {parameter.Name}";
            }

            if (parameterCount < funcDef.Parameters.Count - 1)
            {
                textEditor.Text += ", ";
            }

            currentLineLength += Math.Abs(textEditor.Text.Length - textLengthBeforeAdditions);
            textLengthBeforeAdditions = textEditor.Text.Length;
            parameterCount++;
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