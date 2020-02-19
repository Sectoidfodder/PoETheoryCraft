using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PoETheoryCraft.DataClasses;

namespace PoETheoryCraft.Utils
{
    public class RollOptions
    {
        public bool IgnoreMeta { get; set; } = false;               //ignore metamod locks
        public IList<PoEModData> ForceMods { get; set; } = null;    //always add these
        public ISet<IList<PoEModWeight>> ModWeightGroups { get; set; }  //first matching tag of each list is applied
        public int ILvlCap { get; set; } = 200;                     //if lower than item's ilvl, use this instead
    }
    public static class ModLogic
    {
        public const string PrefixLock = "StrMasterItemGenerationCannotChangePrefixes";
        public const string SuffixLock = "DexMasterItemGenerationCannotChangeSuffixes";
        public const string Prefix = "prefix";
        public const string Suffix = "suffix";
        public static IDictionary<string, IList<string>> CatalystTags { get; } = new Dictionary<string, IList<string>>()
        {
            { "Abrasive Catalyst", new List<string>(){ "jewellery_attack", "attack" } },
            { "Imbued Catalyst", new List<string>(){ "jewellery_caster", "caster" } },
            { "Fertile Catalyst", new List<string>(){ "jewellery_resource", "life", "mana" } },
            { "Tempering Catalyst", new List<string>(){ "jewellery_defense" } },   //"defences" fossil tag doesn't count according to PoEDB
            { "Intrinsic Catalyst", new List<string>(){ "jewellery_attribute" } },
            { "Prismatic Catalyst", new List<string>(){ "jewellery_resistance" } },
            { "Turbulent Catalyst", new List<string>(){ "jewellery_elemental" } }
        };
        public static IList<int> ModCountWeights { set; get; } = new List<int>()
        {
            Properties.Settings.Default.MW4,
            Properties.Settings.Default.MW5,
            Properties.Settings.Default.MW6
        };
        public static IList<int> JewelModCountWeights { set; get; } = new List<int>()
        {
            Properties.Settings.Default.JMW3,
            Properties.Settings.Default.JMW4
        };
        public static IList<int> MagicModCountWeights { set; get; } = new List<int>() 
        { 
            Properties.Settings.Default.MMW1,
            Properties.Settings.Default.MMW2
        };
        //rerolls item to new one of given rarity, basemods is a superset of valid mods, only dict keys are used, values(weights) are recalculated
        public static void RerollItem(ItemCraft item, IDictionary<PoEModData, int> basemods, ItemRarity rarity, RollOptions op = null)
        {
            if (op != null && op.IgnoreMeta)     //to ignore metamods, simply remove them first
                item.ClearCraftedMods();
            item.ClearMods();
            item.Rarity = rarity;

            if (op != null && op.ForceMods != null)
            {
                foreach (PoEModData f in op.ForceMods)
                {
                    item.AddMod(f);
                }
            }

            int modcount = RollModCount(item.Rarity, CraftingDatabase.AllBaseItems[item.SourceData].item_class);
            while (item.LiveMods.Count < modcount)  
            {
                basemods = FindValidMods(item, basemods, op: op);   //assumes that no explicit can increase an item's pool of valid explicits
                PoEModData mod = ChooseMod(basemods);
                if (mod == null)
                    break;
                item.AddMod(mod);
            }
            item.UpdateName();
        }
        //adds one mod to item, automatically updates rarity, basemods is a superset of valid mods, only dict keys are used, values(weights) are recalculated
        public static bool RollAddMod(ItemCraft item, IDictionary<PoEModData, int> basemods)
        {
            PoEModData mod = ChooseMod(FindValidMods(item, basemods));
            if (mod != null)
            {
                item.AddMod(mod);
                return true;
            }
            return false;
        }
        //adds one influenced mod to item, basemods is a superset of valid mods, only dict keys are used, values(weights) are recalculated
        public static bool RollAddInfMod(ItemCraft item, IDictionary<PoEModData, int> basemods, ItemInfluence inf)
        {
            PoEBaseItemData itemtemplate = CraftingDatabase.AllBaseItems[item.SourceData];
            string inftag = itemtemplate.item_class_properties[EnumConverter.InfToTag(inf)];
            if (inftag == null)
                return false;
            item.LiveTags.Add(inftag);
            ISet<string> tagstoadd = new HashSet<string>();
            IDictionary<PoEModData, int> mods = FindValidMods(item, basemods);
            IDictionary<PoEModData, int> infmods = FilterForInfluence(mods, inf, itemtemplate);
            foreach (string s in tagstoadd)
            {
                item.LiveTags.Add(s);
            }
            PoEModData fmod = ChooseMod(infmods);
            if (fmod != null)
            {
                item.AddMod(fmod);
                return true;
            }
            item.LiveTags.Remove(inftag);   //undo influence if no valid mod found
            return false;
        }
        //Returns subset of dict containing mods of given influence for given item type
        public static IDictionary<PoEModData, int> FilterForInfluence(IDictionary<PoEModData, int> dict, ItemInfluence inf, PoEBaseItemData baseitem)
        {
            IDictionary<PoEModData, int> filtereddict = new Dictionary<PoEModData, int>();
            foreach (PoEModData mod in dict.Keys)
            {
                if (GetInfluence(mod, baseitem) == inf)
                {
                    filtereddict.Add(mod, dict[mod]);
                }
            }
            return filtereddict;
        }
        //returns the influence type of the mod (or null), which technically COULD depend on the item base, even if it doesn't currently
        public static ItemInfluence? GetInfluence(PoEModData mod, PoEBaseItemData item)
        {
            foreach (ItemInfluence inf in Enum.GetValues(typeof(ItemInfluence)))
            {
                string inftag = item.item_class_properties[EnumConverter.InfToTag(inf)];
                foreach (PoEModWeight w in mod.spawn_weights)
                {
                    if (w.tag == inftag && w.weight > 0)
                        return inf;
                }
            }
            return null;
        }
        //Starts from db and checks only the item template's domain and tags, used for pruning so we check against ~300 mods per roll instead of ~3000
        //ignoredomain used for forced fossil mods from "delve" domain, only filtering them by spawn weight based on tags and base item
        public static IDictionary<PoEModData, int> FindBaseValidMods(PoEBaseItemData baseitem, ICollection<PoEModData> db, bool ignoredomain = false)
        {
            IDictionary<PoEModData, int> mods = new Dictionary<PoEModData, int>();
            if (baseitem == null)
                return mods;
            //extend tags to allow mods of any influence and special mods enabled by convoking wand's implicit
            ISet<string> extendedtags = new HashSet<string>(baseitem.tags) { "weapon_can_roll_minion_modifiers" };
            foreach (ItemInfluence inf in Enum.GetValues(typeof(ItemInfluence)))
                extendedtags.Add(baseitem.item_class_properties[EnumConverter.InfToTag(inf)]);
            foreach (PoEModData mod in db)
            {
                if (ignoredomain || mod.domain == baseitem.domain)
                {
                    int w = CalcGenWeight(mod, extendedtags);
                    if (w > 0)
                        mods.Add(mod, w);
                }
            }
            return mods;
        }
        //Starts from basemods and checks ilvl, live tags (including influence), existing mod groups, option to ignore prefix/suffix space, checks ilvlcap and modweightgroups from RollOptions
        public static IDictionary<PoEModData, int> FindValidMods(ItemCraft item, IDictionary<PoEModData, int> basemods, bool ignorerarity = false, RollOptions op = null)
        {
            IDictionary<PoEModData, int> mods = new Dictionary<PoEModData, int>();
            if (item == null)
                return mods;
            //check for open prefix/suffix
            bool openprefix = item.ModCountByType(Prefix) < item.GetAffixLimit(ignorerarity);
            bool opensuffix = item.ModCountByType(Suffix) < item.GetAffixLimit(ignorerarity);
            //list existing mod groups
            ISet<string> groups = new HashSet<string>();
            foreach (ModCraft m in item.LiveMods)
            {
                PoEModData modtemplate = CraftingDatabase.AllMods[m.SourceData];
                groups.Add(modtemplate.group);
            }
            int levelcap = (op != null && op.ILvlCap > 0 && op.ILvlCap < item.ItemLevel) ? op.ILvlCap : item.ItemLevel;
            foreach (PoEModData mod in basemods.Keys)
            {
                //intentionally not checking against domain here to allow delve mods, shouldn't be a problem since domain was filtered before
                if (!openprefix && mod.generation_type == Prefix || !opensuffix && mod.generation_type == Suffix)
                    continue;
                if (mod.required_level > levelcap || groups.Contains(mod.group))
                    continue;   
                int w = (op != null) ? CalcGenWeight(mod, item.LiveTags, op.ModWeightGroups) : CalcGenWeight(mod, item.LiveTags);
                if (w > 0)
                    mods.Add(mod, w);
            }
            return mods;
        }
        //Uses benchops to find relevant mod templates in db for the item base
        public static IDictionary<PoEModData, IDictionary<string, int>> FindValidBenchMods(PoEBaseItemData item, ISet<PoEBenchOption> benchops, IDictionary<string, PoEModData> db)
        {
            IDictionary<PoEModData, IDictionary<string, int>> mods = new Dictionary<PoEModData, IDictionary<string, int>>();
            if (item == null)
                return mods;
            foreach (PoEBenchOption b in benchops)
            {
                if (!b.item_classes.Contains(item.item_class))
                    continue;
                PoEModData mod = db[b.mod_id];
                mods.Add(mod, b.cost);
            }
            return mods;
        }
        //picks from a dictionary of mod templates and weights
        private static PoEModData ChooseMod(IDictionary<PoEModData, int> mods)
        {
            int totalweight = mods.Values.Sum();
            int roll = RNG.Gen.Next(totalweight);
            Debug.Write("rolled " + roll + " out of " + totalweight + ", ");
            int counter = 0;
            foreach (PoEModData mod in mods.Keys)
            {
                counter += mods[mod];
                if (counter > roll)
                {
                    Debug.WriteLine(mod.key);
                    return mod;
                }
            }
            return null;
        }
        //applies a list of tags to mod template to calculate final weight
        private static int RollModCount(ItemRarity r, string itemclass)
        {
            if (r == ItemRarity.Magic)
            {
                int roll = RNG.Gen.Next(MagicModCountWeights[0] + MagicModCountWeights[1]);
                return (roll < MagicModCountWeights[0]) ? 1 : 2;
            }
            else if (r== ItemRarity.Rare)
            {
                if (itemclass.Contains("Jewel"))
                {
                    int roll = RNG.Gen.Next(JewelModCountWeights[0] + JewelModCountWeights[1]);
                    return (roll < JewelModCountWeights[0]) ? 3 : 4;
                }
                else
                {
                    int roll = RNG.Gen.Next(ModCountWeights[0] + ModCountWeights[1] + ModCountWeights[2]);
                    return (roll < ModCountWeights[0]) ? 4 : (roll < ModCountWeights[0] + ModCountWeights[1]) ? 5 : 6;
                }
            }
            return 0;
        }
        public static int CalcGenWeight(PoEModData mod, ISet<string> tags, ISet<IList<PoEModWeight>> weightgroups = null)
        {
            int weight = 0;
            if (mod.spawn_weights != null)
            {
                foreach (PoEModWeight w in mod.spawn_weights)
                {
                    if (tags.Contains(w.tag))
                    {
                        weight = w.weight;
                        break;
                    }
                }
            }
            if (mod.generation_weights != null)
            {
                foreach (PoEModWeight w in mod.generation_weights)
                {
                    if (tags.Contains(w.tag))
                    {
                        weight = weight * w.weight / 100;
                        break;
                    }
                }
            }
            if (weightgroups != null)
            {
                foreach (IList<PoEModWeight> l in weightgroups)
                {
                    foreach (PoEModWeight w in l)
                    {
                        if (mod.type_tags.Contains(w.tag))
                        {
                            weight = weight * w.weight / 100;
                            break;
                        }
                    }
                    if (weight == 0)
                        break;
                }
            }
            return weight;
        }
    }
}
