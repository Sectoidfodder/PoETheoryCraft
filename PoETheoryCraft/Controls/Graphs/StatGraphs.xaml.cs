using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using LiveCharts;
using LiveCharts.Configurations;
using LiveCharts.Wpf;
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
        public double Cost { get; set; }
    }
    public partial class StatGraphs : Window
    {
        private readonly CartesianMapper<StatPoint> PercentMapper;
        private readonly CartesianMapper<StatPoint> CostMapper;
        private double Min, Max, Incr;
        public StatGraphs(string statname, FilterCondition filter)
        {
            InitializeComponent();

            Title = "Plot: " + statname.Replace("[property] ", "");
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
            PercentChart.AxisX[0].Title = statname.Replace("[property] ", "");
            CostChart.AxisX[0].Title = statname.Replace("[property] ", "");

            PercentMapper = Mappers.Xy<StatPoint>();
            PercentMapper.X(value => value.X);
            PercentMapper.Y(value => (double)value.Count / value.Total);
            CostMapper = Mappers.Xy<StatPoint>();
            CostMapper.X(value => value.X);
            CostMapper.Y(value => Math.Log(value.Total * value.Cost / value.Count, 10));
            Min = double.NaN;
            Max = double.NaN;
            Incr = double.NaN;

        }
        public void AddSeries(List<double> dat, int total, string currencies, double cost)
        {
            currencies += ": " + cost.ToString("N1") + "c";
            if (double.IsNaN(Min))
            {
                Min = dat[0];
                Max = dat[dat.Count - 1];
                Incr = Math.Max((Max - Min) / 100, 0.01);
                Incr = CleanIncrement(Incr);
                Min = CleanMin(Min, Incr);
                Max = CleanMax(Max, Incr);
            }
            else
            {
                Min = Math.Min(Min, dat[0]);
                Max = Math.Max(Max, dat[dat.Count - 1]);
                Min = CleanMin(Min, Incr);
                Max = CleanMax(Max, Incr);
            }
            ChartValues<StatPoint> p = new ChartValues<StatPoint>();
            int i = 0;
            for (double x = Min; x <= Max; x += Incr)
            {
                if (x + Incr <= dat[0])
                    continue;
                while (i < dat.Count && dat[i] < x)
                {
                    i++;
                }
                if (i >= dat.Count)
                    break;
                p.Add(new StatPoint() { X = x, Count = dat.Count - i, Total = total, Matches = dat.Count, Cost = cost });
            }
            PercentChart.Series.Add(new LineSeries(PercentMapper) { LineSmoothness = 0, Fill = Brushes.Transparent, Values = p, Title = currencies });
            CostChart.Series.Add(new LineSeries(CostMapper) { LineSmoothness = 0, Fill = Brushes.Transparent, Values = p, Title = currencies });
            GraphTabs.Height += 20;
        }
        //rounds increment to 1, 2, or 5 * 10^k
        public static double CleanIncrement(double incr)
        {
            int power = 0;
            while (incr <= 1)
            {
                incr *= 10;
                power++;
            }
            while (incr > 10)
            {
                incr /= 10;
                power--;
            }
            if (incr > 5)
                return 10 / Math.Pow(10, power);
            else if (incr > 2)
                return 5 / Math.Pow(10, power);
            else
                return 2 / Math.Pow(10, power);
        }
        //rounds min down to nearest incr
        public static double CleanMin(double min, double incr)
        {
            return Math.Floor(min / incr) * incr;
        }
        //rounds max up to nearest incr
        public static double CleanMax(double max, double incr)
        {
            return Math.Ceiling(max / incr) * incr;
        }
        public string ChaosFormat(double v)
        {
            return Math.Pow(10, v).ToString("N0") + "c";
        }
        public string PercentageFormat(double v)
        {
            return v.ToString("P");
        }
    }
}
