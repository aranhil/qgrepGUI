using qgrepInterop;
using qgrepControls.Classes;
using qgrepControls.Properties;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Newtonsoft.Json;
using System.Timers;
using System.Windows.Documents;
using System.Windows.Shapes;
using qgrepControls.ColorsWindow;
using Xceed.Wpf.AvalonDock.Properties;
using System.Resources;
using System.Windows.Controls.Primitives;
using Xceed.Wpf.AvalonDock.Controls;
using System.Runtime.InteropServices.Expando;
using qgrepControls.ModelViews;

namespace qgrepControls.SearchWindow
{
    public class ColorEntry
    {
        public string Name = "";
        public System.Drawing.Color Color = System.Drawing.Color.White;
    }

    public class VsColorEntry
    {
        public string Name = "";
        public string Color = "";
        public double Opacity = 1.0f;
    }

    public class ColorScheme
    {
        public string Name = "";
        public ColorEntry[] ColorEntries = new ColorEntry[] { };
        public VsColorEntry[] VsColorEntries = new VsColorEntry[] { };
    }

    public partial class qgrepSearchWindowControl : UserControl
    {
        public IExtensionInterface ExtensionInterface;
        public string ConfigPath = "";
        public ConfigParser ConfigParser = null;
        public ColorScheme[] colorSchemes = new ColorScheme[0];
        public Dictionary<string, Hotkey> bindings = new Dictionary<string, Hotkey>();

        private System.Timers.Timer UpdateTimer = null;
        private string LastResults = "";

        ObservableCollection<SearchResult> searchResults = new ObservableCollection<SearchResult>();
        ObservableCollection<SearchResultGroup> searchResultsGroups = new ObservableCollection<SearchResultGroup>();
        SearchResult selectedSearchResult = null;
        SearchResultGroup selectedSearchResultGroup = null;

        List<string> searchHistory = new List<string>();
        bool searchInputChanged = true;

        static SearchEngine SearchEngine = new SearchEngine();

        public qgrepSearchWindowControl(IExtensionInterface extensionInterface)
        {
            ExtensionInterface = extensionInterface;

            InitializeComponent();
            SearchItemsListBox.DataContext = searchResults;

            InitButton.Visibility = Visibility.Collapsed;
            CleanButton.Visibility = Visibility.Collapsed;
            InitProgress.Visibility = Visibility.Collapsed;
            Overlay.Visibility = Visibility.Collapsed;
            PathsButton.IsEnabled = false;

            SearchCaseSensitive.IsChecked = Settings.Default.CaseSensitive;
            SearchRegEx.IsChecked = Settings.Default.RegEx;
            SearchWholeWord.IsChecked = Settings.Default.WholeWord;

            IncludeRegEx.IsChecked = Settings.Default.IncludesRegEx;
            ExcludeRegEx.IsChecked = Settings.Default.ExcludesRegEx;
            FilterRegEx.IsChecked = Settings.Default.FilterRegEx;

            StartTimer();
            UpdateLastUpdated();
            SolutionLoaded();

            string colorSchemesJson = System.Text.Encoding.Default.GetString(qgrepControls.Properties.Resources.colors_schemes);
            colorSchemes = JsonConvert.DeserializeObject<ColorScheme[]>(colorSchemesJson);

            UpdateColorsFromSettings();
            UpdateFromSettings();

            SearchItemsListBox.ItemsSource = searchResults;

            SearchItemsListBox.Focusable = true;
            SearchItemsTreeView.Focusable = true;

            SearchEngine.ResultCallback = HandleSearchResult;
            SearchEngine.StartSearchCallback = HandleSearchStart;
            SearchEngine.FinishSearchCallback = HandleSearchFinish;
            SearchEngine.StartUpdateCallback = HandleUpdateStart;
            SearchEngine.FinishUpdateCallback = HandleUpdateFinish;
            SearchEngine.UpdateInfoCallback = HandleUpdateMessage;
            SearchEngine.UpdateProgressCallback = HandleProgress;
            SearchEngine.ErrorCallback = HandleErrorMessage;

            bindings = ExtensionInterface.ReadKeyBindings();
            extensionInterface.ApplyKeyBindings(bindings);

            KeyboardButton.Visibility = ExtensionInterface.IsStandalone ? Visibility.Visible : Visibility.Collapsed;
            UpdateShortcutHints();
        }

        private void UpdateShortcutHints()
        {
            SearchCaseSensitive.ToolTip = "Case sensitive (" + bindings["ToggleCaseSensitive"].ToString() + ")";
            SearchWholeWord.ToolTip = "Whole word (" + bindings["ToggleWholeWord"].ToString() + ")";
            SearchRegEx.ToolTip = "Regular expressions (" + bindings["ToggleRegEx"].ToString() + ")";
            IncludeRegEx.ToolTip = "Regular expressions (" + bindings["ToggleRegEx"].ToString() + ")";
            ExcludeRegEx.ToolTip = "Regular expressions (" + bindings["ToggleRegEx"].ToString() + ")";
            FilterRegEx.ToolTip = "Regular expressions (" + bindings["ToggleRegEx"].ToString() + ")";
        }

        private void ResetTimestamp()
        {
            Settings.Default.LastUpdated = DateTime.Now;
            Settings.Default.Save();
            StartTimer();
        }

        private void StartTimer()
        {
            UpdateTimer = new System.Timers.Timer(30000);
            UpdateTimer.Elapsed += UpdateTimer_Elapsed;
            UpdateTimer.Start();
        }

        public static string GetTimeAgoString(DateTime eventTime)
        {
            TimeSpan timeSinceEvent = DateTime.Now - eventTime;

            if (timeSinceEvent.TotalSeconds < 60)
            {
                return "just now";
            }
            else if (timeSinceEvent.TotalMinutes < 60)
            {
                return $"{(int)timeSinceEvent.TotalMinutes}m ago";
            }
            else if (timeSinceEvent.TotalHours < 24)
            {
                return $"{(int)timeSinceEvent.TotalHours}h ago";
            }
            else if (timeSinceEvent.TotalDays < 7)
            {
                return $"{(int)timeSinceEvent.TotalDays}d ago";
            }
            else
            {
                return eventTime.ToString("d MMM yyyy");
            }
        }

        void UpdateLastUpdated()
        {
            if (!SearchEngine.IsBusy)
            {
                if (Settings.Default["LastUpdated"] != null)
                {
                    InitInfo.Content = "Last index update: " + GetTimeAgoString(Settings.Default.LastUpdated);
                }
            }
        }

        private void UpdateTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                UpdateLastUpdated();
            }));
        }

        public static System.Windows.Media.Color ConvertColor(System.Drawing.Color color)
        {
            return System.Windows.Media.Color.FromArgb(color.A, color.R, color.G, color.B);
        }
        public static System.Drawing.Color ConvertColor(System.Windows.Media.Color color)
        {
            return System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B); ;
        }
        public static T FindAncestor<T>(DependencyObject current) where T : DependencyObject
        {
            while (current != null)
            {
                if (current is T ancestor)
                {
                    return ancestor;
                }

                current = VisualTreeHelper.GetParent(current);
            }

            return null;
        }
        public static T GetChildOfType<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj == null) return null;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                var child = VisualTreeHelper.GetChild(depObj, i);

                var result = (child as T) ?? GetChildOfType<T>(child);
                if (result != null) return result;
            }
            return null;
        }

        public void RenameFilter(string oldName, string newName)
        {
            List<string> searchFilters = Settings.Default.SearchFilters.Split(',').ToList();
            int oldFilterIndex = searchFilters.IndexOf(oldName);
            if(oldFilterIndex >= 0)
            {
                searchFilters[oldFilterIndex] = newName;
            }
            Settings.Default.SearchFilters = string.Join(",", searchFilters);
            Settings.Default.Save();
        }

        public void UpdateFilters()
        {
            Visibility visibility = Visibility.Collapsed;

            if (ConfigParser != null)
            {
                FiltersComboBox.ItemsSource = ConfigParser.ConfigProjects;
                FiltersComboBox.SelectedItems.Clear();

                List<string> searchFilters = Settings.Default.SearchFilters.Split(',').ToList();
                foreach(string searchFilter in searchFilters)
                {
                    ConfigProject selectedProject = null;
                    foreach(ConfigProject configProject in ConfigParser.ConfigProjects)
                    {
                        if(configProject.Name == searchFilter)
                        {
                            selectedProject = configProject;
                            break;
                        }
                    }

                    if(selectedProject != null)
                    {
                        FiltersComboBox.SelectedItems.Add(selectedProject);
                    }
                }

                if(FiltersComboBox.SelectedItems.Count == 0 && ConfigParser.ConfigProjects.Count > 0)
                {
                    FiltersComboBox.SelectedItems.Add(ConfigParser.ConfigProjects[0]);
                }

                if (ConfigParser.ConfigProjects.Count > 1)
                {
                    visibility = Visibility.Visible;
                }
            }

            FiltersComboBox.Visibility = visibility;
        }

        public void SolutionLoaded()
        {
            string solutionPath = ExtensionInterface.GetSolutionPath();
            if(solutionPath.Length > 0)
            {
                ConfigParser = new ConfigParser(System.IO.Path.GetDirectoryName(solutionPath));
                ConfigParser.LoadConfig();

                UpdateWarning();
                UpdateFilters();

                InitButton.Visibility = Visibility.Visible;
                CleanButton.Visibility = Visibility.Visible;
                PathsButton.IsEnabled = true;
            }
        }

        public void UpdateWarning()
        {
            WarningText.Visibility = Visibility.Hidden;
            if (!ConfigParser.HasAnyPaths())
            {
                WarningText.Text = "No search folders set.";
                WarningText.Visibility = Visibility.Visible;
            }
        }

        public void UpdateFromSettings()
        {
            Visibility visibility = Settings.Default.ShowIncludes == true ? Visibility.Visible : Visibility.Collapsed;
            IncludeFilesGrid.Visibility = visibility;

            visibility = Settings.Default.ShowExcludes == true ? Visibility.Visible : Visibility.Collapsed;
            ExcludeFilesGrid.Visibility = visibility;

            visibility = Settings.Default.ShowFilter == true ? Visibility.Visible : Visibility.Collapsed;
            FilterResultsGrid.Visibility = visibility;

            visibility = Settings.Default.ShowHistory == true ? Visibility.Visible : Visibility.Collapsed;
            HistoryButton.Visibility = visibility;

            visibility = Settings.Default.SearchInstantly == false ? Visibility.Visible : Visibility.Collapsed;
            SearchButton.Visibility = visibility;

            if (Settings.Default.SearchInstantly)
            {
                Find();
            }
        }

        public void UpdateColorsFromSettings()
        {
            if(ExtensionInterface.IsStandalone && Settings.Default.ColorScheme == 0)
            {
                Settings.Default.ColorScheme = 1;
                Settings.Default.Save();
            }

            Dictionary<string, object> resources = GetResourcesFromColorScheme();

            foreach (var resource in resources)
            {
                Resources[resource.Key] = resource.Value;
            }

            ExtensionInterface.RefreshResources(resources);
        }

        public void UpdateColors(Dictionary<string, System.Windows.Media.Color> colors)
        {
            foreach (var color in colors)
            {
                Resources[color.Key] = new SolidColorBrush(color.Value);
            }
        }

        public Dictionary<string, object> GetResourcesFromColorScheme()
        {
            Dictionary<string, object> results = new Dictionary<string, object>();
            Dictionary<string, SolidColorBrush> brushes = new Dictionary<string, SolidColorBrush>();

            if (Settings.Default.ColorScheme < colorSchemes.Length)
            {
                foreach (ColorEntry colorEntry in colorSchemes[Settings.Default.ColorScheme].ColorEntries)
                {
                    brushes[colorEntry.Name] = new SolidColorBrush(ConvertColor(colorEntry.Color));
                }
                foreach (VsColorEntry colorEntry in colorSchemes[Settings.Default.ColorScheme].VsColorEntries)
                {
                    brushes[colorEntry.Name] = new SolidColorBrush(ConvertColor(ExtensionInterface.GetColor(colorEntry.Color))) { Opacity = colorEntry.Opacity };
                }

                try
                {
                    List<ColorSchemeOverrides> colorSchemeOverrides = JsonConvert.DeserializeObject<List<ColorSchemeOverrides>>(Settings.Default.ColorOverrides);
                    foreach(ColorSchemeOverrides schemeOverrides in  colorSchemeOverrides)
                    {
                        if(schemeOverrides.Name == colorSchemes[Settings.Default.ColorScheme].Name)
                        {
                            foreach(ColorOverride colorOverride in schemeOverrides.ColorOverrides)
                            {
                                brushes[colorOverride.Name] = new SolidColorBrush(ConvertColor(colorOverride.Color));
                            }

                            break;
                        }
                    }
                }
                catch { }
            }

            foreach(KeyValuePair<string, SolidColorBrush> brush in brushes)
            {
                results[brush.Key] = brush.Value;
                results[brush.Key + ".Color"] = brush.Value.Color;
            }

            return results;
        }

        public void LoadColorsFromResources(UserControl userControl)
        {
            Dictionary<string, object> resources = GetResourcesFromColorScheme();
            MainWindow wrapperWindow = FindAncestor<MainWindow>(userControl);

            foreach (var resource in resources)
            {
                userControl.Resources[resource.Key] = resource.Value;

                if (wrapperWindow != null)
                {
                    wrapperWindow.Resources[resource.Key] = resource.Value;
                }
            }
        }

        private void SaveOptions()
        {
            Settings.Default.CaseSensitive = SearchCaseSensitive.IsChecked == true;
            Settings.Default.RegEx = SearchRegEx.IsChecked == true;
            Settings.Default.WholeWord = SearchWholeWord.IsChecked == true;
            Settings.Default.IncludesRegEx = IncludeRegEx.IsChecked == true;
            Settings.Default.ExcludesRegEx = ExcludeRegEx.IsChecked == true;
            Settings.Default.FilterRegEx = FilterRegEx.IsChecked == true;

            Settings.Default.Save();
        }

        private void SetTreeCollapsing(bool isExpanded)
        {
            foreach (SearchResultGroup searchResultGroup in searchResultsGroups)
            {
                if (searchResultGroup.IsExpanded != isExpanded)
                {
                    searchResultGroup.IsExpanded = isExpanded;
                }
            }
        }

        private void ProcessTreeCollapsingDuringPopulation()
        {
            bool expand = false;

            if(Settings.Default.ExpandModeIndex == 2 || Settings.Default.ExpandModeIndex == 1)
            {
                expand = true;
            }

            SetTreeCollapsing(expand);
        }

        private void ProcessTreeCollapsingAfterPopulation()
        {
            bool expand = false;

            if (Settings.Default.ExpandModeIndex == 2)
            {
                expand = true;
            }
            else if (Settings.Default.ExpandModeIndex == 1 && searchResultsGroups.Count + searchResults.Count <= 500)
            {
                expand = true;
            }

            SetTreeCollapsing(expand);
        }

        ObservableCollection<SearchResult> newSearchResults = new ObservableCollection<SearchResult>();
        ObservableCollection<SearchResultGroup> newSearchResultGroups = new ObservableCollection<SearchResultGroup>();
        bool newSearch = false;

        private void AddSearchResultToGroups(SearchResult searchResult, ObservableCollection<SearchResultGroup> searchResultGroups)
        {
            if(searchResultGroups.Count > 0 && searchResultGroups.Last().File == searchResult.File)
            {
                searchResultGroups.Last().SearchResults.Add(searchResult);
                searchResult.Parent = searchResultGroups.Last();
            }
            else
            {
                SearchResultGroup newSearchGroup = new SearchResultGroup()
                {
                    File = searchResult.File,
                    TrimmedFile = ConfigParser.RemovePaths(searchResult.File),
                    SearchResults = new ObservableCollection<SearchResult> { searchResult }
                };

                searchResultGroups.Add(newSearchGroup);
                searchResult.Parent = newSearchGroup;
            }
        }

        private void HandleSearchStart(SearchOptions searchOptions)
        {
            Dispatcher.Invoke(() =>
            {
                newSearch = true;
                selectedSearchResultGroup = null;
                selectedSearchResult = null;

                SearchItemsListBox.Visibility = searchOptions.GroupingMode == 0 ? Visibility.Visible : Visibility.Collapsed;
                SearchItemsTreeView.Visibility = searchOptions.GroupingMode != 0 ? Visibility.Visible : Visibility.Collapsed;
            });
        }

        private void AddResultsBatch(SearchOptions searchOptions)
        {
            if (searchOptions.GroupingMode == 0)
            {
                if (newSearch)
                {
                    SearchItemsListBox.ItemsSource = searchResults = newSearchResults;
                    newSearchResults = new ObservableCollection<SearchResult>();
                    newSearch = false;

                    if(searchResults.Count > 0)
                    {
                        searchResults[0].IsSelected = true;
                    }
                }
                else
                {
                    foreach (SearchResult result in newSearchResults)
                    {
                        searchResults.Add(result);
                    }

                    newSearchResults.Clear();
                }
            }
            else
            {
                if (newSearch)
                {
                    SearchItemsTreeView.ItemsSource = searchResultsGroups = newSearchResultGroups;
                    searchResults = newSearchResults;

                    if(searchResultsGroups.Count > 0)
                    {
                        searchResultsGroups[0].IsSelected = true;
                    }

                    newSearchResultGroups = new ObservableCollection<SearchResultGroup>();
                    newSearchResults = new ObservableCollection<SearchResult>();
                    newSearch = false;
                }
                else
                {
                    foreach (SearchResultGroup resultGroup in newSearchResultGroups)
                    {
                        foreach (SearchResult result in resultGroup.SearchResults)
                        {
                            AddSearchResultToGroups(result, searchResultsGroups);
                            searchResults.Add(result);
                        }
                    }

                    newSearchResultGroups.Clear();
                    newSearchResults.Clear();
                }

                ProcessTreeCollapsingDuringPopulation();
            }

            InfoLabel.Content = string.Format("Showing {0} result(s) for \"{1}\"", searchResults.Count, searchOptions.Query);
        }

        private void HandleSearchResult(string file, string lineNumber, string beginText, string highlight, string endText, SearchOptions searchOptions)
        {
            if (!SearchEngine.IsSearchQueued)
            {
                Dispatcher.Invoke(() =>
                {
                    if (SearchInput.Text.Length != 0 || IncludeFilesInput.Text.Length != 0)
                    {
                        string fileAndLine = "";
                        string trimmedFileAndLine = "";

                        if (file.Length > 0 && lineNumber.Length > 0)
                        {
                            fileAndLine = file + "(" + lineNumber + ")";
                            trimmedFileAndLine = ConfigParser.RemovePaths(fileAndLine);
                        }

                        SearchResult newSearchResult = new SearchResult()
                        {
                            File = file,
                            Line = lineNumber,
                            FileAndLine = fileAndLine,
                            TrimmedFileAndLine = trimmedFileAndLine,
                            BeginText = beginText,
                            HighlightedText = highlight,
                            EndText = endText,
                            FullResult = fileAndLine + beginText + highlight + endText
                        };

                        newSearchResults.Add(newSearchResult);

                        if (searchOptions.GroupingMode == 1)
                        {
                            AddSearchResultToGroups(newSearchResult, newSearchResultGroups);
                        }

                        if (newSearchResults.Count >= 100)
                        {
                            AddResultsBatch(searchOptions);
                        }
                    }
                });
            }
        }

        private void HandleSearchFinish(SearchOptions searchOptions)
        {
            Dispatcher.Invoke(() =>
            {
                if(!SearchEngine.IsSearchQueued)
                {
                    if (SearchInput.Text.Length == 0 && IncludeFilesInput.Text.Length == 0)
                    {
                        searchResults.Clear();
                        searchResultsGroups.Clear();
                        InfoLabel.Content = "";
                    }
                    else
                    {
                        AddResultsBatch(searchOptions);


                        if (searchOptions.GroupingMode == 1)
                        {
                            ProcessTreeCollapsingAfterPopulation();
                        }
                    }
                }
                else
                {
                    newSearch = false;
                    newSearchResultGroups.Clear();
                    newSearchResults.Clear();
                }
            });
        }

        private void Find()
        {
            if (SearchInput.Text.Length != 0)
            {
                SearchOptions searchOptions = new SearchOptions()
                {
                    Query = SearchInput.Text,
                    IncludeFiles = Settings.Default.ShowIncludes && IncludeFilesInput.Text.Length > 0 ? IncludeFilesInput.Text : "",
                    ExcludeFiles = Settings.Default.ShowExcludes && ExcludeFilesInput.Text.Length > 0 ? ExcludeFilesInput.Text : "",
                    FilterResults = Settings.Default.ShowFilter && FilterResultsInput.Text.Length > 0 ? FilterResultsInput.Text : "",
                    CaseSensitive = SearchCaseSensitive.IsChecked == true,
                    WholeWord = SearchWholeWord.IsChecked == true,
                    RegEx = SearchRegEx.IsChecked == true,
                    IncludeFilesRegEx = IncludeRegEx.IsChecked == true,
                    ExcludeFilesRegEx = ExcludeRegEx.IsChecked == true,
                    FilterResultsRegEx = FilterRegEx.IsChecked == true,
                    GroupingMode = Settings.Default.GroupingIndex,
                    Configs = FiltersComboBox.SelectedItems.Cast<ConfigProject>().Select(x => x.Path).ToList(),
                };

                SearchEngine.SearchAsync(searchOptions);
            }
            else if(IncludeFilesInput.Text.Length != 0)
            {
                SearchOptions searchOptions = new SearchOptions()
                {
                    Query = IncludeFilesInput.Text,
                    FilterResults = Settings.Default.ShowFilter && FilterResultsInput.Text.Length > 0 ? FilterResultsInput.Text : "",
                    IncludeFilesRegEx = IncludeRegEx.IsChecked == true,
                    FilterResultsRegEx = FilterRegEx.IsChecked == true,
                    GroupingMode = 0,
                    Configs = FiltersComboBox.SelectedItems.Cast<ConfigProject>().Select(x => x.Path).ToList(),
                };

                SearchEngine.SearchFilesAsync(searchOptions);
            }
            else
            {
                searchResults.Clear();
                searchResultsGroups.Clear();
                InfoLabel.Content = "";
            }
        }

        private void SearchResult_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            DockPanel dockPanel = sender as DockPanel;
            if (dockPanel != null)
            {
                SearchResult searchResult = dockPanel.DataContext as SearchResult;
                if (searchResult != null)
                {
                    if (e.ClickCount == 2)
                    {
                        OpenSearchResult(searchResult);
                    }
                }
            }
        }

        private void OpenSearchResult(SearchResult result)
        {
            if (SearchInput.Text.Length > 0)
            {
                try
                {
                    int lastIndex = result.FileAndLine.LastIndexOf('(');
                    string file = result.FileAndLine.Substring(0, lastIndex);
                    string line = result.FileAndLine.Substring(lastIndex + 1, result.FileAndLine.Length - lastIndex - 2);

                    ExtensionInterface.OpenFile(file, line);
                }
                catch (Exception)
                {
                }
            }
            else
            {
                ExtensionInterface.OpenFile(result.FullResult, "0");
            }
        }
        private void OpenSearchGroup(SearchResultGroup result)
        {
            ExtensionInterface.OpenFile(result.File, "0");
        }

        private void OpenSelectedSearchResult()
        {
            if(selectedSearchResult != null)
            {
                OpenSearchResult(selectedSearchResult);
            }
            else if(selectedSearchResultGroup != null)
            {
                OpenSearchGroup(selectedSearchResultGroup);
            }
        }

        public void CleanDatabase()
        {
            try
            {
                ConfigParser.CleanProjects();
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(new Action(() =>
                {
                    HandleErrorMessage("Cannot clean indexes: " + ex.Message + "\n");
                }));
            }
        }

        private Stopwatch infoUpdateStopWatch = new Stopwatch();
        private Stopwatch progressUpdateStopWatch = new Stopwatch();
        private string lastMessage = "";
        private void HandleUpdateStart()
        {
            Dispatcher.Invoke(() =>
            {
                Overlay.Visibility = Visibility.Visible;
                InitProgress.Visibility = Visibility.Visible;
                InitProgress.Value = 0;
                InitButton.IsEnabled = false;
                CleanButton.IsEnabled = false;
                PathsButton.IsEnabled = false;

                UpdateTimer.Stop();
            });
        }

        private void HandleUpdateFinish()
        {
            Dispatcher.Invoke(new Action(() =>
            {
                Overlay.Visibility = Visibility.Collapsed;
                InitProgress.Visibility = Visibility.Collapsed;
                InitButton.IsEnabled = true;
                CleanButton.IsEnabled = true;
                PathsButton.IsEnabled = true;
                InitInfo.Content = lastMessage;

                infoUpdateStopWatch.Stop();
                progressUpdateStopWatch.Stop();

                ResetTimestamp();
            }));
        }

        private void HandleErrorMessage(string message)
        {
            lastMessage = message;

            Dispatcher.Invoke(new Action(() =>
            {
                InitInfo.Content = message;
            }));
        }

        private void HandleUpdateMessage(string message)
        {
            lastMessage = message;

            if (!infoUpdateStopWatch.IsRunning || infoUpdateStopWatch.ElapsedMilliseconds > 10)
            {
                Dispatcher.Invoke(new Action(() =>
                {
                    InitInfo.Content = message;
                    infoUpdateStopWatch.Restart();
                }));
            }
        }

        private void HandleProgress(int percentage)
        {
            if (!progressUpdateStopWatch.IsRunning || progressUpdateStopWatch.ElapsedMilliseconds > 20)
            {
                Dispatcher.Invoke(new Action(() =>
                {
                    InitProgress.Value = percentage;
                    progressUpdateStopWatch.Restart();
                }));
            }
        }

        private void InitButton_Click(object sender, RoutedEventArgs e)
        {
            SearchEngine.UpdateDatabaseAsync(FiltersComboBox.SelectedItems.Cast<ConfigProject>().Select(x => x.Path).ToList());
        }
        private void CleanButton_Click(object sender, RoutedEventArgs e)
        {
            CleanDatabase();
            SearchEngine.UpdateDatabaseAsync(FiltersComboBox.SelectedItems.Cast<ConfigProject>().Select(x => x.Path).ToList());
        }

        private void CaseSensitive_Click(object sender, RoutedEventArgs e)
        {
            if(Settings.Default.SearchInstantly || !searchInputChanged)
            {
                Find();
            }

            SaveOptions();
        }

        private void SearchRegEx_Click(object sender, RoutedEventArgs e)
        {
            if (Settings.Default.SearchInstantly || !searchInputChanged)
            {
                Find();
            }

            SaveOptions();
        }

        private void SearchWholeWord_Click(object sender, RoutedEventArgs e)
        {
            if (Settings.Default.SearchInstantly || !searchInputChanged)
            {
                Find();
            }

            SaveOptions();
        }

        private void PathsButton_Click(object sender, RoutedEventArgs e)
        {
            if(SearchEngine.IsBusy)
            {
                return;
            }

            ConfigParser.SaveOldCopy();

            ProjectsWindow newProjectsWindow = new ProjectsWindow(this);
            CreateWindow(newProjectsWindow, "Search configurations", this, true).ShowDialog();

            ConfigParser.SaveConfig();
            UpdateFilters();

            if (ConfigParser.IsConfigChanged())
            {
                UpdateWarning();
                SearchEngine.UpdateDatabaseAsync(FiltersComboBox.SelectedItems.Cast<ConfigProject>().Select(x => x.Path).ToList());
            }
        }

        private void AdvancedButton_Click(object sender, RoutedEventArgs e)
        {
            CreateWindow(new qgrepControls.SearchWindow.SettingsWindow(this), "Advanced settings", this).ShowDialog();
        }

        private void AddToSearchHistory(string searchedString)
        {
            if(searchedString.Length > 0)
            {
                searchHistory.Remove(searchedString);
                searchHistory.Add(searchedString);
            }
        }

        private void UserControl_GotFocus(object sender, RoutedEventArgs e)
        {
            if (ExtensionInterface.WindowOpened)
            {
                SearchInput.Focus();

                string selectedText = ExtensionInterface.GetSelectedText();
                if (selectedText.Length > 0)
                {
                    SearchInput.Text = selectedText;
                    SearchInput.CaretIndex = SearchInput.Text.Length;

                    AddToSearchHistory(SearchInput.Text);
                }
                else
                {
                    SearchInput.SelectAll();
                }

                ExtensionInterface.WindowOpened = false;
            }

            System.Diagnostics.Debug.WriteLine(e.OriginalSource);
        }

        private void SearchInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (SearchInput.Text.Length > 0)
            {
                SearchLabel.Visibility = Visibility.Hidden;
            }
            else
            {
                SearchLabel.Visibility = Visibility.Visible;
            }

            if (IncludeFilesInput.Text.Length > 0)
            {
                IncludeFilesLabel.Visibility = Visibility.Hidden;
            }
            else
            {
                IncludeFilesLabel.Visibility = Visibility.Visible;
            }

            if (ExcludeFilesInput.Text.Length > 0)
            {
                ExcludeFilesLabel.Visibility = Visibility.Hidden;
            }
            else
            {
                ExcludeFilesLabel.Visibility = Visibility.Visible;
            }

            if (FilterResultsInput.Text.Length > 0)
            {
                FilterResultsLabel.Visibility = Visibility.Hidden;
            }
            else
            {
                FilterResultsLabel.Visibility = Visibility.Visible;
            }

            searchInputChanged = true;

            if (Settings.Default.SearchInstantly)
            {
                Find();
            }
        }

        private void UserControl_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Down ||
                e.Key == System.Windows.Input.Key.Up ||
                e.Key == System.Windows.Input.Key.PageUp ||
                e.Key == System.Windows.Input.Key.PageDown)
            {
                if (!(!SearchInput.IsFocused && !IncludeFilesInput.IsFocused && !ExcludeFilesInput.IsFocused && !FilterResultsInput.IsFocused))
                {
                    if(SearchItemsListBox.IsVisible)
                    {
                        if(selectedSearchResult == null)
                        {
                            if(searchResults.Count > 0 && searchResults[0].IsSelected)
                            {
                                selectedSearchResult = searchResults[0];
                            }
                        }

                        if(selectedSearchResult != null)
                        {
                            VirtualizingStackPanel virtualizingStackPanel = GetChildOfType<VirtualizingStackPanel>(SearchItemsListBox);

                            virtualizingStackPanel.BringIndexIntoViewPublic(searchResults.IndexOf(selectedSearchResult));
                            ListBoxItem listBoxItem = SearchItemsListBox.ItemContainerGenerator.ContainerFromItem(selectedSearchResult) as ListBoxItem;
                            listBoxItem?.Focus();

                            e.Handled = true;
                        }
                    }
                    else if(SearchItemsTreeView.IsVisible)
                    {
                        if (selectedSearchResult == null && selectedSearchResultGroup == null)
                        {
                            if (searchResultsGroups.Count > 0 && searchResultsGroups[0].IsSelected)
                            {
                                selectedSearchResultGroup = searchResultsGroups[0];
                            }
                        }

                        if (selectedSearchResult != null || selectedSearchResultGroup != null)
                        {
                            mSuppressRequestBringIntoView = true;

                            SearchResultGroup parentSearchResultGroup = selectedSearchResultGroup;

                            if (selectedSearchResult != null)
                            {
                                parentSearchResultGroup = selectedSearchResult.Parent;
                            }

                            VirtualizingStackPanel virtualizingStackPanel = GetChildOfType<VirtualizingStackPanel>(SearchItemsTreeView);
                            virtualizingStackPanel.BringIndexIntoViewPublic(searchResultsGroups.IndexOf(parentSearchResultGroup));
                            TreeViewItem treeViewItem = SearchItemsTreeView.ItemContainerGenerator.ContainerFromItem(parentSearchResultGroup) as TreeViewItem;

                            if (selectedSearchResult != null)
                            {
                                if (treeViewItem != null)
                                {
                                    treeViewItem.ApplyTemplate();
                                    if (treeViewItem.Template.FindName("ItemsHost", treeViewItem) is ItemsPresenter itemsPresenter)
                                    {
                                        itemsPresenter.ApplyTemplate();
                                    }
                                    else
                                    {
                                        itemsPresenter = GetChildOfType<ItemsPresenter>(treeViewItem);
                                        if (itemsPresenter == null)
                                        {
                                            treeViewItem.UpdateLayout();
                                            itemsPresenter = GetChildOfType<ItemsPresenter>(treeViewItem);
                                        }
                                    }

                                    virtualizingStackPanel = GetChildOfType<VirtualizingStackPanel>(treeViewItem);
                                    virtualizingStackPanel.BringIndexIntoViewPublic(parentSearchResultGroup.SearchResults.IndexOf(selectedSearchResult));

                                    treeViewItem = treeViewItem.ItemContainerGenerator.ContainerFromItem(selectedSearchResult) as TreeViewItem;
                                    treeViewItem?.BringIntoView(new Rect(0, 0, 0, 0));
                                    treeViewItem?.Focus();

                                    e.Handled = true;
                                }
                            }
                            else
                            {
                                treeViewItem?.BringIntoView(new Rect(0, 0, 0, 0));
                                treeViewItem?.Focus();

                                e.Handled = true;
                            }

                            mSuppressRequestBringIntoView = false;
                        }
                    }
                }
            }

            if (e.Key == System.Windows.Input.Key.Enter)
            {
                bool openResult = false;

                if(selectedSearchResult != null)
                {
                    if(selectedSearchResult.Parent != null)
                    {
                        TreeViewItem treeViewItem = SearchItemsTreeView.ItemContainerGenerator.ContainerFromItem(selectedSearchResult.Parent) as TreeViewItem;
                        treeViewItem = treeViewItem.ItemContainerGenerator.ContainerFromItem(selectedSearchResult) as TreeViewItem;
                        if(treeViewItem?.IsFocused ?? false)
                        {
                            openResult = true;
                        }
                    }
                    else
                    {
                        ListBoxItem listBoxItem = SearchItemsListBox.ItemContainerGenerator.ContainerFromItem(selectedSearchResult) as ListBoxItem;
                        if (listBoxItem?.IsFocused ?? false)
                        {
                            openResult = true;
                        }
                    }
                }
                else if(selectedSearchResultGroup != null)
                {
                    TreeViewItem treeViewItem = SearchItemsTreeView.ItemContainerGenerator.ContainerFromItem(selectedSearchResultGroup) as TreeViewItem;
                    if (treeViewItem?.IsFocused ?? false)
                    {
                        openResult = true;
                    }
                }

                if (openResult)
                {
                    OpenSelectedSearchResult();
                }
            }
            else if (e.Key == System.Windows.Input.Key.C && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                if (selectedSearchResult != null)
                {
                    CopyText(selectedSearchResult);
                }

                if (selectedSearchResultGroup != null)
                {
                    Clipboard.SetText(selectedSearchResultGroup.File);
                }
            }
        }

        private void Colors_Click(object sender, RoutedEventArgs e)
        {
            CreateWindow(new qgrepControls.ColorsWindow.ColorsWindow(this), "Color settings", this, true).ShowDialog();
        }

        private void SearchInput_MouseEnter(object sender, RoutedEventArgs e)
        {
        }

        private void IncludeRegEx_Click(object sender, RoutedEventArgs e)
        {
            if (Settings.Default.SearchInstantly || !searchInputChanged)
            {
                Find();
            }

            SaveOptions();
        }

        private void ExcludeRegEx_Click(object sender, RoutedEventArgs e)
        {
            if (Settings.Default.SearchInstantly || !searchInputChanged)
            {
                Find();
            }

            SaveOptions();
        }

        private void FilterRegEx_Click(object sender, RoutedEventArgs e)
        {
            if (Settings.Default.SearchInstantly || !searchInputChanged)
            {
                Find();
            }

            SaveOptions();
        }

        private void FiltersComboBox_ItemSelectionChanged(object sender, Xceed.Wpf.Toolkit.Primitives.ItemSelectionChangedEventArgs e)
        {
            List<string> searchFilters = new List<string>();
            foreach(ConfigProject configProject in FiltersComboBox.SelectedItems)
            {
                searchFilters.Add(configProject.Name);
            }

            Settings.Default.SearchFilters = string.Join(",", searchFilters);
            Settings.Default.Save();

            if (Settings.Default.SearchInstantly || !searchInputChanged)
            {
                Find();
            }
        }

        private SearchResult GetSearchResultFromMenuItem(object sender)
        {
            MenuItem menuItem = sender as MenuItem;
            if (menuItem != null)
            {
                DockPanel resultPanel = (menuItem.Parent as ContextMenu)?.PlacementTarget as DockPanel;
                if (resultPanel != null)
                {
                    return resultPanel.DataContext as SearchResult;
                }
            }

            return null;
        }

        private SearchResultGroup GetSearchGroupFromMenuItem(object sender)
        {
            MenuItem menuItem = sender as MenuItem;
            if (menuItem != null)
            {
                DockPanel resultPanel = (menuItem.Parent as ContextMenu)?.PlacementTarget as DockPanel;
                if (resultPanel != null)
                {
                    return resultPanel.DataContext as SearchResultGroup;
                }
            }

            return null;
        }

        private void MenuGoTo_Click(object sender, RoutedEventArgs e)
        {
            SearchResult searchResult = GetSearchResultFromMenuItem(sender);
            if (searchResult != null)
            {
                OpenSearchResult(searchResult);
            }

            SearchResultGroup searchGroup = GetSearchGroupFromMenuItem(sender);
            if (searchGroup != null)
            {
                OpenSearchGroup(searchGroup);
            }
        }

        private void CopyText(SearchResult searchResult)
        {
            string text = searchResult.BeginText + searchResult.HighlightedText + searchResult.EndText;

            if (Settings.Default.TrimSpacesOnCopy)
            {
                text = text.Trim(new char[] { ' ', '\t' });
            }

            Clipboard.SetText(text);
        }

        private void MenuCopyText_Click(object sender, RoutedEventArgs e)
        {
            SearchResult searchResult = GetSearchResultFromMenuItem(sender);
            if(searchResult != null)
            {
                CopyText(searchResult);
            }
        }

        private void MenuCopyFullPath_Click(object sender, RoutedEventArgs e)
        {
            SearchResult searchResult = GetSearchResultFromMenuItem(sender);
            if (searchResult != null)
            {
                int lastIndex = searchResult.FileAndLine.LastIndexOf('(');
                string text = searchResult.FileAndLine.Substring(0, lastIndex);

                Clipboard.SetText(text);
            }

            SearchResultGroup searchGroup = GetSearchGroupFromMenuItem(sender);
            if (searchGroup != null)
            {
                Clipboard.SetText(searchGroup.File);
            }
        }

        private void MenuCopyResult_Click(object sender, RoutedEventArgs e)
        {
            SearchResult searchResult = GetSearchResultFromMenuItem(sender);
            if (searchResult != null)
            {
                Clipboard.SetText(searchResult.FullResult);
            }
        }

        private void MenuCopyAllResults_Click(object sender, RoutedEventArgs e)
        {
            string allResults = LastResults;
            allResults = allResults.Replace("\xB0", "");
            allResults = ConfigParser.RemovePaths(allResults);
            Clipboard.SetText(allResults);
        }

        private void TreeViewItem_RequestBringIntoView(object sender, RequestBringIntoViewEventArgs e)
        {
            // Ignore re-entrant calls
            if (mSuppressRequestBringIntoView)
                return;

            // Cancel the current scroll attempt
            e.Handled = true;

            // Call BringIntoView using a rectangle that extends into "negative space" to the left of our
            // actual control. This allows the vertical scrolling behaviour to operate without adversely
            // affecting the current horizontal scroll position.
            mSuppressRequestBringIntoView = true;

            TreeViewItem tvi = sender as TreeViewItem;
            if (tvi != null)
            {
                Rect newTargetRect = new Rect(-1000, 0, tvi.ActualWidth + 1000, tvi.ActualHeight);
                tvi.BringIntoView(newTargetRect);
            }

            mSuppressRequestBringIntoView = false;
        }
        private bool mSuppressRequestBringIntoView;

        // Correctly handle programmatically selected items
        private void OnSelected(object sender, RoutedEventArgs e)
        {
            ((TreeViewItem)sender).BringIntoView();
            e.Handled = true;
        }

        public MainWindow CreateWindow(UserControl userControl, string title, UserControl owner, bool resizeable = false)
        {
            MainWindow newWindow = new MainWindow
            {
                Title = title,
                Content = userControl,
                SizeToContent = SizeToContent.Manual,
                ResizeMode = resizeable ? ResizeMode.CanResizeWithGrip : ResizeMode.NoResize,
                Width = userControl.Width + 37,
                Height = userControl.Height + 37,
                Owner = qgrepSearchWindowControl.FindAncestor<Window>(owner),
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
            };

            if(resizeable)
            {
                userControl.Width = double.NaN;
                userControl.Height = double.NaN;
            }

            Dictionary<string, object> resources = GetResourcesFromColorScheme();
            foreach (var resource in resources)
            {
                userControl.Resources[resource.Key] = resource.Value;

                if (newWindow != null)
                {
                    newWindow.Resources[resource.Key] = resource.Value;
                }
            }

            return newWindow;
        }

        private void OpenHistoryPopup()
        {
            if (searchHistory.Count > 0)
            {
                HistoryPanel.Items.Clear();

                for(int i = searchHistory.Count - 1; i >= 0; i--)
                {
                    ListBoxItem listBoxItem = new ListBoxItem() { Content = searchHistory[i] };
                    listBoxItem.PreviewMouseDown += HistoryItem_MouseDown;
                    listBoxItem.PreviewKeyDown += HistoryItem_KeyDown;

                    HistoryPanel.Items.Add(listBoxItem);
                }

                Point relativePosition = HistoryButton.PointToScreen(new Point(0, 0));

                HistoryPopup.IsOpen = true;
            }
        }

        private void HistoryButton_Click(object sender, RoutedEventArgs e)
        {
            OpenHistoryPopup();
        }

        public CustomPopupPlacement[] CustomPopupPlacementCallback(Size popupSize, Size targetSize, Point offset)
        {
            CustomPopupPlacement placement = new CustomPopupPlacement(
                new Point(-popupSize.Width + targetSize.Width, targetSize.Height),
                PopupPrimaryAxis.None
            );

            return new CustomPopupPlacement[] { placement };
        }

        private void SearchInput_LostFocus(object sender, RoutedEventArgs e)
        {
            if(SearchInput.Text.Length > 0)
            {
                AddToSearchHistory(SearchInput.Text);
            }
        }

        void OpenHistoryItem(ListBoxItem listBoxItem)
        {
            if (listBoxItem != null)
            {
                SearchInput.Text = listBoxItem.Content as string;
                SearchInput.CaretIndex = SearchInput.Text.Length;
                AddToSearchHistory(SearchInput.Text);
            }

            SearchInput.Focus();
            HistoryPopup.IsOpen = false;
        }

        private void HistoryItem_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            OpenHistoryItem(sender as ListBoxItem);
            e.Handled = true;
        }

        private void HistoryItem_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if(e.Key == Key.Enter)
            {
                OpenHistoryItem(sender as ListBoxItem);
                e.Handled = true;
            }
            else if(e.Key == Key.Escape)
            {
                SearchInput.Focus();
                HistoryPopup.IsOpen = false;
            }
        }

        private bool IgnoreFocusEvents = false;
        private void SearchInput_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Enter)
            {
                AddToSearchHistory(SearchInput.Text);
                Find();
            }
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            AddToSearchHistory(SearchInput.Text);
            Find();
        }

        private void UserControl_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            FrameworkElement frameworkElement = e.OriginalSource as FrameworkElement;
            if (frameworkElement != null)
            {
                SearchResult searchResult = frameworkElement.DataContext as SearchResult;
                if (searchResult != null)
                {
                    selectedSearchResultGroup = null;
                    selectedSearchResult = searchResult;
                }

                SearchResultGroup searchResultGroup = frameworkElement.DataContext as SearchResultGroup;
                if (searchResultGroup != null)
                {
                    selectedSearchResultGroup = searchResultGroup;
                    selectedSearchResult = null;
                }
                System.Diagnostics.Debug.WriteLine(frameworkElement.DataContext);
            }
        }

        public void ToggleCaseSensitive()
        {
            Dispatcher.Invoke(() =>
            {
                SearchCaseSensitive.IsChecked = !SearchCaseSensitive.IsChecked;
                SaveOptions();
                UpdateFromSettings();
            });
        }

        public void ToggleWholeWord()
        {
            Dispatcher.Invoke(() =>
            {
                SearchWholeWord.IsChecked = !SearchWholeWord.IsChecked;
                SaveOptions();
                UpdateFromSettings();
            });
        }

        public void ToggleRegEx()
        {
            Dispatcher.Invoke(() =>
            {
                if(IncludeFilesInput.IsFocused)
                {
                    IncludeRegEx.IsChecked = !IncludeRegEx.IsChecked;
                }
                else if(ExcludeFilesInput.IsFocused)
                {
                    ExcludeRegEx.IsChecked = !ExcludeRegEx.IsChecked;
                }
                else if(FilterResultsInput.IsFocused)
                {
                    FilterRegEx.IsChecked = !FilterRegEx.IsChecked;
                }
                else
                {
                    SearchRegEx.IsChecked = !SearchRegEx.IsChecked;
                }

                SaveOptions();
                UpdateFromSettings();
            });
        }

        public void ToggleIncludeFiles()
        {
            Dispatcher.Invoke(() =>
            {
                Settings.Default.ShowIncludes = !Settings.Default.ShowIncludes;
                Settings.Default.Save();
                UpdateFromSettings();

                if(IncludeFilesInput.IsVisible)
                {
                    IncludeFilesInput.Focus();
                }
            });
        }

        public void ToggleExcludeFiles()
        {
            Dispatcher.Invoke(() =>
            {
                Settings.Default.ShowExcludes = !Settings.Default.ShowExcludes;
                Settings.Default.Save();
                UpdateFromSettings();

                if (ExcludeFilesInput.IsVisible)
                {
                    ExcludeFilesInput.Focus();
                }
            });
        }

        public void ToggleFilterResults()
        {
            Dispatcher.Invoke(() =>
            {
                Settings.Default.ShowFilter = !Settings.Default.ShowFilter;
                Settings.Default.Save();
                UpdateFromSettings();

                if (FilterResultsInput.IsVisible)
                {
                    FilterResultsInput.Focus();
                }
            });
        }

        public void ShowHistory()
        {
            Dispatcher.Invoke(() =>
            {
                OpenHistoryPopup();
                if(HistoryPanel.Items.Count > 0)
                {
                    (HistoryPanel.Items[0] as ListBoxItem).IsSelected = true;
                    (HistoryPanel.Items[0] as ListBoxItem).Focus();
                }
            });
        }

        private void KeyboardButton_Click(object sender, RoutedEventArgs e)
        {
            HotkeysWindow hotkeysWindow = new HotkeysWindow(this);
            MainWindow hotkeysDialog = CreateWindow(hotkeysWindow, "Edit hotkeys", this);
            hotkeysWindow.Dialog = hotkeysDialog;
            hotkeysDialog.ShowDialog();

            if (hotkeysWindow.IsOk)
            {
                bindings = hotkeysWindow.GetBindings();
                ExtensionInterface.SaveKeyBindings(bindings);
                ExtensionInterface.ApplyKeyBindings(bindings);
                UpdateShortcutHints();
            }
        }
    }
}