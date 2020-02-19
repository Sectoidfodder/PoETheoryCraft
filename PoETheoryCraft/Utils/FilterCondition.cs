using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PoETheoryCraft.DataClasses;

namespace PoETheoryCraft.Utils
{
    public struct FilterResult
    {
        public bool Match { get; set; }
        public IDictionary<string, double> Info { get; set; }
    }

    interface FilterCondition
    {
        FilterResult Evaluate(ItemProperties props, IDictionary<string, double> stats);
    }

    class AndCondition : FilterCondition
    {
        public IList<FilterCondition> Subconditions { get; set; }
        public FilterResult Evaluate(ItemProperties props, IDictionary<string, double> stats)
        {
            if (Subconditions == null)
                return new FilterResult() { Match = true };
            bool match = true;
            IDictionary<string, double> info = new Dictionary<string, double>();
            foreach (FilterCondition c in Subconditions)
            {
                FilterResult r = c.Evaluate(props, stats);
                if (!r.Match)
                    match = false;
                if (r.Info != null)
                {
                    foreach (string s in r.Info.Keys)
                    {
                        string testkey = s;
                        int n = 2;
                        while (info.ContainsKey(testkey))  //guarantee unique key
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

    class CountCondition : FilterCondition
    {
        public IList<FilterCondition> Subconditions { get; set; }
        public int Target { get; set; } = 0;
        public FilterResult Evaluate(ItemProperties props, IDictionary<string, double> stats)
        {
            if (Subconditions == null)
                return new FilterResult() { Match = true };
            int count = 0;
            IDictionary<string, double> info = new Dictionary<string, double>();
            foreach (FilterCondition c in Subconditions)
            {
                FilterResult r = c.Evaluate(props, stats);
                if (r.Match)
                    count++;
                if (r.Info != null)
                {
                    foreach (string s in r.Info.Keys)
                    {
                        string testkey = s;
                        int n = 2;
                        while (info.ContainsKey(testkey))  //guarantee unique key
                        {
                            testkey = s + "(" + n + ")";
                            n++;
                        }
                        info.Add(testkey, r.Info[s]);
                    }
                }
            }
            string newkey = "count";
            int m = 2;
            while (info.ContainsKey(newkey))
            {
                newkey = "count(" + m + ")";
                m++;
            }
            info.Add(newkey, count);
            return new FilterResult() { Match = count >= Target, Info = info };
        }
    }

    class NotCondition : FilterCondition
    {
        public IList<FilterCondition> Subconditions { get; set; }
        public FilterResult Evaluate(ItemProperties props, IDictionary<string, double> stats)
        {
            if (Subconditions == null)
                return new FilterResult() { Match = true };
            bool match = true;
            IDictionary<string, double> info = new Dictionary<string, double>();
            foreach (FilterCondition c in Subconditions)
            {
                FilterResult r = c.Evaluate(props, stats);
                if (r.Match)
                    match = false;
                if (r.Info != null)
                {
                    foreach (string s in r.Info.Keys)
                    {
                        string testkey = s;
                        int n = 2;
                        while (info.ContainsKey(testkey))  //guarantee unique key
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

    class ClampCondition : FilterCondition
    {
        public string Template { get; set; }
        public int? Min { get; set; } = null;
        public int? Max { get; set; } = null;
        public FilterResult Evaluate(ItemProperties props, IDictionary<string, double> stats)
        {
            if (Template == null)
                return new FilterResult() { Match = true };
            double? v = ItemParser.GetValueByName(Template, props, stats);
            if (v == null)
                return new FilterResult() { Match = false };
            if (Min != null && v < Min)
                return new FilterResult() { Match = false };
            if (Max != null && v > Max)
                return new FilterResult() { Match = false };
            return new FilterResult() { Match = true };
        }
    }

    class WeightCondition : FilterCondition
    {
        public int? Min { get; set; } = null;
        public int? Max { get; set; } = null;
        public IDictionary<string, double> Weights { get; set; }
        public FilterResult Evaluate(ItemProperties props, IDictionary<string, double> stats)
        {
            if (Weights == null)
                return new FilterResult() { Match = true };
            IDictionary<string, double> info = new Dictionary<string, double>();
            double tally = 0;
            foreach (string template in Weights.Keys)
            {
                double? v = ItemParser.GetValueByName(template, props, stats);
                if (v != null)
                {
                    tally += v.Value * Weights[template];
                }
            }
            info.Add("weight", tally);
            bool match = (Min == null || tally >= Min) && (Max == null || tally <= Max);
            return new FilterResult() { Match = match, Info = info };
        }
    }
}
