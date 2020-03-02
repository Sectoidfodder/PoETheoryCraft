using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
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
using LiveCharts;
using LiveCharts.Wpf;

namespace PoETheoryCraft.Controls.Graphs
{
    /// <summary>
    /// Interaction logic for StatToolTip.xaml
    /// </summary>
    public partial class StatTooltip : IChartTooltip
    {
        public StatTooltip()
        {
            InitializeComponent();
            DataContext = this;
        }
        public event PropertyChangedEventHandler PropertyChanged;
        private TooltipData _data;
        public TooltipData Data
        {
            get { return _data; }
            set
            {
                _data = value;
                OnPropertyChanged("Data");
            }
        }
        public TooltipSelectionMode? SelectionMode { get; set; }
        protected virtual void OnPropertyChanged(string propertyname = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyname));
        }
    }
    public class MatchTooltipConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is StatPoint s))
                return "???";
            double p = (double)s.Count * 100 / s.Matches;
            double inv = 100 / p;
            return p.ToString("N2") + "% of matches (1/" + (inv > 10 ? inv.ToString("N0") : inv.ToString("N1")) + ")";
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class TotalTooltipConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is StatPoint s))
                return "???";
            double p = (double)s.Count * 100 / s.Total;
            double inv = 100 / p;
            return p.ToString("N2") + "% of all rolls (1/" + (inv > 10 ? inv.ToString("N0") : inv.ToString("N1")) + ")";
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class CostTooltipConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is StatPoint s))
                return "???";
            double p = (double)s.Count * 100 / s.Total;
            double inv = 100 / p;
            return "Avg cost: " + (inv * s.Cost).ToString("N1") + "c";
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
