using qgrepControls.Classes;
using qgrepControls.Properties;
using System.Windows.Controls;

namespace qgrepControls.SearchWindow
{
    public partial class FilesSettingsWindow : System.Windows.Controls.UserControl
    {
        public qgrepFilesWindowControl SearchWindow;

        public FilesSettingsWindow(qgrepFilesWindowControl SearchWindow)
        {
            this.SearchWindow = SearchWindow;
            InitializeComponent();

            PathStyleComboBox.SelectedIndex = Settings.Default.FilesPathStyleIndex;
            SearchScopeComboBox.SelectedIndex = Settings.Default.FilesSearchScopeIndex;

            ThemeHelper.UpdateColorsFromSettings(this, SearchWindow.WrapperApp);
        }

        private void PathStyleComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            bool changed = Settings.Default.FilesPathStyleIndex != PathStyleComboBox.SelectedIndex;

            Settings.Default.FilesPathStyleIndex = PathStyleComboBox.SelectedIndex;
            Settings.Default.Save();

            if (changed)
            {
                SearchWindow.UpdateFromSettings();
            }
        }

        private void SearchScopeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            bool changed = Settings.Default.FilesSearchScopeIndex != SearchScopeComboBox.SelectedIndex;

            Settings.Default.FilesSearchScopeIndex = SearchScopeComboBox.SelectedIndex;
            Settings.Default.Save();

            if (changed)
            {
                SearchWindow.UpdateFromSettings();
            }
        }
    }
}
