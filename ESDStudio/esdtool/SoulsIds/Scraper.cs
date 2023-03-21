using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SoulsFormats;
using static SoulsIds.Universe;
using static SoulsIds.GameSpec;

namespace SoulsIds
{
    public class Scraper
    {
        public static readonly Dictionary<string, Namespace> MsgTypes = new Dictionary<string, Namespace>
        {
            // DS3 and Sekiro
            { "NPC\u540d", Namespace.NPC },
            { "\u6b66\u5668\u540d", Namespace.WEAPON },
            { "\u30a2\u30a4\u30c6\u30e0\u540d", Namespace.GOODS },
            { "\u30a4\u30d9\u30f3\u30c8\u30c6\u30ad\u30b9\u30c8", Namespace.ACTION },
            { "\u4f1a\u8a71", Namespace.DIALOGUE },
            { "\u4f1a\u8a71_dlc1", Namespace.DIALOGUE },
            { "\u4f1a\u8a71_dlc2", Namespace.DIALOGUE },
            { "\u9632\u5177\u540d", Namespace.PROTECTOR },
            { "\u30a2\u30af\u30bb\u30b5\u30ea\u540d", Namespace.ACCESSORY },
            // DS1
            { "Weapon_name_", Namespace.WEAPON },
            { "Armor_name_", Namespace.PROTECTOR },
            { "Accessory_name_", Namespace.ACCESSORY },
            { "Item_name_", Namespace.GOODS },
            { "Event_text_", Namespace.ACTION },
            { "Conversation_", Namespace.DIALOGUE },
            // DS2
            { "mapevent", Namespace.ACTION },
            { "npcmenu", Namespace.NPC },
            { "itemname", Namespace.GOODS },
            { "bonfirename", Namespace.BONFIRE },
        };
        public static readonly Dictionary<Namespace, string> ItemParams = new Dictionary<Namespace, string>
        {
            { Namespace.WEAPON, "EquipParamWeapon" },
            { Namespace.PROTECTOR, "EquipParamProtector" },
            { Namespace.ACCESSORY, "EquipParamAccessory" },
            { Namespace.GOODS, "EquipParamGoods" },
        };

        private GameSpec spec;
        private GameEditor editor;

        private Dictionary<string, PARAM> Params;
        public Scraper(GameSpec spec)
        {
            this.spec = spec;
            this.editor = new GameEditor(spec);
        }

        private void LoadParams()
        {
            if (Params == null)
            {
                Params = new GameEditor(spec).LoadParams();
            }
        }

        private static readonly Dictionary<FromGame, string> talkParamMsgId = new Dictionary<FromGame, string>
        {
            [FromGame.DS1R] = "msgId",
            // Just add the male lines for the moment... they have the same talk id
            [FromGame.DS3] = "PcGenderFemale1",
            [FromGame.SDT] = "TalkParamId1",
        };
        public bool ScrapeMsgs(Universe u)
        {
            if (spec.MsgDir == null) return false;
            foreach (KeyValuePair<string, FMG> entry in editor.LoadBnds(spec.MsgDir, (data, name) => FMG.Read(data)).SelectMany(e => e.Value)
                .Concat(editor.Load(spec.MsgDir, name => FMG.Read(name), "*.fmg")).OrderBy(e => e.Key))
            {
                if (MsgTypes.ContainsKey(entry.Key))
                {
                    Namespace type = MsgTypes[entry.Key];
                    foreach (FMG.Entry name in entry.Value.Entries)
                    {
                        u.Names[Obj.Of(type, name.ID)] = name.Text;
                    }
                }
            }
            if (Directory.Exists($@"{spec.GameDir}\{spec.MsgDir}\talk"))
            {
                foreach (KeyValuePair<string, FMG> entry in editor.Load(spec.MsgDir + @"\talk", name => FMG.Read(name), "*.fmg"))
                {
                    foreach (FMG.Entry name in entry.Value.Entries)
                    {
                        u.Names[Obj.Talk(name.ID)] = name.Text;
                    }
                }
            }
            if (spec.ParamFile == null || !talkParamMsgId.ContainsKey(spec.Game)) return false;
            LoadParams();
            if (!Params.ContainsKey("TalkParam")) return false;
            string msgId = talkParamMsgId[spec.Game];
            foreach (PARAM.Row row in Params["TalkParam"].Rows)
            {
                int dialogue = (int)row[msgId].Value;
                if (dialogue > 0)
                {
                    Obj dialogueObj = Obj.Of(Namespace.DIALOGUE, dialogue);
                    if (u.Names.ContainsKey(dialogueObj))
                    {
                        u.Names[Obj.Talk((int)row.ID)] = u.Names[dialogueObj];
                    }
                }
            }
            return true;
        }
        public bool ScrapeItems(Universe u)
        {
            if (spec.ParamFile == null) return false;
            LoadParams();
            if (spec.Game == FromGame.DS2S)
            {
                if (!Params.ContainsKey("ItemLotParam2_Other")) return false;
                foreach (PARAM.Row row in Params["ItemLotParam2_Other"].Rows.Concat(Params["ItemLotParam2_Chr"].Rows))
                {
                    Obj lot = Obj.Lot((int)row.ID);
                    Obj item = Obj.Item(3, (int)(uint)row["Unk2C"].Value);
                    u.Add(Verb.PRODUCES, lot, item);
                    if (u.Names.ContainsKey(item) && !u.Names.ContainsKey(lot)) u.Names[lot] = u.Names[item];
                }
            }
            if (!Params.ContainsKey("ItemLotParam")) return false;
            if (spec.Game == FromGame.DS1R)
            {
                foreach (PARAM.Row row in Params["ItemLotParam"].Rows)
                {
                    Obj lot = Obj.Lot((int)row.ID);
                    int eventFlag = (int)row["getItemFlagId"].Value;
                    if (eventFlag != -1)
                    {
                        u.Add(Verb.WRITES, lot, Obj.EventFlag(eventFlag));
                    }
                    for (int i = 1; i <= 8; i++)
                    {
                        int id = (int)row[$"lotItemId0{i}"].Value;
                        int type = (int)row[$"lotItemCategory0{i}"].Value;
                        if (id != 0 && type != -1)
                        {
                            u.Add(Verb.PRODUCES, lot, Obj.Item((uint)type, id));
                        }
                    }
                }
                foreach (PARAM.Row row in Params["ShopLineupParam"].Rows)
                {
                    if (row.ID >= 9000000) continue;

                    Obj shop = Obj.Shop((int)row.ID);

                    int eventFlag = (int)row["eventFlag"].Value;
                    if (eventFlag != -1)
                    {
                        u.Add(Verb.WRITES, shop, Obj.EventFlag(eventFlag));
                    }

                    int qwc = (int)row["qwcId"].Value;
                    if (qwc != -1)
                    {
                        u.Add(Verb.READS, shop, Obj.EventFlag(qwc));
                    }

                    int type = (byte)row["equipType"].Value;
                    int id = (int)row["equipId"].Value;
                    u.Add(Verb.PRODUCES, shop, Obj.Item((uint)type, id));

                    int material = (int)row["mtrlId"].Value;
                    if (material != -1)
                    {
                        u.Add(Verb.CONSUMES, shop, Obj.Material(material));
                    }
                }
                foreach (PARAM.Row row in Params["EquipMtrlSetParam"].Rows)
                {
                    Obj mat = Obj.Material((int)row.ID);
                    for (int i = 1; i <= 5; i++)
                    {
                        int id = (int)row[$"materialId0{i}"].Value;
                        if (id > 0)
                        {
                            u.Add(Verb.CONSUMES, mat, Obj.Item(3 /* good */, id));
                        }
                    }
                }
                // NPC param file is invalid for DS1R? lot should be itemLotId_1 though
            }
            else if (spec.Game == FromGame.DS3)
            {
                foreach (PARAM.Row row in Params["ItemLotParam"].Rows)
                {
                    Obj lot = Obj.Lot((int)row.ID);
                    int eventFlag = (int)row["getItemFlagId"].Value;
                    if (eventFlag != -1)
                    {
                        u.Add(Verb.WRITES, lot, Obj.EventFlag(eventFlag));
                    }
                    for (int i = 1; i <= 8; i++)
                    {
                        int id = (int)row[$"ItemLotId{i}"].Value;
                        uint type = (uint)row[$"LotItemCategory0{i}"].Value;
                        if (id != 0 && type != 0xFFFFFFFF)
                        {
                            Obj item = Obj.Item(type, id);
                            u.Add(Verb.PRODUCES, lot, item);
                            if (u.Names.ContainsKey(item) && !u.Names.ContainsKey(lot)) u.Names[lot] = u.Names[item];
                        }
                    }
                }
                foreach (PARAM.Row row in Params["ShopLineupParam"].Rows)
                {
                    if (row.ID >= 9000000) continue;

                    Obj shop = Obj.Shop((int)row.ID);

                    int eventFlag = (int)row["EventFlag"].Value;
                    if (eventFlag != -1)
                    {
                        u.Add(Verb.WRITES, shop, Obj.EventFlag(eventFlag));
                    }

                    int qwc = (int)row["qwcID"].Value;
                    if (qwc != -1)
                    {
                        u.Add(Verb.READS, shop, Obj.EventFlag(qwc));
                    }

                    int type = (byte)row["equipType"].Value;
                    int id = (int)row["EquipId"].Value;
                    Obj item = Obj.Item((uint)type, id);
                    u.Add(Verb.PRODUCES, shop, item);
                    if (u.Names.ContainsKey(item)) u.Names[shop] = u.Names[item];

                    int material = (int)row["mtrlId"].Value;
                    if (material != -1)
                    {
                        u.Add(Verb.CONSUMES, shop, Obj.Material(material));
                    }
                }
                foreach (PARAM.Row row in Params["EquipMtrlSetParam"].Rows)
                {
                    Obj mat = Obj.Material((int)row.ID);
                    for (int i = 1; i <= 5; i++)
                    {
                        int id = (int)row[$"MaterialId0{i}"].Value;
                        if (id > 0)
                        {
                            u.Add(Verb.CONSUMES, mat, Obj.Item(3 /* good */, id));
                        }
                    }
                }
                foreach (PARAM.Row row in Params["NpcParam"].Rows)
                {
                    int itemLot = (int)row["ItemLotId1"].Value;
                    if (itemLot != -1)
                    {
                        u.Add(Verb.PRODUCES, Obj.Npc((int)row.ID), Obj.Lot(itemLot));
                    }
                }
            }
            else return false;
            return true;
        }
        public void LoadNames(Universe u)
        {
            if (spec.NameDir == null) return;
            foreach (KeyValuePair<string, string> entry in editor.LoadNames("ModelName", n => n, true))
            {
                // Maybe should combine these namespaces...
                u.Names[Obj.Of(Namespace.OBJ_MODEL, entry.Key)] = entry.Value;
                u.Names[Obj.Of(Namespace.CHR_MODEL, entry.Key)] = entry.Value;
            }
            foreach (KeyValuePair<int, string> entry in editor.LoadNames("CharaInitParam", n => int.Parse(n), true))
            {
                u.Names[Obj.Of(Namespace.NPC, entry.Key)] = entry.Value;
            }
            foreach (KeyValuePair<int, string> entry in editor.LoadNames("ShopQwc", n => int.Parse(n), true))
            {
                u.Names[Obj.Of(Namespace.EVENT_FLAG, entry.Key)] = entry.Value;
            }
        }
        public bool ScrapeMaps(Universe u)
        {
            if (spec.MsbDir == null) return false;
            if (spec.Game == FromGame.SDT)
            {
                Dictionary<string, MSBS> maps = editor.Load(spec.MsbDir, path => MSBS.Read(path));
                foreach (KeyValuePair<string, MSBS> entry in maps)
                {
                    string location = entry.Key;
                    MSBS msb = entry.Value;
                    Obj map = Obj.Map(location);
                    int treasureIndex = 0;
                    foreach (MSBS.Event.Treasure treasure in msb.Events.Treasures)
                    {
                        if (treasure.TreasurePartName != null && treasure.ItemLotID != -1)
                        {
                            Obj treasureObj = Obj.Treasure(location, treasureIndex);
                            Obj part = Obj.Part(location, treasure.TreasurePartName);
                            u.Add(Verb.PRODUCES, part, treasureObj);
                            u.Add(Verb.PRODUCES, treasureObj, Obj.Lot(treasure.ItemLotID));
                        }
                        treasureIndex++;
                    }
                    foreach (MSBS.Event.Talk talk in msb.Events.Talks)
                    {
                        for (int i = 0; i < 2; i++)
                        {
                            string enemyName = talk.EnemyNames[i];
                            int esdId = talk.TalkIDs[i];
                            if (enemyName == null || esdId < 0)
                            {
                                continue;
                            }
                            Obj part = Obj.Part(location, enemyName);
                            u.Add(Verb.CONTAINS, part, Obj.Esd(esdId));
                        }
                    }
                    foreach (MSBS.Entry obj in msb.Parts.GetEntries())
                    {
                        MSBS.Part part = obj as MSBS.Part;
                        if (part == null) continue;
                        Obj partObj = Obj.Part(location, part.Name);
                        if (part.EntityID != -1)
                        {
                            u.Add(Verb.CONTAINS, Obj.Entity(part.EntityID), partObj);
                        }
                        foreach (int groupId in part.EntityGroupIDs.Where(groupID => groupID > 0))
                        {
                            u.Add(Verb.CONTAINS, Obj.Entity(groupId), partObj);
                        }
                        if (part is MSBS.Part.Enemy enemy)
                        {
                            string model = part.Name.Split('_')[0];
                            u.Add(Verb.CONTAINS, partObj, Obj.ChrModel(model));
                            if (enemy.NPCParamID != -1)
                            {
                                u.Add(Verb.CONTAINS, partObj, Obj.Npc(enemy.NPCParamID));
                            }
                        }
                        else if (part is MSBS.Part.Object)
                        {
                            string model = part.Name.Split('_')[0];
                            u.Add(Verb.CONTAINS, partObj, Obj.ObjModel(model));
                        }
                        // This gets a bit noisy, so don't add part to map unless it's already in universe
                        if (u.Nodes.ContainsKey(partObj))
                        {
                            u.Add(Verb.CONTAINS, map, partObj);
                        }
                    }
                }
            }
            else if (spec.Game == FromGame.DS1 || spec.Game == FromGame.DS1R)
            {
                Dictionary<string, MSB1> maps = editor.Load(spec.MsbDir, path => path.Contains("m99") ? null : MSB1.Read(path), "*.msb");
                foreach (KeyValuePair<string, MSB1> entry in maps)
                {
                    string location = entry.Key;
                    Obj map = Obj.Map(location);
                    MSB1 msb = entry.Value;
                    if (msb == null) continue;
                    // For now, just load talk data
                    foreach (MSB1.Entry obj in msb.Parts.GetEntries())
                    {
                        MSB1.Part part = obj as MSB1.Part;
                        if (part == null) continue;
                        Obj partObj = Obj.Part(location, part.Name);
                        if (part.EntityID != -1)
                        {
                            u.Add(Verb.CONTAINS, Obj.Entity(part.EntityID), partObj);
                        }
                        if (part is MSB1.Part.Enemy enemy)
                        {
                            string model = part.Name.Split('_')[0];
                            u.Add(Verb.CONTAINS, partObj, Obj.ChrModel(model));
                            if (enemy.NPCParamID != -1 && model == "c0000")
                            {
                                u.Add(Verb.CONTAINS, partObj, Obj.Human(enemy.NPCParamID));
                            }
                            if (enemy.TalkID != -1)
                            {
                                u.Add(Verb.CONTAINS, partObj, Obj.Esd(enemy.TalkID));
                            }
                        }
                        else if (part is MSB1.Part.Object)
                        {
                            string model = part.Name.Split('_')[0];
                            u.Add(Verb.CONTAINS, partObj, Obj.ObjModel(model));
                        }
                        // This gets a bit noisy, so don't add part to map unless it's already in universe
                        if (u.Nodes.ContainsKey(partObj))
                        {
                            u.Add(Verb.CONTAINS, map, partObj);
                        }
                    }
                }
            }
            else if (spec.Game == FromGame.DS3)
            {
                Dictionary<string, MSB3> maps = editor.Load(spec.MsbDir, path => path.Contains("m99") ? null : MSB3.Read(path));
                foreach (KeyValuePair<string, MSB3> entry in maps)
                {
                    string location = entry.Key;
                    Obj map = Obj.Map(location);
                    MSB3 msb = entry.Value;
                    if (msb == null) continue;
                    // For now, just load talk data
                    foreach (MSB3.Entry obj in msb.Parts.GetEntries())
                    {
                        MSB3.Part part = obj as MSB3.Part;
                        if (part == null) continue;
                        Obj partObj = Obj.Part(location, part.Name);
                        if (part.EventEntityID != -1)
                        {
                            u.Add(Verb.CONTAINS, Obj.Entity(part.EventEntityID), partObj);
                        }
                        if (part is MSB3.Part.Enemy enemy)
                        {
                            string model = part.Name.Split('_')[0];
                            u.Add(Verb.CONTAINS, partObj, Obj.ChrModel(model));
                            if (enemy.CharaInitID > 0)
                            {
                                u.Add(Verb.CONTAINS, partObj, Obj.Human(enemy.CharaInitID));
                            }
                            if (enemy.TalkID != -1)
                            {
                                u.Add(Verb.CONTAINS, partObj, Obj.Esd(enemy.TalkID));
                            }
                        }
                        else if (part is MSB3.Part.Object)
                        {
                            string model = part.Name.Split('_')[0];
                            u.Add(Verb.CONTAINS, partObj, Obj.ObjModel(model));
                        }
                        // This gets a bit noisy, so don't add part to map unless it's already in universe
                        if (u.Nodes.ContainsKey(partObj))
                        {
                            u.Add(Verb.CONTAINS, map, partObj);
                        }
                    }
                }
            }
            else return false;
            return true;
        }
    }
}
