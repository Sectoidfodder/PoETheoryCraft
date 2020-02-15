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
using System.Windows.Shapes;
using PoETheoryCraft.DataClasses;

namespace PoETheoryCraft.Controls
{
    /// <summary>
    /// Interaction logic for PostCraftDialog.xaml
    /// </summary>
    public partial class PostCraftDialog : Window
    {
        private readonly IDictionary<PoEModData, IDictionary<string, int>> BenchCrafts;
        public PostCraftDialog(IDictionary<PoEModData, IDictionary<string, int>> benchcrafts)
        {
            InitializeComponent();
            BenchCrafts = benchcrafts;
            AddRow();
        }
        private void AddRow()
        {
            DockPanel p = new DockPanel() { Margin = new Thickness(5, 0, 5, 5), HorizontalAlignment = HorizontalAlignment.Stretch, Background = Brushes.AliceBlue};
            Button b = new Button() { Content = "X", Width = 20 };
            b.Click += Remove_Click;
            DockPanel.SetDock(b, Dock.Left);
            p.Children.Add(b);
            SearchableComboBox c = new SearchableComboBox() { ItemsSource = new CollectionView(BenchCrafts.Keys), HorizontalAlignment = HorizontalAlignment.Stretch, IsEditable = true, StaysOpenOnEdit = true, IsTextSearchEnabled = false };
            p.Children.Add(c);
            ModList.Children.Add(p);
        }
        private void Add_Click(object sender, RoutedEventArgs e)
        {
            AddRow();
        }
        private void Remove_Click(object sender, RoutedEventArgs e)
        {
            UIElement s = (sender as Button).Parent as UIElement;
            ModList.Children.Remove(s);
        }
    }
}
