using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PoETheoryCraft.DataClasses
{
    public struct PoEEssencetype
    {
        public bool is_corruption_only { get; set; }
        public int tier { get; set; }
    }
    public class PoEEssenceData
    {
        //deserialized directly from essences.min.json
        public int? item_level_restriction { get; set; }
        public int level { get; set; }      //0 corrupted, 1 whispering - 7 deafening
        public IDictionary<string, string> mods { get; set; }   //key: PoEBaseItemData.item_class, value: key to a mod in mod dict
        public string name { get; set; }
        public int spawn_level_max { get; set; }
        public int spawn_level_min { get; set; }
        public PoEEssencetype type { get; set; }
        //copy of the string used to index this template
        public string key { get; set; }
        public override string ToString()
        {
            return name;
        }
    }
}
