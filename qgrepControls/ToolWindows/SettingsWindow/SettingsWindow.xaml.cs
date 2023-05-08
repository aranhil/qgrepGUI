using qgrepControls.Properties;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace qgrepControls.SearchWindow
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
            ShowHistory.IsChecked = Settings.Default.ShowHistory;
            SearchInstantly.IsChecked = Settings.Default.SearchInstantly;
            TrimSpacesOnCopy.IsChecked = Settings.Default.TrimSpacesOnCopy;
            GroupingComboBox.SelectedIndex = Settings.Default.GroupingIndex;
            PathStyleComboBox.SelectedIndex = Settings.Default.PathStyleIndex;
            ExpandModeComboBox.SelectedIndex = Settings.Default.ExpandModeIndex;

            SearchWindow.LoadColorsFromResources(this);
        }

        private void SaveOptions()
        {
            Settings.Default.ShowIncludes = ShowIncludes.IsChecked == true;
            Settings.Default.ShowExcludes = ShowExcludes.IsChecked == true;
            Settings.Default.ShowFilter = ShowFilter.IsChecked == true;
            Settings.Default.ShowHistory = ShowHistory.IsChecked == true;
            Settings.Default.SearchInstantly = SearchInstantly.IsChecked == true;
            Settings.Default.TrimSpacesOnCopy = TrimSpacesOnCopy.IsChecked == true;

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

        private void GroupingComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Settings.Default.GroupingIndex = GroupingComboBox.SelectedIndex;
            Settings.Default.Save();
        }

        private void PathStyleComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Settings.Default.PathStyleIndex = PathStyleComboBox.SelectedIndex;
            Settings.Default.Save();
        }

        private void ExpandModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Settings.Default.ExpandModeIndex = ExpandModeComboBox.SelectedIndex;
            Settings.Default.Save();
        }
    }
}
