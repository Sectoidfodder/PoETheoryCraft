using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PoETheoryCraft.Utils;

namespace PoETheoryCraft.DataClasses
{
    public struct PoEModWeight
    {
        public string tag { get; set; }
        public int weight { get; set; }
    }
    public struct PoEModBuff
    {
        public string id { get; set; }
        public int range { get; set; }
    }
    public struct PoEModEffect
    {
        public string granted_effect_id { get; set; }
        public int level { get; set; }
    }
    public class PoEModStat
    {
        public string id { get; set; }
        public int max { get; set; }
        public int min { get; set; }
    }
    public class PoEModData
    {
        //deserialized directly from mods.min.json
        public ISet<string> adds_tags { get; set; }     //tags added to an item with this mod
        public string domain { get; set; }              //broad category: "item", "crafted", etc
        public string generation_type { get; set; }     //"prefix", "suffix", "unique" for everything else including implicits
        public IList<PoEModWeight> generation_weights { get; set; }     //conditional percent modifiers, order matters since first matching tag applies
        public PoEModBuff grants_buff { get; set; }
        public ISet<PoEModEffect> grants_effects { get; set; }
        public string group { get; set; }               //excludes other mods of the same group on same item
        public bool is_essence_only { get; set; }
        public string name { get; set; }                //"merciless", "redeemer's", etc
        public int required_level { get; set; }         //minimum ilvl to spawn for "item" domain affixes, not sure if it matters for mods that don't naturally spawn
        public IList<PoEModWeight> spawn_weights { get; set; }      //base weights, order matters since first matching tag applies
        public IList<PoEModStat> stats { get; set; }    //statlines granted, order matters when multiple stats make one line of text
        public string type { get; set; }                //an index into item_types.min.json with additional data

        //lookup type in data deserialized from item_types.min.json
        public ISet<string> type_tags { get; set; }     //"cold", "attack", "jewellry_elemental", etc, for fossils and catalysts
        //copy of the string used to index this template
        public string key { get; set; }

        //filled by the translator when template is deserialized
        public string full_translation { get; set; }
        public override string ToString()
        {
            return full_translation ?? name;
        }
    }
}
