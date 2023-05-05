﻿using qgrepControls.SearchWindow;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace qgrepControls.ColorsWindow
{
    public partial class OverrideRow : System.Windows.Controls.UserControl
    {
        public class OverrideRowData
        {
            private System.Drawing.Color color;
            public OverrideRowData(string Name, System.Drawing.Color Color)
            {
                this.Name = Name;
                this.Color = Color;
            }

            public string Name { get; set; } = "";
            public System.Windows.Media.Brush Brush { get; set; }
            public System.Drawing.Color Color
            {
                get
                {
                    return color;
                }
                set
                {
                    Brush = new System.Windows.Media.SolidColorBrush(qgrepSearchWindowControl.ConvertColor(value));
                    color = value;
                }
            }
        }

        public ColorsWindow Parent;
        public OverrideRowData Data;

        public OverrideRow(ColorsWindow Parent, OverrideRowData Data)
        {
            InitializeComponent();

            this.Data = Data;
            this.Parent = Parent;

            this.DataContext = Data;

            Icons.Visibility = Visibility.Collapsed;
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

        private void OverrideGrid_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            Icons.Visibility = Visibility.Visible;
        }

        private void OverrideGrid_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            Icons.Visibility = Visibility.Collapsed;
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            Parent.DeleteOverride(this);
        }
    }
}
