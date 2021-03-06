﻿using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using PoETheoryCraft.Controls;
using PoETheoryCraft.DataClasses;

namespace PoETheoryCraft.Utils
{
    /* 
     * Contains all utilities that calculate and compare stats and properties not stored directly in item objects
     */
    public class ItemCraftComparer : IComparer<ItemCraft>
    {
        private struct ItemParams
        {
            public ItemProperties Properties;
            public IDictionary<string, double> Stats;
        }
        //Cache for each item encountered by this compararer, so the expensive functions only get called once per item
        private readonly IDictionary<ItemCraft, ItemParams> Cache = new Dictionary<ItemCraft, ItemParams>();
        public string Key { get; set; }
        public int Compare(ItemCraft x, ItemCraft y)
        {
            ItemProperties props;
            IDictionary<string, double> stats;
            if (Cache.ContainsKey(x))
            {
                props = Cache[x].Properties;
                stats = Cache[x].Stats;
            }
            else
            {
                props = ItemParser.ParseProperties(x);
                stats = ItemParser.ParseItem(x);
                Cache.Add(x, new ItemParams() { Properties = props, Stats = stats });
            }
            double? vx = ItemParser.GetValueByName(Key, x, props, stats);
            if (Cache.ContainsKey(y))
            {
                props = Cache[y].Properties;
                stats = Cache[y].Stats;
            }
            else
            {
                props = ItemParser.ParseProperties(y);
                stats = ItemParser.ParseItem(y);
                Cache.Add(y, new ItemParams() { Properties = props, Stats = stats });
            }
            double? vy = ItemParser.GetValueByName(Key, y, props, stats);
            if (vx != null)
            {
                if (vy != null)
                    return vx.Value.CompareTo(vy.Value);
                else
                    return 1;
            }
            else
            {
                if (vy != null)
                    return -1;
                else
                    return 0;
            }
        }
    }
    public static class ItemParser
    {
        public static List<double> GetSortedValues(IList<ItemCraft> items, string s, double defaultvalue = 0)
        {
            List<double> values = new List<double>();
            if (items.Count >= Properties.Settings.Default.ProgressBarThreshold)
            {
                int i = 0;
                ProgressDialog p = new ProgressDialog() { Title = "Processing...", Steps = items.Count, ReportStep = Math.Max(items.Count / 100, 1) };
                p.Increment = () =>
                {
                    InsertSorted(GetValueByName(s, items[i], ParseProperties(items[i]), ParseItem(items[i])) ?? defaultvalue, values, 0, values.Count);
                    i++;
                };
                p.ShowDialog();
            }
            else
            {
                foreach (ItemCraft item in items)
                {
                    InsertSorted(GetValueByName(s, item, ParseProperties(item), ParseItem(item)) ?? defaultvalue, values, 0, values.Count);
                }
            }
            return values;
        }
        private static void InsertSorted(double d, List<double> values, int first, int last)
        {
            if (values.Count == 0)
            {
                values.Add(d);
            }
            else if (d <= values[first])
            {
                values.Insert(first, d);
            }
            else if (d >= values[last - 1])
            {
                values.Insert(last, d);
            }
            else
            {
                int mid = first + (last - first) / 2;
                if (d >= values[mid])
                    InsertSorted(d, values, mid, last);
                else
                    InsertSorted(d, values, first, mid);
            }
        }
        public static double? GetValueByName(string s, ItemCraft item, ItemProperties props, IDictionary<string, double> stats)
        {
            if (s.IndexOf("[property]") == 0)
            {
                string p = s.Substring(11);
                switch (p)
                {
                    case "Quality":
                        return props.quality;
                    case "Armour":
                        return props.armour;
                    case "Evasion":
                        return props.evasion;
                    case "Energy Shield":
                        return props.energy_shield;
                    case "Block":
                        return props.block;
                    case "Physical Damage":
                        return ((double)props.physical_damage_min + props.physical_damage_max) / 2;
                    case "Critical Strike Chance":
                        return (double)props.critical_strike_chance / 100;
                    case "Attack Speed":
                        return (double)1000 / props.attack_time;
                    case "Physical DPS":
                        return ((double)props.physical_damage_min + props.physical_damage_max) * 1000 / 2 / props.attack_time;
                    case "# Prefixes":
                        return item.ModCountByType(ModLogic.Prefix);
                    case "# Suffixes":
                        return item.ModCountByType(ModLogic.Suffix);
                    case "# Open Prefixes":
                        return item.GetAffixLimit(true) - item.ModCountByType(ModLogic.Prefix);
                    case "# Open Suffixes":
                        return item.GetAffixLimit(true) - item.ModCountByType(ModLogic.Suffix);
                    default:
                        if (item.TempProps != null && item.TempProps.ContainsKey(p))
                            return item.TempProps[p];
                        else
                            return null;
                }
            }
            else if (s.IndexOf("[pseudo]") == 0)
            {
                double total = 0;
                if (CraftingDatabase.PseudoStats.ContainsKey(s))
                {
                    IDictionary<string, double> definition = CraftingDatabase.PseudoStats[s];
                    foreach (string k in definition.Keys)
                    {
                        double? v = GetValueByName(k, item, props, stats);
                        if (v != null)
                            total += v.Value * definition[k];
                    }
                }
                return total;
            }
            else
            {
                if (stats.ContainsKey(s))
                    return stats[s];
                else
                    return null;
            }
        }
        public static ItemProperties ParseProperties(ItemCraft item)
        {
            IDictionary<string, int> mods = new Dictionary<string, int>();
            IList<string> keys = new List<string>() { "arp", "arf", "evp", "evf", "esp", "esf", "blf", "dp", "mindf", "maxdf", "crp", "asp", "qf" }; //all possible property modifiers
            foreach (string s in keys)
            {
                mods.Add(s, 0);
            }
            foreach (ModCraft m in item.LiveMods)
            {
                ParsePropMods(m, mods);
            }
            PoEBaseItemData itemtemplate = CraftingDatabase.AllBaseItems[item.SourceData];
            int qual = item.BaseQuality + mods["qf"];
            //zero out quality if item class is mismatched
            if (CraftingDatabase.ItemClassNoQuality.Contains(itemtemplate.item_class))
                qual = 0;
            else if (CraftingDatabase.ItemClassCatalyst.Contains(itemtemplate.item_class))
            {
                if (item.QualityType == null)
                    qual = 0;
            }
            else
            {
                if (item.QualityType != null)
                    qual = 0;
            }
            int propqual = item.QualityType == null ? qual : 0;
            return new ItemProperties()
            {
                quality = qual,
                armour = (itemtemplate.properties.armour + mods["arf"]) * (100 + mods["arp"] + propqual) / 100,
                evasion = (itemtemplate.properties.evasion + mods["evf"]) * (100 + mods["evp"] + propqual) / 100,
                energy_shield = (itemtemplate.properties.energy_shield + mods["esf"]) * (100 + mods["esp"] + propqual) / 100,
                block = itemtemplate.properties.block + mods["blf"],
                physical_damage_min = (itemtemplate.properties.physical_damage_min + mods["mindf"]) * (100 + mods["dp"] + propqual) / 100,
                physical_damage_max = (itemtemplate.properties.physical_damage_max + mods["maxdf"]) * (100 + mods["dp"] + propqual) / 100,
                critical_strike_chance = itemtemplate.properties.critical_strike_chance * (100 + mods["crp"]) / 100,
                attack_time = itemtemplate.properties.attack_time * 100 / (100 + mods["asp"])
            };
        }
        private static void ParsePropMods(ModCraft m, IDictionary<string, int> mods)
        {
            foreach (ModRoll s in m.Stats)
            {
                switch (s.ID)
                {
                    case "local_physical_damage_reduction_rating_+%":
                        mods["arp"] += s.Roll;
                        break;
                    case "local_evasion_rating_+%":
                        mods["evp"] += s.Roll;
                        break;
                    case "local_energy_shield_+%":
                        mods["esp"] += s.Roll;
                        break;
                    case "local_armour_and_evasion_+%":
                        mods["arp"] += s.Roll;
                        mods["evp"] += s.Roll;
                        break;
                    case "local_armour_and_energy_shield_+%":
                        mods["arp"] += s.Roll;
                        mods["esp"] += s.Roll;
                        break;
                    case "local_evasion_and_energy_shield_+%":
                        mods["evp"] += s.Roll;
                        mods["esp"] += s.Roll;
                        break;
                    case "local_armour_and_evasion_and_energy_shield_+%":
                        mods["arp"] += s.Roll;
                        mods["evp"] += s.Roll;
                        mods["esp"] += s.Roll;
                        break;
                    case "local_base_physical_damage_reduction_rating":
                        mods["arf"] += s.Roll;
                        break;
                    case "local_base_evasion_rating":
                        mods["evf"] += s.Roll;
                        break;
                    case "local_energy_shield":
                        mods["esf"] += s.Roll;
                        break;
                    case "local_additional_block_chance_%":
                        mods["blf"] += s.Roll;
                        break;
                    case "local_physical_damage_+%":
                        mods["dp"] += s.Roll;
                        break;
                    case "local_minimum_added_physical_damage":
                        mods["mindf"] += s.Roll;
                        break;
                    case "local_maximum_added_physical_damage":
                        mods["maxdf"] += s.Roll;
                        break;
                    case "local_critical_strike_chance_+%":
                        mods["crp"] += s.Roll;
                        break;
                    case "local_attack_speed_+%":
                        mods["asp"] += s.Roll;
                        break;
                    case "local_item_quality_+":
                        mods["qf"] += s.Roll;
                        break;
                    default:
                        break;
                }
            }
        }
        public static IDictionary<string, double> ParseItem(ItemCraft item)
        {
            IDictionary<string, double> tr = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            foreach (ModCraft m in item.LiveMods)
            {
                string stats = m.ToString();
                foreach (string s in stats.Split('\n'))
                {
                    KeyValuePair<string, double> kv = ParseLine(s);
                    if (tr.ContainsKey(kv.Key))
                        tr[kv.Key] += kv.Value;
                    else
                        tr.Add(kv);
                }
            }
            foreach (ModCraft m in item.LiveImplicits)
            {
                string stats = m.ToString();
                foreach (string s in stats.Split('\n'))
                {
                    KeyValuePair<string, double> kv = ParseLine(s);
                    if (tr.ContainsKey(kv.Key))
                        tr[kv.Key] += kv.Value;
                    else
                        tr.Add(kv);
                }
            }
            return tr;
        }
        public static string ReplaceFirst(string s, string target, string newtext)
        {
            int n = s.IndexOf(target);
            if (n < 0)
                return s;
            else
                return s.Substring(0, n) + newtext + s.Substring(n + target.Length);
        }
        public static KeyValuePair<string, double> ParseLine(string s)
        {
            if (s.Contains("1 Added Passive Skill"))
                return new KeyValuePair<string, double>(s, 1);
            string rexpr = @"(\d+\.?\d*)\s+to\s+(\d+\.?\d*)";
            Match m = Regex.Match(s, rexpr);
            if (m.Success)
            {
                string t = ReplaceFirst(s, m.Value, "#");
                double v = (double.Parse(m.Groups[1].ToString()) + double.Parse(m.Groups[2].ToString())) / 2;
                return new KeyValuePair<string, double>(t, v);
            }
            string expr = @"[\+\-]?(\d+\.?\d*)\%?";
            m = Regex.Match(s, expr);
            if (m.Success)
            {
                string t = ReplaceFirst(s, m.Groups[1].ToString(), "#");
                double v = double.Parse(m.Groups[1].ToString());
                if (t.Contains("-#"))
                {
                    t = t.Replace("-#", "+#");
                    v *= -1;
                }
                if (t.Contains("reduced") && !t.Contains("Hinder"))     //don't convert the "with 30% reduced movement speed" reminder text for hinder
                {
                    t = t.Replace("reduced", "increased");
                    v *= -1;
                }
                return new KeyValuePair<string, double>(t, v);
            }
            return new KeyValuePair<string, double>(s, 1);
        }
    }
}
