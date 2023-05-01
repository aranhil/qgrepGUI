using qgrepControls.Properties;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace qgrepControls.ToolWindows
{
    public partial class SettingsWindow : System.Windows.Controls.UserControl
    {
        public qgrepSearchWindowControl SearchWindow;

        public SettingsWindow(qgrepSearchWindowControl SearchWindow)
        {
            this.SearchWindow = SearchWindow;
            InitializeComponent();
            
            ShowIncludes.IsChecked = Settings.Default.ShowIncludes;
            ShowExcludes.IsChecked = Settings.Default.ShowExcludes;
            ShowFilter.IsChecked = Settings.Default.ShowFilter;

            foreach (ColorScheme colorScheme in SearchWindow.colorSchemes)
            {
                ColorSchemeComboBox.Items.Add(new ComboBoxItem() { Content = colorScheme.Name });
            }

            ColorSchemeComboBox.SelectedIndex = Settings.Default.ColorScheme;
            GroupingComboBox.SelectedIndex = Settings.Default.GroupingIndex;

            LoadColorsFromResources();
        }

        private void LoadColorsFromResources()
        {
            Dictionary<string, System.Windows.Media.Color> colors = SearchWindow.GetColorsFromColorScheme();

            foreach (var color in colors)
            {
                Resources[color.Key] = new SolidColorBrush(color.Value);
            }
        }

        private void SaveOptions()
        {
            Settings.Default.ShowIncludes = ShowIncludes.IsChecked == true;
            Settings.Default.ShowExcludes = ShowExcludes.IsChecked == true;
            Settings.Default.ShowFilter = ShowFilter.IsChecked == true;

            Settings.Default.Save();
        }

        private void UpdateInterval_LostFocus(object sender, RoutedEventArgs e)
        {
            SaveOptions();
            SearchWindow.UpdateFromSettings();
        }

        private void UpdateInterval_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                SaveOptions();
                SearchWindow.UpdateFromSettings();
            }
        }
        private void Option_Click(object sender, RoutedEventArgs e)
        {
            SaveOptions();
            SearchWindow.UpdateFromSettings();
        }

        private void ColorSchemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Settings.Default.ColorScheme = ColorSchemeComboBox.SelectedIndex;
            Settings.Default.Save();
            SearchWindow.UpdateColorsFromSettings();
            LoadColorsFromResources();
        }

        private void GroupingComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Settings.Default.GroupingIndex = GroupingComboBox.SelectedIndex;
            Settings.Default.Save();
        }
    }
}
