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
    /// Interaction logic for BulkItemsView.xaml
    /// </summary>
    public partial class BulkItemsView : UserControl
    {
        private int DisplayIndex = 0;
        private IList<ItemCraft> _items;
        public static int ResultsPerPage { get; set; } = Properties.Settings.Default.ResultsPerPage;
        public event EventHandler ItemClick;
        public IList<ItemCraft> Items 
        { 
            get { return _items; }
            set 
            { 
                _items = value;
                DisplayIndex = 0;
                UpdateDisplay();
            }
        }
        public BulkItemsView()
        {
            InitializeComponent();
        }
        public void UpdateDisplay()
        {
            if (Items != null && Items.Count > 0)
            {
                ContentBox.Children.Clear();
                int max = Math.Min(Items.Count, DisplayIndex + ResultsPerPage);
                PageHeader.Text = "Showing " + (DisplayIndex + 1) + "-" + max + " of " + Items.Count + " results";
                for (int k = DisplayIndex; k < max; k++)
                {
                    ItemView panel = new ItemView();
                    panel.UpdateData(Items[k]);
                    panel.ItemClick += ChildItem_Click;
                    ContentBox.Children.Add(panel);
                }
            }
            else
            {
                ContentBox.Children.Clear();
                PageHeader.Text = "Showing 0-0 of 0 results";
            }
        }
        private void ChildItem_Click(object sender, EventArgs e)
        {
            ItemClick?.Invoke(sender, e);
        }
        private void NextPage_Click(object sender, RoutedEventArgs e)
        {
            if (Items.Count > DisplayIndex + ResultsPerPage)
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
                if (i >= _items.Count)
                    break;
                s += _items[i].GetClipboardString() + "\n";
            }
            Clipboard.SetText(s);
        }
        private void ClipboardAll_Click(object sender, EventArgs e)
        {
            string s = "";
            foreach (ItemCraft item in _items)
            {
                s += item.GetClipboardString() + "\n";
            }
            Clipboard.SetText(s);
        }
    }
}
