using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PoETheoryCraft.Controls
{
    public class NumberBox : TextBox
    {
        public double Min { get; set; }
        public double Max { get; set; }
        public bool Valid { get; private set; }
        public bool AllowDouble { get; set; } = false;
        public NumberBox() : base()
        {
            this.TextChanged += CheckText;
        }
        private void CheckText(object sender, RoutedEventArgs e)
        {
            bool valid;
            double n;
            if (AllowDouble)
            {
                valid = double.TryParse(Text, out n);
            }
            else
            {
                valid = int.TryParse(Text, out int m);
                n = m;
            }
            if (valid && n >= Min && n < Max)
            {
                Foreground = Brushes.Black;
                Valid = true;
            }
            else
            {
                Foreground = Brushes.Red;
                Valid = false;
            }
        }
    }
}
