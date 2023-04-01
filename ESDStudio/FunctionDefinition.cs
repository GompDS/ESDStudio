using System;
using System.Collections.Generic;

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

    public void AddParameter(string type, string name, string enumType, string comment)
    {
        Parameters.Add(new FunctionParameter(type, name, enumType, comment));
    }
    
    public void SetReturnValue(string type, string name, string enumType, string comment)
    {
        ReturnValue = new FunctionParameter(type, name, enumType, comment);
    }
}