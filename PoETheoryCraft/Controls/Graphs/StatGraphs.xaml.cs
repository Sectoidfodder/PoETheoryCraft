using System.Collections.Generic;
using System.Linq;
using System.Windows;
using LiveCharts;
using LiveCharts.Configurations;
using PoETheoryCraft.DataClasses;
using PoETheoryCraft.Utils;

namespace PoETheoryCraft.Controls.Graphs
{
    /// <summary>
    /// Interaction logic for StatGraphs.xaml
    /// </summary>
    public class StatPoint
    {
        public double X { get; set; }
        public int Count { get; set; }
        public int Matches { get; set; }
        public int Total { get; set; }
    }
    public partial class StatGraphs : Window
    {
        public ChartValues<StatPoint> CDF { get; set; }
        public StatGraphs(List<double> dat, int total, string statname, IList<PoECurrencyData> currencies, FilterCondition filter)
        {
            InitializeComponent();

            Title = "";
            if (dat.Count == total)
                Title += total + " items";
            else
                Title += dat.Count + " matches out of " + total + " total items";
            string s = "Currency: ";
            foreach (PoECurrencyData d in currencies)
            {
                s += d.name + ", ";
            }
            CurrencyText.Text = s.Trim(new char[]{ ',',' '});
            if (filter == null)
            {
                FilterText.Text = "None";
            }
            else
            {
                IList<string> filterstrings = new List<string>();
                foreach (FilterCondition c in ((AndCondition)filter).Subconditions)
                {
                    filterstrings.Add(c.ToString());
                }
                FilterText.Text = string.Join("\n", filterstrings);
            }

            StatName.Title = statname.Replace("[property] ", "");
            CDF = new ChartValues<StatPoint>();
            if (dat.Count > 0)
            {
                double x = dat[0];
                double incr = (dat[dat.Count - 1] - x) / 100;
                CDF.Add(new StatPoint() { X = x, Count = dat.Count, Matches = dat.Count, Total = total });
                for (int i = 0; i < dat.Count; i++)
                {
                    double newx = dat[i];
                    if (newx <= x + incr)
                        continue;
                    CDF.Add(new StatPoint() { X = newx, Count = dat.Count - i, Matches = dat.Count, Total = total });
                    x = newx;
                }
            }

            CartesianMapper<StatPoint> statmapper = Mappers.Xy<StatPoint>();
            statmapper.X(value => value.X);
            statmapper.Y(value => value.Count);
            Charting.For<StatPoint>(statmapper);

            DataContext = this;
        }
    }
}
