using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace PoETheoryCraft.DataClasses
{
    public struct ItemProperties
    {
        public int armour { get; set; }
        public int energy_shield { get; set; }
        public int evasion { get; set; }
        public int block { get; set; }
        public int attack_time { get; set; }
        public int critical_strike_chance { get; set; }
        public int physical_damage_max { get; set; }
        public int physical_damage_min { get; set; }
    }
    public class PoEBaseItemData
    {
        //deserialized directly from base_items.min.json
        public string domain { get; set; }              //broad category
        public int droplevel { get; set; }
        public ISet<string> implicits { get; set; }     //keys to corresponding mods in mod dict
        public int inventory_height { get; set; }
        public int inventory_width { get; set; }
        public string item_class { get; set; }          //key to extra data in item_classes.min.json
        public string name { get; set; }
        public ItemProperties properties { get; set; }     //defenses, base damage, base attack speed, etc
        public string release_state { get; set; }       //"released", "legacy", "unreleased"
        public IDictionary<string, int> requirements { get; set; }
        public ISet<string> tags { get; set; }          //base tags every item from this template should start with
        public IDictionary<string, string> visual_identity { get; set; }

        //lookup item_class in data deserialized from item_classes.min.json
        public IDictionary<string, string> item_class_properties { get; set; }  //a tag for each influence type and a formatted name for the class
        //copy of the string used to index this template
        public string key { get; set; }

        public override string ToString()
        {
            return name;
        }
    }
}
