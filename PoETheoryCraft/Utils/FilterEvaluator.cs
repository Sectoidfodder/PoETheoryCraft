using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PoETheoryCraft.DataClasses;

namespace PoETheoryCraft.Utils
{
    public static class FilterEvaluator
    {
        public static FilterResult Evaluate(ItemCraft item, FilterCondition condition)
        {
            //call ParseProperties and ParseItem here and pass the results so they don't have to be repeatedly called during evaluation
            return condition.Evaluate(item, ItemParser.ParseProperties(item), ItemParser.ParseItem(item));
        }
    }
    public struct FilterResult
    {
        public bool Match { get; set; }
        public IDictionary<string, double> Info { get; set; }
    }

    public interface FilterCondition
    {
        FilterResult Evaluate(ItemCraft item, ItemProperties props, IDictionary<string, double> stats);
    }

    public class AndCondition : FilterCondition
    {
        public IList<FilterCondition> Subconditions { get; set; }
        public FilterResult Evaluate(ItemCraft item, ItemProperties props, IDictionary<string, double> stats)
        {
            if (Subconditions == null)
                return new FilterResult() { Match = true };
            bool match = true;
            IDictionary<string, double> info = new Dictionary<string, double>();
            foreach (FilterCondition c in Subconditions)
            {
                FilterResult r = c.Evaluate(item, props, stats);
                if (!r.Match)
                    match = false;
                if (r.Info != null)
                {
                    foreach (string s in r.Info.Keys)
                    {
                        string testkey = s;
                        int n = 2;
                        while (info.ContainsKey(testkey) && testkey.IndexOf("[pseudo]") < 0)    //guarantee unique key if it's a count or weight
                        {
                            testkey = s + "(" + n + ")";
                            n++;
                        }
                        info.Add(testkey, r.Info[s]);
                    }
                }
            }
            return new FilterResult() { Match = match, Info = info };
        }
    }

    public class CountCondition : FilterCondition
    {
        public IList<FilterCondition> Subconditions { get; set; }
        public int? Min { get; set; }
        public int? Max { get; set; }
        public FilterResult Evaluate(ItemCraft item, ItemProperties props, IDictionary<string, double> stats)
        {
            if (Subconditions == null)
                return new FilterResult() { Match = true };
            int count = 0;
            IDictionary<string, double> info = new Dictionary<string, double>() { { "Count", 0 } };
            foreach (FilterCondition c in Subconditions)
            {
                FilterResult r = c.Evaluate(item, props, stats);
                if (r.Match)
                    count++;
                if (r.Info != null)
                {
                    foreach (string s in r.Info.Keys)
                    {
                        string testkey = s;
                        int n = 2;
                        while (info.ContainsKey(testkey) && testkey.IndexOf("[pseudo]") < 0)    //guarantee unique key if it's a count or weight
                        {
                            testkey = s + "(" + n + ")";
                            n++;
                        }
                        info.Add(testkey, r.Info[s]);
                    }
                }
            }
            info["Count"] = count;
            bool match = (Min == null || count >= Min) && (Max == null || count <= Max);
            return new FilterResult() { Match = match, Info = info };
        }
    }

    public class NotCondition : FilterCondition
    {
        public IList<FilterCondition> Subconditions { get; set; }
        public FilterResult Evaluate(ItemCraft item, ItemProperties props, IDictionary<string, double> stats)
        {
            if (Subconditions == null)
                return new FilterResult() { Match = true };
            bool match = true;
            IDictionary<string, double> info = new Dictionary<string, double>();
            foreach (FilterCondition c in Subconditions)
            {
                FilterResult r = c.Evaluate(item, props, stats);
                if (r.Match)
                    match = false;
                if (r.Info != null)
                {
                    foreach (string s in r.Info.Keys)
                    {
                        string testkey = s;
                        int n = 2;
                        while (info.ContainsKey(testkey) && testkey.IndexOf("[pseudo]") < 0)    //guarantee unique key if it's a count or weight
                        {
                            testkey = s + "(" + n + ")";
                            n++;
                        }
                        info.Add(testkey, r.Info[s]);
                    }
                }
            }
            return new FilterResult() { Match = match, Info = info };
        }
    }

    public class ClampCondition : FilterCondition
    {
        public string Template { get; set; }
        public double? Min { get; set; } = null;
        public double? Max { get; set; } = null;
        public FilterResult Evaluate(ItemCraft item, ItemProperties props, IDictionary<string, double> stats)
        {
            if (Template == null)
                return new FilterResult() { Match = true };
            double? v = ItemParser.GetValueByName(Template, item, props, stats);
            if (v == null)
                return new FilterResult() { Match = false };
            IDictionary<string, double> info = null;
            if (Template.IndexOf("[pseudo]") == 0)
                info = new Dictionary<string, double>() { { Template, v.Value } };
            if (Min != null && v < Min)
                return new FilterResult() { Match = false , Info = info};
            if (Max != null && v > Max)
                return new FilterResult() { Match = false , Info = info};
            return new FilterResult() { Match = true , Info = info};
        }
    }

    public class WeightCondition : FilterCondition
    {
        public double? Min { get; set; } = null;
        public double? Max { get; set; } = null;
        public IDictionary<string, double> Weights { get; set; }
        public FilterResult Evaluate(ItemCraft item, ItemProperties props, IDictionary<string, double> stats)
        {
            if (Weights == null)
                return new FilterResult() { Match = true };
            IDictionary<string, double> info = new Dictionary<string, double>();
            double tally = 0;
            foreach (string template in Weights.Keys)
            {
                double? v = ItemParser.GetValueByName(template, item, props, stats);
                if (v != null)
                {
                    if (template.IndexOf("[pseudo]") == 0 && !info.ContainsKey(template))
                        info.Add(template, v.Value);
                    tally += v.Value * Weights[template];
                }
            }
            info.Add("Weight", tally);
            bool match = (Min == null || tally >= Min) && (Max == null || tally <= Max);
            return new FilterResult() { Match = match, Info = info };
        }
    }
}
