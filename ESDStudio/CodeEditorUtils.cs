using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ICSharpCode.AvalonEdit.CodeCompletion;

namespace ESDStudio;

public static class CodeEditorUtils
{
    public static readonly Regex Delimiters = new ("[,\\+\\-\\*\\(\\) \n]");

    public static void AddIfMatch(this IList<ICompletionData> collection, string completionText, string enteredText,
        int insertionOffset, int parameterIndex)
    {
        if (Regex.IsMatch(completionText, enteredText, RegexOptions.IgnoreCase))
        {
            collection.Add(new CompletionData(completionText, enteredText, insertionOffset, parameterIndex));
        }
    }
    
    public static void AddMatchingFunctions(this IList<ICompletionData> data, string pattern, string enteredText,
        int insertionOffset, FunctionDefinition parentFunction, int parameterIndex)
    {
        foreach (FunctionDefinition funcDef in XmlData.FunctionDefinitions.Where(x => x.ReturnValue != null))
        {
            if (funcDef.ReturnValue == null) continue;
            if (!Regex.IsMatch(funcDef.ReturnValue.Type, pattern, RegexOptions.IgnoreCase) ||
                !Regex.IsMatch(funcDef.Name, enteredText, RegexOptions.IgnoreCase)) continue;
            if (pattern.Contains("enum") && parentFunction.Parameters[parameterIndex].Type == "enum")
            {
                if (funcDef.ReturnValue.EnumType != parentFunction.Parameters[parameterIndex].EnumType)
                {
                    continue;
                }
            }
            CompletionData newData = new (funcDef.Name, enteredText, insertionOffset, parameterIndex)
            {
                parentFunction = parentFunction
            };
            data.Add(newData);
        }
    }
    
    public static void AddMatchingFunctions(this IList<ICompletionData> data, string enteredText, int insertionOffset)
    {
        foreach (FunctionDefinition funcDef in XmlData.FunctionDefinitions.Where(x => x.ReturnValue == null))
        {
            if (!Regex.IsMatch(funcDef.Name, enteredText, RegexOptions.IgnoreCase)) continue;
            CompletionData newData = new (funcDef.Name, enteredText, insertionOffset, 0);
            data.Add(newData);
        }
    }

    public static void GetParentFunctionInfo(int startIndex, string searchRange, out string parentFunctionName, out int parameterIndex)
    {
        int nextOpenParen = searchRange.LastIndexOf('(', startIndex);
        int nextCloseParen = searchRange.LastIndexOf(')', startIndex);
        if (nextCloseParen > 0 && nextOpenParen > 0)
        {
            while (nextOpenParen < nextCloseParen && nextOpenParen > 0 && nextCloseParen > 0)
            {
                nextOpenParen = searchRange.LastIndexOf('(', nextOpenParen - 1);
                nextCloseParen = searchRange.LastIndexOf(')', nextCloseParen - 1);
            }
        }
        int parentStartIndex = nextOpenParen;
        parentFunctionName = "";
        parameterIndex = 0;
        if (parentStartIndex <= -1) return;
        int i = startIndex;
        for (; i > parentStartIndex; i--)
        {
            if (!Delimiters.Match(searchRange[i].ToString()).Success) continue;
            if (searchRange[i] == ',')
            {
                parameterIndex++;
            }
        }

        i -= 1;
        while (Delimiters.Match(searchRange[i].ToString()).Success == false && i > 0)
        {
            parentFunctionName = parentFunctionName.Insert(0, searchRange[i].ToString());
            i--;
        }
    }
}