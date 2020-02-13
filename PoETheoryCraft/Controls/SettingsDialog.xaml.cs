using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;
using PoETheoryCraft.Utils;

namespace PoETheoryCraft.Controls
{
    /// <summary>
    /// Interaction logic for SettingsDialog.xaml
    /// </summary>
    public partial class SettingsDialog : Window
    {
        public SettingsDialog()
        {
            InitializeComponent();
            Rare4.Text = ModLogic.ModCountWeights[0].ToString();
            Rare5.Text = ModLogic.ModCountWeights[1].ToString();
            Rare6.Text = ModLogic.ModCountWeights[2].ToString();
            RareJ3.Text = ModLogic.JewelModCountWeights[0].ToString();
            RareJ4.Text = ModLogic.JewelModCountWeights[1].ToString();
            Magic1.Text = ModLogic.MagicModCountWeights[0].ToString();
            Magic2.Text = ModLogic.MagicModCountWeights[1].ToString();
            Quality.Text = ItemCraft.DefaultQuality.ToString();
            PerPage.Text = MainWindow.ResultsPerPage.ToString();
            Rare4.TextChanged += Validate_Fields;
            Rare5.TextChanged += Validate_Fields;
            Rare6.TextChanged += Validate_Fields;
            RareJ3.TextChanged += Validate_Fields;
            RareJ4.TextChanged += Validate_Fields;
            Magic1.TextChanged += Validate_Fields;
            Magic2.TextChanged += Validate_Fields;
            Quality.TextChanged += Validate_Fields;
            PerPage.TextChanged += Validate_Fields;
        }
        private void OK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
        private void Default_Click(object sender, RoutedEventArgs e)
        {
            Rare4.Text = 8 + "";
            Rare5.Text = 3 + "";
            Rare6.Text = 1 + "";
            RareJ3.Text = 13 + "";
            RareJ4.Text = 7 + "";
            Magic1.Text = 1 + "";
            Magic2.Text = 1 + "";
        }
        private void Validate_Fields(object sender, RoutedEventArgs e)
        {
            if (Rare4.Valid && Rare5.Valid && Rare6.Valid && RareJ3.Valid && RareJ4.Valid && Magic1.Valid && Magic2.Valid && Quality.Valid && PerPage.Valid)
                OKButton.IsEnabled = true;
            else
                OKButton.IsEnabled = false;
        }
    }
}
