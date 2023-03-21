using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SoulsFormats;

namespace SoulsIds
{
    public class GameSpec
    {
        // For other misc purposes
        public FromGame Game { get; set; }
        // Base game directory
        public string GameDir { get; set; }
        // Relative from game directory. Assumes UXM or equivalent tool has been used, if that is necessary.
        public string EsdDir { get; set; }
        public string MsgDir { get; set; }
        public string MsbDir { get; set; }
        public string ParamFile { get; set; }
        public DCX.Type Dcx { get; set; }
        // Misc name files, relative to current dir
        public string NameDir { get; set; }
        // Param layout files, relative to current dir
        public string LayoutDir { get; set; }

        public GameSpec Clone() => (GameSpec)MemberwiseClone();

        public enum FromGame { UNKNOWN, DS1, DS1R, DS2, DS2S, BB, DS3, SDT }
        public static GameSpec ForGame(FromGame game)
        {
            GameSpec spec = Games.TryGetValue(game, out GameSpec baseSpec) ? baseSpec.Clone() : new GameSpec();
            spec.Game = game;
            return spec;
        }
        // Some default specs
        private static readonly Dictionary<FromGame, GameSpec> Games = new Dictionary<FromGame, GameSpec>
        {
            [FromGame.UNKNOWN] = new GameSpec(),
            [FromGame.DS1] = new GameSpec
            {
                Dcx = DCX.Type.None,
                GameDir = @"C:\Program Files (x86)\Steam\SteamApps\common\Dark Souls Prepare to Die Edition\DATA",
                EsdDir = @"script\talk",  // Also "chr"
                MsgDir = @"msg\ENGLISH",  // English as default
                MsbDir = @"map\MapStudio",
                ParamFile = @"param\GameParam\GameParam.parambnd",
                NameDir = @"dist\DS1R\Names",
                LayoutDir = @"dist\DS1\Layouts",
            },
            [FromGame.DS1R] = new GameSpec
            {
                Dcx = (DCX.Type)DCX.DefaultType.DarkSouls1,
                GameDir = @"C:\Program Files (x86)\Steam\steamapps\common\DARK SOULS REMASTERED",
                EsdDir = @"script\talk",  // Also "chr"
                MsgDir = @"msg\ENGLISH",  // English as default
                MsbDir = @"map\MapStudio",
                ParamFile = @"param\GameParam\GameParam.parambnd.dcx",
                NameDir = @"dist\DS1R\Names",
                LayoutDir = @"dist\DS1R\Layouts",
            },
            [FromGame.DS2] = new GameSpec
            {
                Dcx = (DCX.Type)DCX.DefaultType.DarkSouls1,
                GameDir = @"C:\Program Files (x86)\Steam\steamapps\common\Dark Souls II\Game",
                EsdDir = @"ezstate",
                MsgDir = @"menu\text\english",  // Also in talk subdir
                MsbDir = @"map",                // One msb per subdir
                ParamFile = "gameparam_dlc2.parambnd.dcx",
                NameDir = @"dist\DS2S\Names",
                LayoutDir = @"dist\DS2S\Layouts",  // The same?
            },
            [FromGame.DS2S] = new GameSpec
            {
                Dcx = (DCX.Type)DCX.DefaultType.DarkSouls1,
                GameDir = @"C:\Program Files (x86)\Steam\steamapps\common\Dark Souls II Scholar of the First Sin\Game",
                EsdDir = @"ezstate",
                MsgDir = @"menu\text\english",  // Also in talk subdir
                MsbDir = @"map",                // One msb per subdir
                ParamFile = "gameparam_dlc2.parambnd.dcx",
                NameDir = @"dist\DS2S\Names",
                LayoutDir = @"dist\DS2S\Layouts",
            },
            [FromGame.BB] = new GameSpec
            {
                Dcx = (DCX.Type)DCX.DefaultType.DarkSouls3,
            },
            [FromGame.DS3] = new GameSpec
            {
                Dcx = (DCX.Type)DCX.DefaultType.DarkSouls3,
                GameDir = @"C:\Program Files (x86)\Steam\steamapps\common\DARK SOULS III\Game",
                EsdDir = @"script\talk",
                MsgDir = @"msg\engus",
                MsbDir = @"map\mapstudio",
                ParamFile = "Data0.bdt",
                NameDir = @"dist\DS3\Names",
                LayoutDir = @"dist\DS3\Layouts",
            },
            [FromGame.SDT] = new GameSpec
            {
                Dcx = (DCX.Type)DCX.DefaultType.Sekiro,
                GameDir = @"C:\Program Files (x86)\Steam\steamapps\common\Sekiro",
                EsdDir = @"script\talk",
                MsgDir = @"msg\engus",
                MsbDir = @"map\mapstudio",
                ParamFile = @"param\GameParam\GameParam.parambnd.dcx",
                NameDir = @"dist\SDT\Names",
                LayoutDir = @"dist\SDT\Layouts",
            },
        };
    }
}
