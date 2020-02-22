using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace PoETheoryCraft.Controls
{
    public class SearchableComboBox : ComboBox
    {
        public SearchableComboBox() : base()
        {
            this.Loaded += On_Load;
        }
        private void On_Load(object sender, RoutedEventArgs e)
        {
            this.ApplyTemplate();
            TextBox textBox = this.Template.FindName("PART_EditableTextBox", this) as TextBox;
            textBox.SelectionLength = 0;

            if (textBox != null)
            {
                textBox.TextChanged += delegate
                {
                    this.IsDropDownOpen = true;

                    this.Items.Filter += a =>
                    {
                        if (a.ToString().ToUpper().Contains(textBox.Text.ToUpper()))
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    };

                    textBox.SelectionLength = 0;
                    textBox.CaretIndex = textBox.Text.Length;
                };
            }
        }
    }
}
