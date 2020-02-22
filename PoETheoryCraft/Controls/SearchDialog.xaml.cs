using System;
using System.Collections;
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
using System.Windows.Shapes;
using PoETheoryCraft.Utils;

namespace PoETheoryCraft.Controls
{
    /// <summary>
    /// Interaction logic for SearchDialog.xaml
    /// </summary>
    public partial class SearchDialog : Window
    {
        private readonly ISet<string> Stats;
        public SearchDialog(ISet<string> stats, FilterCondition filter)
        {
            InitializeComponent();
            Stats = stats;
            StatsView.ItemsSource = new List<string>(Stats);
            CollectionViewSource.GetDefaultView(StatsView.ItemsSource).Filter = FilterText;

            GroupTypeBox.ItemsSource = Enum.GetValues(typeof(SearchGroup.GroupType)).Cast<SearchGroup.GroupType>();
            GroupTypeBox.SelectedIndex = 0;

            if (filter is AndCondition)
            {
                foreach (FilterCondition c in ((AndCondition)filter).Subconditions)
                {
                    SearchGroup s = new SearchGroup(c, Stats);
                    s.RemoveGroupClick += RemoveGroup_Click;
                    GroupsPanel.Children.Add(s);
                }
            }
        }
        public FilterCondition GetFilterCondition()
        {
            IList<FilterCondition> subconditions = new List<FilterCondition>();
            foreach (UIElement e in GroupsPanel.Children)
            {
                subconditions.Add(((SearchGroup)e).GetFilterCondition());
            }
            if (subconditions.Count > 0)
                return new AndCondition() { Subconditions = subconditions };
            else
                return null;
        }
        private bool FilterText(object obj)
        {
            if (string.IsNullOrWhiteSpace(SearchBox.Text))
                return true;
            else
                return obj.ToString().IndexOf(SearchBox.Text, StringComparison.OrdinalIgnoreCase) >= 0;
        }
        private void Search_TextChanged(object sender, RoutedEventArgs e)
        {
            if (StatsView != null && StatsView.ItemsSource != null)
                CollectionViewSource.GetDefaultView(StatsView.ItemsSource).Refresh();
        }
        private void AddGroup_Click(object sender, RoutedEventArgs e)
        {
            if (GroupTypeBox.SelectedItem is SearchGroup.GroupType type)
            {
                SearchGroup s = new SearchGroup(type, Stats);
                s.RemoveGroupClick += RemoveGroup_Click;
                GroupsPanel.Children.Add(s);
            }
        }
        private void RemoveGroup_Click(object sender, EventArgs e)
        {
            if (sender is UIElement && GroupsPanel.Children.Contains(sender as UIElement))
                GroupsPanel.Children.Remove(sender as UIElement);
        }
        private void OK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            GroupsPanel.Children.Clear();
        }

    }
}
