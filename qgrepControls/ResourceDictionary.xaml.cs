using qgrepControls.Classes;
using qgrepControls.SearchWindow;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

namespace qgrepControls
{
    /// <summary>
    /// Interaction logic for ResourceDictionary.xaml
    /// </summary>
    public partial class MyResourceDictionary : ResourceDictionary
    {
        public MyResourceDictionary()
        {
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Window window = (sender as FrameworkElement)?.TemplatedParent as Window;
            if (window != null)
                window.Close();

            if (window != null && window.Owner != null)
                window.Owner.Focus();
        }

        SolidColorBrush GetBrush(TextBox textBox, bool IsMouseOver, bool IsSelected)
        {
            if (IsMouseOver)
            {
                return textBox.FindResource("Border.IsMouseOver") as SolidColorBrush;
            }
            else if (IsSelected)
            {
                return textBox.FindResource("Border.IsSelected") as SolidColorBrush;
            }
            else
            {
                return textBox.FindResource("Border") as SolidColorBrush;
            }
        }

        bool BrushesAreTheSame(SolidColorBrush brush1, SolidColorBrush brush2)
        {
            return brush1 != null && brush2 != null &&
                brush1.Color.R == brush2.Color.R &&
                brush1.Color.G == brush2.Color.G &&
                brush1.Color.B == brush2.Color.B;
        }

        private void PART_ContentHost_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            TextBox textBox = (sender as FrameworkElement)?.TemplatedParent as TextBox;
            if(textBox != null)
            {
                SolidColorBrush newBrush = GetBrush(textBox, true, textBox.IsFocused);
                SolidColorBrush oldBrush = GetBrush(textBox, false, textBox.IsFocused);

                if(!BrushesAreTheSame(newBrush, oldBrush))
                {
                    Grid parentGrid = UIHelper.FindAncestor<Grid>(textBox);
                    Panel.SetZIndex(parentGrid, 2);
                }
            }
        }

        private void PART_ContentHost_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            TextBox textBox = (sender as FrameworkElement)?.TemplatedParent as TextBox;
            if (textBox != null)
            {
                Grid parentGrid = UIHelper.FindAncestor<Grid>(textBox);
                Panel.SetZIndex(parentGrid, textBox.IsFocused ? 1 : 0);
            }
        }

        private void PART_ContentHost_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = (sender as FrameworkElement)?.TemplatedParent as TextBox;
            if (textBox != null)
            {
                Grid parentGrid = UIHelper.FindAncestor<Grid>(textBox);
                Panel.SetZIndex(parentGrid, textBox.IsMouseOver ? 2 : 0);
            }
        }

        private void PART_ContentHost_GotFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = (sender as FrameworkElement)?.TemplatedParent as TextBox;
            if (textBox != null)
            {
                SolidColorBrush newBrush = GetBrush(textBox, textBox.IsMouseOver, true);
                SolidColorBrush oldBrush = GetBrush(textBox, textBox.IsMouseOver, false);

                if (!BrushesAreTheSame(newBrush, oldBrush))
                {
                    Grid parentGrid = UIHelper.FindAncestor<Grid>(textBox);
                        Panel.SetZIndex(parentGrid, textBox.IsMouseOver ? 2 : 1);
                }
            }
        }

        private void ListBoxItem_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = true;
        }

        private void ListBoxItem_MouseEnter(object sender, MouseEventArgs e)
        {
            ListBoxItem listBoxItem = (sender as FrameworkElement)?.TemplatedParent as ListBoxItem;
            listBoxItem.IsSelected = true;
            listBoxItem.Focus();
        }

        private void ListBox_MouseLeave(object sender, MouseEventArgs e)
        {
            ListBox listBox = (sender as FrameworkElement)?.TemplatedParent as ListBox;
            if(listBox != null)
            {
                listBox.SelectedIndex = -1;
            }
        }
    }
}
