using Microsoft.VisualStudio.PlatformUI;
using qgrepSearch.Properties;
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
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Xceed.Wpf.Toolkit;

namespace qgrepSearch.ToolWindows
{
    public partial class SettingsWindow : System.Windows.Controls.UserControl
    {
        public qgrepSearchWindowControl SearchWindow;

        private DialogWindow FileExtensionDialog = null;

        public SettingsWindow(qgrepSearchWindowControl SearchWindow)
        {
            this.SearchWindow = SearchWindow;
            InitializeComponent();
            
            ShowIncludes.IsChecked = Settings.Default.ShowIncludes;
            ShowExcludes.IsChecked = Settings.Default.ShowExcludes;
            FileCountThreshold.Text = Settings.Default.UpdateChangesCount + "";
            UpdateDelay.Text = Settings.Default.UpdateDelay + "";
            UpdateFocused.IsChecked = Settings.Default.UpdateFocused;

            LoadColorsFromResources();
        }

        private void LoadColorsFromResources()
        {
            Dictionary<string, System.Windows.Media.Color> colors = SearchWindow.GetColorsFromResources();

            foreach (var color in colors)
            {
                Resources[color.Key] = new SolidColorBrush(color.Value);
            }
        }

        private void SaveOptions()
        {
            Settings.Default.ShowIncludes = ShowIncludes.IsChecked == true;
            Settings.Default.ShowExcludes = ShowExcludes.IsChecked == true;
            Settings.Default.UpdateFocused = UpdateFocused.IsChecked == true;

            try
            {
                int updateDelay = Int32.Parse(UpdateDelay.Text);
                updateDelay = Math.Max(1, Math.Min(3600, updateDelay));
                Settings.Default.UpdateDelay = updateDelay;
            }
            catch (Exception ex)
            {
            }

            try
            {
                int fileCountThreshold = Int32.Parse(FileCountThreshold.Text);
                fileCountThreshold = Math.Max(0, Math.Min(10000, fileCountThreshold));
                Settings.Default.UpdateChangesCount = fileCountThreshold;
            }
            catch (Exception ex)
            {
            }

            Settings.Default.Save();
        }

        private void UpdateInterval_LostFocus(object sender, RoutedEventArgs e)
        {
            SaveOptions();
            SearchWindow.UpdateFromSettings();
            UpdateDelay.Text = Settings.Default.UpdateDelay + "";
            FileCountThreshold.Text = Settings.Default.UpdateChangesCount + "";
        }

        private void UpdateInterval_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                SaveOptions();
                SearchWindow.UpdateFromSettings();
                UpdateDelay.Text = Settings.Default.UpdateDelay + "";
                FileCountThreshold.Text = Settings.Default.UpdateChangesCount + "";
            }
        }
        private void Option_Click(object sender, RoutedEventArgs e)
        {
            SaveOptions();
            SearchWindow.UpdateFromSettings();
        }
    }
}
