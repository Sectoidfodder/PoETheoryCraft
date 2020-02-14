using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace PoETheoryCraft.DataClasses
{
    public class PoECurrencyData
    {
        public string name { get; set; }
        public BitmapImage icon { get; set; }
        public string tooltip { get; set; }
        public string key { get; set; }
        public override string ToString()
        {
            return name;
        }
    }
}
