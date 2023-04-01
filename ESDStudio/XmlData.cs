using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace ESDStudio;

public static class XmlData
{
    public static List<FunctionDefinition> FunctionDefinitions = new();

    public static Dictionary<string, List<Tuple<int, string>>> EnumTemplates = new();

    public static void ReadEnumTemplatesXml()
    {
        string cwd = AppDomain.CurrentDomain.BaseDirectory;
        XElement document = XElement.Load($"{cwd}\\Resources\\EnumTemplates.xml");
        foreach (XElement e in document.Elements())
        {
            XAttribute? key = e.Attribute("key");
            if (key == null) continue;
            List<Tuple<int, string>> enumValues = new();
            foreach (XElement value in e.Elements())
            {
                XAttribute? index = value.Attribute("index");
                XAttribute? name = value.Attribute("name");
                if (index == null || name == null) continue;
                enumValues.Add(new Tuple<int, string>(int.Parse(index.Value), name.Value));
            }
            EnumTemplates.Add(key.Value, enumValues);
        }
        Console.WriteLine();
    }

    public static void ReadFunctionDefXml()
    {
        ReadEnumTemplatesXml();
        string cwd = AppDomain.CurrentDomain.BaseDirectory;
        XElement element = XElement.Load($"{cwd}\\Resources\\FunctionDefinitions.xml");
        foreach (XElement e in element.Elements())
        {
            XAttribute? funcName = e.Attribute("name");
            if (funcName == null) continue;
            FunctionDefinition funcDef = new(funcName.Value);
            XElement? parameterGroup = e.Element("Parameters");
            if (parameterGroup != null)
            {
                foreach (XElement parameter in parameterGroup.Elements())
                {
                    string type = "";
                    XAttribute? typeAttribute = parameter.Attribute("type");
                    if (typeAttribute != null) type = typeAttribute.Value;
                    string name = "";
                    XAttribute? nameAttribute = parameter.Attribute("name");
                    if (nameAttribute != null) name = nameAttribute.Value;
                    string enumType = "";
                    XAttribute? enumAttribute = parameter.Attribute("enum");
                    if (enumAttribute != null) enumType = enumAttribute.Value;
                    string comment = "";
                    XAttribute? commentAttribute = parameter.Attribute("comment");
                    if (commentAttribute != null) comment = commentAttribute.Value;
                    funcDef.AddParameter(type, name, enumType, comment);
                }
            }

            XElement? returnValue = e.Element("ReturnValue");
            if (returnValue != null)
            {
                string type = "";
                XAttribute? typeAttribute = returnValue.Attribute("type");
                if (typeAttribute != null) type = typeAttribute.Value;
                string name = "";
                XAttribute? nameAttribute = returnValue.Attribute("name");
                if (nameAttribute != null) name = nameAttribute.Value;
                string enumType = "";
                XAttribute? enumAttribute = returnValue.Attribute("enum");
                if (enumAttribute != null) enumType = enumAttribute.Value;
                string comment = "";
                XAttribute? commentAttribute = returnValue.Attribute("comment");
                if (commentAttribute != null) comment = commentAttribute.Value;
                funcDef.SetReturnValue(type, name, enumType, comment);
            }
            FunctionDefinitions.Add(funcDef);
        }
        Console.WriteLine();
    }
}