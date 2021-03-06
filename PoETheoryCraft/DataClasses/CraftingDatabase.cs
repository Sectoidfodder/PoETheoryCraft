﻿using System;
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
            "Divine Orb", "Blessed Orb", "Crusader's Exalted Orb", "Hunter's Exalted Orb", "Redeemer's Exalted Orb", "Warlord's Exalted Orb", "Vaal Orb", "Orb of Chance", "Glassblower's Bauble",
            "Imbued Catalyst", "Abrasive Catalyst", "Intrinsic Catalyst", "Tempering Catalyst", "Turbulent Catalyst", "Prismatic Catalyst", "Fertile Catalyst",
            "Remove Crafted Mods", "Do Nothing"
        };
        public static IDictionary<string, double> PriceData { get; set; }               //data pulled from saved local json or poe.ninja
        public static IDictionary<string, PoEModData> Enchantments { get; private set; }
        public static IDictionary<string, PoEModData> CoreMods { get; private set; }    //prefixes and suffixes: ~3k gear, ~200 jewel, ~450 abyss jewel
        public static IDictionary<string, PoEModData> AllMods { get; private set; }     //really big and should only be used for key lookups, not iteration
        public static IDictionary<string, PoEBaseItemData> CoreBaseItems { get; private set; }
        public static IDictionary<string, PoEBaseItemData> AllBaseItems { get; private set; }
        public static ISet<PoEBenchOption> BenchOptions { get; private set; }
        public static IDictionary<string, PoEEssenceData> Essences { get; private set; }
        public static IDictionary<string, PoEFossilData> Fossils { get; private set; }
        public static IDictionary<string, PoECurrencyData> Currencies { get; private set; }
        public static ISet<string> StatTemplates { get; private set; }
        public static IDictionary<string, Dictionary<string, double>> PseudoStats { get; private set; }
        public static IDictionary<string, List<string>> DelveDroponlyMods { get; private set; }
        public static IDictionary<string, List<string>> IncursionDroponlyMods { get; private set; }
        public static int LoadPrices(string pricesfile)
        {
            try
            {
                PriceData = JsonSerializer.Deserialize<Dictionary<string, double>>(File.ReadAllText(pricesfile));
            }
            catch (Exception) 
            {
                return -1;
            }
            return PriceData.Count;
        }
        public static int LoadPseudoStats(string pseudofile, string userpseudofile)
        {
            JsonSerializerOptions jsonoptions = new JsonSerializerOptions() { AllowTrailingCommas = true, ReadCommentHandling = JsonCommentHandling.Skip };
            PseudoStats = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, double>>>(File.ReadAllText(pseudofile), jsonoptions);
            int n = PseudoStats.Count;
            try
            {
                IDictionary<string, Dictionary<string, double>> userpseudostats = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, double>>>(File.ReadAllText(userpseudofile), jsonoptions);
                foreach (string k in userpseudostats.Keys)
                {
                    if (!PseudoStats.ContainsKey(k))
                        PseudoStats.Add(k, userpseudostats[k]);
                }
            }
            catch (Exception)
            {
                Debug.WriteLine("Error loading user pseudo stats");
            }
            Debug.WriteLine(PseudoStats.Count + " pseudo stats loaded (" + (PseudoStats.Count - n) + " user defined)");
            return PseudoStats.Count;
        }
        private static void IncludeTranslations(PoEModData modtemplate)
        {
            IDictionary<string, StatLocalization> trlib = StatTranslator.Data;
            foreach (PoEModStat stat in modtemplate.stats)
            {
                string statkey = stat.id;
                if (!trlib.ContainsKey(statkey))
                    continue;
                StatLocalization sloc = trlib[statkey];
                foreach (LocalizationDefinition def in sloc.definitions)
                {
                    string text = def.text;
                    if (text.Length <= 10)          //omits league names as stats, not sure why they were there to begin with
                        continue;
                    text = text.Replace("{0} to {1}", "{0}");
                    if (text.Contains("{1}"))       //omits two flask charge when crit mods that aren't listed by trade searches anyway
                        continue;
                    if (def.format[0] != "ignore")
                        text = text.Replace("{0}", def.format[0]);
                    if (!text.Contains("Hinder"))   //don't replace "reduced" in the reminder text for hinder
                        text = text.Replace("reduced", "increased");
                    StatTemplates.Add(text);
                }
            }
        }
        private static void InitStatTemplates()
        {
            StatTemplates = new HashSet<string>()
            {
                "[property] Armour", "[property] Evasion", "[property] Energy Shield", "[property] Block", "[property] Physical Damage", "[property] Physical DPS", "[property] Attack Speed", "[property] Critical Strike Chance",
                "[property] # Prefixes", "[property] # Suffixes", "[property] # Open Prefixes", "[property] # Open Suffixes"
            };
            foreach (string k in PseudoStats.Keys)
            {
                StatTemplates.Add(k);
            }
        }
        public static int LoadSpecialMods(string delvefile, string incursionfile)
        {
            DelveDroponlyMods = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(File.ReadAllText(delvefile));
            IncursionDroponlyMods = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(File.ReadAllText(incursionfile));
            return DelveDroponlyMods.Count + IncursionDroponlyMods.Count;
        }
        public static readonly ISet<string> afflictionmods = new HashSet<string>();
        //build mod data templates from mods.min.json and mod_types.min.json, also builds search templates for stats used by relevant mods
        //MUST BE DONE AFTER TRANSLATION DEFINITIONS ARE LOADED IN STATTRANSLATOR
        public static int LoadMods(string modsfile, string typesfile)
        {
            Dictionary<string, Dictionary<string, HashSet<string>>> typesdata = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, HashSet<string>>>>(File.ReadAllText(typesfile));
            AllMods = JsonSerializer.Deserialize<Dictionary<string, PoEModData>>(File.ReadAllText(modsfile));
            CoreMods = new Dictionary<string, PoEModData>();
            Enchantments = new Dictionary<string, PoEModData>();
            InitStatTemplates();
            afflictionmods.Clear();
            Dictionary<string, string> clusterreminders = new Dictionary<string, string>();
            using (StreamReader r = File.OpenText(@"Data\ClusterText.txt"))
            {
                string line;
                string name = null;
                string desc = null;
                while ((line = r.ReadLine()) != null)
                {
                    if (line.Length == 0 && name != null)
                    {
                        clusterreminders.Add(name, desc);
                        name = null;
                        desc = null;
                        continue;
                    }
                    if (name == null)
                        name = line;
                    else if (desc == null)
                        desc = line;
                    else
                        desc += "\n" + line;
                }
                if (name != null)
                {
                    clusterreminders.Add(name, desc);
                }
            }
            //Dictionary<string, List<PoEModWeight>> afflictiondict = new Dictionary<string, List<PoEModWeight>>();
            foreach (string k in AllMods.Keys)
            {
                PoEModData d = AllMods[k];
                //translate once and save string as a property
                StatTranslator.FillTranslationData(d);
                //set type_tags field with a lookup
                d.type_tags = typesdata[d.type]["tags"];
                //set key field
                d.key = k;
                if (d.generation_type == ModLogic.Enchantment)
                    Enchantments.Add(k, AllMods[k]);
                //flag relevant mods to move to core dictionary, "misc" domain is for regular jewels
                if ((d.domain == "item" || d.domain == "abyss_jewel" || d.domain == "misc" || d.domain == "affliction_jewel") && (d.generation_type == ModLogic.Prefix || d.generation_type == ModLogic.Suffix))
                    CoreMods.Add(k, AllMods[k]);
                //every mod worth translating into string templates to search by, doesn't include implicits because idk how to include them w/o also including a ton of useless unique item mods
                if ((d.domain == "item" || d.domain == "abyss_jewel" || d.domain == "misc" || d.domain == "affliction_jewel" || d.domain == "crafted" || d.domain == "delve") && (d.generation_type == ModLogic.Prefix || d.generation_type == ModLogic.Suffix))
                    IncludeTranslations(AllMods[k]);
                if ((d.domain == "affliction_jewel") && (d.generation_type == ModLogic.Prefix || d.generation_type == ModLogic.Suffix))
                {
                    int notableindex = d.full_translation.IndexOf("Skill is");
                    if (notableindex > 0 && !d.full_translation.Contains("Jewel Socket"))
                    {
                        string notablename = d.full_translation.Substring(notableindex + 9);
                        d.tooltip_reminder = clusterreminders[notablename];
                    }
                    //if (d.full_translation.Contains("also grant"))
                    //    Debug.WriteLine(d);
                    foreach (PoEModWeight w in d.spawn_weights)
                    {
                        if (w.tag.Contains("affliction"))
                        {
                            afflictionmods.Add(w.tag);
                            //if (d.full_translation.Contains("also grant"))
                            //    Debug.WriteLine(w.tag);
                            //if (notableindex > 0 && !d.full_translation.Contains("Jewel Socket"))
                            //{
                            //    if (!afflictiondict.ContainsKey(w.tag))
                            //        afflictiondict.Add(w.tag, new List<PoEModWeight>() { new PoEModWeight() { tag = k, weight = w.weight } });
                            //    else
                            //        afflictiondict[w.tag].Add(new PoEModWeight() { tag = k, weight = w.weight });
                            //}
                        }
                    }
                }
            }
            //using (StreamWriter w = File.CreateText("notables.csv"))
            //{
            //    foreach (string s in afflictiondict.Keys)
            //    {
            //        w.WriteLine(s);
            //        foreach (PoEModWeight mw in afflictiondict[s])
            //        {
            //            PoEModData mod = AllMods[mw.tag];
            //            w.WriteLine(mw.weight + ",\"" + mod.full_translation.Substring(mod.full_translation.IndexOf("Skill is") + 9) + "\",\"" + mod.tooltip_reminder + "\"");
            //        }
            //        w.WriteLine("");
            //    }
            //}
            Debug.WriteLine(CoreMods.Count + " core, " + Enchantments.Count + " enchantment, " + AllMods.Count + " total mods loaded");
            Debug.WriteLine(StatTemplates.Count + " statlines loaded");
            IList<string> mediumenchants = new List<string>() { "affliction_area_damage", "affliction_aura_effect", "affliction_brand_damage", "affliction_critical_chance", "affliction_curse_effect", "affliction_fire_damage_over_time_multiplier", "affliction_chaos_damage_over_time_multiplier", "affliction_physical_damage_over_time_multiplier", "affliction_cold_damage_over_time_multiplier", "affliction_damage_over_time_multiplier", "affliction_effect_of_non-damaging_ailments", "affliction_life_and_mana_recovery_from_flasks", "affliction_flask_duration", "affliction_damage_while_you_have_a_herald", "affliction_minion_damage_while_you_have_a_herald", "affliction_minion_life", "affliction_projectile_damage", "affliction_totem_damage", "affliction_trap_and_mine_damage", "affliction_warcry_buff_effect", "affliction_channelling_skill_damage" };
            IList<string> smallenchants = new List<string>() { "affliction_maximum_life", "affliction_chance_to_dodge_attacks", "affliction_cold_resistance", "affliction_chaos_resistance", "affliction_armour", "affliction_evasion", "affliction_fire_resistance", "affliction_maximum_mana", "affliction_maximum_energy_shield", "affliction_lightning_resistance", "affliction_chance_to_block" };
            IList<string> largeenchants = new List<string>() { "affliction_axe_and_sword_damage", "affliction_mace_and_staff_damage", "affliction_dagger_and_claw_damage", "affliction_bow_damage", "affliction_wand_damage", "affliction_damage_with_two_handed_melee_weapons", "affliction_attack_damage_while_dual_wielding_", "affliction_attack_damage_while_holding_a_shield", "affliction_attack_damage_", "affliction_spell_damage", "affliction_chaos_damage", "affliction_cold_damage", "affliction_elemental_damage", "affliction_fire_damage", "affliction_lightning_damage", "affliction_physical_damage", "affliction_minion_damage" };
            IList<PoEModWeight> afflictionmediumspawns = new List<PoEModWeight>
            {
                new PoEModWeight() { tag = "expansion_jewel_medium", weight = 100 },
                new PoEModWeight() { tag = "default", weight = 0 }
            };
            IList<PoEModWeight> afflictionsmallspawns = new List<PoEModWeight>
            {
                new PoEModWeight() { tag = "expansion_jewel_small", weight = 100 },
                new PoEModWeight() { tag = "default", weight = 0 }
            };
            IList<PoEModWeight> afflictionlargespawns = new List<PoEModWeight>
            {
                new PoEModWeight() { tag = "expansion_jewel_large", weight = 100 },
                new PoEModWeight() { tag = "default", weight = 0 }
            };
            IList<PoEModWeight> afflictionnospawns = new List<PoEModWeight>
            {
                new PoEModWeight() { tag = "default", weight = 0 }
            };
            foreach (string s in afflictionmods)
            {
                PoEModData afflictionmod = new PoEModData()
                {
                    adds_tags = new HashSet<string>() { s },
                    domain = "item",
                    generation_type = "enchantment",
                    generation_weights = new List<PoEModWeight>(),
                    group = "SmallPassiveGroup",
                    is_essence_only = false,
                    name = s.Replace("affliction", "small passives grant: "),
                    required_level = 1,
                    stats = null,
                    type = "SmallPassiveType",
                    key = s,
                    type_tags = new HashSet<string>()
                };
                if (mediumenchants.Contains(s))
                    afflictionmod.spawn_weights = afflictionmediumspawns;
                else if (smallenchants.Contains(s))
                    afflictionmod.spawn_weights = afflictionsmallspawns;
                else if (largeenchants.Contains(s))
                    afflictionmod.spawn_weights = afflictionlargespawns;
                else
                    afflictionmod.spawn_weights = afflictionnospawns;
                Enchantments.Add(s, afflictionmod);
                AllMods.Add(s, afflictionmod);
            }
            return CoreMods.Count;
        }

        //build base item data templates from base_items.min.json and item_classes.min.json, also builds core currencies and catalyst data
        public static int LoadBaseItems(string basesfile, string classesfile)
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
                if (AllBaseItems[k].domain == "item" || AllBaseItems[k].domain == "misc" || AllBaseItems[k].domain == "abyss_jewel" || AllBaseItems[k].domain == "affliction_jewel")
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
            AppendCurrencies();
            Debug.WriteLine(CoreBaseItems.Count + " core, " + AllBaseItems.Count + " total base items loaded");
            return CoreBaseItems.Count;
        }
        //Add special "currency" options
        private static void AppendCurrencies()
        {
            PoECurrencyData extra = new PoECurrencyData() { key = "RemoveCraftedMods", name = "Remove Crafted Mods", tooltip = "Remove Crafted Mods" };
            string extrapath = "Icons/currency/RemoveCraftedMods.png";
            Uri extrauri = new Uri(System.IO.Path.Combine(Environment.CurrentDirectory, extrapath));
            try
            {
                BitmapImage extraimg = new BitmapImage(extrauri);
                extra.icon = extraimg;
            }
            catch (Exception)
            {
                Debug.WriteLine("Can't find image " + extrauri);
                extra.icon = null;
            }
            Currencies.Add("RemoveCraftedMods", extra);
            extra = new PoECurrencyData() { key = "DoNothing", name = "Do Nothing", tooltip = "Skip to Post-Craft Actions" };
            extrapath = "Icons/currency/DoNothing.png";
            extrauri = new Uri(System.IO.Path.Combine(Environment.CurrentDirectory, extrapath));
            try
            {
                BitmapImage extraimg = new BitmapImage(extrauri);
                extra.icon = extraimg;
            }
            catch (Exception)
            {
                Debug.WriteLine("Can't find image " + extrauri);
                extra.icon = null;
            }
            Currencies.Add("DoNothing", extra);
        }

        //build bench crafting option templates from bench_crafting_options.min.json
        public static int LoadBenchOptions(string benchfile)
        {
            BenchOptions = JsonSerializer.Deserialize<HashSet<PoEBenchOption>>(File.ReadAllText(benchfile));
            foreach (PoEBenchOption op in BenchOptions)
            {
                if (op.actions != null)
                    op.mod_id = op.actions.add_mod;
            }
            Debug.WriteLine(BenchOptions.Count + " crafting bench options loaded");
            return BenchOptions.Count;
        }

        //build essence templates from essences.min.json
        public static int LoadEssences(string essfile)
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
            return Essences.Count;
        }

        //build fossil templates from fossils.min.json
        public static int LoadFossils(string fosfile)
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
            return Fossils.Count;
        }
    }
}
