using qgrepControls.Classes;
using qgrepControls.Properties;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
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
            ShowOpenHistory.IsChecked = Settings.Default.ShowOpenHistory;
            SearchInstantly.IsChecked = Settings.Default.SearchInstantly;
            TrimSpacesOnCopy.IsChecked = Settings.Default.TrimSpacesOnCopy;
            UpdateIndexAutomatically.IsChecked = Settings.Default.UpdateIndexAutomatically;
            GroupingComboBox.SelectedIndex = Settings.Default.GroupingIndex;
            PathStyleComboBox.SelectedIndex = Settings.Default.PathStyleIndex;
            ExpandModeComboBox.SelectedIndex = Settings.Default.ExpandModeIndex;
            FilterScopeComboBox.SelectedIndex = Settings.Default.FilterSearchScopeIndex;

            ThemeHelper.UpdateColorsFromSettings(this, SearchWindow.WrapperApp);
            UpdateShortcutHints();
        }

        private void UpdateShortcutHints()
        {
            ShowIncludes.ToolTip = string.Format(Properties.Resources.ShortcutTooltipFormat, SearchWindow.bindings["ToggleIncludeFiles"].ToString());
            ShowExcludes.ToolTip = string.Format(Properties.Resources.ShortcutTooltipFormat, SearchWindow.bindings["ToggleExcludeFiles"].ToString());
            ShowFilter.ToolTip = string.Format(Properties.Resources.ShortcutTooltipFormat, SearchWindow.bindings["ToggleFilterResults"].ToString());
            GroupingComboBox.ToolTip = string.Format(Properties.Resources.ShortcutTooltipFormat, SearchWindow.bindings["ToggleGroupBy"].ToString());
            ExpandModeComboBox.ToolTip = string.Format(Properties.Resources.ShortcutTooltipNoChangeFormat, SearchWindow.bindings["ToggleGroupExpand"].ToString());
        }

        private void SaveOptions()
        {
            Settings.Default.ShowIncludes = ShowIncludes.IsChecked == true;
            Settings.Default.ShowExcludes = ShowExcludes.IsChecked == true;
            Settings.Default.ShowFilter = ShowFilter.IsChecked == true;
            Settings.Default.ShowHistory = ShowHistory.IsChecked == true;
            Settings.Default.ShowOpenHistory = ShowOpenHistory.IsChecked == true;
            Settings.Default.SearchInstantly = SearchInstantly.IsChecked == true;
            Settings.Default.TrimSpacesOnCopy = TrimSpacesOnCopy.IsChecked == true;
            Settings.Default.UpdateIndexAutomatically = UpdateIndexAutomatically.IsChecked == true;

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
            bool changed = Settings.Default.GroupingIndex != GroupingComboBox.SelectedIndex;

            Settings.Default.GroupingIndex = GroupingComboBox.SelectedIndex;
            Settings.Default.Save();

            if(changed)
            {
                SearchWindow.UpdateFromSettings();
            }
        }

        private void PathStyleComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            bool changed = Settings.Default.PathStyleIndex != PathStyleComboBox.SelectedIndex;

            Settings.Default.PathStyleIndex = PathStyleComboBox.SelectedIndex;
            Settings.Default.Save();

            if (changed)
            {
                SearchWindow.UpdateFromSettings();
            }
        }

        private void ExpandModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            bool changed = Settings.Default.ExpandModeIndex != ExpandModeComboBox.SelectedIndex;

            Settings.Default.ExpandModeIndex = ExpandModeComboBox.SelectedIndex;
            Settings.Default.Save();

            if (changed)
            {
                SearchWindow.UpdateFromSettings();
            }
        }

        private void UpdateIndexAutomatically_Click(object sender, RoutedEventArgs e)
        {
            SaveOptions();

            if(Settings.Default.UpdateIndexAutomatically)
            {
                SearchWindow.UpdateDatabase(true);
                ConfigParser.AddWatchers();
            }
            else
            {
                ConfigParser.RemoveWatchers();
            }
        }

        private void FilterScopeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            bool changed = Settings.Default.FilterSearchScopeIndex != FilterScopeComboBox.SelectedIndex;

            Settings.Default.FilterSearchScopeIndex = FilterScopeComboBox.SelectedIndex;
            Settings.Default.Save();

            if (changed)
            {
                SearchWindow.UpdateFromSettings();
            }
        }
    }
}
