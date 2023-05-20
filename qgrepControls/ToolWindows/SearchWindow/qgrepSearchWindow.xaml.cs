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
using static System.Net.Mime.MediaTypeNames;

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
        public Dictionary<string, Hotkey> bindings = new Dictionary<string, Hotkey>();

        private System.Timers.Timer UpdateTimer = null;
        private string LastResults = "";

        ObservableCollection<SearchResult> searchResults = new ObservableCollection<SearchResult>();
        ObservableCollection<SearchResultGroup> searchResultsGroups = new ObservableCollection<SearchResultGroup>();
        SearchResult selectedSearchResult = null;
        SearchResultGroup selectedSearchResultGroup = null;

        List<HistoricItem> searchHistory = new List<HistoricItem>();
        ObservableCollection<HistoricItem> shownSearchHistory = new ObservableCollection<HistoricItem>();

        static SearchEngine SearchEngine = new SearchEngine();
        CacheUsageType CacheUsageType = CacheUsageType.Normal;

        public qgrepSearchWindowControl(IExtensionInterface extensionInterface)
        {
            ExtensionInterface = extensionInterface;

            InitializeComponent();
            SearchItemsListBox.DataContext = searchResults;

            InitInfo.Visibility = Visibility.Collapsed;
            InitButton.Visibility = Visibility.Collapsed;
            CleanButton.Visibility = Visibility.Collapsed;
            InitProgress.Visibility = Visibility.Collapsed;
            Overlay.Visibility = Visibility.Collapsed;
            PathsButton.IsEnabled = false;
            SearchInput.IsEnabled = false;
            IncludeFilesInput.IsEnabled = false;
            ExcludeFilesInput.IsEnabled = false;
            FilterResultsInput.IsEnabled = false;
            SearchButton.IsEnabled = false;
            HistoryButton.IsEnabled = false;
            SearchCaseSensitive.IsEnabled = false;
            SearchWholeWord.IsEnabled = false;
            SearchRegEx.IsEnabled = false;
            IncludeRegEx.IsEnabled = false;
            ExcludeRegEx.IsEnabled = false;
            FilterRegEx.IsEnabled = false;

            SearchCaseSensitive.IsChecked = Settings.Default.CaseSensitive;
            SearchRegEx.IsChecked = Settings.Default.RegEx;
            SearchWholeWord.IsChecked = Settings.Default.WholeWord;

            IncludeRegEx.IsChecked = Settings.Default.IncludesRegEx;
            ExcludeRegEx.IsChecked = Settings.Default.ExcludesRegEx;
            FilterRegEx.IsChecked = Settings.Default.FilterRegEx;

            StartLastUpdatedTimer();
            SolutionLoaded();

            ThemeHelper.UpdateColorsFromSettings(this, ExtensionInterface, false);
            ThemeHelper.UpdateFontFromSettings(this, ExtensionInterface);
            ExtensionInterface.RefreshResources(ThemeHelper.GetResourcesFromColorScheme(ExtensionInterface));

            UpdateFromSettings();

            HistoryContextMenu.Loaded += OnContextMenuLoaded;
            HistoryContextMenu.Resources = Resources;

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

        private void OnContextMenuLoaded(object sender, RoutedEventArgs e)
        {
            var listBox = HistoryContextMenu.Template.FindName("PART_ListBox", HistoryContextMenu) as ListBox;
            listBox.Items.Clear();

            if (searchHistory.Count > 0)
            {
                shownSearchHistory.Clear();

                for (int i = searchHistory.Count - 1; i >= 0; i--)
                {
                    HistoricOpen historicOpen = searchHistory[i] as HistoricOpen;
                    if (historicOpen != null)
                    {
                        if (!Settings.Default.ShowOpenHistory)
                        {
                            continue;
                        }

                        historicOpen.OperationVisibility = Settings.Default.ShowOpenHistory ? Visibility.Visible : Visibility.Collapsed;
                        historicOpen.Operation = "Opened ";
                        historicOpen.Text = ConfigParser.RemovePaths(historicOpen.OpenedPath);
                        if (!historicOpen.OpenedLine.Equals("0"))
                        {
                            historicOpen.Text += "(" + historicOpen.OpenedLine + ")";
                        }
                    }

                    HistoricSearch historicSearch = searchHistory[i] as HistoricSearch;
                    if (historicSearch != null)
                    {
                        historicSearch.OperationVisibility = Settings.Default.ShowOpenHistory ? Visibility.Visible : Visibility.Collapsed;
                        historicSearch.Operation = "Searched ";
                        historicSearch.Text = historicSearch.SearchedText;
                    }

                    if (listBox.Items.Count > 0 && !Settings.Default.ShowOpenHistory)
                    {
                        bool skipItem = false;

                        foreach (ListBoxItem currListBoxItem in listBox.Items)
                        {
                            HistoricItem historicItem = currListBoxItem.Content as HistoricItem;
                            if (historicItem != null)
                            {
                                if (historicItem.Text.Equals(searchHistory[i].Text))
                                {
                                    skipItem = true;
                                    break;
                                }
                            }
                        }

                        if (skipItem)
                        {
                            continue;
                        }
                    }

                    ListBoxItem listBoxItem = new ListBoxItem() { Content = searchHistory[i] };
                    listBoxItem.PreviewMouseDown += HistoryItem_MouseDown;
                    listBoxItem.PreviewKeyDown += HistoryItem_KeyDown;

                    listBox.Items.Add(listBoxItem);
                }

                Point screenCoordinates = HistoryButton.PointToScreen(new Point(0, 0));

                listBox.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));
                listBox.Arrange(new Rect(listBox.DesiredSize));

                HistoryContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Absolute;
                HistoryContextMenu.HorizontalOffset = screenCoordinates.X - listBox.ActualWidth;
                HistoryContextMenu.VerticalOffset = screenCoordinates.Y;

                if(listBox.Items.Count > 0)
                {
                    HistoryContextMenu.Opened += (s, e2) =>
                    {
                        HistoryContextMenu.HorizontalOffset = screenCoordinates.X - HistoryContextMenu.ActualWidth;
                    };

                    (listBox.Items[0] as ListBoxItem).IsSelected = true;
                    (listBox.Items[0] as ListBoxItem).Focus();
                }
                else
                {
                    HistoryContextMenu.IsOpen = false;
                }
            }
            else
            {
                HistoryContextMenu.IsOpen = false;
            }
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

        private void StartLastUpdatedTimer()
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
                return eventTime.ToString("g");
            }
        }

        void UpdateLastUpdated()
        {
            if (!SearchEngine.IsBusy)
            {
                DateTime lastUpdated = ConfigParser.GetLastUpdated();
                string timeAgo = lastUpdated == DateTime.MaxValue ? "never" : GetTimeAgoString(lastUpdated);

                InitInfo.Content = "Last index update: " + timeAgo;
            }
        }

        private void UpdateTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                UpdateLastUpdated();
            }));
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

            List<string> searchFilters = Settings.Default.SearchFilters.Split(',').ToList();

            FiltersComboBox.ItemsSource = ConfigParser.Instance.ConfigProjects;
            FiltersComboBox.SelectedItems.Clear();

            foreach (string searchFilter in searchFilters)
            {
                ConfigProject selectedProject = null;
                foreach (ConfigProject configProject in ConfigParser.Instance.ConfigProjects)
                {
                    if (configProject.Name == searchFilter)
                    {
                        selectedProject = configProject;
                        break;
                    }
                }

                if (selectedProject != null)
                {
                    FiltersComboBox.SelectedItems.Add(selectedProject);
                }
            }

            if (FiltersComboBox.SelectedItems.Count == 0 && ConfigParser.Instance.ConfigProjects.Count > 0)
            {
                FiltersComboBox.SelectedItems.Add(ConfigParser.Instance.ConfigProjects[0]);
            }

            if (ConfigParser.Instance.ConfigProjects.Count > 1)
            {
                visibility = Visibility.Visible;
            }

            FiltersComboBox.Visibility = visibility;
        }

        public void SolutionLoaded()
        {
            string solutionPath = ExtensionInterface.GetSolutionPath();
            if(solutionPath.Length > 0)
            {
                ConfigParser.Init(System.IO.Path.GetDirectoryName(solutionPath));

                UpdateWarning();
                UpdateFilters();
                UpdateLastUpdated();

                InitInfo.Visibility = Visibility.Visible;
                InitButton.Visibility = Visibility.Visible;
                CleanButton.Visibility = Visibility.Visible;
                PathsButton.IsEnabled = true;
                SearchInput.IsEnabled = true;
                IncludeFilesInput.IsEnabled = true;
                ExcludeFilesInput.IsEnabled = true;
                FilterResultsInput.IsEnabled = true;
                SearchButton.IsEnabled = true;
                SearchCaseSensitive.IsEnabled = true;
                SearchWholeWord.IsEnabled = true;
                SearchRegEx.IsEnabled = true;
                IncludeRegEx.IsEnabled = true;
                ExcludeRegEx.IsEnabled = true;
                FilterRegEx.IsEnabled = true;
                HistoryButton.IsEnabled = searchHistory.Count > 0 ? true : false;
            }
        }

        public void SolutionUnloaded()
        {
            WarningText.Text = "No solution loaded.";
            WarningText.Visibility = Visibility.Visible;

            FiltersComboBox.Visibility = Visibility.Collapsed;
            ConfigParser.UnloadConfig();

            searchResults.Clear();
            searchResultsGroups.Clear();

            InitInfo.Visibility= Visibility.Collapsed;
            InitButton.Visibility = Visibility.Collapsed;
            CleanButton.Visibility = Visibility.Collapsed;
            InitProgress.Visibility = Visibility.Collapsed;
            Overlay.Visibility = Visibility.Collapsed;
            PathsButton.IsEnabled = false;
            SearchInput.IsEnabled = false;
            IncludeFilesInput.IsEnabled = false;
            ExcludeFilesInput.IsEnabled = false;
            FilterResultsInput.IsEnabled = false;
            SearchInput.Text = "";
            IncludeFilesInput.Text = "";
            ExcludeFilesInput.Text = "";
            FilterResultsInput.Text = "";
            InfoLabel.Content = "";
            SearchButton.IsEnabled = false;
            HistoryButton.IsEnabled = false;
            SearchCaseSensitive.IsEnabled = false;
            SearchWholeWord.IsEnabled = false;
            SearchRegEx.IsEnabled = false;
            IncludeRegEx.IsEnabled = false;
            ExcludeRegEx.IsEnabled = false;
            FilterRegEx.IsEnabled = false;
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

            if(!Settings.Default.SearchInstantly)
            {
                CacheUsageType = CacheUsageType.Forced;
            }

            Find();
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
                    ScrollViewer scrollViewer = UIHelper.GetChildOfType<ScrollViewer>(SearchItemsListBox);
                    if(scrollViewer != null)
                    {
                        scrollViewer.ScrollToTop();
                    }

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
                    ScrollViewer scrollViewer = UIHelper.GetChildOfType<ScrollViewer>(SearchItemsTreeView);
                    if (scrollViewer != null)
                    {
                        scrollViewer.ScrollToTop();
                    }

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
                        else if (file.Length > 0)
                        {
                            fileAndLine = file;
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
                    Configs = GetSelectedConfigProjects(),
                    CacheUsageType = CacheUsageType,
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
                    Configs = GetSelectedConfigProjects(),
                    CacheUsageType = CacheUsageType,
                    BypassHighlight = true
                };

                SearchEngine.SearchFilesAsync(searchOptions);
            }
            else
            {
                searchResults.Clear();
                searchResultsGroups.Clear();
                InfoLabel.Content = "";
            }

            CacheUsageType = CacheUsageType.Normal;
        }

        private List<string> GetSelectedConfigProjects()
        {
            if(ConfigParser.Instance.ConfigProjects.Count == 1)
            {
                return new List<string>() { ConfigParser.Instance.ConfigProjects[0].Path };
            }
            else
            {
                return FiltersComboBox.SelectedItems.Cast<ConfigProject>().Select(x => x.Path).ToList();
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
                        e.Handled = true;
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
                    AddOpenToHistory(file, line);
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
            AddOpenToHistory(result.File, "0");
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

                StartLastUpdatedTimer();
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
            SearchEngine.UpdateDatabaseAsync(ConfigParser.Instance.ConfigProjects.Select(x => x.Path).ToList());

            if (Settings.Default.SearchInstantly)
            {
                CacheUsageType = CacheUsageType.Bypass;
                Find();
            }
        }

        private void CleanButton_Click(object sender, RoutedEventArgs e)
        {
            CleanDatabase();
            SearchEngine.UpdateDatabaseAsync(ConfigParser.Instance.ConfigProjects.Select(x => x.Path).ToList());

            if (Settings.Default.SearchInstantly)
            {
                CacheUsageType = CacheUsageType.Bypass;
                Find();
            }
        }

        private void CaseSensitive_Click(object sender, RoutedEventArgs e)
        {
            if(Settings.Default.SearchInstantly)
            {
                Find();
            }

            SaveOptions();
        }

        private void SearchRegEx_Click(object sender, RoutedEventArgs e)
        {
            if (Settings.Default.SearchInstantly)
            {
                Find();
            }

            SaveOptions();
        }

        private void SearchWholeWord_Click(object sender, RoutedEventArgs e)
        {
            if (Settings.Default.SearchInstantly)
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
            UIHelper.CreateWindow(newProjectsWindow, "Search configurations", ExtensionInterface, this, true).ShowDialog();

            ConfigParser.SaveConfig();

            if (ConfigParser.IsConfigChanged())
            {
                UpdateWarning();
                SearchEngine.UpdateDatabaseAsync(ConfigParser.Instance.ConfigProjects.Select(x => x.Path).ToList());

                CacheUsageType = CacheUsageType.Bypass;
            }

            UpdateFilters();
        }

        private void AdvancedButton_Click(object sender, RoutedEventArgs e)
        {
            UIHelper.CreateWindow(new qgrepControls.SearchWindow.SettingsWindow(this), "Advanced settings", ExtensionInterface, this).ShowDialog();
        }

        private void AddSearchToHistory(string searchedString)
        {
            if(searchedString.Length > 0)
            {
                if(searchHistory.Count > 0)
                {
                    for(int i = searchHistory.Count - 1; i >= 0; i--)
                    {
                        HistoricSearch historicSearch = searchHistory[i] as HistoricSearch;
                        if (historicSearch != null)
                        {
                            if(historicSearch.SearchedText == searchedString)
                            {
                                return;
                            }

                            break;
                        }
                    }
                }

                HistoryButton.IsEnabled = true;
                searchHistory.Add(new HistoricSearch() { SearchedText = searchedString});
            }
        }
        private void AddOpenToHistory(string openedPath, string openedLine)
        {
            if(openedPath.Length > 0)
            {
                if(searchHistory.Count > 0)
                {
                    for (int i = searchHistory.Count - 1; i >= 0; i--)
                    {
                        HistoricOpen historicOpen = searchHistory[i] as HistoricOpen;
                        if (historicOpen != null)
                        {
                            if (historicOpen.OpenedPath == openedPath && historicOpen.OpenedLine == openedLine)
                            {
                                searchHistory.RemoveAt(i);
                                break;
                            }
                        }

                        if (searchHistory[i] is HistoricSearch)
                        {
                            break;
                        }
                    }
                }

                HistoryButton.IsEnabled = true;
                searchHistory.Add(new HistoricOpen() { OpenedPath = openedPath, OpenedLine = openedLine});
            }
        }

        private void UserControl_GotFocus(object sender, RoutedEventArgs e)
        {
            if (ExtensionInterface.SearchWindowOpened)
            {
                SearchInput.Focus();

                string selectedText = ExtensionInterface.GetSelectedText();
                if (selectedText.Length > 0)
                {
                    SearchInput.Text = selectedText;
                    SearchInput.CaretIndex = SearchInput.Text.Length;

                    AddSearchToHistory(SearchInput.Text);
                }
                else
                {
                    SearchInput.SelectAll();
                }
            }

            //System.Diagnostics.Debug.WriteLine(e.OriginalSource);
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
                            VirtualizingStackPanel virtualizingStackPanel = UIHelper.GetChildOfType<VirtualizingStackPanel>(SearchItemsListBox);

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

                            VirtualizingStackPanel virtualizingStackPanel = UIHelper.GetChildOfType<VirtualizingStackPanel>(SearchItemsTreeView);
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
                                        itemsPresenter = UIHelper.GetChildOfType<ItemsPresenter>(treeViewItem);
                                        if (itemsPresenter == null)
                                        {
                                            treeViewItem.UpdateLayout();
                                            itemsPresenter = UIHelper.GetChildOfType<ItemsPresenter>(treeViewItem);
                                        }
                                    }

                                    virtualizingStackPanel = UIHelper.GetChildOfType<VirtualizingStackPanel>(treeViewItem);
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
            UIHelper.CreateWindow(new qgrepControls.ColorsWindow.ColorsWindow(this), "Theme settings", ExtensionInterface, this, true).ShowDialog();
        }

        private void SearchInput_MouseEnter(object sender, RoutedEventArgs e)
        {
        }

        private void IncludeRegEx_Click(object sender, RoutedEventArgs e)
        {
            if (Settings.Default.SearchInstantly)
            {
                Find();
            }

            SaveOptions();
        }

        private void ExcludeRegEx_Click(object sender, RoutedEventArgs e)
        {
            if (Settings.Default.SearchInstantly)
            {
                Find();
            }

            SaveOptions();
        }

        private void FilterRegEx_Click(object sender, RoutedEventArgs e)
        {
            if (Settings.Default.SearchInstantly)
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

            if (Settings.Default.SearchInstantly)
            {
                Find();
            }
        }

        public static SearchResult GetSearchResultFromMenuItem(object sender)
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

        private void HistoryButton_Click(object sender, RoutedEventArgs e)
        {
            HistoryContextMenu.IsOpen = true;
        }

        public CustomPopupPlacement[] CustomPopupPlacementCallback(Size popupSize, Size targetSize, Point offset)
        {
            CustomPopupPlacement placement = new CustomPopupPlacement(
                new Point(-popupSize.Width + targetSize.Width, targetSize.Height),
                PopupPrimaryAxis.None
            );

            return new CustomPopupPlacement[] { placement };
        }

        void OpenHistoryItem(ListBoxItem listBoxItem)
        {
            if (listBoxItem != null)
            {
                HistoricSearch historicSearch = listBoxItem.Content as HistoricSearch;
                if (historicSearch != null)
                {
                    SearchInput.Text = historicSearch.SearchedText as string;
                    SearchInput.CaretIndex = SearchInput.Text.Length;
                    AddSearchToHistory(SearchInput.Text);

                    SearchInput.Focus();
                    if(!Settings.Default.SearchInstantly)
                    {
                        Find();
                    }
                }

                HistoricOpen historicOpen = listBoxItem.Content as HistoricOpen;
                if(historicOpen != null)
                {
                    ExtensionInterface.OpenFile(historicOpen.OpenedPath, historicOpen.OpenedLine);
                    AddOpenToHistory(historicOpen.OpenedPath, historicOpen.OpenedLine);
                }
            }

            HistoryContextMenu.IsOpen = false;
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
                HistoryContextMenu.IsOpen = false;
            }
        }

        private void SearchInput_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Enter)
            {
                AddSearchToHistory(SearchInput.Text);
                Find();
            }
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            AddSearchToHistory(SearchInput.Text);
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

                System.Diagnostics.Debug.WriteLine(e.OriginalSource);
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

        public void ToggleGroupingBy()
        {
            Dispatcher.Invoke(() =>
            {
                Settings.Default.GroupingIndex = 1 - Settings.Default.GroupingIndex;
                Settings.Default.Save();
                UpdateFromSettings();
            });
        }

        public void ShowHistory()
        {
            Dispatcher.Invoke(() =>
            {
                HistoryContextMenu.IsOpen = true;
            });
        }

        private void KeyboardButton_Click(object sender, RoutedEventArgs e)
        {
            HotkeysWindow hotkeysWindow = new HotkeysWindow(this);
            MainWindow hotkeysDialog = UIHelper.CreateWindow(hotkeysWindow, "Edit hotkeys", ExtensionInterface, this);
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

        private void SearchInput_LostFocus(object sender, RoutedEventArgs e)
        {
            if (SearchInput.Text.Length > 0 && Settings.Default.SearchInstantly)
            {
                AddSearchToHistory(SearchInput.Text);
            }
        }

        private void FilterResultsInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (FilterResultsInput.Text.Length > 0)
            {
                FilterResultsLabel.Visibility = Visibility.Hidden;
            }
            else
            {
                FilterResultsLabel.Visibility = Visibility.Visible;
            }

            Find();
        }
    }
}