using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using PoETheoryCraft.Utils;

namespace PoETheoryCraft.Controls
{
    /// <summary>
    /// Interaction logic for ModsView.xaml
    /// </summary>
    public partial class ModsView : UserControl
    {
        //track and sync expanded groups across all instances
        public static readonly ISet<string> ExpandedGroups = new HashSet<string>();
        public ModsView()
        {
            InitializeComponent();
            IsVisibleChanged += RefreshListViews;
        }
        //Shows a list of mods along with their weights
        public void UpdateData(IDictionary<PoEModData, int> mods)
        {
            IDictionary<PoEModData, int> p = new Dictionary<PoEModData, int>();
            IDictionary<PoEModData, int> s = new Dictionary<PoEModData, int>();
            if (mods != null)
            {
                foreach (PoEModData m in mods.Keys)
                {
                    if (m.generation_type == ModLogic.Prefix)
                        p.Add(m, mods[m]);
                    else if (m.generation_type == ModLogic.Suffix)
                        s.Add(m, mods[m]);
                }
            }
            CollectionViewSource.GetDefaultView(p).GroupDescriptions.Add(new PropertyGroupDescription("Key.group"));
            CollectionViewSource.GetDefaultView(s).GroupDescriptions.Add(new PropertyGroupDescription("Key.group"));
            CollectionViewSource.GetDefaultView(p).SortDescriptions.Add(new SortDescription("Key.group", ListSortDirection.Ascending));
            CollectionViewSource.GetDefaultView(s).SortDescriptions.Add(new SortDescription("Key.group", ListSortDirection.Ascending));
            CollectionViewSource.GetDefaultView(p).SortDescriptions.Add(new SortDescription("Key.required_level", ListSortDirection.Ascending));
            CollectionViewSource.GetDefaultView(s).SortDescriptions.Add(new SortDescription("Key.required_level", ListSortDirection.Ascending));
            PrefixList.ItemsSource = p;
            SuffixList.ItemsSource = s;
            int psum = 0;
            int ssum = 0;
            foreach (int n in p.Values)
            {
                psum += n;
            }
            foreach (int n in s.Values)
            {
                ssum += n;
            }
            PrefixTally.Content = psum;
            SuffixTally.Content = ssum;
        }
        //Shows a list of mods along with their crafting costs
        public void UpdateData(IDictionary<PoEModData, IDictionary<string, int>> mods)
        {
            IDictionary<PoEModData, IDictionary<string, int>> p = new Dictionary<PoEModData, IDictionary<string, int>>();
            IDictionary<PoEModData, IDictionary<string, int>> s = new Dictionary<PoEModData, IDictionary<string, int>>();
            if (mods != null)
            {
                foreach (PoEModData m in mods.Keys)
                {
                    if (m.generation_type == ModLogic.Prefix)
                        p.Add(m, mods[m]);
                    else if (m.generation_type == ModLogic.Suffix)
                        s.Add(m, mods[m]);
                }
            }
            CollectionViewSource.GetDefaultView(p).GroupDescriptions.Add(new PropertyGroupDescription("Key.group"));
            CollectionViewSource.GetDefaultView(s).GroupDescriptions.Add(new PropertyGroupDescription("Key.group"));
            CollectionViewSource.GetDefaultView(p).SortDescriptions.Add(new SortDescription("Key.group", ListSortDirection.Ascending));
            CollectionViewSource.GetDefaultView(s).SortDescriptions.Add(new SortDescription("Key.group", ListSortDirection.Ascending));
            CollectionViewSource.GetDefaultView(p).SortDescriptions.Add(new SortDescription("Key.required_level", ListSortDirection.Ascending));
            CollectionViewSource.GetDefaultView(s).SortDescriptions.Add(new SortDescription("Key.required_level", ListSortDirection.Ascending));
            PrefixList.ItemsSource = p;
            SuffixList.ItemsSource = s;
            PrefixTally.Content = "";
            SuffixTally.Content = "";
        }
        //Force one selected item between the two lists
        private void ModList_SelectionChanged(object sender, RoutedEventArgs e)
        {
            if ((sender as ListView).SelectedItem != null)
            {
                if (sender == PrefixList)
                    SuffixList.UnselectAll();
                else
                    PrefixList.UnselectAll();
            }
        }
        private void LogExpanded(object sender, RoutedEventArgs e)
        {
            if ((sender as Expander).Header is StackPanel p)
            {
                string s = (p.Children[0] as TextBlock).Text;
                ExpandedGroups.Add(s);
            }
        }
        private void LogCollapsed(object sender, RoutedEventArgs e)
        {
            if ((sender as Expander).Header is StackPanel p)
            {
                string s = (p.Children[0] as TextBlock).Text;
                ExpandedGroups.Remove(s);
            }
        }
        private void RefreshListViews(object sender, DependencyPropertyChangedEventArgs e)
        {
            ModsView v = sender as ModsView;
            if (v.IsVisible)
            {
                CollectionViewSource.GetDefaultView(v.PrefixList.ItemsSource)?.Refresh();
                CollectionViewSource.GetDefaultView(v.SuffixList.ItemsSource)?.Refresh();
            }
        }
    }
    public class ShouldExpandConverter : IValueConverter
    {
        object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ModsView.ExpandedGroups.Contains(value as string);
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    //Convert PoEModData to its tooltip
    public class ModDataToTooltipConverter : IValueConverter
    {
        object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            PoEModData data = value as PoEModData;
            string s = data.name + "; lvl " + data.required_level;
            foreach (PoEModWeight w in data.spawn_weights)
            {
                if (w.tag == "no_attack_mods" && w.weight == 0)
                    s += "; meta-attack";
                else if (w.tag == "no_caster_mods" && w.weight == 0)
                    s += "; meta-caster";
            }
            s += "; " + string.Join("," , data.type_tags);
            return s;
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    //Convert either an int weight or a Dictionary<string,int> crafting cost to string
    public class CostsToStringConverter : IValueConverter
    {
        object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int)
                return ((int)value).ToString();
            else if (value is IDictionary<string, int>)
                return "cost";
            else
                return "???";
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
