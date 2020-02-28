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
using PoETheoryCraft.Utils;

namespace PoETheoryCraft.Controls
{
    /// <summary>
    /// Interaction logic for SearchGroup.xaml
    /// </summary>
    public partial class SearchGroup : UserControl
    {
        public enum GroupType
        {
            And,
            Not,
            Count,
            Weight
        }
        private struct SearchLine
        {
            public string Label { get; set; }
            public double? Value1 { get; set; }
            public double? Value2 { get; set; }
        }
        public event EventHandler RemoveGroupClick;
        private readonly GroupType Type;
        private readonly ISet<string> Stats;
        public SearchGroup(GroupType type, ISet<string> stats)
        {
            InitializeComponent();
            Type = type;
            Stats = stats;
            AddHeader(null, null);
            AddRow(null, null, null);
        }
        public SearchGroup(FilterCondition c, ISet<string> stats)
        {
            InitializeComponent();
            Stats = stats;
            if (c is WeightCondition w)
            {
                Type = GroupType.Weight;
                AddHeader(w.Min, w.Max);
                foreach (string s in w.Weights.Keys)
                {
                    AddRow(s, w.Weights[s], null);
                }
            }
            else if (c is AndCondition a)
            {
                Type = GroupType.And;
                AddHeader(null, null);
                foreach (FilterCondition sub in a.Subconditions)
                {
                    if (sub is ClampCondition cc)
                    {
                        AddRow(cc.Template, cc.Min, cc.Max);
                    }
                }
            }
            else if (c is NotCondition n)
            {
                Type = GroupType.Not;
                AddHeader(null, null);
                foreach (FilterCondition sub in n.Subconditions)
                {
                    if (sub is ClampCondition cc)
                    {
                        AddRow(cc.Template, cc.Min, cc.Max);
                    }
                }
            }
            else
            {
                Type = GroupType.Count;
                CountCondition cnt = (CountCondition)c;
                AddHeader(cnt.Min, cnt.Max);
                foreach (FilterCondition sub in cnt.Subconditions)
                {
                    if (sub is ClampCondition cc)
                    {
                        AddRow(cc.Template, cc.Min, cc.Max);
                    }
                }
            }
        }
        private void AddHeader(double? v1, double? v2)
        {
            int fields = Type == GroupType.Count || Type == GroupType.Weight ? 2 : 0;
            Grid g = new Grid();
            g.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
            g.ColumnDefinitions.Add(new ColumnDefinition());
            Button b = new Button() { Content = "X", Width = 20, Height = 20 };
            b.Click += RemoveGroup_Click;
            Grid.SetColumn(b, 0);
            g.Children.Add(b);
            TextBlock label = new TextBlock() { Text = Type.ToString(), HorizontalAlignment = HorizontalAlignment.Stretch };
            Grid.SetColumn(label, 1);
            g.Children.Add(label);
            for (int i = 0; i < fields; i++)
            {
                g.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
                NumberBox n = new NumberBox() { Min = int.MinValue, Max = int.MaxValue, AllowDouble = (Type != GroupType.Count), Width = 60, TextAlignment = TextAlignment.Center };
                if (i == 0 && v1 != null)
                    n.Text = v1.ToString();
                if (i == 1 && v2 != null)
                    n.Text = v2.ToString();
                Grid.SetColumn(n, g.ColumnDefinitions.Count - 1);
                g.Children.Add(n);
            }
            ContentPanel.Children.Add(g);
        }
        public FilterCondition GetFilterCondition()
        {
            IList<SearchLine> lines = ParseLines();
            if (Type == GroupType.Weight)
            {
                WeightCondition c = new WeightCondition() { Min = lines[0].Value1, Max = lines[0].Value2, Weights = new Dictionary<string, double>() };
                for (int i=1; i < lines.Count; i++)
                {
                    if (lines[i].Label != null && lines[i].Value1 != null && !c.Weights.ContainsKey(lines[i].Label))
                        c.Weights.Add(lines[i].Label, lines[i].Value1.Value);
                }
                return c.Weights.Count > 0 ? c : null;
            }
            else
            {
                IList<FilterCondition> subconditions = new List<FilterCondition>();
                for (int i = 1; i < lines.Count; i++)
                {
                    if (lines[i].Label != null)
                    {
                        subconditions.Add(new ClampCondition() { Template = lines[i].Label, Min = lines[i].Value1, Max = lines[i].Value2 });
                    }
                }
                if (subconditions.Count == 0)
                {
                    return null;
                }
                else if (Type == GroupType.And)
                {
                    return new AndCondition() { Subconditions = subconditions };
                }
                else if (Type == GroupType.Not)
                {
                    return new NotCondition() { Subconditions = subconditions };
                }
                else
                {
                    return new CountCondition() { Min = (int?)lines[0].Value1, Max = (int?)lines[0].Value2, Subconditions = subconditions };
                }
            }
        }
        private IList<SearchLine> ParseLines()
        {
            IList<SearchLine> lines = new List<SearchLine>();
            foreach (UIElement e in ContentPanel.Children)
            {
                SearchLine line = new SearchLine();
                Grid g = (Grid)e;
                UIElement label = g.Children[1];
                if (label is SearchableComboBox && ((SearchableComboBox)label).SelectedItem != null)
                    line.Label = ((SearchableComboBox)label).SelectedItem.ToString();
                if (g.Children.Count > 2)
                {
                    NumberBox n1 = g.Children[2] as NumberBox;
                    if (n1.Valid)
                        line.Value1 = double.Parse(n1.Text);
                    if (g.Children.Count > 3)
                    {
                        NumberBox n2 = g.Children[3] as NumberBox;
                        if (n2.Valid)
                            line.Value2 = double.Parse(n2.Text);
                    }
                }
                lines.Add(line);
            }
            return lines;
        }
        private void AddRow_Click(object sender, RoutedEventArgs e)
        {
            AddRow(null, null, null);
        }
        private void AddRow(string l, double? v1, double? v2)
        {
            int fields = Type == GroupType.Weight ? 1 : 2;
            Grid g = new Grid();
            g.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
            g.ColumnDefinitions.Add(new ColumnDefinition());
            Button b = new Button() { Content = "X", Width = 20, Height = 20 };
            b.Click += RemoveRow_Click;
            Grid.SetColumn(b, 0);
            g.Children.Add(b);
            SearchableComboBox label = new SearchableComboBox() { ItemsSource = new List<string>(Stats), IsEditable = true, IsTextSearchEnabled = false };
            if (Stats.Contains(l))
                label.SelectedItem = l;
            label.ItemsPanel = new ItemsPanelTemplate(new FrameworkElementFactory(typeof(VirtualizingStackPanel)));
            Grid.SetColumn(label, 1);
            g.Children.Add(label);
            for (int i = 0; i < fields; i++)
            {
                g.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
                NumberBox n = new NumberBox() { Min = int.MinValue, Max = int.MaxValue, AllowDouble = true, Width = 60, TextAlignment = TextAlignment.Center };
                if (i == 0 && v1 != null)
                    n.Text = v1.ToString();
                if (i == 1 && v2 != null)
                    n.Text = v2.ToString();
                Grid.SetColumn(n, g.ColumnDefinitions.Count - 1);
                g.Children.Add(n);
            }
            ContentPanel.Children.Add(g);
        }
        private void RemoveRow_Click(object sender, RoutedEventArgs e)
        {
            if (((Button)sender).Parent is UIElement row && ContentPanel.Children.Contains(row))
                ContentPanel.Children.Remove(row);
        }
        private void RemoveGroup_Click(object sender, RoutedEventArgs e)
        {
            RemoveGroupClick?.Invoke(this, e);
        }
    }
}
