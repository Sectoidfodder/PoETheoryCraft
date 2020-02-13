using System;
using System.Collections.Generic;
using System.Globalization;
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

namespace PoETheoryCraft.Controls
{
    
    /// <summary>
    /// Interaction logic for CurrencySelector.xaml
    /// </summary>
    public partial class CurrencySelector : UserControl
    {
        public class CurrencyEventArgs : EventArgs
        {
            public string Currency { get; set; }
            public PoEEssenceData Essence { get; set; }
            public IList<PoEFossilData> Fossils { get; set; }
        }

        private readonly IList<string> BasicCurrencies = new List<string>() { "chaos", "alch", "scour", "alt", "transmute", "augment", "regal", "exalt", "exalt-redeemer", "exalt-hunter", "exalt-warlord", "exalt-crusader", "annul", "divine", "blessed", "remove-crafted" };
        private readonly IList<string> Catalysts = new List<string>() { "abrasive", "fertile", "imbued", "intrinsic", "prismatic", "tempering", "turbulent" };
        public event EventHandler<CurrencyEventArgs> CurrencySelectionChanged;
        public CurrencySelector()
        {
            InitializeComponent();
            BasicView.ItemsSource = BasicCurrencies;
            CatalystView.ItemsSource = Catalysts;
        }
        public void LoadEssences(ICollection<PoEEssenceData> essences)
        {
            CollectionViewSource.GetDefaultView(essences).Filter = AllowedEssences;
            EssenceView.ItemsSource = essences;
        }
        public void LoadFossils(ICollection<PoEFossilData> fossils)
        {
            CollectionViewSource.GetDefaultView(fossils).Filter = AllowedFossils;
            FossilView.ItemsSource = fossils;
        }
        public void CurrencyTabs_SelectionChanged(object sender, RoutedEventArgs e)
        {
            switch (CurrencyTabs.SelectedIndex)
            {
                case 0:
                    CurrencySelectionChanged(this, new CurrencyEventArgs() { Currency = BasicView.SelectedItem as string });
                    break;
                case 1:
                    SolidColorBrush b = FossilView.SelectedItems.Count > 4 ? Brushes.Red : Brushes.Black;
                    FossilLabel1.Foreground = b;
                    FossilLabel2.Foreground = b;
                    CurrencySelectionChanged(this, new CurrencyEventArgs() { Fossils = ((System.Collections.IList)FossilView.SelectedItems).Cast<PoEFossilData>().ToList() });
                    break;
                case 2:
                    CurrencySelectionChanged(this, new CurrencyEventArgs() { Essence = EssenceView.SelectedItem as PoEEssenceData });
                    break;
                case 3:
                    CurrencySelectionChanged(this, new CurrencyEventArgs() { Currency = CatalystView.SelectedItem as string });
                    break;
                default:
                    return;
            }
        }
        public object GetSelected()
        {
            switch (CurrencyTabs.SelectedIndex)
            {
                case 0:
                    return BasicView.SelectedItem;
                case 1:
                    return FossilView.SelectedItems;
                case 2:
                    return EssenceView.SelectedItem;
                case 3:
                    return CatalystView.SelectedItem;
                default:
                    return null;
            }
        }
        private bool AllowedEssences(object o)
        {
            PoEEssenceData e = o as PoEEssenceData;
            return e.level > 0;
        }
        private bool AllowedFossils(object o)
        {
            PoEFossilData f = o as PoEFossilData;
            return (!f.changes_quality && !f.enchants && !f.mirrors && !f.rolls_lucky && !f.rolls_white_sockets && f.sell_price_mods.Count == 0);
        }
    }

    public class FossilTooltipConverter : IValueConverter
    {
        object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return string.Join("\n", (HashSet<string>)value);
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class EssenceTooltipConverter : IValueConverter
    {
        object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            IDictionary<string, string> mods = (IDictionary<string, string>)value;
            string t = "";
            foreach (string key in mods.Keys)
            {
                t += key + ": " + CraftingDatabase.CoreMods[mods[key]] + "\n";
            }
            return t.Trim('\n');
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class NameToImageConverter : IValueConverter
    {
        object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string name = value as string;
            return "Icons\\fossil\\" + name.Replace(" ", "") + ".png";
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
