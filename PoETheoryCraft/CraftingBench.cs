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
        public bool FillAffix;
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
                if (value != null)
                {
                    //if base type changed, update basevalidmods
                    if (_benchitem == null || _benchitem.SourceData != value.SourceData)
                    {
                        PoEBaseItemData itemtemplate = CraftingDatabase.AllBaseItems[value.SourceData];
                        BaseValidMods = ModLogic.FindBaseValidMods(itemtemplate, CraftingDatabase.CoreMods.Values);
                    }
                }
                else
                {
                    BaseValidMods.Clear();
                }
                _benchitem = value;
            }
        }
        public IList<ItemCraft> MassResults { get; } = new List<ItemCraft>();   //storage for mass crafting results
        private delegate void CurrencyAction(ItemCraft item);
        public CraftingBench()
        {
            CurrencySpent = new Dictionary<string, int>();
        }
        //Adds a mod directly to target item (or bench item) if legal; updates costs if provided; modifies mod pool accordingly if provided
        public string ForceAddMod(PoEModData mod, ItemCraft target = null, IDictionary<string, int> costs = null, IDictionary<PoEModData, int> pool = null)
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
            if (mod.domain == "crafted")   //if crafted check the specific cases of quality craft on item w/ quality mod, and conversion glove mod
            {
                if (target.LiveTags.Contains("local_item_quality"))
                {
                    foreach (PoEModWeight w in mod.spawn_weights)
                    {
                        if (w.tag == "local_item_quality" && w.weight == 0)
                            return "Cannot craft quality on an item with another quality mod";
                    }
                }
                if (target.LiveTags.Contains("has_physical_conversion_mod") && mod.adds_tags.Contains("has_physical_conversion_mod"))
                    return "Item already has a physical conversion mod";
                //This check turned out to the too restrictive. Many crafted mods have 0 spawn weight on item types they should be craftable on.
                //if (ModLogic.CalcGenWeight(mod, target.LiveTags) <= 0)
                //    return "Invalid craft for current item and/or item mods";
            }
            PoEBaseItemData itemtemplate = CraftingDatabase.AllBaseItems[target.SourceData];
            //if it's an influenced mod, add the appropriate tag
            foreach (ItemInfluence inf in Enum.GetValues(typeof(ItemInfluence)))
            {
                if (EnumConverter.InfToNames(inf).Contains(mod.name))
                {
                    string inftag = itemtemplate.item_class_properties[EnumConverter.InfToTag((ItemInfluence)inf)];
                    if (inftag != null)
                        target.LiveTags.Add(inftag);
                    break;
                }
            }
            //if a mod pool is provided, updated it accordingly, otherwise just add the mod directly
            if (pool != null)
                ModLogic.AddModAndTrim(target, pool, mod);
            else
                target.AddMod(mod);
            ItemRarity newrarity = target.GetMinimumRarity();
            if (newrarity > target.Rarity)
                target.Rarity = newrarity;
            if (costs != null && target == BenchItem)
            {
                foreach (string s in costs.Keys)
                {
                    TallyCurrency(s, costs[s]);
                }
            }
            return null;
        }
        //Performs an action once on BenchItem, or many times on copies of it to build mass results
        private void ApplyCurrency(CurrencyAction action, int count)
        {
            if (count == 1)
            {
                action(BenchItem);
            }
            else
            {
                MassResults.Clear();
                for (int n = 0; n < count; n++)
                {
                    ItemCraft copy = BenchItem.Copy();
                    action(copy);
                    MassResults.Add(copy);
                }
            }
        }
        private void DoReroll(RollOptions ops, IDictionary<PoEModData, int> pool, ItemRarity targetrarity, bool ignoremeta, int count)
        {
            ItemCraft dummy = BenchItem.Copy();
            if (ignoremeta)
                dummy.ClearCraftedMods();
            dummy.ClearMods();
            dummy.Rarity = targetrarity;
            IDictionary<PoEModData, int> validatedpool = ModLogic.FindValidMods(dummy, pool, op: ops);
            ApplyCurrency((item) => 
            {
                if (ignoremeta)
                    item.ClearCraftedMods();
                item.ClearMods();
                item.Rarity = targetrarity;
                IDictionary<PoEModData, int> poolcopy = new Dictionary<PoEModData, int>(validatedpool);
                ModLogic.RollItem(item, poolcopy, ops);
                DoPostRoll(item, poolcopy);
                item.GenerateName();
            }, count);
        }
        private bool DoAddMod(ItemRarity targetrarity, bool rename, int count, int qualityconsumed, ItemInfluence? inf = null)
        {
            string inftag = null;
            if (inf != null)
            {
                inftag = CraftingDatabase.AllBaseItems[BenchItem.SourceData].item_class_properties[EnumConverter.InfToTag(inf.Value)];
            }
            ItemCraft dummy = BenchItem.Copy();
            if (inftag != null)
                dummy.LiveTags.Add(inftag);
            dummy.Rarity = targetrarity;
            IDictionary<PoEModData, int> validatedpool = ModLogic.FindValidMods(dummy, BaseValidMods);
            if (inf != null)
                validatedpool = ModLogic.FilterForInfluence(validatedpool, inf.Value);
            if (validatedpool.Count == 0)
                return false;
            ApplyCurrency((item) =>
            {
                if (inftag != null)
                    item.LiveTags.Add(inftag);
                item.Rarity = targetrarity;
                IDictionary<PoEModData, int> poolcopy = new Dictionary<PoEModData, int>(validatedpool);
                ModLogic.RollAddMod(item, poolcopy);
                if (item.QualityType != null)
                    item.BaseQuality -= qualityconsumed;
                if (rename)
                    item.GenerateName();
            }, count);
            return true;
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
            RollOptions op = new RollOptions() { ForceMods = new List<PoEModData>() { mod }, ILvlCap = ess.item_level_restriction ?? 200 };
            if (tries == 1)
                TallyCurrency(ess.key, 1);
            DoReroll(op, BaseValidMods, ItemRarity.Rare, true, tries);
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
            ISet<PoEModData> fossilmods = new HashSet<PoEModData>();    //forced mods from fossils
            int cesscount = 0;                                          //glyphic/tangled corrupted mod count
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
                if (fossil.corrupted_essence_chance > 0)
                {
                    cesscount += fossil.corrupted_essence_chance;
                }
                modweightgroups.Add(fossil.negative_mod_weights);
                modweightgroups.Add(fossil.positive_mod_weights);
            }
            IList<PoEModData> forcedmods = new List<PoEModData>(ModLogic.FindBaseValidMods(itemtemplate, fossilmods, true).Keys);   //filter by spawn_weight first
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
            if (cesscount > 0)
            {
                //check that the item can roll a corrupted ess mod
                ItemCraft clone = BenchItem.Copy();
                clone.ClearMods();
                PoEModData glyphicmod = ModLogic.RollGlyphicMod(clone, modweightgroups);
                if (glyphicmod == null)
                    return "Item cannot roll forced corrupted essence mods";
            }
            RollOptions ops = new RollOptions() { ForceMods = forcedmods, ModWeightGroups = modweightgroups, GlyphicCount = cesscount };
            if (tries == 1)
            {
                foreach (PoEFossilData fossil in fossils)
                {
                    TallyCurrency(fossil.key, 1);
                }
            }
            DoReroll(ops, extendedpool, ItemRarity.Rare, true, tries);
            return null;
        }
        public string ApplyCurrency(PoECurrencyData currency, int tries = 1)
        {
            if (BenchItem == null)
                return "Bench is empty";
            string c = currency.name;
            string res;
            switch (c)
            {
                case "Chaos Orb":
                    if (BenchItem.Rarity != ItemRarity.Rare)
                        return "Invalid item rarity for selected currency";
                    DoReroll(null, BaseValidMods, ItemRarity.Rare, false, tries);
                    break;
                case "Orb of Alteration":
                    if (BenchItem.Rarity != ItemRarity.Magic)
                        return "Invalid item rarity for selected currency";
                    DoReroll(null, BaseValidMods, ItemRarity.Magic, false, tries);
                    break;
                case "Orb of Alchemy":
                    if (BenchItem.Rarity != ItemRarity.Normal)
                        return "Invalid item rarity for selected currency";
                    DoReroll(null, BaseValidMods, ItemRarity.Rare, false, tries);
                    break;
                case "Orb of Transmutation":
                    if (BenchItem.Rarity != ItemRarity.Normal)
                        return "Invalid item rarity for selected currency";
                    DoReroll(null, BaseValidMods, ItemRarity.Magic, false, tries);
                    break;
                case "Exalted Orb":
                    if (BenchItem.Rarity != ItemRarity.Rare)
                        return "Invalid item rarity for selected currency";
                    if (BenchItem.LiveMods.Count >= 2 * BenchItem.GetAffixLimit())
                        return "Item cannot have another mod";
                    res = DoAddMod(ItemRarity.Rare, false, tries, 20) ? null : "Item has no valid rollable mods";
                    if (res != null)
                        return res;
                    break;
                case "Regal Orb":
                    if (BenchItem.Rarity != ItemRarity.Magic)
                        return "Invalid item rarity for selected currency";
                    DoAddMod(ItemRarity.Rare, true, tries, 5);
                    break;
                case "Orb of Augmentation":
                    if (BenchItem.Rarity != ItemRarity.Magic)
                        return "Invalid item rarity for selected currency";
                    if (BenchItem.LiveMods.Count >= 2 * BenchItem.GetAffixLimit())
                        return "Item cannot have another mod";
                    DoAddMod(ItemRarity.Magic, true, tries, 2);
                    break;
                case "Redeemer's Exalted Orb":
                    res = ApplyInfExalt(ItemInfluence.Redeemer, tries);
                    if (res != null)
                        return res;
                    break;
                case "Hunter's Exalted Orb":
                    res = ApplyInfExalt(ItemInfluence.Hunter, tries);
                    if (res != null)
                        return res;
                    break;
                case "Warlord's Exalted Orb":
                    res = ApplyInfExalt(ItemInfluence.Warlord, tries);
                    if (res != null)
                        return res;
                    break;
                case "Crusader's Exalted Orb":
                    res = ApplyInfExalt(ItemInfluence.Crusader, tries);
                    if (res != null)
                        return res;
                    break;
                case "Orb of Annulment":
                    if (BenchItem.Rarity == ItemRarity.Normal)
                        return "Invalid item rarity for selected currency";
                    ApplyCurrency((item) =>
                    {
                        item.RemoveRandomMod();
                        if (item.QualityType != null)
                            item.BaseQuality -= 20;
                    }, tries);
                    break;
                case "Divine Orb":
                    if (BenchItem.Rarity == ItemRarity.Normal)
                        return "Invalid item rarity for selected currency";
                    ApplyCurrency((item) =>
                    {
                        item.RerollExplicits();
                    }, tries);
                    break;
                case "Blessed Orb":
                    ApplyCurrency((item) =>
                    {
                        item.RerollImplicits();
                    }, tries);
                    break;
                case "Orb of Scouring":
                    if (BenchItem.Rarity == ItemRarity.Normal)
                        return "Invalid item rarity for selected currency";
                    ApplyCurrency((item) =>
                    {
                        item.ClearMods();
                        item.Rarity = item.GetMinimumRarity();
                        item.GenerateName();
                    }, tries);
                    break;
                case "Remove Crafted Mods":
                    ApplyCurrency((item) =>
                    {
                        item.ClearCraftedMods();
                    }, tries);
                    break;
                case "Do Nothing":
                    IDictionary<PoEModData, int> validatedpool = ModLogic.FindValidMods(BenchItem, BaseValidMods);
                    ApplyCurrency((item) => 
                    {
                        DoPostRoll(item, new Dictionary<PoEModData, int>(validatedpool));
                    }, tries);
                    break;
                case "Abrasive Catalyst":
                case "Fertile Catalyst":
                case "Imbued Catalyst":
                case "Intrinsic Catalyst":
                case "Prismatic Catalyst":
                case "Tempering Catalyst":
                case "Turbulent Catalyst":
                    PoEBaseItemData itemtemplate = CraftingDatabase.AllBaseItems[BenchItem.SourceData];
                    if (!CraftingDatabase.ItemClassCatalyst.Contains(itemtemplate.item_class))
                        return "Invalid item type for catalysts";
                    if (BenchItem.QualityType == c && BenchItem.BaseQuality >= 20)
                        return "Item already has max catalyst quality";
                    ApplyCurrency((item) =>
                    {
                        item.ApplyCatalyst(c);
                    }, tries);
                    break;
                default:
                    return "Unrecognized currency selected";
            }
            if (tries == 1)
            {
                string currencykey = currency.key;
                if (currencykey == "RemoveCraftedMods")
                    currencykey = "Metadata/Items/Currency/CurrencyConvertToNormal";
                if (currencykey != "DoNothing")
                    TallyCurrency(currency.key, 1);
            }
                
            return null;
        }
        private string ApplyInfExalt(ItemInfluence inf, int tries)
        {
            if (BenchItem.Rarity != ItemRarity.Rare)
                return "Invalid item rarity for selected currency";
            if (BenchItem.ItemLevel < 68)
                return "Item must be ilvl 68+";
            if (BenchItem.LiveMods.Count >= 2 * BenchItem.GetAffixLimit())
                return "Item cannot have another mod";
            if (BenchItem.GetInfluences().Count > 0)
                return "Item already has influence";
            bool res = DoAddMod(ItemRarity.Rare, false, tries, 20, inf);
            if (res)
                return null;
            else
                return "Item has no valid rollable mods";
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
        private void DoPostRoll(ItemCraft item, IDictionary<PoEModData, int> pool)
        {
            if (PostRoll.TryCrafts != null) 
            {
                foreach (KeyValuePair<PoEModData, IDictionary<string, int>> kv in PostRoll.TryCrafts)
                {
                    string ret = ForceAddMod(kv.Key, item, kv.Value, pool);
                    if (ret == null)
                        break;
                }
            }
            if (PostRoll.FillAffix)
            {
                while (item.LiveMods.Count < item.GetAffixLimit() * 2)
                {
                    ModLogic.RollAddMod(item, pool);
                    if (item.QualityType != null)
                    {
                        if (item.Rarity == ItemRarity.Rare)
                            item.BaseQuality -= 20;
                        else
                            item.BaseQuality -= 2;
                    }
                }
            }
            if (PostRoll.Maximize)
                item.MaximizeMods();
        }
    }
}
