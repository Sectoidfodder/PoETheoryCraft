using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PoETheoryCraft.DataClasses
{
    public class PoEBenchOption
    {
        //deserialized directly from crafting_bench_options.min.json
        public string bench_group { get; set; }             //only for grouping in bench UI
        public int bench_tier { get; set; }
        public IDictionary<string, int> cost { get; set; }  //currency cost
        public ISet<string> item_classes { get; set; }      //corresponds to PoEBaseItemData.item_class for allowed craft targets
        public string master { get; set; }
        public string mod_id { get; set; }                  //key to the corresponding mod in mod dict
    }
}
