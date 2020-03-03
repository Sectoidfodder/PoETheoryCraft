using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using PoETheoryCraft.DataClasses;
using PoETheoryCraft.Utils;

namespace PoETheoryCraft
{

    //Represents one actual in-game item, made from a PoEBaseItemData template
    public class ItemCraft
    {
        public static int DefaultQuality = Properties.Settings.Default.ItemQuality;
        public string SourceData { get; }           //key to PoEBaseItemData this is derived from
        public ISet<string> LiveTags { get; }       //derived from template but can change during crafting
        public int ItemLevel { get; }
        public ItemRarity Rarity { get; set; }
        public IList<ModCraft> LiveMods { get; }
        public IList<ModCraft> LiveImplicits { get; }
        public IList<ModCraft> LiveEnchantments { get; }
        public string ItemName { get; private set; }
        private int _basequality;
        public int BaseQuality                      //clamp min and max; update mods if catalyst qual changed
        {
            get { return _basequality; } 
            set 
            {
                int cap = QualityType == null ? 30 : 20;
                value = Math.Max(Math.Min(value, cap), 0);
                if (value != _basequality)
                {
                    _basequality = value;
                    if (QualityType != null)
                    {
                        foreach (ModCraft mod in LiveMods)
                        {
                            UpdateModQuality(mod, QualityType);
                        }
                        foreach (ModCraft mod in LiveImplicits)
                        {
                            UpdateModQuality(mod, QualityType);
                        }
                    }
                }
            }
        }
        //null for normal qual, or the name of a catalyst, ex: "Imbued Catalyst"
        public string QualityType { get; set; }
        //temporary properties managed by item filter/search functions, so pseudos and weights can be shown
        public IDictionary<string, double> TempProps { get; set; } = new Dictionary<string, double>();
        public ItemCraft(PoEBaseItemData data, int level = 100, ISet<ItemInfluence> influences = null)
        {
            SourceData = data.key;
            _basequality = DefaultQuality;
            QualityType = null;
            ItemLevel = level;
            Rarity = ItemRarity.Normal;
            ItemName = data.name;
            LiveTags = new HashSet<string>(data.tags);
            if (influences != null)
            {
                foreach (ItemInfluence inf in influences)
                {
                    string s = data.item_class_properties[EnumConverter.InfToTag(inf)];
                    if (s != null)
                        LiveTags.Add(s);
                }
            }
            LiveMods = new List<ModCraft>();
            LiveImplicits = new List<ModCraft>();
            LiveEnchantments = new List<ModCraft>();
            foreach (string s in data.implicits)
            {
                AddImplicit(CraftingDatabase.AllMods[s]);
            }
        }
        //deep copy everything
        private ItemCraft(ItemCraft item)
        {
            SourceData = item.SourceData;
            _basequality = item.BaseQuality;
            QualityType = item.QualityType;
            LiveTags = new HashSet<string>(item.LiveTags);
            ItemLevel = item.ItemLevel;
            Rarity = item.Rarity;
            LiveMods = new List<ModCraft>();
            foreach (ModCraft m in item.LiveMods)
            {
                LiveMods.Add(m.Copy());
            }
            LiveImplicits = new List<ModCraft>();
            foreach (ModCraft m in item.LiveImplicits)
            {
                LiveImplicits.Add(m.Copy());
            }
            LiveEnchantments = new List<ModCraft>();
            foreach (ModCraft m in item.LiveEnchantments)
            {
                LiveEnchantments.Add(m.Copy());
            }
            ItemName = item.ItemName;
        }
        public ItemCraft Copy()
        {
            return new ItemCraft(this);
        }
        public int ModCountByType(string type, bool lockedonly = false)
        {
            int count = 0;
            foreach (ModCraft m in LiveMods)
            {
                PoEModData modtemplate = CraftingDatabase.AllMods[m.SourceData];
                if (modtemplate.generation_type == type && (!lockedonly || m.IsLocked))
                    count++;
            }
            return count;
        }
        public int GetAffixLimit(bool ignorerarity = false)
        {
            PoEBaseItemData itemtemplate = CraftingDatabase.AllBaseItems[SourceData];
            if (ignorerarity)
            {
                return itemtemplate.item_class.Contains("Jewel") ? 2 : 3;
            }
            switch (Rarity)
            {
                case ItemRarity.Rare:
                    return itemtemplate.item_class.Contains("Jewel") ? 2 : 3;
                case ItemRarity.Magic:
                    return 1;
                default:
                    return 0;
            }
        }
        public ISet<ItemInfluence> GetInfluences()
        {
            ISet<ItemInfluence> infs = new HashSet<ItemInfluence>();
            PoEBaseItemData itemtemplate = CraftingDatabase.AllBaseItems[SourceData];
            foreach (ItemInfluence inf in Enum.GetValues(typeof(ItemInfluence)))
            {
                if (LiveTags.Contains(itemtemplate.item_class_properties[EnumConverter.InfToTag(inf)]))
                    infs.Add(inf);
            }
            return infs;
        }
        public bool RerollImplicits()
        {
            if (LiveImplicits.Count == 0)
                return false;
            foreach (ModCraft m in LiveImplicits)
            {
                m.Reroll();
            }
            return true;
        }
        //divines each mod, obeying "of prefixes" and "of suffixes" metamods and locked mods
        public bool RerollExplicits()
        {
            bool prefixlock = false;
            bool suffixlock = false;
            foreach (ModCraft m in LiveMods)
            {
                PoEModData modtemplate = CraftingDatabase.AllMods[m.SourceData];
                if (modtemplate.key == ModLogic.PrefixLock)
                    prefixlock = true;
                if (modtemplate.key == ModLogic.SuffixLock)
                    suffixlock = true;
            }
            bool valid = false;
            foreach (ModCraft m in LiveMods)
            {
                PoEModData modtemplate = CraftingDatabase.AllMods[m.SourceData];
                if (!m.IsLocked && !(prefixlock && modtemplate.generation_type == ModLogic.Prefix) && !(suffixlock && modtemplate.generation_type == ModLogic.Suffix))
                {
                    m.Reroll();
                    valid = true;
                }
            }
            return valid;
        }
        //removes one mod at random, obeying prefix/suffix lock, and leaving locked mods
        public bool RemoveRandomMod()
        {
            bool prefixlock = false;
            bool suffixlock = false;
            foreach (ModCraft m in LiveMods)
            {
                PoEModData modtemplate = CraftingDatabase.AllMods[m.SourceData];
                if (modtemplate.key == ModLogic.PrefixLock)
                    prefixlock = true;
                if (modtemplate.key == ModLogic.SuffixLock)
                    suffixlock = true;
            }
            IList<ModCraft> choppingblock = new List<ModCraft>();
            foreach (ModCraft m in LiveMods)
            {
                PoEModData modtemplate = CraftingDatabase.AllMods[m.SourceData];
                if (!m.IsLocked && !(prefixlock && modtemplate.generation_type == ModLogic.Prefix) && !(suffixlock && modtemplate.generation_type == ModLogic.Suffix))
                    choppingblock.Add(m);
            }
            if (choppingblock.Count > 0)
            {
                int n = RNG.Gen.Next(choppingblock.Count);
                LiveMods.Remove(choppingblock[n]);
                return true;
            }
            else
                return false;
            
        }
        //remove crafted mods, ignoring metamod locks
        public bool ClearCraftedMods()
        {
            int removedcount = 0;
            for (int i = LiveMods.Count - 1; i >= 0; i--)
            {
                ModCraft m = LiveMods[i];
                PoEModData modtemplate = CraftingDatabase.AllMods[m.SourceData];
                if (!m.IsLocked && modtemplate.domain == "crafted")
                {
                    RemoveModAt(i);
                    removedcount++;
                }
            }
            return removedcount > 0;
        }
        //remove all mods or all crafted mods, obeying prefix/suffix lock, leaving locked mods, and downgrading rarity if necessary
        public void ClearMods()
        {
            bool prefixlock = false;
            bool suffixlock = false;
            foreach (ModCraft m in LiveMods)
            {
                PoEModData modtemplate = CraftingDatabase.AllMods[m.SourceData];
                if (modtemplate.key == ModLogic.PrefixLock)
                    prefixlock = true;
                if (modtemplate.key == ModLogic.SuffixLock)
                    suffixlock = true;
            }
            for (int i = LiveMods.Count - 1; i >= 0; i--)
            {
                ModCraft m = LiveMods[i];
                PoEModData modtemplate = CraftingDatabase.AllMods[m.SourceData];
                if (!m.IsLocked && !(prefixlock && modtemplate.generation_type == ModLogic.Prefix) && !(suffixlock && modtemplate.generation_type == ModLogic.Suffix))
                    RemoveModAt(i);
            }
        }
        public void AddMod(PoEModData data)
        {
            ModCraft m = new ModCraft(data);
            LiveMods.Add(m);
            LiveTags.UnionWith(data.adds_tags);
            UpdateModQuality(m, QualityType);
        }
        public void AddImplicit(PoEModData data)
        {
            ModCraft m = new ModCraft(data);
            LiveImplicits.Add(m);
            LiveTags.UnionWith(data.adds_tags);
            UpdateModQuality(m, QualityType);
        }
        public void AddEnchantment(PoEModData data)
        {
            ModCraft m = new ModCraft(data);
            LiveEnchantments.Add(m);
            LiveTags.UnionWith(data.adds_tags);
            UpdateModQuality(m, QualityType);
        }
        public void ApplyCatalyst(string tag)
        {
            if (tag == null)
                return;
            if (QualityType != tag)
            {
                QualityType = tag;
                _basequality = 0;       //set this instead of property to avoid triggering extra mod update
            }
            if (Rarity == ItemRarity.Normal)
            {
                BaseQuality += 5;
            }
            else if (Rarity == ItemRarity.Magic)
            {
                BaseQuality += 2;
            }
            else
            {
                BaseQuality += 1;
            }
        }
        public void MaximizeMods()
        {
            foreach (ModCraft mod in LiveMods)
            {
                mod.Maximize();
            }
            foreach (ModCraft mod in LiveImplicits)
            {
                mod.Maximize();
            }
        }
        private void UpdateModQuality(ModCraft mod, string name)
        {
            PoEModData modtemplate = CraftingDatabase.AllMods[mod.SourceData];
            IList<string> tags;
            if (name != null && ModLogic.CatalystTags.Keys.Contains(name))
                tags = ModLogic.CatalystTags[name];
            else
                tags = new List<string>();
            bool match = false;
            foreach (string s in tags)
            {
                if (modtemplate.type_tags.Contains(s))
                {
                    match = true;
                    break;
                }
            }
            if (modtemplate.type_tags.Contains(ModLogic.CatalystIgnore))
                match = false;
            if (match)
                mod.Quality = BaseQuality;
            else
                mod.Quality = 0;
        }
        public ItemRarity GetMinimumRarity()
        {
            int prefixcount = ModCountByType(ModLogic.Prefix);
            int suffixcount = ModCountByType(ModLogic.Suffix);
            if (prefixcount == 0 && suffixcount == 0)
                return ItemRarity.Normal;
            else if (prefixcount <= 1 && suffixcount <= 1)
                return ItemRarity.Magic;
            else
                return ItemRarity.Rare;
        }
        //removes a mod and updates the item's tags accordingly
        private void RemoveModAt(int n)
        {
            PoEModData modtemplate = CraftingDatabase.AllMods[LiveMods[n].SourceData];
            foreach (string tag in modtemplate.adds_tags)    //for each tag, only remove if no other live mods or implicits are applying the tag
            {
                bool shouldremove = true;
                for (int i = 0; i < LiveMods.Count; i++)
                {
                    if (i == n)
                        continue;
                    PoEModData othertemplate = CraftingDatabase.AllMods[LiveMods[i].SourceData];
                    if (othertemplate.adds_tags.Contains(tag))
                    {
                        shouldremove = false;
                        break;
                    }
                }
                for (int i = 0; i < LiveImplicits.Count; i++)
                {
                    PoEModData othertemplate = CraftingDatabase.AllMods[LiveImplicits[i].SourceData];
                    if (othertemplate.adds_tags.Contains(tag))
                    {
                        shouldremove = false;
                        break;
                    }
                }
                if (shouldremove)
                    LiveTags.Remove(tag);
            }
            LiveMods.RemoveAt(n);
        }
        public string GetClipboardString()
        {
            string s = "Rarity: ";
            if (Rarity == ItemRarity.Rare)
                s += "Rare";
            else if (Rarity == ItemRarity.Magic)
                s += "Magic";
            else
                s += "Normal";
            s += "\n" + ItemName;
            s += "\n--------";
            PoEBaseItemData itemtemplate = CraftingDatabase.AllBaseItems[SourceData];
            s += "\n" + itemtemplate.item_class;
            ItemProperties props = ItemParser.ParseProperties(this);
            int q = props.quality;
            if (q > 0)
            {
                string t;
                switch (QualityType)
                {
                    case "Prismatic Catalyst":
                        t = "Quality (Resistance Modifiers): ";
                        break;
                    case "Fertile Catalyst":
                        t = "Quality (Life and Mana Modifiers): ";
                        break;
                    case "Intrinsic Catalyst":
                        t = "Quality (Attribute Modifiers): ";
                        break;
                    case "Tempering Catalyst":
                        t = "Quality (Defence Modifiers): ";
                        break;
                    case "Abrasive Catalyst":
                        t = "Quality (Attack Modifiers): ";
                        break;
                    case "Imbued Catalyst":
                        t = "Quality (Caster Modifiers): ";
                        break;
                    case "Turbulent Catalyst":
                        t = "Quality (Elemental Damage): ";
                        break;
                    default:
                        t = "Quality: ";
                        break;
                }
                s += "\n" + t + q + "%";
            }
            if (props.block > 0)
                s += "\nChance to Block: " + props.block + "%";
            if (props.armour > 0)
                s += "\nArmour: " + props.armour;
            if (props.evasion > 0)
                s += "\nEvasion: " + props.evasion;
            if (props.energy_shield > 0)
                s += "\nEnergy Shield: " + props.energy_shield;
            if (props.physical_damage_max > 0)
                s += "\nPhysical Damage: " + props.physical_damage_min + "-" + props.physical_damage_max;
            if (props.critical_strike_chance > 0)
                s += "\nCritical Strike Chance: " + ((double)props.critical_strike_chance / 100).ToString("N2") + "%";
            if (props.attack_time > 0)
                s += "\nAttacks per Second: " + ((double)1000 / props.attack_time).ToString("N2");
            s += "\n--------";
            s += "\nItem Level: " + ItemLevel;
            if (LiveImplicits.Count > 0)
            {
                s += "\n--------";
                foreach (ModCraft m in LiveImplicits)
                {
                    s += "\n" + m;
                }
            }
            if (LiveMods.Count > 0)
            {
                s += "\n--------";
                foreach (ModCraft m in LiveMods)
                {
                    s += "\n" + m;
                }
            }
            s += "\n";
            return s;
        }
        public override string ToString()
        {
            return ItemName;
        }
        public void GenerateName()
        {
            PoEBaseItemData itemtemplate = CraftingDatabase.AllBaseItems[SourceData];
            ItemName = itemtemplate.name;
            if (Rarity == ItemRarity.Rare)
            {
                ItemName = GenRareName() + "\n" + itemtemplate.name;
            }
            else if (Rarity == ItemRarity.Magic)
            {
                foreach (ModCraft m in LiveMods)
                {
                    PoEModData modtemplate = CraftingDatabase.AllMods[m.SourceData];
                    if (modtemplate.generation_type == ModLogic.Prefix)
                        ItemName = modtemplate.name + " " + ItemName;
                    else if (modtemplate.generation_type == ModLogic.Suffix)
                        ItemName = ItemName + " " + modtemplate.name;
                }
            }
        }
        private string GenRareName()
        {
            PoEBaseItemData itemtemplate = CraftingDatabase.AllBaseItems[SourceData];
            if (namesuffix.Keys.Contains(itemtemplate.item_class))
            {
                string key;
                if (itemtemplate.item_class == "Shield" && itemtemplate.properties.energy_shield > 0 && itemtemplate.properties.armour == 0 && itemtemplate.properties.evasion == 0)
                    key = "Spirit Shield";
                else
                    key = itemtemplate.item_class;
                IList<string> suf = namesuffix[key];
                return nameprefix[RNG.Gen.Next(nameprefix.Count)] + " " + suf[RNG.Gen.Next(suf.Count)];
            }
            else
            {
                return fnames[RNG.Gen.Next(fnames.Count)] + " " + snames[RNG.Gen.Next(snames.Count)];
            }
        }
        private readonly static IList<string> fnames = "Swift,Unceasing,Vengeful,Lone,Cold,Hot,Purple,Brutal,Flying,Driving,Blind,Demon,Enduring,Defiant,Lost,Dying,Falling,Soaring,Twisted,Glass,Bleeding,Broken,Silent,Red,Black,Dark,Sectoid,Fallen,Patient,Burning,Final,Lazy,Morbid,Crimson,Cursed,Frozen,Bloody,Banished,First,Severed,Empty,Spectral,Sacred,Stone,Shattered,Hidden,Rotting,Devil's,Forgotten,Blinding,Fading,Crystal,Secret,Cryptic".Split(',');
        private readonly static IList<string> snames = "Engine,Chant,Heart,Justice,Law,Thunder,Moon,Heat,Fear,Star,Apollo,Prophet,Hero,Hydra,Serpent,Crown,Thorn,Empire,Line,Fall,Summer,Druid,God,Savior,Stallion,Hawk,Vengeance,Calm,Knife,Sword,Dream,Sleep,Stroke,Flame,Spark,Fist,Dirge,Grave,Shroud,Breath,Smoke,Giant,Whisper,Night,Throne,Pipe,Blade,Daze,Pyre,Tears,Mother,Crone,King,Father,Priest,Dawn,Fodder,Hammer,Shield,Hymn,Vanguard,Sentinel,Stranger,Bell,Mist,Fog,Jester,Scepter,Ring,Skull,Paramour,Palace,Mountain,Rain,Gaze,Future,Gift".Split(',');
        private readonly static IList<string> nameprefix = "Agony,Apocalypse,Armageddon,Beast,Behemoth,Blight,Blood,Bramble,Brimstone,Brood,Carrion,Cataclysm,Chimeric,Corpse,Corruption,Damnation,Death,Demon,Dire,Dragon,Dread,Doom,Dusk,Eagle,Empyrean,Fate,Foe,Gale,Ghoul,Gloom,Glyph,Golem,Grim,Hate,Havoc,Honour,Horror,Hypnotic,Kraken,Loath,Maelstrom,Mind,Miracle,Morbid,Oblivion,Onslaught,Pain,Pandemonium,Phoenix,Plague,Rage,Rapture,Rune,Skull,Sol,Soul,Sorrow,Spirit,Storm,Tempest,Torment,Vengeance,Victory,Viper,Vortex,Woe,Wrath".Split(',');
        private readonly static IDictionary<string, IList<string>> namesuffix = new Dictionary<string, IList<string>>()
        {
            { "Spirit Shield" , "Ancient,Anthem,Call,Chant,Charm,Emblem,Guard,Mark,Pith,Sanctuary,Song,Spell,Star,Ward,Weaver,Wish".Split(',') },
            { "Shield" , "Aegis,Badge,Barrier,Bastion,Bulwark,Duty,Emblem,Fend,Guard,Mark,Refuge,Rock,Rook,Sanctuary,Span,Tower,Watch,Wing".Split(',') },
            { "Body Armour" , "Carapace,Cloak,Coat,Curtain,Guardian,Hide,Jack,Keep,Mantle,Pelt,Salvation,Sanctuary,Shell,Shelter,Shroud,Skin,Suit,Veil,Ward,Wrap".Split(',') },
            { "Helmet" , "Brow,Corona,Cowl,Crest,Crown,Dome,Glance,Guardian,Halo,Horn,Keep,Peak,Salvation,Shelter,Star,Veil,Visage,Visor,Ward".Split(',') },
            { "Gloves" , "Caress,Claw,Clutches,Fingers,Fist,Grasp,Grip,Hand,Hold,Knuckle,Mitts,Nails,Palm,Paw,Talons,Touch,Vise".Split(',') },
            { "Boots" , "Dash,Goad,Hoof,League,March,Pace,Road,Slippers,Sole,Span,Spark,Spur,Stride,Track,Trail,Tread,Urge".Split(',') },
            { "Amulet" , "Beads,Braid,Charm,Choker,Clasp,Collar,Idol,Gorget,Heart,Locket,Medallion,Noose,Pendant,Rosary,Scarab,Talisman,Torc".Split(',') },
            { "Ring" , "Band,Circle,Coil,Eye,Finger,Grasp,Grip,Gyre,Hold,Knot,Knuckle,Loop,Nail,Spiral,Turn,Twirl,Whorl".Split(',') },
            { "Belt" , "Bind,Bond,Buckle,Clasp,Cord,Girdle,Harness,Lash,Leash,Lock,Locket,Shackle,Snare,Strap,Tether,Thread,Trap,Twine".Split(',') },
            { "Quiver" , "Arrow,Barb,Bite,Bolt,Brand,Dart,Flight,Hail,Impaler,Nails,Needle,Quill,Rod,Shot,Skewer,Spear,Spike,Spire,Stinger".Split(',') },
            { "One Hand Axe" , "Bane,Beak,Bite,Butcher,Edge,Etcher,Gnash,Hunger,Mangler,Rend,Roar,Sever,Slayer,Song,Spawn,Splitter,Sunder,Thirst".Split(',') },
            { "Two Hand Axe" , "Bane,Beak,Bite,Butcher,Edge,Etcher,Gnash,Hunger,Mangler,Rend,Roar,Sever,Slayer,Song,Spawn,Splitter,Sunder,Thirst".Split(',') },
            { "One Hand Mace" , "Bane,Batter,Blast,Blow,Blunt,Brand,Breaker,Burst,Crack,Crusher,Grinder,Knell,Mangler,Ram,Roar,Ruin,Shatter,Smasher,Star,Thresher,Wreck".Split(',') },
            { "Two Hand Mace" , "Bane,Batter,Blast,Blow,Blunt,Brand,Breaker,Burst,Crack,Crusher,Grinder,Knell,Mangler,Ram,Roar,Ruin,Shatter,Smasher,Star,Thresher,Wreck".Split(',') },
            { "Sceptre" , "Bane,Blow,Breaker,Call,Chant,Crack,Crusher,Cry,Gnarl,Grinder,Knell,Ram,Roar,Smasher,Song,Spell,Star,Weaver".Split(',') },
            { "Staff" , "Bane,Beam,Branch,Call,Chant,Cry,Gnarl,Goad,Mast,Pile,Pillar,Pole,Post,Roar,Song,Spell,Spire,Weaver".Split(',') },
            { "Warstaff" , "Bane,Beam,Branch,Call,Chant,Cry,Gnarl,Goad,Mast,Pile,Pillar,Pole,Post,Roar,Song,Spell,Spire,Weaver".Split(',') },
            { "One Hand Sword" , "Bane,Barb,Beak,Bite,Edge,Fang,Gutter,Hunger,Impaler,Needle,Razor,Saw,Scalpel,Scratch,Sever,Skewer,Slicer,Song,Spike,Spiker,Stinger,Thirst".Split(',') },
            { "Thrusting One Hand Sword" , "Bane,Barb,Beak,Bite,Edge,Fang,Gutter,Hunger,Impaler,Needle,Razor,Saw,Scalpel,Scratch,Sever,Skewer,Slicer,Song,Spike,Spiker,Stinger,Thirst".Split(',') },
            { "Two Hand Sword" , "Bane,Barb,Beak,Bite,Edge,Fang,Gutter,Hunger,Impaler,Needle,Razor,Saw,Scalpel,Scratch,Sever,Skewer,Slicer,Song,Spike,Spiker,Stinger,Thirst".Split(',') },
            { "Dagger" , "Bane,Barb,Bite,Edge,Etcher,Fang,Gutter,Hunger,Impaler,Needle,Razor,Scalpel,Scratch,Sever,Skewer,Slicer,Song,Spike,Stinger,Thirst".Split(',') },
            { "Rune Dagger" , "Bane,Barb,Bite,Edge,Etcher,Fang,Gutter,Hunger,Impaler,Needle,Razor,Scalpel,Scratch,Sever,Skewer,Slicer,Song,Spike,Stinger,Thirst".Split(',') },
            { "Claw" , "Bane,Bite,Edge,Fang,Fist,Gutter,Hunger,Impaler,Needle,Razor,Roar,Scratch,Skewer,Slicer,Song,Spike,Stinger,Talons,Thirst".Split(',') },
            { "Bow" , "Arch,Bane,Barrage,Blast,Branch,Breeze,Fletch,Guide,Horn,Mark,Nock,Rain,Reach,Siege,Song,Stinger,Strike,Thirst,Thunder,Twine,Volley,Wind,Wing".Split(',') },
            { "Wand" , "Bane,Barb,Bite,Branch,Call,Chant,Charm,Cry,Edge,Gnarl,Goad,Needle,Scratch,Song,Spell,Spire,Thirst,Weaver".Split(',') }
        };
    }
}
