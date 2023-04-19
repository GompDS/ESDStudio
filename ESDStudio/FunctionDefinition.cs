using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace ESDStudio;

public class FunctionDefinition
{
    public FunctionDefinition(string name)
    {
        Name = name;
    }

    public string Name;
    public List<FunctionParameter> Parameters = new();
    public FunctionParameter? ReturnValue;

    public void AddParameter(string type, string name, string enumType, string comment, bool optional)
    {
        Parameters.Add(new FunctionParameter(type, name, enumType, comment, optional));
    }
    
    public void SetReturnValue(string type, string name, string enumType, string comment)
    {
        ReturnValue = new FunctionParameter(type, name, enumType, comment, false);
    }

    public string MakeNumberValuesDescriptive(string code)
    {
        int nextFuncIndex = code.IndexOf(Name + "(", StringComparison.Ordinal);
        while (nextFuncIndex > -1)
        {
            int startIndex = nextFuncIndex + Name.Length + 1;
            int nextOpenParen = code.IndexOf('(', startIndex);
            int nextCloseParen = code.IndexOf(')', startIndex);
            if (nextCloseParen == -1) break;
            if (nextOpenParen > -1)
            {
                while (nextOpenParen < nextCloseParen)
                {
                    nextOpenParen = code.IndexOf('(', nextOpenParen + 1);
                    nextCloseParen = code.IndexOf(')', nextCloseParen + 1);
                }
            }
            int endIndex = nextCloseParen;
            if (Parameters.Any(x => x.IsEnum || x.Type == "bool"))
            {
                code = MakeParametersDescriptive(code, startIndex, endIndex);
            }
            if (ReturnValue is { Type: "bool" or "enum" })
            {
                code = MakeReturnValueDescriptive(code, endIndex + 1);
            }
            nextFuncIndex = code.IndexOf(Name + "(", nextFuncIndex + Name.Length,  StringComparison.Ordinal);
        }

        return code;
    }

    private string MakeParametersDescriptive(string code, int startIndex, int endIndex)
    {
        string funcArgumentRange = code.Substring(startIndex, endIndex - startIndex);
        for (int i = 0; i < Parameters.Count; i++)
        {
            int nextCommaIndex = funcArgumentRange.IndexOf(',');
            if (nextCommaIndex == -1)
            {
                nextCommaIndex = endIndex - startIndex;
            }

            if (Parameters[i].IsEnum || Parameters[i].Type == "bool")
            {
                string stringValue = funcArgumentRange.Substring(0, nextCommaIndex);
                if (int.TryParse(stringValue, out int intValue))
                {
                    string valueToInsert = "";

                    if (Parameters[i].IsEnum)
                    {
                        XmlData.EnumTemplates.TryGetValue(Parameters[i].EnumType,
                            out List<Tuple<int, string>>? enumValues);
                        Tuple<int, string>? valueStringPair =
                            enumValues?.FirstOrDefault(x => x.Item1 == intValue);
                        if (valueStringPair != null)
                        {
                            string valueName = valueStringPair.Item2;
                            code = code.Remove(startIndex, nextCommaIndex);
                            endIndex -= nextCommaIndex;
                            valueToInsert = $"{Parameters[i].EnumType}.{valueName}";
                        }
                    }
                    else if (intValue is 0 or 1)
                    {
                        code = code.Remove(startIndex, nextCommaIndex);
                        endIndex -= nextCommaIndex;
                        valueToInsert = intValue == 1 ? "true" : "false";
                    }

                    if (valueToInsert.Length == 0) continue;

                    if (i > 0)
                    {
                        valueToInsert = valueToInsert.Insert(0, " ");
                    }

                    code = code.Insert(startIndex, valueToInsert);
                    endIndex += valueToInsert.Length;
                    funcArgumentRange = code.Substring(startIndex, endIndex - startIndex);
                    nextCommaIndex = funcArgumentRange.IndexOf(',');
                }
            }

            startIndex += nextCommaIndex + 1;
            if (startIndex <= endIndex)
            {
                funcArgumentRange = code.Substring(startIndex, endIndex - startIndex);
            }
        }

        return code;
    }

    private string MakeReturnValueDescriptive(string code, int startIndex)
    {
        if (ReturnValue == null) return code;
        int equalsIndex = code.IndexOf("==", startIndex, StringComparison.Ordinal);
        if (equalsIndex > -1)
        {
            for (int i = startIndex; i < equalsIndex; i++)
            {
                if (code[i] is not (' ' or '\n' or '\r' or '\t')) return code;
            }

            equalsIndex += 2;
            var match = Regex.Match(code.Substring(equalsIndex), @"\d+");
            if (!match.Success) return code;
            int valueIndex = match.Index + equalsIndex;
            for (int i = equalsIndex; i < valueIndex; i++)
            {
                if (code[i] is not (' ' or '\n' or '\r' or '\t')) return code;
            }

            int intValue = int.Parse(match.Value);
            string valueToInsert = "";
            if (ReturnValue.IsEnum)
            {
                XmlData.EnumTemplates.TryGetValue(ReturnValue.EnumType,
                    out List<Tuple<int, string>>? enumValues);
                Tuple<int, string>? valueStringPair =
                    enumValues?.FirstOrDefault(x => x.Item1 == intValue);
                if (valueStringPair != null)
                {
                    valueToInsert = $"{ReturnValue.EnumType}.{valueStringPair.Item2}";
                }
            }
            else
            {
                if (intValue == 0)
                {
                    valueToInsert = "false";
                }
                else if (intValue == 1)
                {
                    valueToInsert = "true";
                }
            }

            if (valueToInsert.Length == 0) return code;
            code = code.Remove(valueIndex, match.Value.Length);
            code = code.Insert(valueIndex, valueToInsert);
        }
        return code;
        
    }
}