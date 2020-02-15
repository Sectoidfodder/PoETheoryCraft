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

namespace PoETheoryCraft.Controls
{
    /// <summary>
    /// Interaction logic for SearchDialog.xaml
    /// </summary>
    public partial class SearchDialog : Window
    {
        public SearchDialog(ISet<string> stats)
        {
            InitializeComponent();
            StatsView.ItemsSource = stats;
            CollectionViewSource.GetDefaultView(stats).Filter = FilterText;
            CollectionViewSource.GetDefaultView(stats).SortDescriptions.Clear();
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
            CollectionViewSource.GetDefaultView(StatsView.ItemsSource).Refresh();
        }
        private void OK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
        private void Clear_Click(object sender, RoutedEventArgs e)
        {

        }

    }
}
