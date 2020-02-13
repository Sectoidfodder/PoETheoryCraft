using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using PoETheoryCraft.DataClasses;

namespace PoETheoryCraft.Controls
{
    /// <summary>
    /// Interaction logic for BaseItemPicker.xaml
    /// </summary>
    public partial class BaseItemDialog : Window
    {
        public PoEBaseItemData SelectedBase { get; set; }
        public BaseItemDialog(IDictionary<string, PoEBaseItemData> dict)
        {
            InitializeComponent();
            OKButton.IsEnabled = false;
            System.ComponentModel.ICollectionView view = CollectionViewSource.GetDefaultView(dict);
            if (view.GroupDescriptions.Count == 0)
                view.GroupDescriptions.Add(new PropertyGroupDescription("Value.item_class"));
            view.Filter = NameFilter;
            ItemNameView.ItemsSource = dict;
        }
        private void OK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
        private void ItemNameView_Select(object sender, RoutedEventArgs e)
        {
            KeyValuePair<string, PoEBaseItemData>? kv = (sender as ListView).SelectedItem as KeyValuePair<string, PoEBaseItemData>?;
            PoEBaseItemData d = kv?.Value;
            if (d != null)
            {
                SelectedBase = d;
                OKButton.IsEnabled = true;
                ItemInfoBox.Text = JsonSerializer.Serialize(d, new JsonSerializerOptions() { WriteIndented = true });
            }
            else
            {
                OKButton.IsEnabled = false;
            }
        }
        private void ItemFilter_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            CollectionViewSource.GetDefaultView(ItemNameView.ItemsSource).Refresh();
        }
        private bool NameFilter(object item)
        {
            if (string.IsNullOrEmpty(ItemFilter.Text))
                return true;
            else
            {
                KeyValuePair<string, PoEBaseItemData>? kv = item as KeyValuePair<string, PoEBaseItemData>?;
                PoEBaseItemData d = kv?.Value;
                if (d == null)
                    return false;
                return d.name.IndexOf(ItemFilter.Text, StringComparison.OrdinalIgnoreCase) >= 0;
            }
        }
    }
}
