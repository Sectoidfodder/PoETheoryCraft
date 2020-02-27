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
    /// Interaction logic for BulkItemsView.xaml
    /// </summary>
    public partial class BulkItemsView : UserControl
    {
        private int DisplayIndex = 0;
        public static int ResultsPerPage { get; set; } = Properties.Settings.Default.ResultsPerPage;
        private bool _sortasc = true;
        private string _sortby;
        public string SortBy
        {
            get { return _sortby; }
            set
            {
                if (value != _sortby)
                {
                    _sortby = value;
                    _sortasc = true;
                }
                else
                {
                    _sortasc = !_sortasc;
                }
                DisplayIndex = 0;
                SortAndFilter();
                UpdateDisplay();
            }
        }
        private FilterCondition _filter;
        public FilterCondition Filter
        {
            get { return _filter; }
            set
            {
                if (_filter != value)
                {
                    _filter = value;
                    DisplayIndex = 0;
                    SortAndFilter();
                    UpdateDisplay();
                }
            }
        }
        public event EventHandler ItemClick;
        public event EventHandler BenchMoveClick;
        private IList<ItemCraft> _items;
        public IList<ItemCraft> Items 
        { 
            get { return _items; }
            set 
            { 
                _items = value;
                _sortby = null;
                DisplayIndex = 0;
                SortAndFilter();
                UpdateDisplay();
            }
        }
        private IList<ItemCraft> FilteredItems;
        public BulkItemsView()
        {
            InitializeComponent();
            SortIndicator.Text = "Click on any stat to sort";
        }
        public void SortAndFilter()
        {
            FilteredItems = Items;
            if (FilteredItems == null)
                return;
            if (Filter != null)
            {
                FilteredItems = new List<ItemCraft>();
                foreach (ItemCraft item in Items)
                {
                    FilterResult res = FilterEvaluator.Evaluate(item, Filter);
                    item.TempProps = res.Info;
                    if (res.Match)
                        FilteredItems.Add(item);
                }
            }
            if (SortBy != null)
            {
                ((List<ItemCraft>)FilteredItems).Sort(new ItemCraftComparer() { Key = SortBy });
                if (_sortasc)
                    ((List<ItemCraft>)FilteredItems).Reverse();
            }
        }
        public void UpdateDisplay()
        {
            ContentBox.Children.Clear();
            if (Items != null && Items.Count > 0)
            {
                if (FilteredItems.Count > 0)
                {
                    int max = Math.Min(FilteredItems.Count, DisplayIndex + ResultsPerPage);
                    if (Filter != null)
                        PageHeader.Text = (DisplayIndex + 1) + "-" + max + " of " + FilteredItems.Count + " matches in " + Items.Count + " results";
                    else
                        PageHeader.Text = (DisplayIndex + 1) + "-" + max + " of " + FilteredItems.Count + " results";
                    for (int k = DisplayIndex; k < max; k++)
                    {
                        ItemView panel = new ItemView();
                        panel.UpdateData(FilteredItems[k]);
                        panel.ItemClick += ChildItem_Click;
                        MenuItem menumove = new MenuItem() { Header = "Move to Bench", Tag = panel.SourceItem };
                        menumove.Click += BenchMove_Click;
                        panel.ContextMenu.Items.Add(menumove);
                        ContentBox.Children.Add(panel);
                    }
                }
                else
                {
                    PageHeader.Text = "0-0 of 0 matches in " + Items.Count + " results";
                }
            }
            else
            {
                PageHeader.Text = "0-0 of 0 results";
            }
            if (SortBy != null)
                SortIndicator.Text = "Sorting by " + (_sortasc ? ">" : "<") + " : " + SortBy.Replace("[property] ", "") + " (click again to reverse)";
            else
                SortIndicator.Text = "Click on any stat to sort";
        }
        private void BenchMove_Click(object sender, EventArgs e)
        {
            BenchMoveClick?.Invoke(sender, e);
        }
        private void ChildItem_Click(object sender, EventArgs e)
        {
            ItemClick?.Invoke(sender, e);
        }
        private void NextPage_Click(object sender, RoutedEventArgs e)
        {
            if (FilteredItems.Count > DisplayIndex + ResultsPerPage)
            {
                DisplayIndex += ResultsPerPage;
                UpdateDisplay();
            }
        }
        private void PrevPage_Click(object sender, RoutedEventArgs e)
        {
            if (DisplayIndex > 0)
            {
                DisplayIndex = Math.Max(0, DisplayIndex - ResultsPerPage);
                UpdateDisplay();
            }
        }
        private void ClipboardPage_Click(object sender, EventArgs e)
        {
            string s = "";
            for (int i=DisplayIndex; i<DisplayIndex + ResultsPerPage; i++)
            {
                if (i >= FilteredItems.Count)
                    break;
                s += FilteredItems[i].GetClipboardString() + "\n";
            }
            Clipboard.SetText(s);
        }
        private void ClipboardAll_Click(object sender, EventArgs e)
        {
            string s = "";
            foreach (ItemCraft item in FilteredItems)
            {
                s += item.GetClipboardString() + "\n";
            }
            Clipboard.SetText(s);
        }
    }
}
