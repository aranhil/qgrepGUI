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

namespace qgrepControls.SearchWindow
{
    partial class SearchResultGroup : INotifyPropertyChanged
    {
        private bool isSelected = false;
        private bool isExpanded = false;

        public int Index { get; set; }
        public string File { get; set; } = "";
        public string TrimmedFile { get; set; } = "";
        public ObservableCollection<SearchResult> SearchResults { get; set; } = new ObservableCollection<SearchResult>();

        public bool IsSelected
        {
            get
            {
                return isSelected;
            }
            set
            {
                isSelected = value;
                OnPropertyChanged();
            }
        }
        public bool IsExpanded
        {
            get
            {
                return isExpanded;
            }
            set
            {
                isExpanded = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    partial class SearchResult: INotifyPropertyChanged
    {
        private bool isSelected;

        public int Index { get; set; }
        public string File { get; set; }
        public string Line { get; set; }
        public string TrimmedFileAndLine { get; set; }
        public string FileAndLine { get; set; }
        public string BeginText { get; set; }
        public string EndText { get; set; }
        public string HighlightedText { get; set; }
        public string FullResult { get; set; }
        public SearchResultGroup Parent { get; set; }

        public bool IsSelected
        {
            get
            {
                return isSelected;
            }
            set
            {
                isSelected = value;
                OnPropertyChanged();
            }
        }
        public bool IsExpanded
        {
            get
            {
                return false;
            }
            set
            {
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

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

        public string Errors = "";
        private System.Timers.Timer UpdateTimer = null;
        private string LastResults = "";

        ObservableCollection<SearchResult> searchResults = new ObservableCollection<SearchResult>();
        int selectedSearchResult = -1;

        ObservableCollection<SearchResultGroup> searchResultsGroups = new ObservableCollection<SearchResultGroup>();
        int selectedSearchResultGroup = -1;

        List<string> searchHistory = new List<string>();
        bool searchInputChanged = true;
        string lastSearchedString = "";

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
            SearchEngine.StartCallback = HandleSearchStart;
            SearchEngine.FinishCallback = HandleSearchFinish;
        }

        private void ResetTimestamp()
        {
            Settings.Default.LastUpdated = DateTime.Now;
            Settings.Default.Save();
            StartTimer();
        }

        private void StartTimer()
        {
            UpdateTimer = new System.Timers.Timer(10000);
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
            if (!EngineBusy)
            {
                if (Settings.Default["LastUpdated"] != null)
                {
                    InitInfo.Content = "Last updated: " + GetTimeAgoString(Settings.Default.LastUpdated);
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
            Settings.Default.ShowExcludes = ExcludeRegEx.IsChecked == true;
            Settings.Default.FilterRegEx = FilterRegEx.IsChecked == true;

            Settings.Default.Save();
        }

        private int GetGroupingMode()
        {
            if(SearchInput.Text.Length == 0 && IncludeFilesInput.Text.Length != 0)
            {
                return 0;
            }

            return Settings.Default.GroupingIndex;
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
            }
            else
            {
                SearchResultGroup newSearchGroup = new SearchResultGroup()
                {
                    Index = 0,
                    File = searchResult.File,
                    TrimmedFile = ConfigParser.RemovePaths(searchResult.File),
                    SearchResults = new ObservableCollection<SearchResult> { searchResult }
                };

                searchResultGroups.Add(newSearchGroup);
            }
        }

        private void HandleSearchStart()
        {
            Dispatcher.Invoke(() =>
            {
                newSearch = true;

                int groupingMode = GetGroupingMode();
                SearchItemsListBox.Visibility = groupingMode == 0 ? Visibility.Visible : Visibility.Collapsed;
                SearchItemsTreeView.Visibility = groupingMode != 0 ? Visibility.Visible : Visibility.Collapsed;
            });
        }

        private void AddResultsBatch()
        {
            if (GetGroupingMode() == 0)
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

                InfoLabel.Content = string.Format("Showing {0} result(s) for \"{1}\"", searchResults.Count, lastSearchedString);
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
                InfoLabel.Content = string.Format("Showing {0} result(s) for \"{1}\"", searchResults.Count, lastSearchedString);
            }
        }

        private void HandleSearchResult(string file, string lineNumber, string beginText, string highlight, string endText)
        {
            if (!SearchEngine.IsSearchQueued)
            {
                Dispatcher.Invoke(() =>
                {
                    string fileAndLine = file + "(" + lineNumber + ")";
                    string trimmedFileAndLine = ConfigParser.RemovePaths(fileAndLine);

                    SearchResult newSearchResult = new SearchResult()
                    {
                        Index = 0,
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

                    if (GetGroupingMode() == 1)
                    {
                        AddSearchResultToGroups(newSearchResult, newSearchResultGroups);
                    }

                    if (newSearchResults.Count >= 100)
                    {
                        AddResultsBatch();
                    }
                });
            }
        }

        private void HandleSearchFinish()
        {
            Dispatcher.Invoke(() =>
            {
                if(!SearchEngine.IsSearchQueued)
                {
                    AddResultsBatch();
                }
                else
                {
                    newSearch = false;
                    newSearchResultGroups.Clear();
                    newSearchResults.Clear();
                }

                if(GetGroupingMode() == 1)
                {
                    ProcessTreeCollapsingAfterPopulation();
                }
            });
        }

        private void Find()
        {
            string configs = "";
            for (int i = 0; i < FiltersComboBox.SelectedItems.Count; i++)
            {
                ConfigProject configProject = FiltersComboBox.SelectedItems[i] as ConfigProject;
                if (configProject != null)
                {
                    configs += configProject.Path;
                    if (i < FiltersComboBox.SelectedItems.Count - 1)
                    {
                        configs += ",";
                    }
                }
            }

            if (SearchInput.Text.Length != 0)
            {
                lastSearchedString = SearchInput.Text;

                if (newSearch)
                {
                    newSearchResults = new ObservableCollection<SearchResult>();
                }

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
                    Configs = configs,
                };

                SearchEngine.SearchAsync(searchOptions);
            }
            else
            {
                searchResults.Clear();
            }
        }

        private void SearchResult_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            DockPanel dockPanel = sender as DockPanel;
            if (dockPanel != null)
            {
                SearchResult selectedResult = GetSelectedSearchResult();
                if (selectedResult != null)
                {
                    selectedResult.IsSelected = false;
                }

                SearchResultGroup selectedGroup = GetSelectedSearchGroup();
                if (selectedGroup != null)
                {
                    selectedGroup.IsSelected = false;
                }

                SearchResult newSelectedItem = dockPanel.DataContext as SearchResult;
                if (newSelectedItem != null)
                {
                    newSelectedItem.IsSelected = true;
                    selectedSearchResult = newSelectedItem.Index;
                    selectedSearchResultGroup = -1;
                }

                if (!Settings.Default.SearchInstantly)
                {
                    if (GetGroupingMode() == 0)
                    {
                        SearchItemsListBox.Focus();
                        e.Handled = true;
                    }
                }

                if (e.ClickCount == 2)
                {
                    OpenSelectedStackPanel();
                }
            }
        }

        private void SearchGroup_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            DockPanel dockPanel = sender as DockPanel;
            if (dockPanel != null)
            {
                SearchResult selectedResult = GetSelectedSearchResult();
                if (selectedResult != null)
                {
                    selectedResult.IsSelected = false;
                }

                SearchResultGroup selectedGroup = GetSelectedSearchGroup();
                if (selectedGroup != null)
                {
                    selectedGroup.IsSelected = false;
                }

                SearchResultGroup newSelectedItem = dockPanel.DataContext as SearchResultGroup;
                if (newSelectedItem != null)
                {
                    newSelectedItem.IsSelected = true;
                    selectedSearchResultGroup = newSelectedItem.Index;
                    selectedSearchResult = -1;
                }

                if (e.ClickCount == 2)
                {
                    TreeViewItem treeViewItem = FindAncestor<TreeViewItem>(dockPanel);
                    if(treeViewItem != null)
                    {
                        treeViewItem.IsExpanded = !treeViewItem.IsExpanded;
                    }
                }
            }

            e.Handled = true;
        }

        private SearchResult GetSelectedSearchResult()
        {
            if(GetGroupingMode() == 0)
            {
                if (selectedSearchResult >= 0 && selectedSearchResult < searchResults.Count)
                {
                    return searchResults[selectedSearchResult];
                }
            }
            else
            {
                foreach(SearchResultGroup searchResultGroup in searchResultsGroups)
                {
                    foreach(SearchResult searchResult in searchResultGroup.SearchResults)
                    {
                        if(searchResult.Index == selectedSearchResult)
                        {
                            return searchResult;
                        }
                    }
                }
            }

            return null;
        }
        private SearchResultGroup GetSelectedSearchGroup()
        {
            foreach (SearchResultGroup searchResultGroup in searchResultsGroups)
            {
                if (searchResultGroup.Index == selectedSearchResultGroup)
                {
                    return searchResultGroup;
                }
            }

            return null;
        }

        private void OpenSelectedStackPanel()
        {
            SearchResult searchResult = GetSelectedSearchResult();
            if (searchResult != null)
            {
                OpenSearchResult(searchResult);
            }

            SearchResultGroup searchGroup = GetSelectedSearchGroup();
            if (searchGroup != null)
            {
                OpenSearchGroup(searchGroup);
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

        private Stopwatch sw = new Stopwatch();
        private bool EngineBusy = false;
        private bool QueueFind = false;
        private bool QueueUpdate = false;

        private void ProcessErrorMessage(string message)
        {
            if(message.Length == 0)
            {
                return;
            }

            Dispatcher.Invoke(new Action(() =>
            {
                Errors += "[" + DateTime.Now.ToString() + "] " + message;
                InitInfo.Content = message;
            }));
        }

        private bool ProcessInitMessage(string message)
        {
            if (message.Contains("%"))
            {
                int percentageIndex = message.IndexOf("%");
                int numberIndex = percentageIndex - 1;
                for (; numberIndex >= 0; numberIndex--)
                {
                    if (!"0123456789".Contains(message[numberIndex]))
                    {
                        break;
                    }
                }

                string progressString = message.Substring(numberIndex + 1, percentageIndex - numberIndex - 1);
                int progress = Int32.Parse(progressString);

                if (sw.ElapsedMilliseconds > 10)
                {
                    Dispatcher.Invoke(new Action(() =>
                    {
                        InitProgress.Value = progress;
                        sw.Restart();
                    }));
                }

            }
            else
            {
                Dispatcher.Invoke(new Action(() =>
                {
                    InitInfo.Content = message;
                }));
            }

            return false;
        }

        private void ProcessQueue()
        {
            if (QueueFind)
            {
                Find();
                QueueFind = false;
            }
            else if(QueueUpdate)
            {
                UpdateDatabase();
                QueueUpdate = false;
            }
        }

        public void CleanDatabase()
        {
            try
            {
                foreach(ConfigProject configProject in ConfigParser.ConfigProjects)
                {
                    string directory = System.IO.Path.GetDirectoryName(configProject.Path);
                    File.Delete(directory + "\\" + Name + ".qgd");
                    File.Delete(directory + "\\" + Name + ".qgf");
                }
            }
            catch(Exception ex)
            {
                Dispatcher.Invoke(new Action(() =>
                {
                    ProcessErrorMessage("Cannot delete config files: " + ex.Message + "\n");
                }));
            }
        }

        public void UpdateDatabase()
        {
            UpdateTimer.Start();

            if (EngineBusy)
            {
                QueueUpdate = true;
                return;
            }

            if (!sw.IsRunning)
            {
                sw.Start();
            }

            EngineBusy = true;

            Task.Run(() =>
            {
                foreach(ConfigProject configProject in ConfigParser.ConfigProjects)
                {
                    QGrepWrapper.StringCallback stringCallback = new QGrepWrapper.StringCallback(ProcessInitMessage);
                    QGrepWrapper.ErrorCallback errorsCallback = new QGrepWrapper.ErrorCallback(ProcessErrorMessage);
                    List<string> parameters = new List<string>
                    {
                        "qgrep",
                        "update",
                        configProject.Path
                    };
                    QGrepWrapper.CallQGrepAsync(parameters, stringCallback, errorsCallback, null);
                }

                Dispatcher.Invoke(new Action(() =>
                {
                    Overlay.Visibility = Visibility.Collapsed;
                    InitProgress.Visibility = Visibility.Collapsed;
                    InitButton.IsEnabled = true;
                    CleanButton.IsEnabled = true;
                    PathsButton.IsEnabled = true;
                    sw.Stop();

                    EngineBusy = false;
                    QueueFind = true;
                    ProcessQueue();
                    ResetTimestamp();
                }));
            });

            Overlay.Visibility = Visibility.Visible;
            InitProgress.Visibility = Visibility.Visible;
            InitProgress.Value = 0;
            InitButton.IsEnabled = false;
            CleanButton.IsEnabled = false;
            PathsButton.IsEnabled = false;
        }

        private void InitButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateDatabase();
        }
        private void CleanButton_Click(object sender, RoutedEventArgs e)
        {
            CleanDatabase();
            UpdateDatabase();
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
            if(EngineBusy)
            {
                return;
            }

            ProjectsWindow newProjectsWindow = new ProjectsWindow(this);
            CreateWindow(newProjectsWindow, "Search configurations", this).ShowDialog();

            if(!newProjectsWindow.NothingChanged)
            {
                UpdateWarning();
                UpdateDatabase();
            }
        }

        private void AdvancedButton_Click(object sender, RoutedEventArgs e)
        {
            CreateWindow(new qgrepControls.SearchWindow.SettingsWindow(this), "Advanced settings", this).ShowDialog();
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

                    if ((searchHistory.Count == 0 || !searchHistory.Contains(SearchInput.Text)) && SearchInput.Text.Length > 0)
                    {
                        searchHistory.Add(selectedText);
                    }
                }
                else
                {
                    SearchInput.SelectAll();
                }

                ExtensionInterface.WindowOpened = false;
            }

            //System.Diagnostics.Debug.Write(e.OriginalSource);
            //System.Diagnostics.Debug.Write(" ");
            //System.Diagnostics.Debug.Write(e.Source);
            //System.Diagnostics.Debug.Write(" ");
            //System.Diagnostics.Debug.WriteLine(e.RoutedEvent);
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
            //if (GetGroupingMode() == 0)
            //{
            //    if (searchResults.Count > 0)
            //    {
            //        if (e.Key == System.Windows.Input.Key.Up)
            //        {
            //            e.Handled = true;
            //        }
            //        else if (e.Key == System.Windows.Input.Key.Down)
            //        {
            //            if (!(!SearchInput.IsFocused && !IncludeFilesInput.IsFocused && !ExcludeFilesInput.IsFocused && !FilterResultsInput.IsFocused))
            //            {
            //                VirtualizingStackPanel virtualizingStackPanel = GetChildOfType<VirtualizingStackPanel>(SearchItemsListBox);

            //                virtualizingStackPanel.BringIndexIntoViewPublic(0);
            //                ListBoxItem listBoxItem = SearchItemsListBox.ItemContainerGenerator.ContainerFromItem(searchResults[0]) as ListBoxItem;

            //                //listBoxItem?.BringIntoView();
            //                listBoxItem?.Focus();
            //                //Keyboard.Focus(listBoxItem);
            //                SearchItemsListBox.SelectedIndex = 0;
            //                e.Handled = true;
            //            }
            //        }
            //    }
            //}
                    //        else if (e.Key == System.Windows.Input.Key.PageUp)
                    //        {
                    //            if (searchResults.Count > 0)
                    //            {
                    //                int newIndex = Math.Max(0, selectedSearchResult - (int)(Math.Floor(SearchItemsListBox.ActualHeight / 16.0f)));
                    //                if(newIndex >= 0 && newIndex < searchResults.Count)
                    //                {
                    //                    newSelectedSearchResult = searchResults[newIndex];
                    //                }
                    //            }

                    //            e.Handled = true;
                    //        }
                    //        else if (e.Key == System.Windows.Input.Key.PageDown)
                    //        {
                    //            if (searchResults.Count > 0)
                    //            {
                    //                int newIndex = Math.Min(searchResults.Count - 1, selectedSearchResult + (int)(Math.Floor(SearchItemsListBox.ActualHeight / 16.0f)));
                    //                if (newIndex > 0 && newIndex < searchResults.Count)
                    //                {
                    //                    newSelectedSearchResult = searchResults[newIndex];
                    //                }
                    //            }

                    //            e.Handled = true;
                    //        }
                    //        else if (e.Key == System.Windows.Input.Key.Home)
                    //        {
                    //            if (searchResults.Count > 0)
                    //            {
                    //                newSelectedSearchResult = searchResults[0];
                    //            }

                    //            e.Handled = true;
                    //        }
                    //        else if (e.Key == System.Windows.Input.Key.End)
                    //        {
                    //            if (searchResults.Count > 0)
                    //            {
                    //                newSelectedSearchResult = searchResults[searchResults.Count - 1];
                    //            }

                    //            e.Handled = true;
                    //        }
                    //    }
                    //}
                    //else if (GetGroupingMode() == 1)
                    //{
                    //    if (e.Key == System.Windows.Input.Key.Down)
                    //    {
                    //        if (selectedSearchResult < 0 && selectedSearchResultGroup < 0 && searchResultsGroups.Count > 0)
                    //        {
                    //            newSelectedSearchResultGroup = searchResultsGroups[0];
                    //        }
                    //        else
                    //        {
                    //            newSelectedSearchResult = oldSelectedSearchResult;
                    //            newSelectedSearchResultGroup = oldSelectedSearchResultGroup;

                    //            GetNextSearchResultOrGroup(ref newSelectedSearchResultGroup, ref newSelectedSearchResult);
                    //        }

                    //        e.Handled = true;
                    //    }
                    //    else if (e.Key == System.Windows.Input.Key.Up)
                    //    {
                    //        if (selectedSearchResult < 0 && selectedSearchResultGroup < 0 && searchResultsGroups.Count > 0)
                    //        {
                    //            newSelectedSearchResultGroup = searchResultsGroups[0];
                    //        }
                    //        else
                    //        {
                    //            newSelectedSearchResult = oldSelectedSearchResult;
                    //            newSelectedSearchResultGroup = oldSelectedSearchResultGroup;

                    //            GetPreviousSearchResultOrGroup(ref newSelectedSearchResultGroup, ref newSelectedSearchResult);
                    //        }

                    //        e.Handled = true;
                    //    }
                    //    else if (e.Key == System.Windows.Input.Key.PageDown)
                    //    {
                    //        if (selectedSearchResult < 0 && selectedSearchResultGroup < 0 && searchResultsGroups.Count > 0)
                    //        {
                    //            newSelectedSearchResultGroup = searchResultsGroups[0];
                    //        }
                    //        else
                    //        {
                    //            newSelectedSearchResult = oldSelectedSearchResult;
                    //            newSelectedSearchResultGroup = oldSelectedSearchResultGroup;

                    //            int steps = (int)(Math.Floor(SearchItemsTreeView.ActualHeight / 16.0f)) - 1;
                    //            for(int i = 0; i < steps; i++)
                    //            {
                    //                GetNextSearchResultOrGroup(ref newSelectedSearchResultGroup, ref newSelectedSearchResult);
                    //            }
                    //        }

                    //        e.Handled = true;
                    //    }
                    //    else if (e.Key == System.Windows.Input.Key.PageUp)
                    //    {
                    //        if (selectedSearchResult < 0 && selectedSearchResultGroup < 0 && searchResultsGroups.Count > 0)
                    //        {
                    //            newSelectedSearchResultGroup = searchResultsGroups[0];
                    //        }
                    //        else
                    //        {
                    //            newSelectedSearchResult = oldSelectedSearchResult;
                    //            newSelectedSearchResultGroup = oldSelectedSearchResultGroup;

                    //            int steps = (int)(Math.Floor(SearchItemsTreeView.ActualHeight / 16.0f)) - 1;
                    //            for (int i = 0; i < steps; i++)
                    //            {
                    //                GetPreviousSearchResultOrGroup(ref newSelectedSearchResultGroup, ref newSelectedSearchResult);
                    //            }
                    //        }

                    //        e.Handled = true;
                    //    }
                    //    else if (e.Key == System.Windows.Input.Key.Home)
                    //    {
                    //        if (searchResultsGroups.Count > 0)
                    //        {
                    //            newSelectedSearchResultGroup = searchResultsGroups[0];
                    //        }

                    //        e.Handled = true;
                    //    }
                    //    else if (e.Key == System.Windows.Input.Key.End)
                    //    {
                    //        if (searchResultsGroups.Count > 0)
                    //        {
                    //            if(searchResultsGroups[searchResultsGroups.Count - 1].IsExpanded && searchResultsGroups[searchResultsGroups.Count - 1].SearchResults.Count > 0)
                    //            {
                    //                newSelectedSearchResult = searchResultsGroups[searchResultsGroups.Count - 1].SearchResults.Last();
                    //            }
                    //            else
                    //            {
                    //                newSelectedSearchResultGroup = searchResultsGroups[searchResultsGroups.Count - 1];
                    //            }
                    //        }

                    //        e.Handled = true;
                    //    }
                    //    else if (e.Key == System.Windows.Input.Key.Left)
                    //    {
                    //        if(oldSelectedSearchResultGroup != null)
                    //        {
                    //            oldSelectedSearchResultGroup.IsExpanded = false;
                    //            newSelectedSearchResultGroup = oldSelectedSearchResultGroup;
                    //        }
                    //        else if(oldSelectedSearchResult != null)
                    //        {
                    //            newSelectedSearchResultGroup = oldSelectedSearchResult.Parent;
                    //            newSelectedSearchResult = null;
                    //        }

                    //        e.Handled = true;
                    //    }
                    //    else if (e.Key == System.Windows.Input.Key.Right)
                    //    {
                    //        if(oldSelectedSearchResultGroup != null)
                    //        {
                    //            if(!oldSelectedSearchResultGroup.IsExpanded)
                    //            {
                    //                oldSelectedSearchResultGroup.IsExpanded = true;
                    //                newSelectedSearchResultGroup = oldSelectedSearchResultGroup;
                    //            }
                    //            else if(oldSelectedSearchResultGroup.SearchResults.Count > 0)
                    //            {
                    //                newSelectedSearchResult = oldSelectedSearchResultGroup.SearchResults[0];
                    //                newSelectedSearchResultGroup = null;
                    //            }
                    //            else
                    //            {
                    //                newSelectedSearchResultGroup = oldSelectedSearchResultGroup;
                    //            }
                    //        }
                    //        else if(oldSelectedSearchResult != null)
                    //        {
                    //            newSelectedSearchResult = oldSelectedSearchResult;
                    //        }

                    //        e.Handled = true;
                    //    }
                    //}

                    //if (e.Handled)
                    //{
                    //    if (oldSelectedSearchResult != null)
                    //    {
                    //        oldSelectedSearchResult.IsSelected = false;
                    //    }

                    //    if (oldSelectedSearchResultGroup != null)
                    //    {
                    //        oldSelectedSearchResultGroup.IsSelected = false;
                    //    }

                    //    if (!Settings.Default.SearchInstantly)
                    //    {
                    //        if (GetGroupingMode() == 0)
                    //        {
                    //            if(!SearchItemsListBox.IsFocused)
                    //            {
                    //                if(selectedSearchResult >= 0 && selectedSearchResult < searchResults.Count)
                    //                {
                    //                    newSelectedSearchResult = searchResults[selectedSearchResult];
                    //                }
                    //            }

                    //            SearchItemsListBox.Focus();
                    //        }
                    //        else if (GetGroupingMode() == 1)
                    //        {
                    //            if ((Keyboard.FocusedElement as TreeViewItem) == null)
                    //            {
                    //                if (oldSelectedSearchResult != null || oldSelectedSearchResultGroup != null)
                    //                {
                    //                    newSelectedSearchResult = oldSelectedSearchResult;
                    //                    newSelectedSearchResultGroup = oldSelectedSearchResultGroup;
                    //                }
                    //            }

                    //            SearchItemsTreeView.Focus();
                    //        }
                    //    }

                    //    if (newSelectedSearchResult != null)
                    //    {
                    //        selectedSearchResult = newSelectedSearchResult.Index;
                    //        newSelectedSearchResult.IsSelected = true;
                    //    }
                    //    else
                    //    {
                    //        selectedSearchResult = -1;
                    //    }

                    //    if (newSelectedSearchResultGroup != null)
                    //    {
                    //        selectedSearchResultGroup = newSelectedSearchResultGroup.Index;
                    //        newSelectedSearchResultGroup.IsSelected = true;
                    //    }
                    //    else
                    //    {
                    //        selectedSearchResultGroup = -1;
                    //    }

                    //    if (GetGroupingMode() == 0)
                    //    {
                    //        VirtualizingStackPanel virtualizingStackPanel = GetChildOfType<VirtualizingStackPanel>(SearchItemsListBox);

                    //        virtualizingStackPanel.BringIndexIntoViewPublic(newSelectedSearchResult.Index);
                    //        ListBoxItem listBoxItem = SearchItemsListBox.ItemContainerGenerator.ContainerFromItem(newSelectedSearchResult) as ListBoxItem;

                    //        listBoxItem?.BringIntoView();
                    //    }
                    //    else if(GetGroupingMode() == 1)
                    //    {
                    //        mSuppressRequestBringIntoView = true;

                    //        VirtualizingStackPanel virtualizingStackPanel = GetChildOfType<VirtualizingStackPanel>(SearchItemsTreeView);

                    //        if (newSelectedSearchResult != null)
                    //        {
                    //            newSelectedSearchResultGroup = newSelectedSearchResult.Parent;
                    //        }

                    //        virtualizingStackPanel.BringIndexIntoViewPublic(newSelectedSearchResultGroup.Index);
                    //        TreeViewItem treeViewItem = SearchItemsTreeView.ItemContainerGenerator.ContainerFromItem(newSelectedSearchResultGroup) as TreeViewItem;

                    //        if (newSelectedSearchResult != null)
                    //        {
                    //            if (treeViewItem != null)
                    //            {
                    //                treeViewItem.ApplyTemplate();
                    //                if (treeViewItem.Template.FindName("ItemsHost", treeViewItem) is ItemsPresenter itemsPresenter)
                    //                {
                    //                    itemsPresenter.ApplyTemplate();
                    //                }
                    //                else
                    //                {
                    //                    itemsPresenter = GetChildOfType<ItemsPresenter>(treeViewItem);
                    //                    if (itemsPresenter == null)
                    //                    {
                    //                        treeViewItem.UpdateLayout();
                    //                        itemsPresenter = GetChildOfType<ItemsPresenter>(treeViewItem);
                    //                    }
                    //                }

                    //                virtualizingStackPanel = GetChildOfType<VirtualizingStackPanel>(treeViewItem);
                    //                virtualizingStackPanel.BringIndexIntoViewPublic(newSelectedSearchResultGroup.SearchResults.IndexOf(newSelectedSearchResult));

                    //                treeViewItem = treeViewItem.ItemContainerGenerator.ContainerFromItem(newSelectedSearchResult) as TreeViewItem;
                    //                treeViewItem?.BringIntoView(new Rect(0, 0, 0, 0));
                    //            }
                    //        }
                    //        else
                    //        {
                    //            treeViewItem?.BringIntoView(new Rect(0, 0, 0, 0));
                    //        }

                    //        mSuppressRequestBringIntoView = false;
                    //    }
                    //}

                    if (e.Key == System.Windows.Input.Key.Enter)
            {
                bool openResult = false;

                if (Settings.Default.SearchInstantly)
                {
                    openResult = true;
                }
                else if (!SearchInput.IsFocused && !IncludeFilesInput.IsFocused && !ExcludeFilesInput.IsFocused && !FilterResultsInput.IsFocused)
                {
                    openResult = true;
                }

                if (openResult)
                {
                    OpenSelectedStackPanel();
                }
            }
            else if (e.Key == System.Windows.Input.Key.C && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                SearchResult searchResult = GetSelectedSearchResult();
                if (searchResult != null)
                {
                    string text = searchResult.BeginText + searchResult.HighlightedText + searchResult.EndText;
                    CopyText(text);
                }

                SearchResultGroup searchGroup = GetSelectedSearchGroup();
                if (searchGroup != null)
                {
                    Clipboard.SetText(searchGroup.File);
                }
            }
        }

        private void Colors_Click(object sender, RoutedEventArgs e)
        {
            CreateWindow(new qgrepControls.ColorsWindow.ColorsWindow(this), "Color settings", this).ShowDialog();
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

        private void CopyText(string text)
        {
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
                string text = searchResult.BeginText + searchResult.HighlightedText + searchResult.EndText;
                CopyText(text);
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
            allResults = allResults.Replace("\xB1", "");
            allResults = allResults.Replace("\xB2", "");
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
                //tvi.BringIntoView(newTargetRect);
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

        public MainWindow CreateWindow(UserControl userControl, string title, UserControl owner)
        {
            MainWindow newWindow = new MainWindow
            {
                Title = title,
                Content = userControl,
                SizeToContent = SizeToContent.WidthAndHeight,
                ResizeMode = ResizeMode.NoResize,
                Owner = qgrepSearchWindowControl.FindAncestor<Window>(owner),
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
            };

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

        private void HistoryButton_Click(object sender, RoutedEventArgs e)
        {
            if(searchHistory.Count > 0)
            {
                HistoryPanel.Children.Clear();

                foreach(string historyItem in searchHistory)
                {
                    MenuItem newMenuItem = new MenuItem() { Header = historyItem };
                    newMenuItem.Click += HistoryItem_Click;

                    HistoryPanel.Children.Add(newMenuItem);
                }

                HistoryPopup.IsOpen = true;
            }
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
                if ((searchHistory.Count == 0 || !searchHistory.Contains(SearchInput.Text)) && SearchInput.Text.Length > 0)
                {
                    searchHistory.Add(SearchInput.Text);
                }
            }
        }

        private void HistoryItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = sender as MenuItem;
            if(menuItem != null)
            {
                SearchInput.Text = menuItem.Header as string;

                if(!Settings.Default.SearchInstantly)
                {
                    Find();
                }
            }

            HistoryPopup.IsOpen = false;
        }

        private bool IgnoreFocusEvents = false;
        private void SearchInput_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Enter)
            {
                if ((searchHistory.Count == 0 || !searchHistory.Contains(SearchInput.Text)) && SearchInput.Text.Length > 0)
                {
                    searchHistory.Add(SearchInput.Text);
                }

                Find();
            }
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            if ((searchHistory.Count == 0 || !searchHistory.Contains(SearchInput.Text)) && SearchInput.Text.Length > 0)
            {
                searchHistory.Add(SearchInput.Text);
            }

            Find();
        }
    }
}