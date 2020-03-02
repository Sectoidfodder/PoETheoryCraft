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
using System.Globalization;

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
            Icons.LoadImages();

            InitializeComponent();

            //must be done first so mod templates are translated as they're loaded
            StatTranslator.LoadStatLocalization(@"Data\stat_translations.min.json");

            CraftingDatabase.LoadPseudoStats(@"Data\pseudo_stats.json", @"user_pseudo_stats.json");
            CraftingDatabase.LoadMods(@"Data\mods.min.json", @"Data\mod_types.min.json");
            CraftingDatabase.LoadBaseItems(@"Data\base_items.min.json", @"Data\item_classes.min.json");
            CraftingDatabase.LoadBenchOptions(@"Data\crafting_bench_options.min.json");
            CraftingDatabase.LoadEssences(@"Data\essences.min.json");
            CraftingDatabase.LoadFossils(@"Data\fossils.min.json");
            CraftingDatabase.LoadPrices("pricedata.json");

            CurrencyBox.LoadEssences(CraftingDatabase.Essences.Values);
            CurrencyBox.LoadFossils(CraftingDatabase.Fossils.Values);
            CurrencyBox.LoadCurrencies(CraftingDatabase.Currencies.Values);

            Bench = new CraftingBench();
            ModPreview.Bench = Bench;
            ModPreview.Currency = CurrencyBox;
            CurrencyTally.ItemsSource = Bench.CurrencySpent;

            PostCraftButton.Content = new Image() { Source = Icons.Plus };
            SearchButton.Content = new Image() { Source = Icons.Search };
            BigBox.Text = "";
            BigBox.FontWeight = FontWeights.Bold;
            BigBox.Foreground = Brushes.Red;
            RepeatCountBox.Max = Properties.Settings.Default.BulkCraftLimit;
            RepeatCountBox.TextChanged += CheckRepeatCount;
            RepeatCountBox.Text = "10000";
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
            object kv = ModPreview.GetSelected();
            if (kv == null)
                return;
            string res;
            if (kv is KeyValuePair<PoEModData, int> kvint)
            {
                res = Bench.ForceAddMod(kvint.Key);
            }
            else if (kv is KeyValuePair<PoEModData, IDictionary<string, int>> kvdict)
            {

                res = Bench.ForceAddMod(kvdict.Key, costs: kvdict.Value);
            }
            else
            {
                BigBox.Text = "Unrecognized key-value format for selected mod";
                return;
            }
            if (res != null)
                BigBox.Text = res;
            else
            {
                ItemSlot.UpdateData(Bench.BenchItem);
                ModPreview.UpdatePreviews();
                CollectionViewSource.GetDefaultView(Bench.CurrencySpent).Refresh();
            }
        }
        private void Prices_Click(object sender, RoutedEventArgs e)
        {
            PricesDialog d = new PricesDialog() { Owner = this };
            d.ShowDialog();
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
                if (Bench.BenchItem != null)
                {
                    Bench.BenchItem.BaseQuality = n1;
                    ItemSlot.UpdateData(Bench.BenchItem);
                }
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
            if (c is PoEEssenceData)
            {
                res = Bench.ApplyEssence(c as PoEEssenceData, n);
                if (res != null)
                    BigBox.Text = res;
            }
            else if (c is PoECurrencyData)
            {
                res = Bench.ApplyCurrency(c as PoECurrencyData, n);
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
                if (c is PoECurrencyData data)
                    RepeatResults.CurrenciesUsed = new List<PoECurrencyData>() { data };
                else
                    RepeatResults.CurrenciesUsed = ((System.Collections.IList)c).Cast<PoECurrencyData>().ToList();
                RepeatResults.SortBy = null;
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
            if (c is PoEEssenceData)
            {
                res = Bench.ApplyEssence(c as PoEEssenceData);
                if (res != null)
                    BigBox.Text = res;
            }
            else if (c is PoECurrencyData)
            {
                res = Bench.ApplyCurrency(c as PoECurrencyData);
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
                CollectionViewSource.GetDefaultView(Bench.CurrencySpent).Refresh();
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
            SearchDialog d = new SearchDialog(CraftingDatabase.StatTemplates, RepeatResults.Filter) { Owner = this };
            bool? res = d.ShowDialog();
            if (!res.HasValue || !res.Value)
                return;
            FilterCondition filter = d.GetFilterCondition();
            if (filter == null)
                SearchButton.ClearValue(Button.BackgroundProperty);
            else
                SearchButton.Background = Brushes.Green;
            RepeatResults.Filter = filter;
        }
        private void ItemParam_Click(object sender, EventArgs e)
        {
            TextBlock tb = (TextBlock)sender;
            string sortby = tb.Tag != null ? "[property] " + tb.Tag : ItemParser.ParseLine(tb.Text.Split('\n')[0]).Key;
            RepeatResults.SortBy = sortby;
        }
        private void BenchMove_Click(object sender, EventArgs e)
        {
            ItemCraft item = ((MenuItem)sender).Tag as ItemCraft;
            bool samebase = item != null && Bench.BenchItem != null && item.SourceData == Bench.BenchItem.SourceData;

            Bench.BenchItem = item.Copy();
            ItemSlot.UpdateData(Bench.BenchItem);
            ModPreview.UpdatePreviews();
            if (!samebase)
            {
                ModPreview.UpdateCrafts();
                Bench.PostRoll = new PostRollOptions();
                PostCraftButton.ClearValue(Button.BackgroundProperty);
            }
        }
        private void BenchItem_Edited(object sender, EventArgs e)
        {
            ModPreview.UpdatePreviews();
        }
        private void CheckRepeatCount(object sender, RoutedEventArgs e)
        {
            RepeatButton.IsEnabled = RepeatCountBox.Valid;
        }
        private void CurrencyTally_Clear(object sender, RoutedEventArgs e)
        {
            Bench.CurrencySpent.Clear();
            CollectionViewSource.GetDefaultView(Bench.CurrencySpent).Refresh();
        }
    }

    public class KeyToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string s = value as string;
            BitmapImage icon = null;
            if (CraftingDatabase.Currencies.ContainsKey(s))
                icon = CraftingDatabase.Currencies[s].icon;
            else if (CraftingDatabase.Fossils.ContainsKey(s))
                icon = CraftingDatabase.Fossils[s].icon;
            else if (CraftingDatabase.Essences.ContainsKey(s))
                icon = CraftingDatabase.Essences[s].icon;
            return icon;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
