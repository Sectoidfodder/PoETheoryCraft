using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PoETheoryCraft.DataClasses;
using PoETheoryCraft.Utils;

namespace PoETheoryCraft
{
    public struct PostRollOptions
    {
        public bool Maximize;
        public IList<KeyValuePair<PoEModData, IDictionary<string, int>>> TryCrafts; //ghetto ordered dict
    }
    public class CraftingBench
    {
        public IDictionary<string, int> CurrencySpent { get; private set; }
        public IDictionary<PoEModData, int> BaseValidMods { get; private set; } //starting point for all crafts: a subset of core mods valid for BenchItem's source template
        public PostRollOptions PostRoll { get; set; }
        private ItemCraft _benchitem;   //item on bench, setting it also updates base mod pool
        public ItemCraft BenchItem { 
            get { return _benchitem; }
            set
            {
                _benchitem = value;
                if (_benchitem != null)
                {
                    PoEBaseItemData itemtemplate = CraftingDatabase.AllBaseItems[_benchitem.SourceData];
                    BaseValidMods = ModLogic.FindBaseValidMods(itemtemplate, CraftingDatabase.CoreMods.Values);
                }
                else
                {
                    BaseValidMods.Clear();
                }
            }
        }
        public IList<ItemCraft> MassResults { get; } = new List<ItemCraft>();   //storage for mass crafting results
        private string _sortby;
        public string SortBy 
        {
            get { return _sortby; } 
            set
            {
                _sortby = value;
                if (MassResults != null && MassResults.Count > 0)
                {
                    ((List<ItemCraft>)MassResults).Sort(new ItemCraftComparer() { Key = _sortby });
                    ((List<ItemCraft>)MassResults).Reverse();
                }
            }
        }
        public CraftingBench()
        {
            CurrencySpent = new Dictionary<string, int>();
        }
        public string ForceAddMod(PoEModData mod, ItemCraft target = null, IDictionary<string, int> costs = null)
        {
            target = target ?? BenchItem;
            if (mod.generation_type == ModLogic.Prefix)
            {
                if (target.ModCountByType(ModLogic.Prefix) >= target.GetAffixLimit(true))
                    return "Item cannot have another prefix";
            }
            else
            {
                if (target.ModCountByType(ModLogic.Suffix) >= target.GetAffixLimit(true))
                    return "Item cannot have another suffix";
            }
            foreach (ModCraft livemod in target.LiveMods)
            {
                PoEModData modtemplate = CraftingDatabase.AllMods[livemod.SourceData];
                if (modtemplate.group == mod.group)
                    return "Item already has a mod in this mod group";
            }
            PoEBaseItemData itemtemplate = CraftingDatabase.AllBaseItems[target.SourceData];
            ItemInfluence? inf = ModLogic.GetInfluence(mod, itemtemplate);
            if (inf != null)
            {
                string inftag = itemtemplate.item_class_properties[EnumConverter.InfToTag((ItemInfluence)inf)];
                if (inftag != null)
                    target.LiveTags.Add(inftag);
            }
            target.AddMod(mod);
            if (costs != null)
            {
                foreach (string s in costs.Keys)
                {
                    TallyCurrency(s, costs[s]);
                }
            }
            return null;
        }
        public string ApplyEssence(PoEEssenceData ess, int tries = 1)
        {
            if (BenchItem == null)
                return "Bench is empty";
            if (ess.level <= 5 && BenchItem.Rarity != ItemRarity.Normal || ess.level > 5 && BenchItem.Rarity != ItemRarity.Rare)
                return "Invalid item rarity for selected essence";
            string itemclass = CraftingDatabase.AllBaseItems[BenchItem.SourceData].item_class;
            if (itemclass == "Rune Dagger")
                itemclass = "Dagger";
            if (itemclass == "Warstaff")
                itemclass = "Staff";
            if (!ess.mods.Keys.Contains(itemclass))
                return "Invalid item class for selected essence";
            PoEModData mod = CraftingDatabase.CoreMods[ess.mods[itemclass]];
            if (mod.generation_type == ModLogic.Prefix && BenchItem.ModCountByType(ModLogic.Prefix, true) >= BenchItem.GetAffixLimit(true))
                return "Item does not have space for forced prefix";
            else if (mod.generation_type == ModLogic.Suffix && BenchItem.ModCountByType(ModLogic.Suffix, true) >= BenchItem.GetAffixLimit(true))
                return "Item does not have space for forced suffix";
            RollOptions op = new RollOptions() { IgnoreMeta = true, ForceMods = new List<PoEModData>() { mod }, ILvlCap = ess.item_level_restriction ?? 200 };
            if (tries == 1)
            {
                ModLogic.RerollItem(BenchItem, BaseValidMods, ItemRarity.Rare, op);
                TallyCurrency(ess.key, 1);
                DoPostRoll(BenchItem);
            }
            else
            {
                MassResults.Clear();
                for (int n = 0; n < tries; n++)
                {
                    ItemCraft target = BenchItem.Copy();
                    ModLogic.RerollItem(target, BaseValidMods, ItemRarity.Rare, op);
                    DoPostRoll(target);
                    MassResults.Add(target);
                }
            }
            return null;
        }
        public string ApplyFossils(IList<PoEFossilData> fossils, int tries = 1)
        {
            if (BenchItem == null)
                return "Bench is empty";
            if (BenchItem.Rarity == ItemRarity.Magic)
            {
                return "Cannot apply fossils to a magic item";
            }
            ISet<IList<PoEModWeight>> modweightgroups = new HashSet<IList<PoEModWeight>>();
            ISet<PoEModData> fossilmods = new HashSet<PoEModData>();  //forced mods from fossils
            ISet<PoEModData> cessmods = new HashSet<PoEModData>();    //forced mods from essences (glyphic, tangled)
            IDictionary<PoEModData, int> extendedpool = new Dictionary<PoEModData, int>(BaseValidMods); //extra rollable mods
            PoEBaseItemData itemtemplate = CraftingDatabase.AllBaseItems[BenchItem.SourceData];
            foreach (PoEFossilData fossil in fossils)
            {
                foreach (string t in fossil.forbidden_tags)
                {
                    if (BenchItem.LiveTags.Contains(t))
                        return "Invalid item class for one or more selected fossils";
                }
                if (fossil.allowed_tags.Count > 0)
                {
                    bool allowed = false;
                    foreach (string t in fossil.allowed_tags)
                    {
                        if (BenchItem.LiveTags.Contains(t))
                        {
                            allowed = true;
                            break;
                        }
                    }
                    if (!allowed)
                        return "Invalid item class for one or more selected fossils";
                }
                foreach (string t in fossil.added_mods)
                {
                    if (!extendedpool.ContainsKey(CraftingDatabase.AllMods[t]))
                        extendedpool.Add(CraftingDatabase.AllMods[t], 0);
                }
                foreach (string t in fossil.forced_mods)
                {
                    fossilmods.Add(CraftingDatabase.AllMods[t]);
                }
                if (fossil.corrupted_essence_chance > 0 && RNG.Gen.Next(100) < fossil.corrupted_essence_chance)
                {
                    IList<PoEModData> c = new List<PoEModData>();
                    string itemclass = itemtemplate.item_class;
                    if (itemclass == "Rune Dagger")
                        itemclass = "Dagger";
                    if (itemclass == "Warstaff")
                        itemclass = "Staff";
                    foreach (PoEEssenceData ess in CraftingDatabase.Essences.Values)
                    {
                        if (ess.type.is_corruption_only && ess.mods.Keys.Contains(itemclass))
                        {
                            PoEModData m = CraftingDatabase.CoreMods[ess.mods[itemclass]];
                            if (!cessmods.Contains(m))
                                c.Add(m);
                        }
                    }
                    if (c.Count > 0)
                        cessmods.Add(c[RNG.Gen.Next(c.Count)]);
                }
                modweightgroups.Add(fossil.negative_mod_weights);
                modweightgroups.Add(fossil.positive_mod_weights);
            }
            IList<PoEModData> forcedmods = new List<PoEModData>(ModLogic.FindBaseValidMods(itemtemplate, fossilmods, true).Keys);   //filter by spawn_weight first
            foreach (PoEModData m in cessmods)  //add corrupted essence mods after filter (they wouldn't pass filter)
            {
                forcedmods.Add(m);
            }
            int fprefix = 0;
            int fsuffix = 0;
            foreach (PoEModData m in forcedmods)
            {
                if (m.generation_type == ModLogic.Prefix)
                    fprefix++;
                else
                    fsuffix++;
            }
            if (BenchItem.ModCountByType(ModLogic.Prefix, true) + fprefix > BenchItem.GetAffixLimit(true))
                return "Item does not have space for forced prefix";
            else if (BenchItem.ModCountByType(ModLogic.Suffix, true) + fsuffix > BenchItem.GetAffixLimit(true))
                return "Item does not have space for forced suffix";
            RollOptions ops = new RollOptions() { ForceMods = forcedmods, IgnoreMeta = true, ModWeightGroups = modweightgroups };
            if (tries == 1)
            {
                ModLogic.RerollItem(BenchItem, extendedpool, ItemRarity.Rare, ops);
                foreach (PoEFossilData fossil in fossils)
                {
                    TallyCurrency(fossil.key, 1);
                }
                DoPostRoll(BenchItem);
            }
            else
            {
                MassResults.Clear();
                for (int n = 0; n < tries; n++)
                {
                    ItemCraft target = BenchItem.Copy();
                    ModLogic.RerollItem(target, extendedpool, ItemRarity.Rare, ops);
                    DoPostRoll(target);
                    MassResults.Add(target);
                }
            }
            return null;
        }
        public string ApplyCurrency(PoECurrencyData currency, int tries = 1)
        {
            if (BenchItem == null)
                return "Bench is empty";
            string c = currency.name;
            bool hasclearedmassresults = false;
            bool rolled = false;
            bool success;
            string res;
            for (int n = 0; n < tries; n++)
            {
                ItemCraft target = tries == 1 ? BenchItem : BenchItem.Copy();
                switch (c)
                {
                    case "Chaos Orb":
                        if (target.Rarity != ItemRarity.Rare)
                            return "Invalid item rarity for selected currency";
                        ModLogic.RerollItem(target, BaseValidMods, ItemRarity.Rare);
                        rolled = true;
                        break;
                    case "Orb of Alteration":
                        if (target.Rarity != ItemRarity.Magic)
                            return "Invalid item rarity for selected currency";
                        ModLogic.RerollItem(target, BaseValidMods, ItemRarity.Magic);
                        break;
                    case "Alt Plus Aug":
                        if (target.Rarity != ItemRarity.Magic)
                            return "Invalid item rarity for selected currency";
                        ModLogic.RerollItem(target, BaseValidMods, ItemRarity.Magic);
                        if (target.LiveMods.Count == 1)
                        {
                            ModLogic.RollAddMod(target, BaseValidMods);
                            if (target.QualityType != null)
                                target.BaseQuality -= 2;
                        }
                        break;
                    case "Divine Orb":
                        if (target.Rarity == ItemRarity.Normal)
                            return "Invalid item rarity for selected currency";
                        success = target.RerollExplicits();
                        if (!success)
                            return "No mods can be rerolled";
                        break;
                    case "Orb of Annulment":
                        if (target.Rarity == ItemRarity.Normal)
                            return "Invalid item rarity for selected currency";
                        success = target.RemoveRandomMod();
                        if (!success)
                            return "No mods can be removed";
                        if (target.QualityType != null)
                            target.BaseQuality -= 20;
                        break;
                    case "Orb of Augmentation":
                        if (target.Rarity != ItemRarity.Magic)
                            return "Invalid item rarity for selected currency";
                        if (target.LiveMods.Count > 1)
                            return "Item cannot have another mod";
                        ModLogic.RollAddMod(target, BaseValidMods);
                        if (target.QualityType != null)
                            target.BaseQuality -= 2;
                        break;
                    case "Regal Orb":
                        if (target.Rarity != ItemRarity.Magic)
                            return "Invalid item rarity for selected currency";
                        target.Rarity = ItemRarity.Rare;
                        ModLogic.RollAddMod(target, BaseValidMods);
                        if (target.QualityType != null)
                            target.BaseQuality -= 5;
                        break;
                    case "Exalted Orb":
                        if (target.Rarity != ItemRarity.Rare)
                            return "Invalid item rarity for selected currency";
                        if (target.LiveMods.Count >= 2 * target.GetAffixLimit())
                            return "Item cannot have another mod";
                        ModLogic.RollAddMod(target, BaseValidMods);
                        if (target.QualityType != null)
                            target.BaseQuality -= 20;
                        break;
                    case "Redeemer's Exalted Orb":
                        res = ApplyInfExalt(target, ItemInfluence.Redeemer);
                        if (res != null)
                            return res;
                        break;
                    case "Hunter's Exalted Orb":
                        res = ApplyInfExalt(target, ItemInfluence.Hunter);
                        if (res != null)
                            return res;
                        break;
                    case "Warlord's Exalted Orb":
                        res = ApplyInfExalt(target, ItemInfluence.Warlord);
                        if (res != null)
                            return res;
                        break;
                    case "Crusader's Exalted Orb":
                        res = ApplyInfExalt(target, ItemInfluence.Crusader);
                        if (res != null)
                            return res;
                        break;
                    case "Orb of Scouring":
                        if (target.Rarity == ItemRarity.Normal)
                            return "Invalid item rarity for selected currency";
                        target.ClearMods();
                        break;
                    case "Orb of Alchemy":
                        if (target.Rarity != ItemRarity.Normal)
                            return "Invalid item rarity for selected currency";
                        ModLogic.RerollItem(target, BaseValidMods, ItemRarity.Rare);
                        rolled = true;
                        break;
                    case "Orb of Transmutation":
                        if (target.Rarity != ItemRarity.Normal)
                            return "Invalid item rarity for selected currency";
                        ModLogic.RerollItem(target, BaseValidMods, ItemRarity.Magic);
                        break;
                    case "Remove Crafted Mods":
                        target.ClearMods(true);
                        break;
                    case "Blessed Orb":
                        success = target.RerollImplicits();
                        if (!success)
                            return "No mods can be rerolled";
                        break;
                    case "Abrasive Catalyst":
                    case "Fertile Catalyst":
                    case "Imbued Catalyst":
                    case "Intrinsic Catalyst":
                    case "Prismatic Catalyst":
                    case "Tempering Catalyst":
                    case "Turbulent Catalyst":
                        PoEBaseItemData itemtemplate = CraftingDatabase.AllBaseItems[target.SourceData];
                        if (!CraftingDatabase.ItemClassCatalyst.Contains(itemtemplate.item_class))
                            return "Invalid item type for catalysts";
                        if (target.QualityType == c && target.BaseQuality >= 20)
                            return "Item already has max catalyst quality";
                        target.ApplyCatalyst(c);
                        break;
                    default:
                        return "Unrecognized currency selected";
                }
                if (tries != 1)
                {
                    if (!hasclearedmassresults)         //wait until here to clear old list so it stays if the first iteration returns an error message
                    {
                        MassResults.Clear();
                        hasclearedmassresults = true;
                    }
                    MassResults.Add(target);
                }
                else
                {
                    TallyCurrency(currency.key, 1);
                }
                if (rolled)
                    DoPostRoll(target);
            }
            return null;
        }
        private void TallyCurrency(string k, int n)
        {
            if (k == "RemoveCraftedMods")
                k = "Metadata/Items/Currency/CurrencyConvertToNormal";
            if (CurrencySpent.ContainsKey(k))
                CurrencySpent[k] += n;
            else
                CurrencySpent.Add(k, n);
        }
        private void DoPostRoll(ItemCraft item)
        {
            if (PostRoll.TryCrafts != null) 
            {
                foreach (KeyValuePair<PoEModData, IDictionary<string, int>> kv in PostRoll.TryCrafts)
                {
                    string ret = ForceAddMod(kv.Key, item, kv.Value);
                    if (ret == null)
                        break;
                }
            }
            if (PostRoll.Maximize)
                item.MaximizeMods();
        }
        private string ApplyInfExalt(ItemCraft target, ItemInfluence inf)
        {
            if (target.Rarity != ItemRarity.Rare)
                return "Invalid item rarity for selected currency";
            if (target.ItemLevel < 68)
                return "Item must be ilvl 68+";
            if (target.LiveMods.Count >= 2 * target.GetAffixLimit())
                return "Item cannot have another mod";
            if (target.GetInfluences().Count > 0)
                return "Item already has influence";
            bool res = ModLogic.RollAddInfMod(target, BaseValidMods, inf);
            if (res)
                return null;
            else
                return "Item has no valid rollable mods";
        }
    }
}
