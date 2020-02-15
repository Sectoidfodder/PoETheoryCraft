using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
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
using PoETheoryCraft.Controls;
using PoETheoryCraft.Utils;
using Microsoft.Win32;

namespace PoETheoryCraft
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly CraftingBench Bench;
        public MainWindow()
        {
            InitializeComponent();

            //must be done first so mod templates are translated as they're loaded
            StatTranslator.LoadStatLocalization(@"Data\stat_translations.min.json");

            CraftingDatabase.LoadMods(@"Data\mods.min.json", @"Data\mod_types.min.json");
            CraftingDatabase.LoadBaseItems(@"Data\base_items.min.json", @"Data\item_classes.min.json");
            CraftingDatabase.LoadBenchOptions(@"Data\crafting_bench_options.min.json");
            CraftingDatabase.LoadEssences(@"Data\essences.min.json");
            CraftingDatabase.LoadFossils(@"Data\fossils.min.json");

            CurrencyBox.LoadEssences(CraftingDatabase.Essences.Values);
            CurrencyBox.LoadFossils(CraftingDatabase.Fossils.Values);
            CurrencyBox.LoadCurrencies(CraftingDatabase.Currencies.Values);

            Bench = new CraftingBench();
            ModPreview.Bench = Bench;
            ModPreview.Currency = CurrencyBox;

            BigBox.Text = "";
            BigBox.FontWeight = FontWeights.Bold;
            BigBox.Foreground = Brushes.Red;
            RepeatCountBox.TextChanged += CheckRepeatCount;
        }
        private void ItemBaseButton_Click(object sender, RoutedEventArgs e)
        {
            if (CraftingDatabase.CoreBaseItems == null)
                return;

            BaseItemDialog d = new BaseItemDialog(CraftingDatabase.CoreBaseItems) { Owner = this };
            bool? res = d.ShowDialog();
            if (!res.HasValue || !res.Value || d.ItemNameView.SelectedItem == null)
                return;

            if (int.TryParse(d.ILvlBox.Text, out int ilvl))
                ilvl = (ilvl < 1) ? 1 : (ilvl > 100) ? 100 : ilvl;
            else
                ilvl = 100;
            ISet<ItemInfluence> infs = new HashSet<ItemInfluence>();
            if (d.ShaperCheck.IsChecked ?? false)
                infs.Add(ItemInfluence.Shaper);
            if (d.ElderCheck.IsChecked ?? false)
                infs.Add(ItemInfluence.Elder);
            if (d.RedeemerCheck.IsChecked ?? false)
                infs.Add(ItemInfluence.Redeemer);
            if (d.HunterCheck.IsChecked ?? false)
                infs.Add(ItemInfluence.Hunter);
            if (d.WarlordCheck.IsChecked ?? false)
                infs.Add(ItemInfluence.Warlord);
            if (d.CrusaderCheck.IsChecked ?? false)
                infs.Add(ItemInfluence.Crusader);

            Bench.BenchItem = new ItemCraft(d.SelectedBase, ilvl, infs);
            ItemSlot.UpdateData(Bench.BenchItem);
            ModPreview.UpdatePreviews();
            ModPreview.UpdateCrafts();
            Bench.PostRoll = new PostRollOptions();
            PostCraftButton.ClearValue(Button.BackgroundProperty);
        }
        private void ForceAdd_Click(object sender, RoutedEventArgs e)
        {
            PoEModData mod = ModPreview.GetSelected();
            if (mod == null)
                return;
            string res = Bench.ForceAddMod(mod);
            if (res != null)
                BigBox.Text = res;
            else
            {
                ItemSlot.UpdateData(Bench.BenchItem);
                ModPreview.UpdatePreviews();
            }
        }
        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            SettingsDialog d = new SettingsDialog() { Owner = this };
            bool? res = d.ShowDialog();
            if (!res.HasValue || !res.Value)
                return;
            if (int.TryParse(d.Rare4.Text, out int n1) && int.TryParse(d.Rare5.Text, out int n2) && int.TryParse(d.Rare6.Text, out int n3) && (n1 >= 0 && n2 >= 0 && n3 >= 0))
            {
                ModLogic.ModCountWeights = new List<int>() { n1, n2, n3 };
                Properties.Settings.Default.MW4 = n1;
                Properties.Settings.Default.MW5 = n2;
                Properties.Settings.Default.MW6 = n3;
            }
            if (int.TryParse(d.RareJ3.Text, out n1) && int.TryParse(d.RareJ4.Text, out n2) && (n1 >= 0 && n2 >= 0))
            {
                ModLogic.JewelModCountWeights = new List<int>() { n1, n2 };
                Properties.Settings.Default.JMW3 = n1;
                Properties.Settings.Default.JMW4 = n2;
            }
            if (int.TryParse(d.Magic1.Text, out n1) && int.TryParse(d.Magic2.Text, out n2) && (n1 >= 0 && n2 >= 0))
            {
                ModLogic.MagicModCountWeights = new List<int>() { n1, n2 };
                Properties.Settings.Default.MMW1 = n1;
                Properties.Settings.Default.MMW2 = n2;
            }
            if (int.TryParse(d.PerPage.Text, out n1) && n1 > 0)
            {
                BulkItemsView.ResultsPerPage = n1;
                Properties.Settings.Default.ResultsPerPage = n1;
                RepeatResults.UpdateDisplay();
            }
            if (int.TryParse(d.Quality.Text, out n1) && n1 > 0)
            {
                ItemCraft.DefaultQuality = n1;
                Properties.Settings.Default.ItemQuality = n1;
                Bench.BenchItem.BaseQuality = n1;
                ItemSlot.UpdateData(Bench.BenchItem);
                foreach (ItemCraft item in Bench.MassResults)
                {
                    item.BaseQuality = n1;
                }
                RepeatResults.UpdateDisplay();
            }
            Properties.Settings.Default.Save();
        }
        private void Currency_SelectionChanged(object sender, CurrencySelector.CurrencyEventArgs e)
        {
            ModPreview.UpdatePreviews();
        }
        private void Repeat_Click(object sender, RoutedEventArgs e)
        {
            BigBox.Text = "";
            object c = CurrencyBox.GetSelected();
            if (c == null)
            {
                BigBox.Text = "No currency selected";
                return;
            }
            if (!RepeatCountBox.Valid || !int.TryParse(RepeatCountBox.Text, out int n))
            {
                BigBox.Text = "Error parsing try count";
                return;
            }
            string res;
            if (c is string)
            {
                res = Bench.ApplyCurrency(c as string, n);
                if (res != null)
                    BigBox.Text = res;
            }
            else if (c is PoEEssenceData)
            {
                res = Bench.ApplyEssence(c as PoEEssenceData, n);
                if (res != null)
                    BigBox.Text = res;
            }
            else
            {
                IEnumerable<PoEFossilData> collection = ((System.Collections.IList)c).Cast<PoEFossilData>();
                if (collection != null && collection.Count() > 0)
                {
                    res = Bench.ApplyFossils(collection.ToList(), n);
                    if (res != null)
                        BigBox.Text = res;
                }
                else
                {
                    BigBox.Text = "No fossil selected";
                    return;
                }
            }
            if (res == null)
            {
                RepeatResults.Items = Bench.MassResults;
            }
        }
        private void CraftButton_Click(object sender, RoutedEventArgs e)
        {
            BigBox.Text = "";
            object c = CurrencyBox.GetSelected();
            if (c == null)
            {
                BigBox.Text = "No currency selected";
                return;
            }
            string res;
            if (c is string)
            {
                res = Bench.ApplyCurrency(c as string);
                if (res != null)
                    BigBox.Text = res;
            }
            else if (c is PoEEssenceData)
            {
                res = Bench.ApplyEssence(c as PoEEssenceData);
                if (res != null)
                    BigBox.Text = res;
            }
            else
            {
                IEnumerable<PoEFossilData> collection = ((System.Collections.IList)c).Cast<PoEFossilData>();
                if (collection != null && collection.Count() > 0)
                {
                    res = Bench.ApplyFossils(collection.ToList());
                    if (res != null)
                        BigBox.Text = res;
                }
                else
                {
                    BigBox.Text = "No fossil selected";
                    return;
                }
            }
            if (res == null)
            {
                ItemSlot.UpdateData(Bench.BenchItem);
                ModPreview.UpdatePreviews();
            }
        }
        private void PostCraftButton_Click(object sender, RoutedEventArgs e)
        {
            if (Bench.BenchItem == null)
            {
                BigBox.Text = "Bench is empty";
                return;
            }
            PoEBaseItemData itemtemplate = CraftingDatabase.AllBaseItems[Bench.BenchItem.SourceData];
            PostCraftDialog d = new PostCraftDialog(ModLogic.FindValidBenchMods(itemtemplate, CraftingDatabase.BenchOptions, CraftingDatabase.AllMods), Bench.PostRoll) { Owner = this };
            bool? res = d.ShowDialog();
            if (!res.HasValue || !res.Value)
                return;
            PostRollOptions ops = d.GetPostRollOptions();
            if (!ops.Maximize && ops.TryCrafts.Count == 0)
                PostCraftButton.ClearValue(Button.BackgroundProperty);
            else
                PostCraftButton.Background = Brushes.Green;
            Bench.PostRoll = ops;
        }
        private void Search_Click(object sender, RoutedEventArgs e)
        {
            SearchDialog d = new SearchDialog(CraftingDatabase.StatTemplates) { Owner = this };
            bool? res = d.ShowDialog();
            if (!res.HasValue || !res.Value)
                return;
        }
        private void CheckRepeatCount(object sender, RoutedEventArgs e)
        {
            RepeatButton.IsEnabled = RepeatCountBox.Valid;
        }
    }
}
