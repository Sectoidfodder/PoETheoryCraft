using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PoETheoryCraft.DataClasses;
using PoETheoryCraft.Utils;

namespace PoETheoryCraft
{
    public class CraftingBench
    {
        private bool _extendedmodview;
        public bool ExtendedModView     //ValidMods will ignore BenchItem's available prefix/suffix space if true
        {
            get { return _extendedmodview; }
            set
            {
                if (value != _extendedmodview)
                {
                    _extendedmodview = value;
                    UpdatePreviews();
                }
            }
        }   
        public IDictionary<string, int> CurrencySpent { get; private set; }
        public IDictionary<PoEModData, int> BaseValidMods { get; private set; } //all valid mods for BenchItem's source PoEBaseItemData, starting point for crafting
        public IDictionary<PoEModData, int> ValidMods { get; private set; }     //actual valid mods for BenchItem's current state and selected currencies, for view only
        public IDictionary<PoEModData, IDictionary<string, int>> ValidBenchMods { get; private set; }   //valid bench mods for BenchItem's current state
        //public IDictionary<PoEModData, string> ValidSpecialMods { get; private set; }   //extra non-weighted mods from fossils/essences
        private PoEEssenceData PreviewEssence;
        private IList<PoEFossilData> PreviewFossils;
        private string PreviewCurrency;
        private ItemCraft _benchitem;   //item on bench, setting it also updates mod pools
        public ItemCraft BenchItem { 
            get { return _benchitem; }
            set
            {
                _benchitem = value;
                if (_benchitem != null)
                {
                    PoEBaseItemData itemtemplate = CraftingDatabase.AllBaseItems[_benchitem.SourceData];
                    BaseValidMods = ModLogic.FindBaseValidMods(itemtemplate, CraftingDatabase.CoreMods.Values);
                    ValidBenchMods = ModLogic.FindValidBenchMods(itemtemplate, CraftingDatabase.BenchOptions, CraftingDatabase.AllMods);
                    UpdatePreviews();
                }
                else
                {
                    BaseValidMods.Clear();
                    ValidMods.Clear();
                    ValidBenchMods.Clear();
                }
            }
        }
        public IList<ItemCraft> MassResults { get; } = new List<ItemCraft>();   //storage for mass crafting results
        public CraftingBench()
        {
            CurrencySpent = new Dictionary<string, int>();
            ExtendedModView = Properties.Settings.Default.IgnoreAffixCap;
        }
        private void UpdatePreviews()
        {
            if (BenchItem == null)
                return;
            RollOptions ops = new RollOptions();
            IDictionary<PoEModData, int> extendedpool = new Dictionary<PoEModData, int>(BaseValidMods);
            string tagtoremove = null;
            ISet<string> tagstoadd = new HashSet<string>();
            ItemInfluence inf = ItemInfluence.Shaper;   //using this for null/invalid since there's no shaper exalt
            PoEBaseItemData itemtemplate = CraftingDatabase.AllBaseItems[BenchItem.SourceData];
            if (PreviewFossils != null && PreviewFossils.Count > 0)
            {
                ISet<IList<PoEModWeight>> modweightgroups = new HashSet<IList<PoEModWeight>>();
                //ICollection<PoEModData> forcedmods = new List<PoEModData>();
                foreach (PoEFossilData fossil in PreviewFossils)
                {
                    foreach (string t in fossil.added_mods)
                    {
                        if (!extendedpool.ContainsKey(CraftingDatabase.AllMods[t]))
                            extendedpool.Add(CraftingDatabase.AllMods[t], 0);
                    }
                    //foreach (string t in fossil.forced_mods)
                    //{
                    //    forcedmods.Add(CraftingDatabase.MiscMods[t]);
                    //}
                    modweightgroups.Add(fossil.negative_mod_weights);
                    modweightgroups.Add(fossil.positive_mod_weights);
                }
                //forcedmods = ModLogic.FindBaseValidMods(BenchItem.SourceData, forcedmods, true).Keys;   //filter by spawn_weight first
                ops.ModWeightGroups = modweightgroups;
            }
            else if (PreviewEssence != null)
            {
                ops.ILvlCap = PreviewEssence.item_level_restriction ?? 200;
            }
            else if (PreviewCurrency != null && BenchItem.GetInfluences().Count == 0)
            {
                if (PreviewCurrency == "exalt-redeemer")
                    inf = ItemInfluence.Redeemer;
                else if (PreviewCurrency == "exalt-hunter")
                    inf = ItemInfluence.Hunter;
                else if (PreviewCurrency == "exalt-warlord")
                    inf = ItemInfluence.Warlord;
                else if (PreviewCurrency == "exalt-crusader")
                    inf = ItemInfluence.Crusader;
                if (inf != ItemInfluence.Shaper)
                {
                    string tag = itemtemplate.item_class_properties[EnumConverter.InfToTag(inf)];
                    if (tag != null)    //temporarily add influence tag
                    {
                        tagtoremove = tag;
                        BenchItem.LiveTags.Add(tag);
                    }
                    if (ModLogic.InfExaltIgnoreMeta && BenchItem.LiveTags.Contains("no_attack_mods"))
                    {
                        BenchItem.LiveTags.Remove("no_attack_mods");
                        tagstoadd.Add("no_attack_mods");
                    }
                    if (ModLogic.InfExaltIgnoreMeta && BenchItem.LiveTags.Contains("no_caster_mods"))
                    {
                        BenchItem.LiveTags.Remove("no_caster_mods");
                        tagstoadd.Add("no_caster_mods");
                    }
                }
            }
            ValidMods = ModLogic.FindValidMods(BenchItem, extendedpool, ExtendedModView, ops);
            if (inf != ItemInfluence.Shaper)
                ValidMods = ModLogic.FilterForInfluence(ValidMods, inf, itemtemplate);
            if (tagtoremove != null)
                BenchItem.LiveTags.Remove(tagtoremove);
            foreach (string s in tagstoadd)
            {
                BenchItem.LiveTags.Add(s);
            }
        }
        public string AddMod(PoEModData mod)
        {
            if (mod.generation_type == ModLogic.Prefix)
            {
                if (BenchItem.ModCountByType(ModLogic.Prefix) >= BenchItem.GetAffixLimit(true))
                    return "Item cannot have another prefix";
            }
            else
            {
                if (BenchItem.ModCountByType(ModLogic.Suffix) >= BenchItem.GetAffixLimit(true))
                    return "Item cannot have another suffix";
            }
            foreach (ModCraft livemod in BenchItem.LiveMods)
            {
                PoEModData modtemplate = CraftingDatabase.AllMods[livemod.SourceData];
                if (modtemplate.group == mod.group)
                    return "Item already has a mod in this mod group";
            }
            BenchItem.AddMod(mod);
            UpdatePreviews();
            return null;
        }
        public bool RemovePreviewCurrency()
        {
            if (PreviewEssence == null && PreviewFossils == null && PreviewCurrency == null)
                return false;
            PreviewFossils = null;
            PreviewEssence = null;
            PreviewCurrency = null;
            UpdatePreviews();
            return BenchItem != null;
        }
        public bool SetPreviewCurrency(string c)
        {
            if (PreviewEssence == null && PreviewFossils == null && PreviewCurrency == c)
                return false;
            PreviewFossils = null;
            PreviewEssence = null;
            PreviewCurrency = c;
            UpdatePreviews();
            return BenchItem != null;
        }
        public bool SetPreviewEssence(PoEEssenceData ess)
        {
            if (PreviewEssence == ess && PreviewFossils == null && PreviewCurrency == null)
                return false;
            PreviewFossils = null;
            PreviewCurrency = null;
            PreviewEssence = ess;
            UpdatePreviews();
            return BenchItem != null;
        }
        public bool SetPreviewFossils(IList<PoEFossilData> fossils)
        {
            if (PreviewCurrency == null && PreviewEssence == null && PreviewFossils != null && PreviewFossils.Count == fossils.Count)
            {
                bool skip = true;
                foreach (PoEFossilData f in fossils)
                {
                    if (!PreviewFossils.Contains(f))
                    {
                        skip = false;
                        break;
                    }
                }
                if (skip)
                    return false;
            }
            PreviewFossils = fossils;
            PreviewEssence = null;
            PreviewCurrency = null;
            UpdatePreviews();
            return BenchItem != null;
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
                UpdatePreviews();
            }
            else
            {
                MassResults.Clear();
                for (int n = 0; n < tries; n++)
                {
                    ItemCraft target = BenchItem.Copy();
                    ModLogic.RerollItem(target, BaseValidMods, ItemRarity.Rare, op);
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
                UpdatePreviews();
            }
            else
            {
                MassResults.Clear();
                for (int n = 0; n < tries; n++)
                {
                    ItemCraft target = BenchItem.Copy();
                    ModLogic.RerollItem(target, extendedpool, ItemRarity.Rare, ops);
                    MassResults.Add(target);
                }
            }
            return null;
        }
        public string ApplyCurrency(string c, int tries = 1)
        {
            if (BenchItem == null)
                return "Bench is empty";
            bool hasclearedmassresults = false;
            string res;
            for (int n = 0; n < tries; n++)
            {
                ItemCraft target = tries == 1 ? BenchItem : BenchItem.Copy();
                switch (c)
                {
                    case "chaos":
                        if (target.Rarity != ItemRarity.Rare)
                            return "Invalid item rarity for selected currency";
                        ModLogic.RerollItem(target, BaseValidMods, ItemRarity.Rare);
                        break;
                    case "alt":
                        if (target.Rarity != ItemRarity.Magic)
                            return "Invalid item rarity for selected currency";
                        ModLogic.RerollItem(target, BaseValidMods, ItemRarity.Magic);
                        break;
                    case "divine":
                        if (target.Rarity == ItemRarity.Normal)
                            return "Invalid item rarity for selected currency";
                        target.RerollExplicits();
                        break;
                    case "annul":
                        if (target.Rarity == ItemRarity.Normal)
                            return "Invalid item rarity for selected currency";
                        target.RemoveRandomMod();
                        if (target.QualityType != null)
                            target.BaseQuality -= 20;
                        break;
                    case "augment":
                        if (target.Rarity != ItemRarity.Magic)
                            return "Invalid item rarity for selected currency";
                        if (target.LiveMods.Count > 1)
                            return "Item cannot have another mod";
                        ModLogic.RollAddMod(target, BaseValidMods);
                        if (target.QualityType != null)
                            target.BaseQuality -= 2;
                        break;
                    case "regal":
                        if (target.Rarity != ItemRarity.Magic)
                            return "Invalid item rarity for selected currency";
                        target.Rarity = ItemRarity.Rare;
                        ModLogic.RollAddMod(target, BaseValidMods);
                        if (target.QualityType != null)
                            target.BaseQuality -= 5;
                        break;
                    case "exalt":
                        if (target.Rarity != ItemRarity.Rare)
                            return "Invalid item rarity for selected currency";
                        if (target.LiveMods.Count >= 2 * target.GetAffixLimit())
                            return "Item cannot have another mod";
                        ModLogic.RollAddMod(target, BaseValidMods);
                        if (target.QualityType != null)
                            target.BaseQuality -= 20;
                        break;
                    case "exalt-redeemer":
                        res = ApplyInfExalt(target, ItemInfluence.Redeemer);
                        if (res != null)
                            return res;
                        break;
                    case "exalt-hunter":
                        res = ApplyInfExalt(target, ItemInfluence.Hunter);
                        if (res != null)
                            return res;
                        break;
                    case "exalt-warlord":
                        res = ApplyInfExalt(target, ItemInfluence.Warlord);
                        if (res != null)
                            return res;
                        break;
                    case "exalt-crusader":
                        res = ApplyInfExalt(target, ItemInfluence.Crusader);
                        if (res != null)
                            return res;
                        break;
                    case "scour":
                        if (target.Rarity == ItemRarity.Normal)
                            return "Invalid item rarity for selected currency";
                        target.ClearMods();
                        break;
                    case "alch":
                        if (target.Rarity != ItemRarity.Normal)
                            return "Invalid item rarity for selected currency";
                        ModLogic.RerollItem(target, BaseValidMods, ItemRarity.Rare);
                        break;
                    case "transmute":
                        if (target.Rarity != ItemRarity.Normal)
                            return "Invalid item rarity for selected currency";
                        ModLogic.RerollItem(target, BaseValidMods, ItemRarity.Magic);
                        break;
                    case "remove-crafted":
                        target.ClearMods(true);
                        break;
                    case "blessed":
                        target.RerollImplicits();
                        break;
                    case "abrasive":
                    case "fertile":
                    case "imbued":
                    case "intrinsic":
                    case "prismatic":
                    case "tempering":
                    case "turbulent":
                        PoEBaseItemData itemtemplate = CraftingDatabase.AllBaseItems[target.SourceData];
                        if (!CraftingDatabase.ItemClassCatalyst.Contains<string>(itemtemplate.item_class))
                            return "Invalid item type for catalysts";
                        string tag = EnumConverter.CatalystToTag(c);
                        if (target.QualityType == tag && target.BaseQuality >= 20)
                            return "Item already has max catalyst quality";
                        target.ApplyCatalyst(tag);
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
            }
            if (tries == 1)
                UpdatePreviews();
            return null;
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
