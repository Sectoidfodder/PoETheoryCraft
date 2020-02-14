using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using PoETheoryCraft.Utils;

namespace PoETheoryCraft.DataClasses
{
    public static class CraftingDatabase
    {
        public static readonly IList<string> ItemClassCatalyst = new List<string>() { "Amulet", "Ring", "Belt" };
        public static readonly IList<string> ItemClassNoQuality = new List<string>() { "Jewel", "AbyssJewel" };
        public static readonly IList<string> CurrencyIndex = new List<string>()        //names to look for in base_items.json to make up the Currencies dict
        {
            "Chaos Orb", "Orb of Alchemy", "Orb of Scouring", "Orb of Transmutation", "Orb of Alteration", "Orb of Augmentation", "Regal Orb", "Exalted Orb", "Orb of Annulment",
            "Divine Orb", "Blessed Orb", "Crusader's Exalted Orb", "Hunter's Exalted Orb", "Redeemer's Exalted Orb", "Warlord's Exalted Orb",
            "Imbued Catalyst", "Abrasive Catalyst", "Intrinsic Catalyst", "Tempering Catalyst", "Turbulent Catalyst", "Prismatic Catalyst", "Fertile Catalyst"
        };
        public static IDictionary<string, PoEModData> CoreMods { get; private set; }    //prefixes and suffixes: ~3k gear, ~200 jewel, ~450 abyss jewel
        public static IDictionary<string, PoEModData> AllMods { get; private set; }     //really big and should only be used for key lookups, not iteration
        public static IDictionary<string, PoEBaseItemData> CoreBaseItems { get; private set; }
        public static IDictionary<string, PoEBaseItemData> AllBaseItems { get; private set; }
        public static ISet<PoEBenchOption> BenchOptions { get; private set; }
        public static IDictionary<string, PoEEssenceData> Essences { get; private set; }
        public static IDictionary<string, PoEFossilData> Fossils { get; private set; }
        public static IDictionary<string, PoECurrencyData> Currencies { get; private set; }
        
        //build mod data templates from mods.min.json and mod_types.min.json
        public static void LoadMods(string modsfile, string typesfile)
        {
            Dictionary<string, Dictionary<string, HashSet<string>>> typesdata = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, HashSet<string>>>>(File.ReadAllText(typesfile));
            AllMods = JsonSerializer.Deserialize<Dictionary<string, PoEModData>>(File.ReadAllText(modsfile));
            CoreMods = new Dictionary<string, PoEModData>();
            foreach (string k in AllMods.Keys)
            {
                PoEModData d = AllMods[k];
                //translate once and save string as a property
                StatTranslator.FillTranslationData(d);
                //set type_tags field with a lookup
                d.type_tags = typesdata[d.type]["tags"];
                //set key field
                d.key = k;
                //flag relevant mods to move to core dictionary, "misc" domain is for regular jewels
                if ((d.domain == "item" || d.domain == "abyss_jewel" || d.domain == "misc") && (d.generation_type == ModLogic.Prefix || d.generation_type == ModLogic.Suffix))
                    CoreMods.Add(k, AllMods[k]);
            }
            Debug.WriteLine(CoreMods.Count + " core, " + AllMods.Count + " total mods loaded");
        }

        //build base item data templates from base_items.min.json and item_classes.min.json, also builds core currencies and catalyst data
        public static void LoadBaseItems(string basesfile, string classesfile)
        {
            Dictionary<string, Dictionary<string, string>> classesdata = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(File.ReadAllText(classesfile));
            AllBaseItems = JsonSerializer.Deserialize<Dictionary<string, PoEBaseItemData>>(File.ReadAllText(basesfile));
            CoreBaseItems = new Dictionary<string, PoEBaseItemData>();
            Currencies = new Dictionary<string, PoECurrencyData>();
            foreach (string k in AllBaseItems.Keys)
            {
                //set item_class_properties field with a lookup
                AllBaseItems[k].item_class_properties = classesdata[AllBaseItems[k].item_class];
                //set key field
                AllBaseItems[k].key = k;
                //flag relevant items to move to core dictionary, "misc" domain only contains regular jewels as of 3.9
                if (AllBaseItems[k].domain == "item" || AllBaseItems[k].domain == "misc" || AllBaseItems[k].domain == "abyss_jewel")
                    CoreBaseItems.Add(k, AllBaseItems[k]);
                //make currency or catalyst data if it's in the list of relevant currencies
                if (CurrencyIndex.Contains(AllBaseItems[k].name))
                {
                    PoECurrencyData currency = new PoECurrencyData() { key = k, name = AllBaseItems[k].name, tooltip = AllBaseItems[k].name };
                    string imgpath = "Icons/currency/" + AllBaseItems[k].name.Replace(" " , "").Replace("'" , "") + ".png";
                    Uri imguri = new Uri(System.IO.Path.Combine(Environment.CurrentDirectory, imgpath));
                    try
                    {
                        BitmapImage img = new BitmapImage(imguri);
                        currency.icon = img;
                    }
                    catch (Exception)
                    {
                        Debug.WriteLine("Can't find image " + imguri);
                        currency.icon = null;
                    }
                    Currencies.Add(k, currency);
                }
            }
            Debug.WriteLine(CoreBaseItems.Count + " core, " + AllBaseItems.Count + " total base items loaded");
        }

        //build bench crafting option templates from bench_crafting_options.min.json
        public static void LoadBenchOptions(string benchfile)
        {
            BenchOptions = JsonSerializer.Deserialize<HashSet<PoEBenchOption>>(File.ReadAllText(benchfile));
            Debug.WriteLine(BenchOptions.Count + " crafting bench options loaded");
        }

        //build essence templates from essences.min.json
        public static void LoadEssences(string essfile)
        {
            Essences = JsonSerializer.Deserialize<Dictionary<string, PoEEssenceData>>(File.ReadAllText(essfile));
            foreach (string k in Essences.Keys)
            {
                PoEEssenceData ess = Essences[k];
                ess.key = k;
                string t = "";
                foreach (string key in ess.mods.Keys)
                {
                    t += key + ": " + CraftingDatabase.CoreMods[ess.mods[key]] + "\n";
                }
                ess.tooltip = ess.name + ":\n" + t.Trim('\n');
                string imgpath = "Icons/essence/" + ess.name.Substring(ess.name.LastIndexOf(' ') + 1) + ess.level + ".png";
                Uri imguri = new Uri(System.IO.Path.Combine(Environment.CurrentDirectory, imgpath));
                try
                {
                    BitmapImage img = new BitmapImage(imguri);
                    ess.icon = img;
                }
                catch (Exception)
                {
                    Debug.WriteLine("Can't find image " + imguri);
                    ess.icon = null;
                }
            }
            Debug.WriteLine(Essences.Count + " essences loaded");
        }

        //build fossil templates from fossils.min.json
        public static void LoadFossils(string fosfile)
        {
            Fossils = JsonSerializer.Deserialize<Dictionary<string, PoEFossilData>>(File.ReadAllText(fosfile));
            foreach (string k in Fossils.Keys)
            {
                Fossils[k].key = k;
                Fossils[k].tooltip = Fossils[k].name + ":\n" + string.Join("\n", Fossils[k].descriptions);
                string imgpath = "Icons/fossil/" + Fossils[k].name.Replace(" ", "") + ".png";
                Uri imguri = new Uri(System.IO.Path.Combine(Environment.CurrentDirectory, imgpath));
                try
                {
                    BitmapImage img = new BitmapImage(imguri);
                    Fossils[k].icon = img;
                }
                catch (Exception)
                {
                    Debug.WriteLine("Can't find image " + imguri);
                    Fossils[k].icon = null;
                }
            }
            Debug.WriteLine(Fossils.Count + " fossils loaded");
        }
    }
}
