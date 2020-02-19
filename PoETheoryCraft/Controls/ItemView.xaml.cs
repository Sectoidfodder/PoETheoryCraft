using System;
using System.Collections.Generic;
//using System.ComponentModel;
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
using PoETheoryCraft.Utils;
using PoETheoryCraft.DataClasses;

namespace PoETheoryCraft.Controls
{
    /// <summary>
    /// Interaction logic for ItemView.xaml
    /// </summary>
    public partial class ItemView : UserControl
    {
        private UIElement MouseTarget;
        public event EventHandler ItemClick;
        public ItemCraft SourceItem { get; private set; }
        public ItemView()
        {
            InitializeComponent();
        }
        public void UpdateData(ItemCraft item)
        {
            SourceItem = item;
            PoEBaseItemData itemtemplate = CraftingDatabase.AllBaseItems[item.SourceData];
            if (item != null)
            {
                ItemNameBox.ToolTip = "tags: " + string.Join(", ", item.LiveTags);
                ItemNameBox.Foreground = new SolidColorBrush(EnumConverter.RarityToColor(item.Rarity));
                ItemNameBox.Text = item.ItemName;
                ItemDataBox.Text = "ilvl: " + item.ItemLevel + " ";
                if (item.LiveTags.Contains(itemtemplate.item_class_properties[EnumConverter.InfToTag(ItemInfluence.Shaper)]))
                    ItemDataBox.Text += "  Shaper";
                if (item.LiveTags.Contains(itemtemplate.item_class_properties[EnumConverter.InfToTag(ItemInfluence.Elder)]))
                    ItemDataBox.Text += "  Elder";
                if (item.LiveTags.Contains(itemtemplate.item_class_properties[EnumConverter.InfToTag(ItemInfluence.Redeemer)]))
                    ItemDataBox.Text += "  Redeemer";
                if (item.LiveTags.Contains(itemtemplate.item_class_properties[EnumConverter.InfToTag(ItemInfluence.Hunter)]))
                    ItemDataBox.Text += "  Hunter";
                if (item.LiveTags.Contains(itemtemplate.item_class_properties[EnumConverter.InfToTag(ItemInfluence.Warlord)]))
                    ItemDataBox.Text += "  Warlord";
                if (item.LiveTags.Contains(itemtemplate.item_class_properties[EnumConverter.InfToTag(ItemInfluence.Crusader)]))
                    ItemDataBox.Text += "  Crusader";
                ImplicitBox.Children.Clear();
                foreach (ModCraft m in item.LiveImplicits)
                {
                    TextBlock tb = new TextBlock() { TextWrapping = TextWrapping.Wrap, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(184, 218, 242)) };
                    tb.Text = m.ToString();
                    tb.ToolTip = CraftingDatabase.AllMods[m.SourceData].ToString();
                    HookEvents(tb);
                    ImplicitBox.Children.Add(tb);
                }
                ItemModBox.Children.Clear();
                foreach (ModCraft m in item.LiveMods)
                {
                    PoEModData modtemplate = CraftingDatabase.AllMods[m.SourceData];
                    DockPanel dock = new DockPanel();

                    string header = "";
                    if (modtemplate.generation_type == ModLogic.Prefix)
                        header = "[P] ";
                    else if (modtemplate.generation_type == ModLogic.Suffix)
                        header = "[S] ";
                    TextBlock affix = new TextBlock() { FontWeight = FontWeights.Bold, Foreground = Brushes.DarkGray, Text = header };
                    DockPanel.SetDock(affix, Dock.Right);
                    dock.Children.Add(affix);

                    StackPanel sp = new StackPanel();
                    IList<string> statlines= m.ToString().Split('\n');
                    foreach (string s in statlines)
                    {
                        TextBlock tb = new TextBlock() { TextWrapping = TextWrapping.Wrap, FontWeight = FontWeights.Bold, Text = s, ToolTip = modtemplate.name + ": " + modtemplate };
                        if (modtemplate.domain == "crafted")
                            tb.Foreground = new SolidColorBrush(Color.FromRgb(184, 218, 242));
                        else
                            tb.Foreground = new SolidColorBrush(Color.FromRgb(136, 136, 255));
                        HookEvents(tb);
                        sp.Children.Add(tb);
                    }
                    dock.Children.Add(sp);
                    ItemModBox.Children.Add(dock);
                }
                FillPropertyBox(item);
            }
            else {
                ItemNameBox.Foreground = Brushes.White;
                ItemNameBox.Text = "";
                ItemDataBox.Text = "";
                PropertyBox.Children.Clear();
                ImplicitBox.Children.Clear();
                ItemModBox.Children.Clear();
            }
        }
        private void HookEvents(TextBlock tb)
        {
            tb.MouseEnter += Mouse_HighLight;
            tb.MouseLeave += Mouse_Revert;
            tb.MouseDown += Mouse_Down;
            tb.MouseUp += Mouse_Up;
        }
        private void Mouse_Revert(object sender, MouseEventArgs e)
        {
            ((TextBlock)sender).Background = Brushes.Black;
        }
        private void Mouse_HighLight(object sender, MouseEventArgs e)
        {
            ((TextBlock)sender).Background = Brushes.Gray;
        }
        private void Mouse_Down(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                MouseTarget = (UIElement)sender;
        }
        private void Mouse_Up(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && MouseTarget == sender)
            {
                ItemClick?.Invoke(sender, EventArgs.Empty);
            }
        }
        private void Clipboard_Copy(object sender, EventArgs e)
        {
            if (SourceItem != null)
                Clipboard.SetText(SourceItem.GetClipboardString());
        }
        private void FillPropertyBox(ItemCraft item)
        {
            PropertyBox.Children.Clear();
            PseudoPropBox.Children.Clear();
            SolidColorBrush white = new SolidColorBrush(Colors.White);
            SolidColorBrush blue = new SolidColorBrush(Color.FromRgb(136, 136, 255));
            SolidColorBrush gray = new SolidColorBrush(Colors.DarkGray);
            ItemProperties p = ItemParser.ParseProperties(item);
            ItemProperties sp = CraftingDatabase.AllBaseItems[item.SourceData].properties;
            StackPanel panel;
            if (p.quality > 0)
            {
                panel = new StackPanel() { Orientation = Orientation.Horizontal };
                string t;
                switch (item.QualityType)
                {
                    case "Prismatic Catalyst":
                        t = "Quality (Resistance Modifiers): ";
                        break;
                    case "Fertile Catalyst":
                        t = "Quality (Life and Mana Modifiers): ";
                        break;
                    case "Intrinsic Catalyst":
                        t = "Quality (Attribute Modifiers): ";
                        break;
                    case "Tempering Catalyst":
                        t = "Quality (Defence Modifiers): ";
                        break;
                    case "Abrasive Catalyst":
                        t = "Quality (Attack Modifiers): ";
                        break;
                    case "Imbued Catalyst":
                        t = "Quality (Caster Modifiers): ";
                        break;
                    case "Turbulent Catalyst":
                        t = "Quality (Elemental Damage): ";
                        break;
                    default:
                        t = "Quality: ";
                        break;
                }
                panel.Children.Add(new TextBlock() { Text = t , FontWeight = FontWeights.Bold, Foreground = gray });
                TextBlock tb = new TextBlock() { Text = "+" + p.quality + "%", FontWeight = FontWeights.Bold, Foreground = blue, Tag = "Quality" };
                HookEvents(tb);
                panel.Children.Add(tb);
                PropertyBox.Children.Add(panel);
            }
            if (p.block > 0)
            {                
                panel = new StackPanel() { Orientation = Orientation.Horizontal };
                panel.Children.Add(new TextBlock() { Text = "Chance to Block: ", FontWeight = FontWeights.Bold, Foreground = gray });
                TextBlock tb = new TextBlock() { Text = p.block.ToString(), FontWeight = FontWeights.Bold, Foreground = (p.block == sp.block) ? white : blue, Tag = "Block" };
                HookEvents(tb);
                panel.Children.Add(tb);
                PropertyBox.Children.Add(panel);
            }
            if (p.armour > 0)
            {
                panel = new StackPanel() { Orientation = Orientation.Horizontal };
                panel.Children.Add(new TextBlock() { Text = "Armour: ", FontWeight = FontWeights.Bold, Foreground = gray });
                TextBlock tb = new TextBlock() { Text = p.armour.ToString(), FontWeight = FontWeights.Bold, Foreground = (p.armour == sp.armour) ? white : blue, Tag = "Armour" };
                HookEvents(tb);
                panel.Children.Add(tb);
                PropertyBox.Children.Add(panel);
            }
            if (p.evasion > 0)
            {
                panel = new StackPanel() { Orientation = Orientation.Horizontal };
                panel.Children.Add(new TextBlock() { Text = "Evasion: ", FontWeight = FontWeights.Bold, Foreground = gray });
                TextBlock tb = new TextBlock() { Text = p.evasion.ToString(), FontWeight = FontWeights.Bold, Foreground = (p.evasion == sp.evasion) ? white : blue, Tag = "Evasion" };
                HookEvents(tb);
                panel.Children.Add(tb);
                PropertyBox.Children.Add(panel);
            }
            if (p.energy_shield > 0)
            {
                panel = new StackPanel() { Orientation = Orientation.Horizontal };
                panel.Children.Add(new TextBlock() { Text = "Energy Shield: ", FontWeight = FontWeights.Bold, Foreground = gray });
                TextBlock tb = new TextBlock() { Text = p.energy_shield.ToString(), FontWeight = FontWeights.Bold, Foreground = (p.energy_shield == sp.energy_shield) ? white : blue, Tag = "Energy Shield" };
                HookEvents(tb);
                panel.Children.Add(tb);
                PropertyBox.Children.Add(panel);
            }
            if (p.physical_damage_max > 0)
            {
                panel = new StackPanel() { Orientation = Orientation.Horizontal };
                panel.Children.Add(new TextBlock() { Text = "Physical Damage: ", FontWeight = FontWeights.Bold, Foreground = gray });
                TextBlock tb = new TextBlock() { Text = p.physical_damage_min + "-" + p.physical_damage_max, FontWeight = FontWeights.Bold, Foreground = (p.physical_damage_max == sp.physical_damage_max) ? white : blue, Tag = "Physical Damage" };
                HookEvents(tb);
                panel.Children.Add(tb);
                PropertyBox.Children.Add(panel);
            }
            if (p.critical_strike_chance > 0)
            {
                panel = new StackPanel() { Orientation = Orientation.Horizontal };
                panel.Children.Add(new TextBlock() { Text = "Critical Strike Chance: ", FontWeight = FontWeights.Bold, Foreground = gray });
                TextBlock tb = new TextBlock() { Text = ((double)p.critical_strike_chance / 100).ToString("N2"), FontWeight = FontWeights.Bold, Foreground = (p.critical_strike_chance == sp.critical_strike_chance) ? white : blue, Tag = "Critical Strike Chance" };
                HookEvents(tb);
                panel.Children.Add(tb);
                PropertyBox.Children.Add(panel);
            }
            if (p.attack_time > 0)
            {
                panel = new StackPanel() { Orientation = Orientation.Horizontal };
                panel.Children.Add(new TextBlock() { Text = "Attacks per Second: ", FontWeight = FontWeights.Bold, Foreground = gray });
                TextBlock tb = new TextBlock() { Text = ((double)1000 / p.attack_time).ToString("N2"), FontWeight = FontWeights.Bold, Foreground = (p.attack_time == sp.attack_time) ? white : blue, Tag = "Attack Speed" };
                HookEvents(tb);
                panel.Children.Add(tb);
                PropertyBox.Children.Add(panel);
            }
            if (p.physical_damage_max > 0 && p.attack_time > 0)
            {
                panel = new StackPanel() { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, FlowDirection = FlowDirection.RightToLeft };
                TextBlock tb = new TextBlock() { Text = ((p.physical_damage_min + p.physical_damage_max) * 500 / (double)p.attack_time).ToString("N2"), FontWeight = FontWeights.Bold, Foreground = blue, Tag = "Physical DPS" };
                HookEvents(tb);
                panel.Children.Add(tb);
                panel.Children.Add(new TextBlock() { Text = "DPS: ", FontWeight = FontWeights.Bold, Foreground = gray, FlowDirection = FlowDirection.LeftToRight});
                PseudoPropBox.Children.Add(panel);
            }
        }
    }
}
