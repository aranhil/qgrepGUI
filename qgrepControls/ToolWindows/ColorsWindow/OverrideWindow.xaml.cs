using qgrepControls.Classes;
using qgrepControls.SearchWindow;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace qgrepControls.ColorsWindow
{
    public partial class OverrideWindow : System.Windows.Controls.UserControl
    {
        public ColorsWindow Parent;
        public IExtensionWindow Dialog = null;
        public delegate void Callback(bool accepted);
        private Callback ResultCallback;
        public bool IsOK = false;

        public OverrideWindow(ColorsWindow Parent)
        {
            this.Parent = Parent;

            InitializeComponent();
            LoadColorsFromResources();
        }

        private void LoadColorsFromResources()
        {
            Dictionary<string, SolidColorBrush> colors = Parent.Parent.GetBrushesFromColorScheme();

            foreach (var color in colors)
            {
                Resources[color.Key] = color.Value;
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

        private void OverrideName_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            OverrideColor.SelectedColor = qgrepSearchWindowControl.ConvertColor((OverrideName.SelectedItem as ComboBoxColorItem).Color);
        }
    }
}
