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
using LiveCharts;
using LiveCharts.Defaults;

namespace PoETheoryCraft.Controls
{
    /// <summary>
    /// Interaction logic for StatGraphs.xaml
    /// </summary>
    public partial class StatGraphs : Window
    {
        public ChartValues<ObservablePoint> CDF { get; set; }
        public StatGraphs()
        {
            InitializeComponent();

            CDF = new ChartValues<ObservablePoint>();
            for (int i=5; i<15; i++)
            {
                CDF.Add(new ObservablePoint(i, 100 - Math.Pow(i - 5, 2)));
            }

            DataContext = this;
        }
        public void UpdateData(List<double> dat, string statname)
        {
            StatName.Title = statname.Replace("[property] ", "");
            //ChartValues<ObservablePoint> cdfpoints = new ChartValues<ObservablePoint>();
            ChartValues<ObservablePoint> cdfpoints = CDF;
            cdfpoints.Clear();
            if (dat.Count > 0)
            {
                double x = dat[0];
                double incr = (dat[dat.Count - 1] - x) / 100;
                cdfpoints.Add(new ObservablePoint(x, dat.Count));
                for (int i = 0; i< dat.Count; i++)
                {
                    double newx = dat[i];
                    if (newx <= x + incr)
                        continue;
                    cdfpoints.Add(new ObservablePoint(newx, dat.Count - i));
                    x = newx;
                }
            }
            //CDF = cdfpoints;
            //CollectionViewSource.GetDefaultView(CDF).Refresh();
        }
    }
}
