using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
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
    /// Interaction logic for PricesDialog.xaml
    /// </summary>
    public partial class PricesDialog : Window
    {
        public PricesDialog(IDictionary<string, double> prices)
        {
            InitializeComponent();
            UpdateData(prices);
        }
        private void LoadPrices(WebClient w, string url, Dictionary<string, double> dict, IList<string> filter = null)
        {
            string s = w.DownloadString(url);
            PoENinjaData dat = JsonSerializer.Deserialize<PoENinjaData>(s);
            foreach (PoENinjaCurrency c in dat.lines)
            {
                if (c.name != null && !dict.ContainsKey(c.name) && (filter == null || filter.Contains(c.name)))
                    dict.Add(c.name, c.chaosValue);
            }
        }
        private void UpdateData(IDictionary<string, double> dict)
        {
            if (dict == null)
                return;
            DataPanel.Children.Clear();
            foreach (string s in dict.Keys)
            {
                StackPanel p = new StackPanel() { Orientation = Orientation.Horizontal , Margin = new Thickness(2,1,2,1) };
                p.Children.Add(new TextBlock() { Text = s, Margin = new Thickness(0,0,5,0) });
                p.Children.Add(new NumberBox() { Width = 40, Text = dict[s].ToString(), Min = 0, AllowDouble = true });
                DataPanel.Children.Add(p);
            }
        }
        private void GetPrices(object sender, EventArgs e)
        {
            Dictionary<string, double> dict = new Dictionary<string, double>();
            using (WebClient w = new WebClient())
            {
                try
                {
                    LoadPrices(w, "https://poe.ninja/api/data/ItemOverview?league=Metamorph&type=Resonator", dict);
                    LoadPrices(w, "https://poe.ninja/api/data/ItemOverview?league=Metamorph&type=Fossil", dict);
                    LoadPrices(w, "https://poe.ninja/api/data/CurrencyOverview?league=Metamorph&type=Currency", dict, CraftingDatabase.CurrencyIndex);
                    LoadPrices(w, "https://poe.ninja/api/data/ItemOverview?league=Metamorph&type=Essence", dict);
                }
                catch (Exception err)
                {
                    MessageBox.Show("Error retrieving data: " + err.Message);
                    return;
                }
            }
            UpdateData(dict);
            //DataControl.ItemsSource = dict; 
        }
        private void OK_Click(object sender, EventArgs e)
        {
            Dictionary<string, double> dict = new Dictionary<string, double>();
            foreach (UIElement ele in DataPanel.Children)
            {
                UIElementCollection col = ((StackPanel)ele).Children;
                string s = ((TextBlock)col[0]).Text;
                NumberBox n = (NumberBox)col[1];
                if (!n.Valid)
                {
                    MessageBox.Show("One or more values are invalid");
                    return;
                }
                dict.Add(s, double.Parse(n.Text));
            }
            try
            {
                string filestring = JsonSerializer.Serialize<Dictionary<string, double>>(dict, new JsonSerializerOptions() { WriteIndented = true });
                File.WriteAllText("pricedata.json", filestring);
            }
            catch (Exception)
            {
                MessageBox.Show("Error saving to pricedata.json");
                return;
            }
            CraftingDatabase.PriceData = dict;
            DialogResult = true;
        }
    }
    public class PoENinjaData
    {
        public IList<PoENinjaCurrency> lines { get; set; }
    }
    public class PoENinjaCurrency
    {
        public string name { get; set; }
        public double chaosValue { get; set; }
        //currency data can have differently named fields, so just alias them
        public string currencyTypeName { get { return name; } set { name = value; } }
        public double chaosEquivalent { get { return chaosValue; } set { chaosValue = value; } }
    }
}
