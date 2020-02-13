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
        public int Min { get; set; }
        public int Max { get; set; }
        public bool Valid { get; private set; }
        public NumberBox() : base()
        {
            this.TextChanged += CheckText;
        }
        private void CheckText(object sender, RoutedEventArgs e)
        {
            bool valid = int.TryParse(Text, out int n);
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
