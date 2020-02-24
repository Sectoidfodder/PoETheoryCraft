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
        public IList<PoEModData> ForceMods { get; set; } = null;        //always add these
        public ISet<IList<PoEModWeight>> ModWeightGroups { get; set; }  //first matching tag of each list is applied
        public int ILvlCap { get; set; } = 200;                         //if lower than item's ilvl, use this instead
        public int GlyphicCount { get; set; } = 0;                      //100 if glyphic, 10 if tangled, 110 if both
    }
    public static class ModLogic
    {
        private static readonly double CatalystEffect = Properties.Settings.Default.CatalystEffect;
        public const string PrefixLock = "StrMasterItemGenerationCannotChangePrefixes";
        public const string SuffixLock = "DexMasterItemGenerationCannotChangeSuffixes";
        public const string CatalystIgnore = "jewellery_quality_ignore";
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

        //Fills item with mods based on its rarity, pulling from basemods, destructively modifies basemods to reflect the rollable pool for new item
        public static bool RollItem(ItemCraft item, IDictionary<PoEModData, int> basemods, RollOptions op = null)
        {

            if (op != null && op.ForceMods != null)
            {
                foreach (PoEModData f in op.ForceMods)
                {
                    AddModAndTrim(item, basemods, f);
                }
            }
            if (op != null && op.GlyphicCount > 0)
            {
                for (int i = 0; i < op.GlyphicCount / 100 + ((RNG.Gen.Next(100) < op.GlyphicCount % 100) ? 1 : 0); i++)
                {
                    PoEModData glyphicmod = RollGlyphicMod(item, op.ModWeightGroups);
                    if (glyphicmod == null)
                        break;
                    AddModAndTrim(item, basemods, glyphicmod);
                }
            }

            int modcount = RollModCount(item.Rarity, CraftingDatabase.AllBaseItems[item.SourceData].item_class);
            while (item.LiveMods.Count < modcount)  
            {
                PoEModData mod = ChooseMod(basemods);
                if (mod == null)
                    break;
                AddModAndTrim(item, basemods, mod);
            }
            return true;
        }

        //Adds mod to item, destructively modifies basemods to reflect the new rollable pool
        private static void AddModAndTrim(ItemCraft item, IDictionary<PoEModData, int> basemods, PoEModData mod)
        {
            ISet<string> newtags = new HashSet<string>(mod.adds_tags);
            ISet<string> oldtags = new HashSet<string>(item.LiveTags);
            foreach (string s in oldtags)
            {
                newtags.Remove(s);
            }
            string addedgroup = mod.group;
            item.AddMod(mod);
            string affixfill = null;
            if (mod.generation_type == Prefix && item.ModCountByType(Prefix) >= item.GetAffixLimit())
                affixfill = Prefix;
            else if (mod.generation_type == Suffix && item.ModCountByType(Suffix) >= item.GetAffixLimit())
                affixfill = Suffix;
            TrimMods(basemods, oldtags, newtags, addedgroup, affixfill);
        }
        //Destructively modifies basedmods according to tags added, group added, and prefix/suffix filled if neccesary
        private static void TrimMods(IDictionary<PoEModData, int> basemods, ISet<string> oldtags, ISet<string> newtags, string addedgroup, string affixfill)
        {
            foreach (PoEModData mod in basemods.Keys.ToList<PoEModData>())
            {
                if (mod.group == addedgroup || mod.generation_type == affixfill)
                {
                    basemods.Remove(mod);
                    continue;
                }
                if (newtags.Count > 0)
                {
                    //if tags were added, adjust weight by the ratio of the new/old spawn and generation weights
                    int weight = basemods[mod];
                    int oldw = -1;
                    int neww = -1;
                    foreach (PoEModWeight w in mod.spawn_weights)
                    {
                        if (oldw == -1 && oldtags.Contains(w.tag))
                        {
                            oldw = w.weight;
                            break;
                        }
                        if (neww == -1 && newtags.Contains(w.tag))
                            neww = w.weight;
                    }
                    if (neww != -1)
                        weight = weight * neww / oldw;
                    if (mod.generation_weights != null)
                    {
                        oldw = -1;
                        neww = -1;
                        foreach (PoEModWeight w in mod.generation_weights)
                        {
                            if (oldw == -1 && oldtags.Contains(w.tag))
                            {
                                oldw = w.weight;
                                break;
                            }
                            if (neww == -1 && newtags.Contains(w.tag))
                                neww = w.weight;
                        }
                        if (neww != -1)
                            weight = weight * neww / oldw;
                    }
                    if (weight > 0)
                        basemods[mod] = weight;
                    else
                        basemods.Remove(mod);
                }
            }
        }
        //rolls a corrupted fossil mod for the given item and fossil weight modifiers, or null if none possible
        public static PoEModData RollGlyphicMod(ItemCraft item, ISet<IList<PoEModWeight>> weightmods)
        {
            IDictionary<PoEModData, int> mods = new Dictionary<PoEModData, int>();
            string itemclass = CraftingDatabase.AllBaseItems[item.SourceData].item_class;
            if (itemclass == "Rune Dagger")
                itemclass = "Dagger";
            if (itemclass == "Warstaff")
                itemclass = "Staff";
            //check for open prefix/suffix
            bool openprefix = item.ModCountByType(Prefix) < item.GetAffixLimit(true);
            bool opensuffix = item.ModCountByType(Suffix) < item.GetAffixLimit(true);
            //track existing mod groups
            ISet<string> groups = new HashSet<string>();
            foreach (ModCraft m in item.LiveMods)
            {
                PoEModData modtemplate = CraftingDatabase.AllMods[m.SourceData];
                groups.Add(modtemplate.group);
            }
            IList<string> catalysttags = null;
            if (item.QualityType != null && CatalystTags.ContainsKey(item.QualityType))
                catalysttags = CatalystTags[item.QualityType];
            foreach (PoEEssenceData ess in CraftingDatabase.Essences.Values)
            {
                if (ess.type.is_corruption_only && ess.mods.Keys.Contains(itemclass))
                {
                    PoEModData m = CraftingDatabase.CoreMods[ess.mods[itemclass]];
                    if (m.generation_type == Prefix && !openprefix)
                        continue;
                    if (m.generation_type == Suffix && !opensuffix)
                        continue;
                    if (groups.Contains(m.group))
                        continue;
                    int weight = CalcGenWeight(m, item.LiveTags, weightmods, catalysttags, item.BaseQuality, 1000);
                    if (weight > 0)
                        mods.Add(m, weight);
                }
            }
            if (mods.Count == 0)
                return null;
            else
                return ChooseMod(mods);
        }
        //adds one mod from basemods to item, updates rarity, destructively modifies basemods to reflect new rollable pool, returns false if no mod was able to be added
        public static bool RollAddMod(ItemCraft item, IDictionary<PoEModData, int> basemods, ItemInfluence? inf = null)
        {
            PoEModData mod = ChooseMod(basemods, inf);
            if (mod != null)
            {
                AddModAndTrim(item, basemods, mod);
                return true;
            }
            return false;
        }
        //adds one influenced mod from basemods to item, returns trimmed pool of influenced mods
        //public static IDictionary<PoEModData, int> RollAddInfMod(ItemCraft item, IDictionary<PoEModData, int> basemods, ItemInfluence inf)
        //{
        //    PoEBaseItemData itemtemplate = CraftingDatabase.AllBaseItems[item.SourceData];
        //    string inftag = itemtemplate.item_class_properties[EnumConverter.InfToTag(inf)];
        //    if (inftag == null)
        //        return false;
        //    item.LiveTags.Add(inftag);
        //    ISet<string> tagstoadd = new HashSet<string>();
        //    IDictionary<PoEModData, int> mods = FindValidMods(item, basemods);
        //    IDictionary<PoEModData, int> infmods = FilterForInfluence(mods, inf, itemtemplate);
        //    foreach (string s in tagstoadd)
        //    {
        //        item.LiveTags.Add(s);
        //    }
        //    PoEModData fmod = ChooseMod(infmods);
        //    if (fmod != null)
        //    {
        //        item.AddMod(fmod);
        //        return true;
        //    }
        //    item.LiveTags.Remove(inftag);   //undo influence if no valid mod found
        //    return false;
        //}
        //Returns subset of dict containing mods of given influence for given item type
        public static IDictionary<PoEModData, int> FilterForInfluence(IDictionary<PoEModData, int> dict, ItemInfluence inf)
        {
            IDictionary<PoEModData, int> filtereddict = new Dictionary<PoEModData, int>();
            IList<string> infnames = EnumConverter.InfToNames(inf);
            foreach (PoEModData mod in dict.Keys)
            {
                if (infnames.Contains(mod.name))
                {
                    filtereddict.Add(mod, dict[mod]);
                }
            }
            return filtereddict;
        }
        ////returns the influence type of the mod (or null), which technically COULD depend on the item base, even if it doesn't currently
        //public static ItemInfluence? GetInfluence(PoEModData mod, PoEBaseItemData item)
        //{
        //    foreach (ItemInfluence inf in Enum.GetValues(typeof(ItemInfluence)))
        //    {
        //        string inftag = item.item_class_properties[EnumConverter.InfToTag(inf)];
        //        foreach (PoEModWeight w in mod.spawn_weights)
        //        {
        //            if (w.tag == inftag && w.weight > 0)
        //                return inf;
        //        }
        //    }
        //    return null;
        //}
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
                IList<string> catalysttags = null;
                if (item.QualityType != null && CatalystTags.ContainsKey(item.QualityType))
                    catalysttags = CatalystTags[item.QualityType];
                int w = CalcGenWeight(mod, item.LiveTags, op?.ModWeightGroups, catalysttags, item.BaseQuality);
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
        private static PoEModData ChooseMod(IDictionary<PoEModData, int> mods, ItemInfluence? inf = null)
        {
            IDictionary<PoEModData, int> targetmods = (inf == null) ? mods : FilterForInfluence(mods, inf.Value);
            int totalweight = targetmods.Values.Sum();
            int roll = RNG.Gen.Next(totalweight);
            int counter = 0;
            foreach (PoEModData mod in targetmods.Keys)
            {
                counter += targetmods[mod];
                if (counter > roll)
                {
                    //Debug.Write("rolled " + roll + " out of " + totalweight + ", ");
                    //Debug.WriteLine(mod.key);
                    return mod;
                }
            }
            return null;
        }
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
        //baseweightoverride used for corrupted essence mods from glyphic/tangled because their templates have no base weights
        private static int CalcGenWeight(PoEModData mod, ISet<string> tags, ISet<IList<PoEModWeight>> weightgroups = null, IList<string> catalysttags = null, int catalystquality = 0, int? baseweightoverride = null)
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
            if (baseweightoverride != null)
                weight = baseweightoverride.Value;
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
            if (!mod.type_tags.Contains(CatalystIgnore) && catalysttags != null)
            {
                bool applycatqual = false;
                foreach (string t in mod.type_tags)
                {
                    if (catalysttags.Contains(t))
                    {
                        applycatqual = true;
                        break;
                    }
                }
                if (applycatqual)
                    weight = (int)(weight * (100 + catalystquality * CatalystEffect) / 100);
            }
            if (weightgroups != null)
            {
                double sumnegs = 0;
                int sumadds = 0;
                foreach (IList<PoEModWeight> l in weightgroups)
                {
                    foreach (PoEModWeight w in l)
                    {
                        if (mod.type_tags.Contains(w.tag))
                        {
                            if (w.weight > 100)
                                sumadds += w.weight;
                            else if (w.weight != 0)
                                sumnegs += (double)100 / w.weight;
                            else
                                weight = 0;
                            break;
                        }
                    }
                    if (weight == 0)
                        break;
                }
                weight = weight * Math.Max(sumadds, 100) / 100;
                weight = (int)(weight / Math.Max(sumnegs, 1));
            }
            return weight;
        }
    }
}
