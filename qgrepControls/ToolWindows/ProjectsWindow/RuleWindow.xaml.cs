using System;
using System.Collections.Generic;
using System.IO;
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
using Xceed.Wpf.Toolkit;

namespace qgrepSearch.ToolWindows
{
    public partial class RuleWindow : System.Windows.Controls.UserControl
    {
        public ProjectsWindow Parent;
        public Window Dialog = null;
        public delegate void Callback(bool accepted);
        private Callback ResultCallback;
        public bool IsOK = false;

        public RuleWindow(ProjectsWindow Parent)
        {
            this.Parent = Parent;

            InitializeComponent();
            LoadColorsFromResources();
        }

        private void LoadColorsFromResources()
        {
            Dictionary<string, System.Windows.Media.Color> colors = Parent.Parent.GetColorsFromColorScheme();

            foreach (var color in colors)
            {
                Resources[color.Key] = new SolidColorBrush(color.Value);
            }
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            IsOK = true;
            if(Dialog != null)
            {
                Dialog.Close();
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            IsOK = false;
            if (Dialog != null)
            {
                Dialog.Close();
            }
        }

        private void UserControl_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if(e.Key == Key.Enter)
            {
                IsOK = true;
                if (Dialog != null)
                {
                    Dialog.Close();
                }

            }
            else if(e.Key == Key.Escape)
            {
                IsOK= false;
                if (Dialog != null)
                {
                    Dialog.Close();
                }
            }
        }

        private void PredefinedButton_Click(object sender, RoutedEventArgs e)
        {
            PredefinedPopup.IsOpen = !PredefinedPopup.IsOpen;
        }

        private void ComboBoxItem_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            PredefinedPopup.IsOpen = false;
            ComboBoxItem comboBox = sender as ComboBoxItem;
            if (comboBox != null)
            {
                GroupRegEx.Text += comboBox.Tag as String;
            }
        }

        private void UserControl_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            PredefinedPopup.IsOpen = false;
        }
    }
}
