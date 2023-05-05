using qgrepControls.Properties;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using Xceed.Wpf.Toolkit;

namespace qgrepControls.ToolWindows
{
    public partial class ColorsWindow : System.Windows.Controls.UserControl
    {
        public qgrepSearchWindowControl SearchWindow;

        public ColorsWindow(qgrepSearchWindowControl SearchWindow)
        {
            this.SearchWindow = SearchWindow;
            InitializeComponent();

            Feedback.Content = "";
            LoadColorsFromResources();
        }

        private void LoadColorsFromResources()
        {
            Dictionary<string, SolidColorBrush> colors = SearchWindow.GetBrushesFromColorScheme();

            foreach (var color in colors)
            {
                ColorPicker colorPicker = (ColorPicker)this.FindName(color.Key);
                Resources[color.Key] = color.Value;
                colorPicker.SelectedColor = color.Value.Color;
            }
        }

        private void Load_Click(object sender, RoutedEventArgs e)
        {
            SearchWindow.UpdateColorsFromSettings();
            LoadColorsFromResources();

            Feedback.Content = "Loaded colors from settings!";
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            Feedback.Content = "Saved colors to settings!";
        }

        private void Apply_Click(object sender, RoutedEventArgs e)
        {
            Feedback.Content = "Applied colors. Don't forget to save them!";
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            Settings.Default.Reset();
            SearchWindow.UpdateColorsFromSettings();
            LoadColorsFromResources();

            Feedback.Content = "Reset the colors to default!";
        }
    }
}
