using System;
using System.Collections.Generic;
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
        public ModsControl()
        {
            InitializeComponent();
        }
        public void UpdateCrafts()
        {
            if (Bench == null || Bench.BenchItem == null)
            {
                CraftedModsDisplay.UpdateData(new Dictionary<PoEModData, int>());
                return;
            }
            PoEBaseItemData itemtemplate = CraftingDatabase.AllBaseItems[Bench.BenchItem.SourceData];
            CraftedModsDisplay.UpdateData(ModLogic.FindValidBenchMods(itemtemplate, CraftingDatabase.BenchOptions, CraftingDatabase.AllMods));
        }
        public void UpdatePreviews()
        {
            if (Bench == null || Bench.BenchItem == null || Currency == null)
            {
                WeightedModsDisplay.UpdateData(new Dictionary<PoEModData, int>());
                return;
            }
            ItemCraft itemcopy = Bench.BenchItem.Copy();
            RollOptions ops = new RollOptions();
            IDictionary<PoEModData, int> extendedpool = new Dictionary<PoEModData, int>(Bench.BaseValidMods);
            PoEBaseItemData itemtemplate = CraftingDatabase.AllBaseItems[itemcopy.SourceData];
            //Shaper = null/invalid; inf and temp tags used for the 4 conquerors' exalts
            ItemInfluence inf = ItemInfluence.Shaper;

            object c = Currency.GetSelected();
            if (c is PoEEssenceData)
            {
                ops.ILvlCap = ((PoEEssenceData)c).item_level_restriction ?? 200;
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
                        string tag = itemtemplate.item_class_properties[EnumConverter.InfToTag(inf)];
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
                //ICollection<PoEModData> forcedmods = new List<PoEModData>();
                foreach (PoEFossilData fossil in fossils)
                {
                    foreach (string t in fossil.added_mods)
                    {
                        if (!extendedpool.ContainsKey(CraftingDatabase.AllMods[t]))
                            extendedpool.Add(CraftingDatabase.AllMods[t], 0);
                    }
                    //foreach (string t in fossil.forced_mods)
                    //{
                    //    forcedmods.Add(CraftingDatabase.MiscMods[t]);
                    //}
                    modweightgroups.Add(fossil.negative_mod_weights);
                    modweightgroups.Add(fossil.positive_mod_weights);
                }
                ops.ModWeightGroups = modweightgroups;
                itemcopy.ClearCraftedMods();
                itemcopy.ClearMods();
            }
            
            IDictionary<PoEModData, int> validmods = ModLogic.FindValidMods(itemcopy, extendedpool, true, ops);
            if (inf != ItemInfluence.Shaper)    //if a conq exalt is selected, only show influenced mods
                validmods = ModLogic.FilterForInfluence(validmods, inf);

            WeightedModsDisplay.UpdateData(validmods);
        }
        public object GetSelected()
        {
            if (!((ModTabs.SelectedItem as TabItem).Content is ModsView activeview))
                return null;
            if (activeview.PrefixList.SelectedItem != null)
                return activeview.PrefixList.SelectedItem;
            else
                return activeview.SuffixList.SelectedItem;
        }
    }
}
