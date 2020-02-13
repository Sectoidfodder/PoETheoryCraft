using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PoETheoryCraft.DataClasses
{
    public class PoEFossilData
    {
        //deserialized directly from fossils.min.json
        public ISet<string> added_mods { get; set; }    //mods added to rollable pool
        public ISet<string> allowed_tags { get; set; }  //IF NONEMPTY, applied item must have one of these tags
        public ISet<string> blocked_descriptions { get; set; }
        public bool changes_quality { get; set; }       //perfect fossil
        public int corrupted_essence_chance { get; set; }       //100 for glyphic, 10 for tangled
        public ISet<string> descriptions { get; set; }
        public bool enchants { get; set; }              //enchanted fossil
        public ISet<string> forbidden_tags { get; set; }        //applied item cannot have any of these tags
        public ISet<string> forced_mods { get; set; }   //mods always added to item if possible
        public bool mirrors { get; set; }               //fractured fossil
        public string name { get; set; }
        public IList<PoEModWeight> negative_mod_weights { get; set; }   //percent multipliers <100, first matching applies
        public IList<PoEModWeight> positive_mod_weights { get; set; }   //percent multipliers >100, first matching applies
        public bool rolls_lucky { get; set; }           //sanctified fossil
        public bool rolls_white_sockets { get; set; }   //encrusted fossil
        public ISet<string> sell_price_mods { get; set; }       //gilded fossil
        //copy of string used to index this template
        public string key { get; set; }
        public override string ToString()
        {
            return name;
        }
    }
}
