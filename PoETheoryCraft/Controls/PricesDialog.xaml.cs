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

namespace PoETheoryCraft.Controls
{
    /// <summary>
    /// Interaction logic for PricesDialog.xaml
    /// </summary>
    public partial class PricesDialog : Window
    {
        public PricesDialog()
        {
            InitializeComponent();
        }
        private void GetPrices(object sender, EventArgs e)
        {
            string s = null;
            using (WebClient w = new WebClient())
            {
                try
                {
                    s = w.DownloadString("https://poe.ninja/api/data/ItemOverview?league=Metamorph&type=Fossil");
                }
                catch (WebException)
                {
                    DataBlock.Text = "web exception";
                }
            }
            if (s != null)
            {
                PoENinjaData dat = JsonSerializer.Deserialize<PoENinjaData>(s);
                Dictionary<string, double> pricedict = new Dictionary<string, double>();
                foreach (PoENinjaCurrency c in dat.lines)
                {
                    if (c.name != null && !pricedict.ContainsKey(c.name))
                        pricedict.Add(c.name, c.chaosValue);
                }
                DataBlock.Text = "";
                foreach (string n in pricedict.Keys)
                {
                    DataBlock.Text += n + ": " + pricedict[n] + "\n";
                }
                try
                {
                    string filestring = JsonSerializer.Serialize<Dictionary<string, double>>(pricedict, new JsonSerializerOptions() { WriteIndented = true });
                    File.WriteAllText("pricedata.json", filestring);
                    Title = "Prices loaded and saved to pricedata.json";
                }
                catch (Exception)
                {
                   
                }
                
            }
            else
            {
                DataBlock.Text = "Error retrieving prices from poe.ninja";
            }
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
    }
}
