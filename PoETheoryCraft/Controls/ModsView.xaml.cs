using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
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
        //public static readonly bool DefaultExpand = Properties.Settings.Default.ExpandGroups;
        //public static readonly ISet<string> ExpandedGroups = new HashSet<string>();
        public static int psum = 0;
        public static int ssum = 0;
        public Predicate<object> Filter { get; set; }
        public ModsView()
        {
            InitializeComponent();
            IsVisibleChanged += On_Visible;
        }
        //split mods into prefix and suffix lists
        public void UpdateData(IDictionary<PoEModData, object> mods)
        {
            IDictionary<PoEModData, object> p = new Dictionary<PoEModData, object>();
            IDictionary<PoEModData, object> s = new Dictionary<PoEModData, object>();
            if (mods != null)
            {
                foreach (PoEModData m in mods.Keys)
                {
                    if (m.generation_type == ModLogic.Suffix)
                        s.Add(m, mods[m]);
                    else
                        p.Add(m, mods[m]);
                }
            }
            CollectionViewSource.GetDefaultView(p).GroupDescriptions.Add(new PropertyGroupDescription("Key.group"));
            CollectionViewSource.GetDefaultView(s).GroupDescriptions.Add(new PropertyGroupDescription("Key.group"));
            CollectionViewSource.GetDefaultView(p).SortDescriptions.Add(new SortDescription("Key.group", ListSortDirection.Ascending));
            CollectionViewSource.GetDefaultView(s).SortDescriptions.Add(new SortDescription("Key.group", ListSortDirection.Ascending));
            CollectionViewSource.GetDefaultView(p).SortDescriptions.Add(new SortDescription("Key.required_level", ListSortDirection.Ascending));
            CollectionViewSource.GetDefaultView(s).SortDescriptions.Add(new SortDescription("Key.required_level", ListSortDirection.Ascending));
            if (Filter != null)
            {
                CollectionViewSource.GetDefaultView(p).Filter = Filter;
                CollectionViewSource.GetDefaultView(s).Filter = Filter;
            }
            PrefixList.ItemsSource = p;
            SuffixList.ItemsSource = s;
            int ps = 0;
            int ss = 0;
            foreach (object o in p.Values)
            {
                if (o is int n)
                    ps += n;
            }
            foreach (object o in s.Values)
            {
                if (o is int n)
                    ss += n;
            }
            if (ps > 0 || ss > 0)
            {
                psum = ps;
                ssum = ss;
                PrefixTally.Content = psum + " (" + ((double)psum * 100 / (psum + ssum)).ToString("N2") + "%)";
                SuffixTally.Content = ssum + " (" + ((double)ssum * 100 / (psum + ssum)).ToString("N2") + "%)";
            }
            else
            {
                PrefixTally.Content = "";
                SuffixTally.Content = "";
            }
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
        //private void LogExpanded(object sender, RoutedEventArgs e)
        //{
        //    if ((sender as Expander).Header is StackPanel p)
        //    {
        //        string s = (p.Children[0] as TextBlock).Text;
        //        if (DefaultExpand)
        //            ExpandedGroups.Remove(s);
        //        else
        //            ExpandedGroups.Add(s);
        //    }
        //}
        //private void LogCollapsed(object sender, RoutedEventArgs e)
        //{
        //    if ((sender as Expander).Header is StackPanel p)
        //    {
        //        string s = (p.Children[0] as TextBlock).Text;
        //        if (DefaultExpand)
        //            ExpandedGroups.Add(s);
        //        else
        //            ExpandedGroups.Remove(s);
        //    }
        //}
        private void On_Visible(object sender, DependencyPropertyChangedEventArgs e)
        {
            RefreshListViews();
        }
        public void RefreshListViews()
        {
            if (IsVisible)
            {
                CollectionViewSource.GetDefaultView(PrefixList.ItemsSource)?.Refresh();
                CollectionViewSource.GetDefaultView(SuffixList.ItemsSource)?.Refresh();
            }
        }
    }
    //public class ShouldExpandConverter : IValueConverter
    //{
    //    object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
    //    {
    //        return ModsView.ExpandedGroups.Contains(value as string) ^ ModsView.DefaultExpand;
    //    }

    //    object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    //    {
    //        throw new NotImplementedException();
    //    }
    //}
    public class ModDataToBrushConverter : IValueConverter
    {
        private static readonly SolidColorBrush WarlordBrush = new SolidColorBrush(Color.FromRgb(255, 255, 120));
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            KeyValuePair<PoEModData, object> kv = (KeyValuePair<PoEModData, object>)value;
            PoEModData data = kv.Key;
            if (kv.Value is string)
            {
                if (data.name == "Subterranean" || data.name == "of the Underground")
                    return Brushes.Gold;
                else
                    return Brushes.Orchid;
            }
            else
            {
                if (data.name == "Subterranean" || data.name == "of the Underground")
                    return Brushes.Gold;
                if (Utils.EnumConverter.InfToNames(ItemInfluence.Shaper).Contains(data.name))
                    return Brushes.DodgerBlue;
                if (Utils.EnumConverter.InfToNames(ItemInfluence.Elder).Contains(data.name))
                    return Brushes.Gray;
                if (Utils.EnumConverter.InfToNames(ItemInfluence.Redeemer).Contains(data.name))
                    return Brushes.LightBlue;
                if (Utils.EnumConverter.InfToNames(ItemInfluence.Hunter).Contains(data.name))
                    return Brushes.LightGreen;
                if (Utils.EnumConverter.InfToNames(ItemInfluence.Warlord).Contains(data.name))
                    return WarlordBrush;
                if (Utils.EnumConverter.InfToNames(ItemInfluence.Crusader).Contains(data.name))
                    return Brushes.Pink;
            }
            return Brushes.White;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
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
    public struct CostObject
    {
        public string Label { get; set; }
        public BitmapImage Icon { get; set; }
    }
    //Convert either an int weight or a Dictionary<string,int> crafting cost to string
    public class CostsToDisplayConverter : IValueConverter
    {
        object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            IList<CostObject> objs = new List<CostObject>();
            if (value is int v)
            {
                double p = (double)v * 100 / (ModsView.psum + ModsView.ssum);
                objs.Add(new CostObject() { Label = v + " (" + p.ToString("N2") + "%)" });
            }
            else if (value is string s)
            {
                objs.Add(new CostObject() { Label = s });
            }
            else if (value is IDictionary<string, int> d)
            {
                foreach (string k in d.Keys)
                {
                    BitmapImage icon = null;
                    if (CraftingDatabase.Currencies.ContainsKey(k))
                        icon = CraftingDatabase.Currencies[k].icon;
                    else if (CraftingDatabase.Fossils.ContainsKey(k))
                        icon = CraftingDatabase.Fossils[k].icon;
                    else if (CraftingDatabase.Essences.ContainsKey(k))
                        icon = CraftingDatabase.Essences[k].icon;
                    objs.Add(new CostObject() { Label = d[k] + "x", Icon = icon });
                }
            }
            else
                objs.Add(new CostObject() { Label = "???" });
            return objs;
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
