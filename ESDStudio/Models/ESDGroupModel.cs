using System.Collections.Generic;

namespace ESDStudio.Models;

public class ESDGroupModel
{
    public string Name { get; set; }
    public List<BNDModel> Members { get; }

    public ESDGroupModel(string name)
    {
        Name = name;
        Members = new List<BNDModel>();
    }
}