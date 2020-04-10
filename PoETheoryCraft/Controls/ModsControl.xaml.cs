using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using PoETheoryCraft.DataClasses;
using PoETheoryCraft.Utils;

namespace PoETheoryCraft.Controls
{
    /// <summary>
    /// Looks at a CraftingBench and a CurrencySelector in order to display relevant mods for the benched item and selected currency
    /// </summary>
    public partial class ModsControl : UserControl
    {
        public CraftingBench Bench { get; set; }
        public CurrencySelector Currency { get; set; }
        public event EventHandler<AddModEventArgs> AddMod;
        public class AddModEventArgs : EventArgs
        {
            public object SelectedMod { get; set; }
        }
        public ModsControl()
        {
            InitializeComponent();
            WeightedModsDisplay.Filter = FilterMods;
            CraftedModsDisplay.Filter = FilterMods;
            SpecialModsDisplay.Filter = FilterMods;
            EnchantmentsView.IsVisibleChanged += Enchantments_OnVisible;
        }
        public void UpdateEnchantments()
        {
            if (Bench == null || Bench.BenchItem == null)
            {
                EnchantmentsView.ItemsSource = new Dictionary<PoEModData, int>();
                //EnchantmentsDisplay.UpdateData(new Dictionary<PoEModData, int>());
                return;
            }
            PoEBaseItemData itemtemplate = CraftingDatabase.AllBaseItems[Bench.BenchItem.SourceData];
            IDictionary<PoEModData, int> enchs = ModLogic.FindValidEnchantments(itemtemplate, CraftingDatabase.Enchantments);
            IDictionary<PoEModData, object> mods = new Dictionary<PoEModData, object>();
            foreach (PoEModData d in enchs.Keys)
            {
                mods.Add(d, enchs[d]);
            }
            CollectionViewSource.GetDefaultView(mods).Filter = FilterMods;
            EnchantmentsView.ItemsSource = mods;
        }
        public void UpdateCrafts()
        {
            if (Bench == null || Bench.BenchItem == null)
            {
                CraftedModsDisplay.UpdateData(new Dictionary<PoEModData, object>());
                SpecialModsDisplay.UpdateData(new Dictionary<PoEModData, object>());
                return;
            }
            PoEBaseItemData itemtemplate = CraftingDatabase.AllBaseItems[Bench.BenchItem.SourceData];
            IDictionary<PoEModData, IDictionary<string, int>> results = ModLogic.FindValidBenchMods(itemtemplate, CraftingDatabase.BenchOptions, CraftingDatabase.AllMods);
            Dictionary<PoEModData, object> mods = new Dictionary<PoEModData, object>();
            foreach (PoEModData d in results.Keys)
            {
                mods.Add(d, results[d]);
            }
            CraftedModsDisplay.UpdateData(mods);
            IDictionary<PoEModData, object> specresults = new Dictionary<PoEModData, object>();
            if (CraftingDatabase.DelveDroponlyMods.ContainsKey(itemtemplate.item_class))
            {
                foreach (string modid in CraftingDatabase.DelveDroponlyMods[itemtemplate.item_class])
                {
                    if (CraftingDatabase.AllMods.ContainsKey(modid))
                        specresults.Add(CraftingDatabase.AllMods[modid], "Drop-only: Delve");
                }
            }
            if (CraftingDatabase.IncursionDroponlyMods.ContainsKey(itemtemplate.item_class))
            {
                foreach (string modid in CraftingDatabase.IncursionDroponlyMods[itemtemplate.item_class])
                {
                    if (CraftingDatabase.AllMods.ContainsKey(modid))
                        specresults.Add(CraftingDatabase.AllMods[modid], "Drop-only: Incursion");
                }
            }
            SpecialModsDisplay.UpdateData(specresults);
        }
        public void UpdatePreviews()
        {
            if (Bench == null || Bench.BenchItem == null || Currency == null)
            {
                WeightedModsDisplay.UpdateData(new Dictionary<PoEModData, object>());
                return;
            }
            ItemCraft itemcopy = Bench.BenchItem.Copy();
            RollOptions ops = new RollOptions();
            IDictionary<PoEModData, int> extendedpool = new Dictionary<PoEModData, int>(Bench.BaseValidMods);
            PoEBaseItemData itemtemplate = CraftingDatabase.AllBaseItems[itemcopy.SourceData];
            //Shaper = null/invalid; inf and temp tags used for the 4 conquerors' exalts
            ItemInfluence inf = ItemInfluence.Shaper;
            IList<PoEModData> forcedmods = new List<PoEModData>();
            int cesscount = 0;
            object c = Currency.GetSelected();
            if (c is PoEEssenceData e)
            {
                string itemclass = itemtemplate.item_class;
                if (itemclass == "Rune Dagger")
                    itemclass = "Dagger";
                if (itemclass == "Warstaff")
                    itemclass = "Staff";
                if (e.mods.Keys.Contains(itemclass))
                    forcedmods.Add(CraftingDatabase.CoreMods[e.mods[itemclass]]);
                ops.ILvlCap = e.item_level_restriction ?? 200;
                itemcopy.ClearCraftedMods();
                itemcopy.ClearMods();
            }
            else if (c is PoECurrencyData)    
            {
                string currency = ((PoECurrencyData)c).name;
                if (itemcopy.GetInfluences().Count == 0)
                {
                    if (currency == "Redeemer's Exalted Orb")
                        inf = ItemInfluence.Redeemer;
                    else if (currency == "Hunter's Exalted Orb")
                        inf = ItemInfluence.Hunter;
                    else if (currency == "Warlord's Exalted Orb")
                        inf = ItemInfluence.Warlord;
                    else if (currency == "Crusader's Exalted Orb")
                        inf = ItemInfluence.Crusader;
                    if (inf != ItemInfluence.Shaper)
                    {
                        string tag = itemtemplate.item_class_properties[Utils.EnumConverter.InfToTag(inf)];
                        if (tag != null)    //temporarily add influence tag
                        {
                            itemcopy.LiveTags.Add(tag);
                        }
                    }
                }
                if (inf == ItemInfluence.Shaper)    //if selected item isn't a conq exalt
                {
                    //if it's a reroll currency, preview as if the item has been scoured
                    if (currency == "Chaos Orb" || currency == "Orb of Alchemy" || currency == "Orb of Transmutation" || currency == "Orb of Alteration")
                        itemcopy.ClearMods();
                }
            }
            else if (c != null)
            {
                IList<PoEFossilData> fossils = ((System.Collections.IList)c).Cast<PoEFossilData>().ToList();
                ISet<IList<PoEModWeight>> modweightgroups = new HashSet<IList<PoEModWeight>>();
                foreach (PoEFossilData fossil in fossils)
                {
                    if (fossil.rolls_lucky)
                        ops.Sanctified = true;
                    foreach (string t in fossil.added_mods)
                    {
                        if (!extendedpool.ContainsKey(CraftingDatabase.AllMods[t]))
                            extendedpool.Add(CraftingDatabase.AllMods[t], 0);
                    }
                    foreach (string t in fossil.forced_mods)
                    {
                        forcedmods.Add(CraftingDatabase.AllMods[t]);
                    }
                    forcedmods = new List<PoEModData>(ModLogic.FindBaseValidMods(itemtemplate, forcedmods, true).Keys);
                    if (fossil.corrupted_essence_chance > 0)
                    {
                        cesscount += fossil.corrupted_essence_chance;
                    }
                    modweightgroups.Add(fossil.negative_mod_weights);
                    modweightgroups.Add(fossil.positive_mod_weights);
                }
                ops.ModWeightGroups = modweightgroups;
                itemcopy.ClearCraftedMods();
                itemcopy.ClearMods();
            }
            foreach (PoEModData d in forcedmods)
            {
                itemcopy.AddMod(d);
            }
            IDictionary<PoEModData, int> validmods = ModLogic.FindValidMods(itemcopy, extendedpool, true, ops);
            if (inf != ItemInfluence.Shaper)    //if a conq exalt is selected, only show influenced mods
                validmods = ModLogic.FilterForInfluence(validmods, inf);
            IDictionary<PoEModData, object> mods = new Dictionary<PoEModData, object>();
            foreach (PoEModData d in validmods.Keys)
            {
                mods.Add(d, validmods[d]);
            }
            foreach (PoEModData d in forcedmods)
            {
                mods.Add(d, "Always");
            }
            if (cesscount > 0)
            {
                IDictionary<PoEModData, int> glyphicmods = ModLogic.FindGlyphicMods(itemcopy, ops.ModWeightGroups, ops.Sanctified);
                if (glyphicmods.Count > 0)
                {
                    int weightsum = 0;
                    foreach (PoEModData d in glyphicmods.Keys)
                    {
                        weightsum += glyphicmods[d];
                    }
                    foreach (PoEModData d in glyphicmods.Keys)
                    {
                        mods.Add(d, "Special: " + ((double)cesscount / 100).ToString("0.#") + " x " + ((double)glyphicmods[d] * 100 / weightsum).ToString("N0") + "%");
                    }
                }
            }
            WeightedModsDisplay.UpdateData(mods);
        }
        private bool FilterMods(object o)
        {
            if (String.IsNullOrEmpty(EnchSearchBox.Text))
                return true;
            else
            {
                KeyValuePair<PoEModData, object>? kv = o as KeyValuePair<PoEModData, object>?;
                if (kv == null)
                    return false;
                else
                    return (kv.Value.Key.ToString().IndexOf(EnchSearchBox.Text, StringComparison.OrdinalIgnoreCase) >= 0);
            } 
        }
        private void Mods_Filter(object sender, TextChangedEventArgs e)
        {
            if (EnchantmentsView.IsVisible)
                RefreshEnchantmentsView();
            WeightedModsDisplay.RefreshListViews();
            CraftedModsDisplay.RefreshListViews();
            SpecialModsDisplay.RefreshListViews();
        }
        private void Enchantments_OnVisible(object sender, DependencyPropertyChangedEventArgs e)
        {
            RefreshEnchantmentsView();
        }
        private void RefreshEnchantmentsView()
        {
            CollectionViewSource.GetDefaultView(EnchantmentsView.ItemsSource)?.Refresh();
        }
        private void ForceAdd_Click(object sender, RoutedEventArgs e)
        {
            if (((ModTabs.SelectedItem as TabItem).Content is ModsView activeview))
            {
                if (activeview.PrefixList.SelectedItem != null)
                    AddMod?.Invoke(this, new AddModEventArgs() { SelectedMod = activeview.PrefixList.SelectedItem });
                else
                    AddMod?.Invoke(this, new AddModEventArgs() { SelectedMod = activeview.SuffixList.SelectedItem });
            }
            else if (ModTabs.SelectedIndex == 3)
            {
                AddMod?.Invoke(this, new AddModEventArgs() { SelectedMod = EnchantmentsView.SelectedItem });
            }
        }
    }
}
