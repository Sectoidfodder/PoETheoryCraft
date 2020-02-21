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
        public struct SearchLine
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

            int fields = Type == GroupType.Count || Type == GroupType.Weight ? 2 : 0;
            Grid g = new Grid();
            g.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
            g.ColumnDefinitions.Add(new ColumnDefinition());
            Button b = new Button() { Content = "X", Width = 20 };
            b.Click += RemoveGroup_Click;
            Grid.SetColumn(b, 0);
            g.Children.Add(b);
            TextBlock label = new TextBlock() { Text = Type.ToString(), HorizontalAlignment = HorizontalAlignment.Stretch };
            Grid.SetColumn(label, 1);
            g.Children.Add(label);
            for (int i = 0; i < fields; i++)
            {
                g.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
                NumberBox n = new NumberBox() { Min = int.MinValue, Max = int.MaxValue, AllowDouble = true, Width = 60, TextAlignment = TextAlignment.Center };
                Grid.SetColumn(n, g.ColumnDefinitions.Count - 1);
                g.Children.Add(n);
            }
            ContentPanel.Children.Add(g);
            AddRow_Click(null, null);
        }
        private void AddRow_Click(object sender, RoutedEventArgs e)
        {
            int fields = Type == GroupType.Count ? 0 : Type == GroupType.Weight ? 1 : 2;
            Grid g = new Grid();
            g.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
            g.ColumnDefinitions.Add(new ColumnDefinition());
            Button b = new Button() { Content = "X" , Width = 20 };
            b.Click += RemoveRow_Click;
            Grid.SetColumn(b, 0);
            g.Children.Add(b);
            ComboBox label = new ComboBox() { ItemsSource = Stats, IsEditable = true, StaysOpenOnEdit = true };
            label.ItemsPanel = new ItemsPanelTemplate(new FrameworkElementFactory(typeof(VirtualizingStackPanel)));
            Grid.SetColumn(label, 1);
            g.Children.Add(label);
            for (int i=0; i < fields; i++)
            {
                g.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
                NumberBox n = new NumberBox() { Min = int.MinValue, Max = int.MaxValue, AllowDouble = true, Width = 60, TextAlignment = TextAlignment.Center };
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
