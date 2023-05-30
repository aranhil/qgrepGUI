using qgrepControls.Classes;
using qgrepControls.Properties;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Controls.Primitives;
using qgrepControls.ModelViews;
using System.Windows.Media;
using System.Threading;
using System.Globalization;
using System.Reflection;
using System.Resources;

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

    public partial class qgrepSearchWindowControl : UserControl, ISearchEngineEventsHandler
    {
        public IWrapperApp WrapperApp;
        public Dictionary<string, Hotkey> bindings = new Dictionary<string, Hotkey>();

        private string LastResults = "";

        ObservableCollection<SearchResult> searchResults = new ObservableCollection<SearchResult>();
        ObservableCollection<SearchResultGroup> searchResultsGroups = new ObservableCollection<SearchResultGroup>();
        SearchResult selectedSearchResult = null;
        SearchResultGroup selectedSearchResultGroup = null;

        List<HistoricItem> searchHistory = new List<HistoricItem>();
        ObservableCollection<HistoricItem> shownSearchHistory = new ObservableCollection<HistoricItem>();

        CacheUsageType CacheUsageType = CacheUsageType.Normal;
        ResourceManager ResourceManager;

        public qgrepSearchWindowControl(IWrapperApp WrapperApp)
        {
            this.WrapperApp = WrapperApp;
            ResourceManager = new ResourceManager("qgrepControls.Properties.Resources", Assembly.GetExecutingAssembly());

            Thread.CurrentThread.CurrentCulture = new CultureInfo("ro-RO");
            Thread.CurrentThread.CurrentUICulture = new CultureInfo("ro-RO");

            SearchEngine.Instance.StartUpdateCallback += HandleUpdateStart;
            SearchEngine.Instance.FinishUpdateCallback += HandleUpdateFinish;
            SearchEngine.Instance.UpdateInfoCallback += HandleUpdateMessage;
            SearchEngine.Instance.UpdateProgressCallback += HandleProgress;
            SearchEngine.Instance.UpdateErrorCallback += HandleErrorMessage;

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

            if (WrapperApp.LoadConfigAtStartup())
            {
                SolutionLoaded();
            }

            ThemeHelper.UpdateColorsFromSettings(this, WrapperApp, false);
            ThemeHelper.UpdateFontFromSettings(this, WrapperApp);
            WrapperApp.RefreshResources(ThemeHelper.GetResourcesFromColorScheme(WrapperApp));

            UpdateFromSettings();

            HistoryContextMenu.Loaded += OnContextMenuLoaded;
            HistoryContextMenu.Resources = Resources;

            SearchItemsListBox.ItemsSource = searchResults;

            SearchItemsListBox.Focusable = true;
            SearchItemsTreeView.Focusable = true;

            bindings = WrapperApp.ReadKeyBindings();
            WrapperApp.ApplyKeyBindings(bindings);

            KeyboardButton.Visibility = WrapperApp.IsStandalone ? Visibility.Visible : Visibility.Collapsed;
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
                        historicOpen.SetOperationText(ResourceManager.GetString("HistoricOpen"));
                        historicOpen.Text = ConfigParser.RemovePaths(historicOpen.OpenedPath, Settings.Default.PathStyleIndex);
                        //if (!historicOpen.OpenedLine.Equals("0"))
                        //{
                        //    historicOpen.Text += "(" + historicOpen.OpenedLine + ")";
                        //}
                    }

                    HistoricSearch historicSearch = searchHistory[i] as HistoricSearch;
                    if (historicSearch != null)
                    {
                        historicSearch.OperationVisibility = Settings.Default.ShowOpenHistory ? Visibility.Visible : Visibility.Collapsed;
                        historicSearch.SetOperationText(ResourceManager.GetString("HistoricSearch"));
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

                if (listBox.Items.Count > 0)
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
            SearchCaseSensitive.ToolTip = string.Format(ResourceManager.GetString("SearchCaseSensitive", new CultureInfo("ro-RO")), bindings["ToggleRegEx"].ToString());
            SearchWholeWord.ToolTip = string.Format(ResourceManager.GetString("SearchWholeWord"), bindings["ToggleRegEx"].ToString());
            SearchRegEx.ToolTip = string.Format(ResourceManager.GetString("RegEx"), bindings["ToggleRegEx"].ToString());
            IncludeRegEx.ToolTip = string.Format(ResourceManager.GetString("RegEx"), bindings["ToggleRegEx"].ToString());
            ExcludeRegEx.ToolTip = string.Format(ResourceManager.GetString("RegEx"), bindings["ToggleRegEx"].ToString());
            FilterRegEx.ToolTip = string.Format(ResourceManager.GetString("RegEx"), bindings["ToggleRegEx"].ToString());
        }

        public void RenameFilter(string oldName, string newName)
        {
            List<string> searchFilters = Settings.Default.SearchFilters.Split(',').ToList();
            int oldFilterIndex = searchFilters.IndexOf(oldName);
            if (oldFilterIndex >= 0)
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
            string solutionPath = WrapperApp.GetConfigPath(Settings.Default.UseGlobalPath);
            if (solutionPath.Length > 0)
            {
                ConfigParser.Initialize(solutionPath);
                ConfigParser.Instance.FilesChanged += FilesChanged;
                ConfigParser.Instance.FilesAddedOrRemoved += FilesAddedOrRemoved; ;

                if (Settings.Default.UpdateIndexAutomatically)
                {
                    UpdateDatabase(true);
                }

                UpdateWarning();
                UpdateFilters();
                SearchEngine.Instance.UpdateLastUpdated();

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
            WarningText.Text = ResourceManager.GetString("NoSolutionLoaded");
            WarningText.Visibility = Visibility.Visible;

            FiltersComboBox.Visibility = Visibility.Collapsed;
            ConfigParser.UnloadConfig();
            ConfigParser.Instance.FilesChanged -= FilesChanged;

            searchResults.Clear();
            searchResultsGroups.Clear();

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

        bool QueueFindWhenVisible = true;

        private void FilesChanged(List<string> modifiedFiles)
        {
            TaskRunner.RunOnUIThreadAsync(() =>
            {
                UpdateDatabase(true, modifiedFiles);
            });
        }

        private void FilesAddedOrRemoved()
        {
            TaskRunner.RunOnUIThreadAsync(() =>
            {
                UpdateDatabase(true);
            });
        }

        public void UpdateWarning()
        {
            WarningText.Visibility = Visibility.Hidden;
            if (!ConfigParser.HasAnyPaths())
            {
                WarningText.Text = ResourceManager.GetString("NoSearchFoldersSet");
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

            if (!Settings.Default.SearchInstantly)
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

        private bool? OverrideExpansion = null;
        private bool CurrentExpansion = false;

        private void SetTreeCollapsing(bool isExpanded)
        {
            CurrentExpansion = isExpanded;
            foreach (SearchResultGroup searchResultGroup in searchResultsGroups)
            {
                if (searchResultGroup.IsExpanded != isExpanded)
                {
                    searchResultGroup.SetIsExpanded(isExpanded);
                }
            }
        }

        private void ProcessTreeCollapsing()
        {
            bool expand = false;

            if (OverrideExpansion == null)
            {
                if (Settings.Default.ExpandModeIndex == 2)
                {
                    expand = true;
                }
                else if (Settings.Default.ExpandModeIndex == 1 && searchResultsGroups.Count + searchResults.Count < 500)
                {
                    expand = true;
                }
            }
            else
            {
                expand = OverrideExpansion ?? false;
            }

            SetTreeCollapsing(expand);
        }

        ObservableCollection<SearchResult> newSearchResults = new ObservableCollection<SearchResult>();
        ObservableCollection<SearchResultGroup> newSearchResultGroups = new ObservableCollection<SearchResultGroup>();
        bool newSearch = false;

        private void AddSearchResultToGroups(SearchResult searchResult, ObservableCollection<SearchResultGroup> searchResultGroups)
        {
            if (searchResultGroups.Count > 0 && searchResultGroups.Last().File == searchResult.File)
            {
                searchResultGroups.Last().SearchResults.Add(searchResult);
                searchResult.Parent = searchResultGroups.Last();
            }
            else
            {
                SearchResultGroup newSearchGroup = new SearchResultGroup()
                {
                    File = searchResult.File,
                    TrimmedFile = ConfigParser.RemovePaths(searchResult.File, Settings.Default.PathStyleIndex),
                    SearchResults = new ObservableCollection<SearchResult> { searchResult }
                };

                searchResultGroups.Add(newSearchGroup);
                searchResult.Parent = newSearchGroup;
            }
        }

        uint BackgroundColor = 0;

        public void OnStartSearchEvent(SearchOptions searchOptions)
        {
            TaskRunner.RunOnUIThread(() =>
            {
                newSearch = true;
                selectedSearchResultGroup = null;
                selectedSearchResult = null;
                OverrideExpansion = null;

                if (!WrapperApp.IsStandalone)
                {
                    BackgroundColor = ThemeHelper.GetBackgroundColor(this);
                }

                SearchItemsListBox.Visibility = searchOptions.GroupingMode == 0 ? Visibility.Visible : Visibility.Collapsed;
                SearchItemsTreeView.Visibility = searchOptions.GroupingMode != 0 ? Visibility.Visible : Visibility.Collapsed;
                ErrorLabel.Content = "";
            });
        }

        private void AddResultsBatch(SearchOptions searchOptions)
        {
            if (searchOptions.GroupingMode == 0)
            {
                if (newSearch)
                {
                    ScrollViewer scrollViewer = UIHelper.GetChildOfType<ScrollViewer>(SearchItemsListBox);
                    if (scrollViewer != null)
                    {
                        scrollViewer.ScrollToTop();
                    }

                    SearchItemsListBox.ItemsSource = searchResults = newSearchResults;
                    newSearchResults = new ObservableCollection<SearchResult>();
                    newSearch = false;

                    if (searchResults.Count > 0)
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

                    if (searchResultsGroups.Count > 0)
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

                ProcessTreeCollapsing();
            }

            InfoLabel.Content = string.Format(ResourceManager.GetString("ShowingResults"), searchResults.Count, searchOptions.Query);
        }

        private double GetScreenHeight()
        {
            if (Settings.Default.GroupingIndex == 0)
            {
                return SearchItemsListBox.ActualHeight;
            }
            else
            {
                return SearchItemsTreeView.ActualHeight;
            }
        }

        private bool NewResultsFitScreen()
        {
            if (Settings.Default.GroupingIndex == 1)
            {
                double totalNewSize = 0;
                bool expandAll = (Settings.Default.ExpandModeIndex == 1 && newSearchResultGroups.Count + newSearchResults.Count < 500) || Settings.Default.ExpandModeIndex == 2;

                foreach (SearchResultGroup resultGroup in newSearchResultGroups)
                {
                    totalNewSize += Settings.Default.GroupHeight;
                    if (expandAll)
                    {
                        totalNewSize += Settings.Default.LineHeight * resultGroup.SearchResults.Count;
                    }

                    if (totalNewSize > GetScreenHeight())
                    {
                        return true;
                    }
                }

                return false;
            }
            else
            {
                return newSearchResults.Count * Settings.Default.LineHeight > GetScreenHeight();
            }
        }

        int LastUpdateInterval = 0;
        CountdownTimer UpdateTimer = new CountdownTimer();

        private bool EnoughTimePassed()
        {
            if(!UpdateTimer.IsStarted())
            {
                UpdateTimer.Start(100);
                LastUpdateInterval = 100;
                return false;
            }
            else
            {
                if(UpdateTimer.HasExpired())
                {
                    LastUpdateInterval += 200;
                    UpdateTimer.Reset(LastUpdateInterval);
                    return true;
                }

                return false;
            }
        }

        public void OnResultEvent(string file, string lineNumber, string beginText, string highlight, string endText, SearchOptions searchOptions)
        {
            if (!SearchEngine.Instance.IsSearchQueued)
            {
                TaskRunner.RunOnUIThread(() =>
                {
                    if (SearchInput.Text.Length != 0 || IncludeFilesInput.Text.Length != 0)
                    {
                        string fileAndLine = "";
                        string trimmedFileAndLine = "";

                        if (file.Length > 0 && lineNumber.Length > 0)
                        {
                            fileAndLine = string.Format(ResourceManager.GetString("FileAndLine"), file, lineNumber);
                            trimmedFileAndLine = string.Format(ResourceManager.GetString("FileAndLine"), ConfigParser.RemovePaths(file, Settings.Default.PathStyleIndex), lineNumber);
                        }
                        else if (file.Length > 0)
                        {
                            fileAndLine = file;
                            trimmedFileAndLine = ConfigParser.RemovePaths(fileAndLine, Settings.Default.PathStyleIndex);
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

                        if (searchOptions.IsFileSearch)
                        {
                            WrapperApp.GetIcon(newSearchResult.FullResult, BackgroundColor, newSearchResult);
                        }

                        newSearchResults.Add(newSearchResult);

                        if (searchOptions.GroupingMode == 1)
                        {
                            AddSearchResultToGroups(newSearchResult, newSearchResultGroups);
                        }

                        if (newSearch && NewResultsFitScreen() || (EnoughTimePassed() && SearchWindowControl.IsKeyboardFocusWithin))
                        {
                            AddResultsBatch(searchOptions);
                        }
                    }
                });
            }
        }

        public void OnErrorEvent(string message, SearchOptions searchOptions)
        {
            TaskRunner.RunOnUIThread(() =>
            {
                ErrorLabel.Content = message;
            });
        }

        public void OnFinishSearchEvent(SearchOptions searchOptions)
        {
            TaskRunner.RunOnUIThread(() =>
            {
                if (!SearchEngine.Instance.IsSearchQueued)
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
                    }
                }
                else
                {
                    newSearch = false;
                    newSearchResultGroups.Clear();
                    newSearchResults.Clear();
                }

                UpdateTimer.Stop();
            });
        }

        private void Find()
        {
            if (SearchInput.Text.Length != 0)
            {
                SearchOptions searchOptions = new SearchOptions()
                {
                    EventsHandler = this,
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

                SearchEngine.Instance.SearchAsync(searchOptions);
            }
            else if (IncludeFilesInput.Text.Length != 0)
            {
                SearchOptions searchOptions = new SearchOptions()
                {
                    EventsHandler = this,
                    Query = IncludeFilesInput.Text,
                    RegEx = IncludeRegEx.IsChecked == true,
                    FilterResults = Settings.Default.ShowFilter && FilterResultsInput.Text.Length > 0 ? FilterResultsInput.Text : "",
                    FilterResultsRegEx = FilterRegEx.IsChecked == true,
                    GroupingMode = 0,
                    Configs = GetSelectedConfigProjects(),
                    CacheUsageType = CacheUsageType,
                    IsFileSearch = true,
                };

                SearchEngine.Instance.SearchFilesAsync(searchOptions);
            }
            else
            {
                searchResults.Clear();
                searchResultsGroups.Clear();
                InfoLabel.Content = "";
                ErrorLabel.Content = "";
            }

            CacheUsageType = CacheUsageType.Normal;
        }

        private List<string> GetSelectedConfigProjects()
        {
            if (ConfigParser.Instance.ConfigProjects.Count == 1)
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
                    WrapperApp.OpenFile(result.File, result.Line);
                    AddOpenToHistory(result.File, result.Line);
                }
                catch (Exception)
                {
                }
            }
            else
            {
                WrapperApp.OpenFile(result.FullResult, "0");
            }
        }
        private void OpenSearchGroup(SearchResultGroup result)
        {
            WrapperApp.OpenFile(result.File, "0");
            AddOpenToHistory(result.File, "0");
        }

        private void OpenSelectedSearchResult()
        {
            if (selectedSearchResult != null)
            {
                OpenSearchResult(selectedSearchResult);
            }
            else if (selectedSearchResultGroup != null)
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
                TaskRunner.RunOnUIThreadAsync(() =>
                {
                    HandleErrorMessage(string.Format(ResourceManager.GetString("CannotCleanIndex"), ex.Message), null);
                });
            }
        }

        private Stopwatch infoUpdateStopWatch = new Stopwatch();
        private Stopwatch progressUpdateStopWatch = new Stopwatch();
        private string lastMessage = "";
        private void HandleUpdateStart(DatabaseUpdate databaseUpdate)
        {
            TaskRunner.RunOnUIThread(() =>
            {
                if (databaseUpdate == null || !databaseUpdate.IsSilent)
                {
                    Overlay.Visibility = Visibility.Visible;
                    InitProgress.Value = 0;
                    InitButton.IsEnabled = false;
                    CleanButton.IsEnabled = false;
                    PathsButton.IsEnabled = false;
                }
                else
                {
                    InitButton.IsEnabled = false;
                    CleanButton.IsEnabled = false;
                    PathsButton.IsEnabled = false;

                    if (SearchWindowControl.IsVisible || WrapperApp.IsStandalone)
                    {
                        if (Settings.Default.SearchInstantly)
                        {
                            CacheUsageType = CacheUsageType.Bypass;
                            Find();
                        }
                    }
                    else
                    {
                        QueueFindWhenVisible = true;
                    }
                }
            });
        }

        private void HandleUpdateFinish(DatabaseUpdate databaseUpdate)
        {
            TaskRunner.RunOnUIThread(() =>
            {
                if (databaseUpdate == null || !databaseUpdate.IsSilent)
                {
                    Overlay.Visibility = Visibility.Collapsed;
                    InitProgress.Visibility = Visibility.Collapsed;
                    InitButton.IsEnabled = true;
                    CleanButton.IsEnabled = true;
                    PathsButton.IsEnabled = true;
                    InitInfo.Content = lastMessage;

                    infoUpdateStopWatch.Stop();
                    progressUpdateStopWatch.Stop();
                }
                else
                {
                    InitButton.IsEnabled = true;
                    CleanButton.IsEnabled = true;
                    PathsButton.IsEnabled = true;

                    SearchEngine.Instance.UpdateLastUpdated();
                }
            });
        }

        private void HandleErrorMessage(string message, DatabaseUpdate databaseUpdate)
        {
            lastMessage = message;

            TaskRunner.RunOnUIThread(() =>
            {
                if (databaseUpdate == null || !databaseUpdate.IsSilent)
                {
                    InitInfo.Content = message;
                }
            });
        }

        private void HandleUpdateMessage(string message, DatabaseUpdate databaseUpdate)
        {
            lastMessage = message;

            if (!infoUpdateStopWatch.IsRunning || infoUpdateStopWatch.ElapsedMilliseconds > 10)
            {
                TaskRunner.RunOnUIThread(() =>
                {
                    if (databaseUpdate == null || !databaseUpdate.IsSilent)
                    {
                        InitInfo.Content = message;
                        infoUpdateStopWatch.Restart();
                    }
                });
            }
        }

        private void HandleProgress(double percentage, DatabaseUpdate databaseUpdate)
        {
            if (!progressUpdateStopWatch.IsRunning || progressUpdateStopWatch.ElapsedMilliseconds > 20)
            {
                TaskRunner.RunOnUIThread(() =>
                {
                    if (databaseUpdate == null || !databaseUpdate.IsSilent)
                    {
                        InitProgress.Value = percentage;
                        InitProgress.Visibility = percentage >= 0 ? Visibility.Visible : Visibility.Collapsed;
                        progressUpdateStopWatch.Restart();
                    }
                });
            }
        }

        public void UpdateDatabase(bool silently = false, List<string> modifiedFiles = null)
        {
            if (!ConfigParser.IsInitialized())
            {
                return;
            }

            if (!silently)
            {
                Overlay.Visibility = Visibility.Visible;
                InitProgress.Value = 0;
            }

            InitButton.IsEnabled = false;
            CleanButton.IsEnabled = false;
            PathsButton.IsEnabled = false;

            SearchEngine.Instance.UpdateDatabaseAsync(new DatabaseUpdate()
            {
                ConfigPaths = ConfigParser.Instance.ConfigProjects.Select(x => x.Path).ToList(),
                IsSilent = silently,
                Files = modifiedFiles
            });

            if (Settings.Default.SearchInstantly)
            {
                CacheUsageType = CacheUsageType.Bypass;
                Find();
            }
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
            if (Settings.Default.SearchInstantly)
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
            if (SearchEngine.Instance.IsBusy)
            {
                return;
            }

            ConfigParser.SaveOldCopy();

            ProjectsWindow newProjectsWindow = new ProjectsWindow(this);
            UIHelper.CreateWindow(newProjectsWindow, ResourceManager.GetString("SearchConfigurations"), WrapperApp, this, true).ShowDialog();

            ConfigParser.SaveConfig();

            if (ConfigParser.IsConfigChanged())
            {
                UpdateWarning();
                UpdateDatabase();

                CacheUsageType = CacheUsageType.Bypass;
            }

            UpdateFilters();
        }

        private void AdvancedButton_Click(object sender, RoutedEventArgs e)
        {
            UIHelper.CreateWindow(new qgrepControls.SearchWindow.SettingsWindow(this), ResourceManager.GetString("AdvancedSettings"), WrapperApp, this).ShowDialog();
        }

        private void AddSearchToHistory(string searchedString)
        {
            if (searchedString.Length > 0)
            {
                if (searchHistory.Count > 0)
                {
                    for (int i = searchHistory.Count - 1; i >= 0; i--)
                    {
                        HistoricSearch historicSearch = searchHistory[i] as HistoricSearch;
                        if (historicSearch != null)
                        {
                            if (historicSearch.SearchedText == searchedString)
                            {
                                return;
                            }

                            break;
                        }
                    }
                }

                HistoryButton.IsEnabled = true;
                searchHistory.Add(new HistoricSearch() { SearchedText = searchedString });
            }
        }
        private void AddOpenToHistory(string openedPath, string openedLine)
        {
            if (openedPath.Length > 0)
            {
                if (searchHistory.Count > 0)
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
                searchHistory.Add(new HistoricOpen() { OpenedPath = openedPath, OpenedLine = openedLine });
            }
        }

        private void SearchWindowControl_GotFocus(object sender, RoutedEventArgs e)
        {
            if (WrapperApp.SearchWindowOpened)
            {
                SearchInput.Focus();

                string selectedText = WrapperApp.GetSelectedText();
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

        private void SearchWindowControl_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Down ||
                e.Key == System.Windows.Input.Key.Up ||
                e.Key == System.Windows.Input.Key.PageUp ||
                e.Key == System.Windows.Input.Key.PageDown)
            {
                if (!(!SearchInput.IsFocused && !IncludeFilesInput.IsFocused && !ExcludeFilesInput.IsFocused && !FilterResultsInput.IsFocused))
                {
                    if (SearchItemsListBox.IsVisible)
                    {
                        if (selectedSearchResult == null)
                        {
                            if (searchResults.Count > 0 && searchResults[0].IsSelected)
                            {
                                selectedSearchResult = searchResults[0];
                            }
                        }

                        if (selectedSearchResult != null)
                        {
                            VirtualizingStackPanel virtualizingStackPanel = UIHelper.GetChildOfType<VirtualizingStackPanel>(SearchItemsListBox);

                            virtualizingStackPanel.BringIndexIntoViewPublic(searchResults.IndexOf(selectedSearchResult));
                            ListBoxItem listBoxItem = SearchItemsListBox.ItemContainerGenerator.ContainerFromItem(selectedSearchResult) as ListBoxItem;
                            listBoxItem?.Focus();

                            e.Handled = true;
                        }
                    }
                    else if (SearchItemsTreeView.IsVisible)
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

                if (selectedSearchResult != null)
                {
                    if (selectedSearchResult.Parent != null)
                    {
                        TreeViewItem treeViewItem = SearchItemsTreeView.ItemContainerGenerator.ContainerFromItem(selectedSearchResult.Parent) as TreeViewItem;
                        treeViewItem = treeViewItem.ItemContainerGenerator.ContainerFromItem(selectedSearchResult) as TreeViewItem;
                        if (treeViewItem?.IsFocused ?? false)
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
                else if (selectedSearchResultGroup != null)
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
            UIHelper.CreateWindow(new qgrepControls.ColorsWindow.ColorsWindow(this), ResourceManager.GetString("ThemeSettings"), WrapperApp, this, true).ShowDialog();
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
            foreach (ConfigProject configProject in FiltersComboBox.SelectedItems)
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
            if (searchResult != null)
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
            allResults = ConfigParser.RemovePaths(allResults, Settings.Default.PathStyleIndex);
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
                    if (!Settings.Default.SearchInstantly)
                    {
                        Find();
                    }
                }

                HistoricOpen historicOpen = listBoxItem.Content as HistoricOpen;
                if (historicOpen != null)
                {
                    WrapperApp.OpenFile(historicOpen.OpenedPath, historicOpen.OpenedLine);
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
            if (e.Key == Key.Enter)
            {
                OpenHistoryItem(sender as ListBoxItem);
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                SearchInput.Focus();
                HistoryContextMenu.IsOpen = false;
            }
        }

        private void SearchInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
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

        private void SearchWindowControl_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
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
            TaskRunner.RunOnUIThreadAsync(() =>
            {
                SearchCaseSensitive.IsChecked = !SearchCaseSensitive.IsChecked;
                SaveOptions();
                UpdateFromSettings();
            });
        }

        public void ToggleWholeWord()
        {
            TaskRunner.RunOnUIThreadAsync(() =>
            {
                SearchWholeWord.IsChecked = !SearchWholeWord.IsChecked;
                SaveOptions();
                UpdateFromSettings();
            });
        }

        public void ToggleRegEx()
        {
            TaskRunner.RunOnUIThreadAsync(() =>
            {
                if (IncludeFilesInput.IsFocused)
                {
                    IncludeRegEx.IsChecked = !IncludeRegEx.IsChecked;
                }
                else if (ExcludeFilesInput.IsFocused)
                {
                    ExcludeRegEx.IsChecked = !ExcludeRegEx.IsChecked;
                }
                else if (FilterResultsInput.IsFocused)
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
            TaskRunner.RunOnUIThreadAsync(() =>
            {
                Settings.Default.ShowIncludes = !Settings.Default.ShowIncludes;
                Settings.Default.Save();
                UpdateFromSettings();

                if (IncludeFilesInput.IsVisible)
                {
                    IncludeFilesInput.Focus();
                }
            });
        }

        public void ToggleExcludeFiles()
        {
            TaskRunner.RunOnUIThreadAsync(() =>
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
            TaskRunner.RunOnUIThreadAsync(() =>
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

        public void ToggleGroupBy()
        {
            TaskRunner.RunOnUIThreadAsync(() =>
            {
                Settings.Default.GroupingIndex = 1 - Settings.Default.GroupingIndex;
                Settings.Default.Save();
                UpdateFromSettings();
            });
        }

        public void ToggleGroupExpand()
        {
            TaskRunner.RunOnUIThreadAsync(() =>
            {
                OverrideExpansion = !(OverrideExpansion ?? CurrentExpansion);
                SetTreeCollapsing(OverrideExpansion ?? false);
            });
        }

        public void ShowHistory()
        {
            TaskRunner.RunOnUIThreadAsync(() =>
            {
                HistoryContextMenu.IsOpen = true;
            });
        }

        private void KeyboardButton_Click(object sender, RoutedEventArgs e)
        {
            HotkeysWindow hotkeysWindow = new HotkeysWindow(this);
            MainWindow hotkeysDialog = UIHelper.CreateWindow(hotkeysWindow, ResourceManager.GetString("EditHotkeys"), WrapperApp, this);
            hotkeysWindow.Dialog = hotkeysDialog;
            hotkeysDialog.ShowDialog();

            if (hotkeysWindow.IsOk)
            {
                bindings = hotkeysWindow.GetBindings();
                WrapperApp.SaveKeyBindings(bindings);
                WrapperApp.ApplyKeyBindings(bindings);
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

        private void SearchWindowControl_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (QueueFindWhenVisible)
            {
                if (Settings.Default.SearchInstantly)
                {
                    CacheUsageType = CacheUsageType.Bypass;
                    Find();
                }

                QueueFindWhenVisible = false;
            }
        }

        private void TreeViewItem_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                DependencyObject originalSource = (DependencyObject)e.OriginalSource;

                while ((originalSource != null) && !(originalSource is TreeViewItem))
                {
                    try
                    {
                        originalSource = VisualTreeHelper.GetParent(originalSource) ?? LogicalTreeHelper.GetParent(originalSource);
                    }
                    catch (InvalidOperationException)
                    {
                        originalSource = LogicalTreeHelper.GetParent(originalSource);
                    }
                }

                if (originalSource is TreeViewItem item)
                {
                    item.IsSelected = true;
                    e.Handled = true;
                }
            }
            catch { }
        }
    }
}