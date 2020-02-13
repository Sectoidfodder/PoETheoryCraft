using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace PoETheoryCraft
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public void Unhandled_Exception(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show(e.Exception.InnerException.Message, e.Exception.InnerException.GetType().ToString(), MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
        }
    }
}
