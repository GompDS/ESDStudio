using System;
using System.Collections.Generic;

namespace ESDStudio;

public class FunctionParameter
{
    public string Type;
    public string Name;
    public bool IsEnum;
    public string EnumType;
    public string Comment;

    public FunctionParameter(string type, string name, string enumType, string comment)
    {
        Type = type;
        Name = name;
        EnumType = enumType;
        if (EnumType.Length > 0) IsEnum = true;
        Comment = comment;
    }
}