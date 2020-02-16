﻿using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using PoETheoryCraft.DataClasses;

namespace PoETheoryCraft.Utils
{
    public class StatLocalization       //base "chunk" format from json file
    {
        [JsonPropertyName("English")]
        public IList<LocalizationDefinition> definitions { get; set; }
        public bool hidden { get; set; } = false;
        public IList<string> ids { get; set; }  //stat ids that match this chunk, may be a subset or superset partial match for mod template's stats
    }
    public class LocalizationDefinition //represents one possible translation for a chunk
    {
        public IList<IDictionary<string, int>> condition { set; get; }  //min/max for each number in a stat for this to apply
        public IList<string> format { set; get; }                       //format for each number in a stat, or "ignore"
        public IList<IList<string>> index_handlers { set; get; }        //special operations for each number "negate", "divide_by_one_hundred", etc
        [JsonPropertyName("string")]
        public string text { set; get; }                                //text with {n} placeholder where each number goes
    }
    public class ItemCraftComparer : IComparer<ItemCraft>
    {
        public string Key { get; set; }
        public int Compare(ItemCraft x, ItemCraft y)
        {
            if (Key == null)
                return 0;
            else if (Key.Contains("[property]")) 
            {
                ItemProperties px = x.GetLiveProperties();
                ItemProperties py = y.GetLiveProperties();
                switch (Key.Substring(11))
                {
                    case "Quality":
                        return x.GetTotalQuality().CompareTo(y.GetTotalQuality());
                    case "Armour":
                        return px.armour.CompareTo(py.armour);
                    case "Evasion":
                        return px.evasion.CompareTo(py.evasion);
                    case "Energy Shield":
                        return px.energy_shield.CompareTo(py.energy_shield);
                    case "Block":
                        return px.block.CompareTo(py.block);
                    case "Physical Damage":
                        return ((double)px.physical_damage_min + px.physical_damage_max).CompareTo((double)py.physical_damage_min + py.physical_damage_max);
                    case "Critical Strike Chance":
                        return px.critical_strike_chance.CompareTo(py.critical_strike_chance);
                    case "Attack Speed":
                        return py.attack_time.CompareTo(px.attack_time);
                    case "Physical DPS":
                        return (((double)px.physical_damage_min + px.physical_damage_max) / px.attack_time).CompareTo((((double)py.physical_damage_min + py.physical_damage_max) / py.attack_time));
                    default:
                        return 0;
                }
            }
            else
            {
                IDictionary<string, double> tx = StatTranslator.ParseItem(x);
                IDictionary<string, double> ty = StatTranslator.ParseItem(y);
                if (tx.Keys.Contains(Key))
                {
                    if (ty.Keys.Contains(Key))
                        return tx[Key].CompareTo(ty[Key]);
                    else
                        return 1;
                }
                else
                {
                    if (ty.Keys.Contains(Key))
                        return -1;
                    else
                        return 0;
                }
            }
            throw new System.NotImplementedException();
        }
    }
    public static class StatTranslator
    {
        public static IDictionary<string, StatLocalization> Data { get; private set; } = new Dictionary<string, StatLocalization>();
        public static KeyValuePair<string, double> ParseLine(string s)
        {
            string rexpr = @"([\+\-]?\d+\.?\d*)\%?\s+to\s+([\+\-]?\d+\.?\d*)\%?";
            Match m = Regex.Match(s, rexpr);
            if (m.Success)
                return new KeyValuePair<string, double>(s.Replace(m.Value, "#"), (double.Parse(m.Groups[1].ToString()) + double.Parse(m.Groups[2].ToString())) / 2);
            string expr = @"([\+\-]?\d+\.?\d*)\%?";
            m = Regex.Match(s, expr);
            if (m.Success)
                return new KeyValuePair<string, double>(s.Replace(m.Value, "#"), double.Parse(m.Groups[1].ToString()));
            return new KeyValuePair<string, double>(s, 1);
            
        }
        public static IDictionary<string, double> ParseItem(ItemCraft item)
        {
            IDictionary<string, double> tr = new Dictionary<string, double>();
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
        public static void LoadStatLocalization(string locpath)
        {
            IList<StatLocalization> rawdata = JsonSerializer.Deserialize<List<StatLocalization>>(File.ReadAllText(locpath));
            //index each translation by its stat ids for easy lookup
            Data = new Dictionary<string, StatLocalization>();
            foreach (StatLocalization chunk in rawdata)
            {
                foreach (string id in chunk.ids)
                {
                    Data.Add(id, chunk);
                }
            }
            Debug.WriteLine(Data.Count + " localizations loaded");
        }
        public static void FillTranslationData(PoEModData mod)
        {
            mod.full_translation = TranslateModData(mod);
        }
        //made private for new implementation - this is only called once on load
        //accurate when called from a mod's ToString(), but repeating lots of live translations is slow
        private static string TranslateModData(PoEModData mod)
        {
            IList<PoEModStat> statscopy = new List<PoEModStat>(mod.stats);
            IList<string> lines = new List<string>();

            while (statscopy.Count > 0)
            {
                string id = statscopy[0].id;
                if (!Data.ContainsKey(id))
                {
                    //Debug.WriteLine("skipping stat translation for " + id);
                    statscopy.RemoveAt(0);
                    continue;
                }
                StatLocalization chunk = Data[id];
                IList<int> min = new List<int>();
                IList<int> max = new List<int>();
                for (int i = 0; i < chunk.ids.Count; i++)       //copy out minmax and remove handled stats from statcopy
                {
                    int found = -1;
                    for (int j = 0; j < statscopy.Count; j++)
                    {
                        if (statscopy[j].id == chunk.ids[i])
                        {
                            found = j;
                            min.Add(statscopy[j].min);
                            max.Add(statscopy[j].max);
                            break;
                        }
                    }
                    if (found < 0)
                    {
                        min.Add(0);
                        max.Add(0);
                    }
                    else
                        statscopy.RemoveAt(found);
                }

                if (chunk.hidden)
                    continue;
                LocalizationDefinition def = null;
                foreach (LocalizationDefinition d in chunk.definitions)     //find matching definition
                {
                    if (MeetsCondition(max, d.condition))
                    {
                        def = d;
                        break;
                    }
                }
                if (def == null)
                    continue;
                string linetext = BuildText(def.text, min, max, def.format, def.index_handlers);
                lines.Add(linetext);
            }
            return string.Join("\n", lines);
        }
        //accurate when called from a mod's ToString(), but repeating lots of live translations is slow
        public static string TranslateModCraft(ModCraft mod, int modquality)
        {
            IList<ModRoll> statscopy = new List<ModRoll>(mod.Stats);
            IList<string> lines = new List<string>();

            while (statscopy.Count > 0)
            {
                string id = statscopy[0].ID;
                if (!Data.ContainsKey(id))
                {
                    Debug.WriteLine("skipping stat translation for " + id);
                    statscopy.RemoveAt(0);
                    continue;
                }
                StatLocalization chunk = Data[id];
                IList<int> rolls = new List<int>();
                for (int i=0; i<chunk.ids.Count; i++)       //copy out roll and remove handled stats from statcopy
                {
                    int found = -1;
                    for (int j=0; j<statscopy.Count; j++)
                    {
                        if (statscopy[j].ID == chunk.ids[i])
                        {
                            found = j;
                            rolls.Add(statscopy[j].Roll * (100 + modquality) / 100);
                            break;
                        }
                    }
                    if (found < 0)
                        rolls.Add(0);
                    else
                        statscopy.RemoveAt(found);
                }

                if (chunk.hidden)
                    continue;
                LocalizationDefinition def=null;
                foreach (LocalizationDefinition d in chunk.definitions)     //find matching definition
                {
                    if (MeetsCondition(rolls, d.condition))
                    {
                        def = d;
                        break;
                    }
                }
                string linetext = BuildText(def.text, rolls, def.format, def.index_handlers);
                lines.Add(linetext);
            }
            return string.Join("\n", lines);
        }
        private static bool MeetsCondition(IList<int> rolls, IList<IDictionary<string, int>> conds)
        {
            for (int i=0; i<rolls.Count; i++)
            {
                foreach (string req in conds[i].Keys)
                {
                    if (req == "min")
                    {
                        if (rolls[i] < conds[i][req])
                            return false;
                    }
                    else if (req == "max")
                    {
                        if (rolls[i] > conds[i][req])
                            return false;
                    }
                }
            }
            return true;
        }
        private static string BuildText(string template, IList<int> min, IList<int> max, IList<string> formats, IList<IList<string>> handlers)
        {
            for (int i = 0; i < formats.Count; i++)
            {
                if (formats[i] == "ignore")
                    continue;
                double minv = min[i];
                double maxv = max[i];
                foreach (string h in handlers[i])
                {
                    minv = ApplyHandler(minv, h);
                    maxv = ApplyHandler(maxv, h);
                }
                string dblformat = (minv * 100 % 100 == 0 && maxv * 100 % 100 == 0) ? "N0" : "N2";
                string f = formats[i];
                if (maxv == minv)
                {
                    string rollstring = f.Replace("#", maxv.ToString(dblformat));
                    rollstring = rollstring.Replace("+-", "-");
                    template = template.Replace("{" + i + "}", rollstring);
                }
                else
                {
                    f = f.Replace("+", "");
                    string rollstring = f.Replace("#", "(" + minv.ToString(dblformat) + "-" + maxv.ToString(dblformat) + ")");
                    template = template.Replace("{" + i + "}", rollstring);
                }
            }
            return template;
        }
        private static string BuildText(string template, IList<int> values, IList<string> formats, IList<IList<string>> handlers)
        {
            for (int i=0; i < formats.Count; i++)
            {
                if (formats[i] == "ignore")
                    continue;
                double v = values[i];
                foreach (string h in handlers[i])
                {
                    v = ApplyHandler(v, h);
                }
                string dblformat = v * 100 % 100 == 0 ? "N0" : "N2";
                string rollstring = formats[i].Replace("#", v.ToString(dblformat));
                rollstring = rollstring.Replace("+-", "-");
                template = template.Replace("{" + i + "}", rollstring);
            }
            return template;
        }
        private static double ApplyHandler(double n, string h)
        {
            switch (h)
            {
                case "times_twenty":
                    return n * 20;
                case "negate":
                    return n * -1;
                case "per_minute_to_per_second_1dp":
                case "per_minute_to_per_second_0dp":
                case "per_minute_to_per_second_2dp":
                case "per_minute_to_per_second_2dp_if_required":
                case "per_minute_to_per_second":
                    return n / 60;
                case "milliseconds_to_seconds_0dp":
                case "milliseconds_to_seconds_2dp":
                case "milliseconds_to_seconds":
                    return n / 1000;
                case "divide_by_one_hundred_2dp":
                case "divide_by_one_hundred":
                    return n / 100;
                case "divide_by_ten_0dp":
                case "deciseconds_to_seconds":
                case "divide_by_twenty_then_double_0dp":
                    return n / 10;
                case "divide_by_fifteen_0dp":
                    return n / 15;
                case "divide_by_twelve":
                    return n / 12;
                case "divide_by_six":
                    return n / 6;
                case "divide_by_two_0dp":
                    return n / 2;
                case "60%_of_value":
                    return n * .6;
                //case "mod_value_to_item_class":         //poorjoy's asylum mod
                //case "multiplicative_damage_modifier":  //legacy marylene's fallacy mod
                //case "canonical_stat":                  //why does this exist
                //case "old_leech_percent":               //these last 3 seem to be deprecated
                //case "30%_of_value":
                //case "old_leech_permyriad":
                default:
                    return n;
            }
        }
    }
}
