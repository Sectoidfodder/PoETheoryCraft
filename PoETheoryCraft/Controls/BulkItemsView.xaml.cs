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
                    ContentBox.Children.Add(panel);
                }
            }
            else
            {
                ContentBox.Children.Clear();
                PageHeader.Text = "Showing 0-0 of 0 results";
            }
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
    }
}
