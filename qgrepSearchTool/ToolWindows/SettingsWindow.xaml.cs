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
    public class FileExtensionsGroup
    {
        public int Index;
        public string Name;
        public string RegEx;

        public void Delete()
        {
            SettingsWindow.ExtensionGroups.RemoveAt(Index);
            SettingsWindow.SaveExtensions();
            SettingsWindow.LoadExtensions();
        }

        public void Edit()
        {
            SettingsWindow.ExtensionGroups[Index].Name = Name;
            SettingsWindow.ExtensionGroups[Index].RegEx = RegEx;
            SettingsWindow.SaveExtensions();
            SettingsWindow.LoadExtensions();
        }

        public SettingsWindow SettingsWindow;
    }

    public partial class SettingsWindow : System.Windows.Controls.UserControl
    {
        public qgrepSearchWindowControl SearchWindow;
        public List<FileExtensionsGroup> ExtensionGroups = new List<FileExtensionsGroup>();

        private FileExtensionWindow FileExtensionWindow = null;
        private DialogWindow FileExtensionDialog = null;
        private FileExtensionsGroup NewFileExtensionsGroup;

        public SettingsWindow(qgrepSearchWindowControl SearchWindow)
        {
            this.SearchWindow = SearchWindow;
            InitializeComponent();
            
            ShowIncludes.IsChecked = Settings.Default.ShowIncludes;
            ShowExcludes.IsChecked = Settings.Default.ShowExcludes;
            FileCountThreshold.Text = Settings.Default.UpdateChangesCount + "";
            UpdateDelay.Text = Settings.Default.UpdateDelay + "";
            UpdateFocused.IsChecked = Settings.Default.UpdateFocused;
            CacheLocation.Text = SearchWindow.SearchPath;

            LoadExtensions();

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

        public void LoadExtensions()
        {
            ExtensionGroups.Clear();
            ExtensionsPanel.Children.Clear();

            if (File.Exists(SearchWindow.ConfigPath))
            {
                string[] allConfigLines = File.ReadAllLines(SearchWindow.ConfigPath);
                for (int i = 0; i < allConfigLines.Length; i++)
                {
                    if (allConfigLines[i].StartsWith("# ") && i + 1 < allConfigLines.Length && allConfigLines[i + 1].StartsWith("include "))
                    {
                        string groupName = allConfigLines[i].Substring(2);
                        string groupRegEx = allConfigLines[i + 1].Substring(8);

                        ExtensionGroups.Add(new FileExtensionsGroup { SettingsWindow = this, Name = groupName, RegEx = groupRegEx });
                        i++;
                    }
                }
            }

            ExtensionGroups = ExtensionGroups.OrderBy(o => o.Name).ToList();
            SearchWindow.LoadExtensions();

            int currentIndex = 0;
            foreach (var extensionsGroup in ExtensionGroups)
            {
                extensionsGroup.Index = currentIndex++;
                ExtensionsPanel.Children.Add(new FileExtensionRow(SearchWindow, extensionsGroup));
            }
        }

        public void SaveExtensions()
        {
            if (File.Exists(SearchWindow.ConfigPath))
            {
                string[] allConfigLines = File.ReadAllLines(SearchWindow.ConfigPath);
                string firstLine = allConfigLines[0];

                allConfigLines = new string[ExtensionGroups.Count * 2 + 2];
                allConfigLines[0] = firstLine;
                allConfigLines[1] = "";

                int currentConfigLine = 2;

                ExtensionGroups = ExtensionGroups.OrderBy(o => o.Name).ToList();
                foreach (var extensionsGroup in ExtensionGroups)
                {
                    allConfigLines[currentConfigLine++] = "# " + extensionsGroup.Name;
                    allConfigLines[currentConfigLine++] = "include " + extensionsGroup.RegEx;
                }

                File.WriteAllLines(SearchWindow.ConfigPath, allConfigLines);
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

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if(Directory.Exists(CacheLocation.Text))
            {
                SearchWindow.SetSearchPath(CacheLocation.Text);
                SearchWindow.CleanDatabase();
                SearchWindow.UpdateDatabase();
            }
        }
        private void Browse_Click(object sender, RoutedEventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                fbd.Reset();
                fbd.RootFolder = Environment.SpecialFolder.MyComputer;
                fbd.SelectedPath = CacheLocation.Text;

                DialogResult result = fbd.ShowDialog();

                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    if(Directory.Exists(fbd.SelectedPath))
                    {
                        CacheLocation.Text = fbd.SelectedPath;
                    }
                }
            }
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

        private void AddWindowResultCallback(bool accepted)
        {
            if (accepted)
            {
                if (FileExtensionWindow != null)
                {
                    ExtensionGroups.Add(new FileExtensionsGroup 
                    {
                        Name = FileExtensionWindow.GroupName.Text, 
                        RegEx = FileExtensionWindow.GroupRegEx.Text
                    });

                    SaveExtensions();
                    LoadExtensions();
                }
            }

            FileExtensionWindow = null;
            FileExtensionDialog.Close();
        }

        private void AddExtension_Click(object sender, RoutedEventArgs e)
        {
            FileExtensionWindow = new qgrepSearch.ToolWindows.FileExtensionWindow(SearchWindow, new FileExtensionWindow.Callback(AddWindowResultCallback));
            FileExtensionWindow.GroupName.Text = "Custom group";
            FileExtensionWindow.GroupRegEx.Text = "\\.()$";

            FileExtensionDialog = new DialogWindow
            {
                Title = "Add group",
                Content = FileExtensionWindow,
                SizeToContent = SizeToContent.WidthAndHeight,
                ResizeMode = ResizeMode.NoResize,
                HasMinimizeButton = false,
                HasMaximizeButton = false,
            };

            FileExtensionDialog.ShowModal();
        }

        private void ResetExtensions_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists(SearchWindow.ConfigPath))
            {
                string defaultExtensions = "# C#, VB.NET\ninclude \\.(cs|vb)$\n# C/C++\ninclude \\.(cpp|cxx|cc|c|hpp|hxx|hh|h|inl)$\n# D\ninclude \\.(d)$\n# Delphi, Pascal\ninclude \\.(dpr|pas|pp|inc|dfm|lfm|lpi|lpr|dpk|dproj|spp)$\n# F#, OCaml, Haskell\ninclude \\.(fs|fsi|fsx|ml|mli|hs)$\n# Go\ninclude \\.(go)$\n# HTML, CSS\ninclude \\.(htm|html|css|sass|scss)$\n# Java, JavaScript, Kotlin, TypeScript\ninclude \\.(java|js|kt|kts|ts|tsx)$\n# Julia\ninclude \\.(jl)$\n# Lua, Squirrel\ninclude \\.(lua|nut)$\n# Markdown, reStructuredText, simple text\ninclude \\.(md|rst|txt)$\n# Nim\ninclude \\.(nim)$\n# Objective C/C++\ninclude \\.(m|mm)$\n# Perl, Python, Ruby\ninclude \\.(pl|py|pm|rb)$\n# PHP, ActionScript\ninclude \\.(php|as)$\n# Rust\ninclude \\.(rs)$\n# Shaders\ninclude \\.(hlsl|glsl|cg|fx|cgfx)$\n# XML, JSON, CSV\ninclude \\.(xml|json|csv)$\n# Zig\ninclude \\.(zig)$";
                List<string> defaultExtensionsLines = defaultExtensions.Split('\n').ToList();
                string[] allConfigLines = File.ReadAllLines(SearchWindow.ConfigPath);

                defaultExtensionsLines.Insert(0, "");
                defaultExtensionsLines.Insert(0, allConfigLines[0]);

                File.WriteAllLines(SearchWindow.ConfigPath, defaultExtensionsLines);
            }

            LoadExtensions();
        }
    }
}
