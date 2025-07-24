using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ESDLang.Doc;
using ICSharpCode.AvalonEdit;

namespace ESDStudio.UserControls;

public partial class CompletionDataContent : UserControl
{
    public CompletionDataContent(string completionName)
    {
        InitializeComponent();

        CompletionName.Text = completionName;
        ESDDocumentation.MethodDoc? funcDef = Project.Current.Game.TalkMethods.FirstOrDefault(x => x.Name == completionName);
        if (funcDef != null)
        {
            CompletionName.Text += "()";

            if (funcDef.ReturnValue != null)
            {
                CompletionDataType.Text += funcDef.ReturnValue.Type;
            }
            else
            {
                CompletionDataType.Text += "void";
            }
        }
        
    }
}