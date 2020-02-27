using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace PoETheoryCraft.Utils
{
    public static class Icons
    {
        public static BitmapImage Lock;
        public static BitmapImage Unlock;
        public static BitmapImage Settings;
        public static BitmapImage Plus;
        public static BitmapImage Search;
        public static void LoadImages()
        {
            LoadImage(ref Lock, "Icons/lock.png");
            LoadImage(ref Unlock, "Icons/unlock.png");
            LoadImage(ref Settings, "Icons/settings.png");
            LoadImage(ref Plus, "Icons/plus.png");
            LoadImage(ref Search, "Icons/search.png");
        }
        private static void LoadImage(ref BitmapImage img, string path)
        {
            Uri imguri = new Uri(System.IO.Path.Combine(Environment.CurrentDirectory, path));
            try
            {
                img = new BitmapImage(imguri);
            }
            catch (Exception)
            {
                Debug.WriteLine("Failed to load image " + path);
                img = null;
            }
        }
    }
}
