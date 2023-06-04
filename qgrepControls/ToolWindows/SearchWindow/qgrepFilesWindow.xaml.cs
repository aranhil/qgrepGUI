using qgrepControls.Classes;
using qgrepControls.Properties;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using qgrepControls.ModelViews;
using System.Windows.Documents;
using System.Xml.Linq;
using System.Windows.Input;

namespace qgrepControls.SearchWindow
{
    public partial class qgrepFilesWindowControl : UserControl, ISearchEngineEventsHandler
    {
        public IWrapperApp WrapperApp;
        public bool IsActiveDocumentCpp = false;

        public qgrepFilesWindowControl(IWrapperApp WrapperApp)
        {
            InitializeComponent();

            IncludeFilesInput.Focus();

            this.WrapperApp = WrapperApp;
            ConfigParser.Initialize(WrapperApp.GetConfigPath(Settings.Default.UseGlobalPath));

            ThemeHelper.UpdateColorsFromSettings(this, WrapperApp, false);
            ThemeHelper.UpdateFontFromSettings(this, WrapperApp);

            SearchEngine.Instance.StartUpdateCallback += HandleUpdateStart;
            SearchEngine.Instance.FinishUpdateCallback += HandleUpdateFinish;
            SearchEngine.Instance.UpdateInfoCallback += HandleUpdateMessage;
            SearchEngine.Instance.UpdateProgressCallback += HandleProgress;
            SearchEngine.Instance.UpdateErrorCallback += HandleErrorMessage;

            InitButton.IsEnabled = SearchEngine.Instance.IsBusy ? false : true;
            CleanButton.IsEnabled = SearchEngine.Instance.IsBusy ? false : true;

            InitProgress.Visibility = Visibility.Collapsed;
            SearchEngine.Instance.ShowLastUpdateMessage();
            UpdateFilters();

            IsActiveDocumentCpp = Settings.Default.CppHeaderInclusion && WrapperApp.IsActiveDocumentCpp();
        }
        public void UpdateFromSettings()
        {
            Find();
        }

        private void SearchInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (IncludeFilesInput.Text.Length > 0)
            {
                IncludeFilesLabel.Visibility = Visibility.Hidden;
            }
            else
            {
                IncludeFilesLabel.Visibility = Visibility.Visible;
            }

            Find();
        }

        private void Find()
        {
            if (IncludeFilesInput.Text.Length != 0)
            {
                SearchOptions searchOptions = new SearchOptions()
                {
                    EventsHandler = this,
                    Query = IncludeFilesInput.Text,
                    RegEx = true,
                    FilterResults = "",
                    IncludeFilesRegEx = false,
                    FilterResultsRegEx = false,
                    GroupingMode = 0,
                    Configs = GetSelectedConfigProjects(),
                    CacheUsageType = CacheUsageType.Normal,
                    BypassHighlight = true,
                    FileSearchUnorderedKeywords = true,
                    IsFileSearch = true,
                };

                SearchEngine.Instance.SearchFilesAsync(searchOptions);
            }
            else
            {
                searchResults.Clear();
            }
        }

        uint BackgroundColor = 0;

        static int CalculateScore(string wordsString, string inputString)
        {
            int score = 0;

            inputString = inputString.ToLower();
            foreach (string word in wordsString.ToLower().Split(' '))
            {
                int index = inputString.IndexOf(word);
                if (index != -1)
                {
                    score += index;
                }
            }

            return score;
        }

        ObservableCollection<SearchResult> searchResults = new ObservableCollection<SearchResult>();
        ObservableCollection<SearchResult> newSearchResults = new ObservableCollection<SearchResult>();

        public void OnStartSearchEvent(SearchOptions searchOptions)
        {
            TaskRunner.RunOnUIThread(() =>
            {
                selectedSearchResult = -1;

                if(!WrapperApp.IsStandalone)
                {
                    BackgroundColor = ThemeHelper.GetBackgroundColor(this);
                }
            });
        }

        private void AddResultsBatch(SearchOptions searchOptions)
        {
            if (searchOptions.IsNewSearch)
            {
                SearchItemsListBox.ItemsSource = searchResults = newSearchResults;
                newSearchResults = new ObservableCollection<SearchResult>();
                searchOptions.IsNewSearch = false;

                if (searchResults.Count > 0)
                {
                    SelectSearchResult(searchResults[0]);
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

            SelectBestMatch(searchOptions);
        }

        private void SelectBestMatch(SearchOptions searchOptions)
        {
            SearchResult bestResult = null;
            int bestScore = int.MaxValue;

            foreach (SearchResult result in searchResults)
            {
                string trimmedFileAndLine = ConfigParser.RemovePaths(result.FileAndLine, Settings.Default.FilesSearchScopeIndex);
                int score = CalculateScore(searchOptions.Query, trimmedFileAndLine);
                if(score < bestScore)
                {
                    bestScore = score;
                    bestResult = result;
                }
            }

            if(bestResult != null)
            {
                SelectSearchResult(bestResult);
            }
        }

        public void OnResultEvent(string file, string lineNumber, string beginText, string highlight, string endText, SearchOptions searchOptions)
        {
            if (!SearchEngine.Instance.IsSearchQueued)
            {
                TaskRunner.RunOnUIThread(() =>
                {
                    if (IncludeFilesInput.Text.Length != 0)
                    {
                        string fileAndLine = "";
                        string trimmedFileAndLine = "";

                        if(beginText.Length > 0)
                        {
                            fileAndLine = beginText;
                            trimmedFileAndLine = ConfigParser.RemovePaths(beginText, Settings.Default.FilesPathStyleIndex);
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
                            FullResult = fileAndLine + beginText + highlight + endText,
                            IsActiveDocumentCpp = IsActiveDocumentCpp
                        };

                        if(searchOptions.IsFileSearch)
                        {
                            WrapperApp.GetIcon(fileAndLine, BackgroundColor, newSearchResult);
                        }

                        newSearchResults.Add(newSearchResult);

                        if (searchOptions.IsNewSearch && newSearchResults.Count >= 500)
                        {
                            AddResultsBatch(searchOptions);
                        }
                    }
                });
            }
        }

        public void OnFinishSearchEvent(SearchOptions searchOptions)
        {
            TaskRunner.RunOnUIThread(() =>
            {
                if (!SearchEngine.Instance.IsSearchQueued)
                {
                    if (IncludeFilesInput.Text.Length == 0)
                    {
                        searchResults.Clear();
                    }
                    else
                    {
                        AddResultsBatch(searchOptions);
                    }
                }
                else
                {
                    newSearchResults.Clear();
                }
            });
        }

        int selectedSearchResult = -1;

        private void UserControl_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            SearchResult newSelectedSearchResult = null;
            SearchResult oldSelectedSearchResult = selectedSearchResult >= 0 && selectedSearchResult < searchResults.Count ? searchResults[selectedSearchResult] : null;

            if (e.Key == System.Windows.Input.Key.Up)
            {
                if (selectedSearchResult > 0 && selectedSearchResult < searchResults.Count)
                {
                    newSelectedSearchResult = searchResults[selectedSearchResult - 1];
                }
                else if (searchResults.Count > 0)
                {
                    newSelectedSearchResult = searchResults[0];
                }

                e.Handled = true;
            }
            else if (e.Key == System.Windows.Input.Key.Down)
            {
                if (selectedSearchResult < searchResults.Count - 1 && selectedSearchResult >= 0)
                {
                    newSelectedSearchResult = searchResults[selectedSearchResult + 1];
                }
                else if (searchResults.Count > 0 && selectedSearchResult < 0)
                {
                    newSelectedSearchResult = searchResults[0];
                }

                e.Handled = true;

            }
            else if (e.Key == System.Windows.Input.Key.PageUp)
            {
                if (searchResults.Count > 0)
                {
                    int newIndex = Math.Max(0, selectedSearchResult - (int)Math.Floor(SearchItemsListBox.ActualHeight / Settings.Default.LineHeight));
                    if (newIndex >= 0 && newIndex < searchResults.Count)
                    {
                        newSelectedSearchResult = searchResults[newIndex];
                    }
                }

                e.Handled = true;
            }
            else if (e.Key == System.Windows.Input.Key.PageDown)
            {
                if (searchResults.Count > 0)
                {
                    int newIndex = Math.Min(searchResults.Count - 1, selectedSearchResult + (int)Math.Floor(SearchItemsListBox.ActualHeight / Settings.Default.LineHeight));
                    if (newIndex > 0 && newIndex < searchResults.Count)
                    {
                        newSelectedSearchResult = searchResults[newIndex];
                    }
                }

                e.Handled = true;
            }

            if (e.Handled)
            {
                if (oldSelectedSearchResult != null)
                {
                    oldSelectedSearchResult.IsSelected = false;
                }

                if(newSelectedSearchResult != null)
                {
                    SelectSearchResult(newSelectedSearchResult);
                }
            }

            if (e.Key == System.Windows.Input.Key.Enter)
            {
                SearchResult searchResult = GetSelectedSearchResult();
                if (searchResult != null)
                {
                    if (IsActiveDocumentCpp && (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                    {
                        IncludeSearchResult(searchResult);
                    }
                    else
                    {
                        OpenSearchResult(searchResult);
                    }
                }
            }
            else if (e.Key == System.Windows.Input.Key.C && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                SearchResult selectedSearchResult = GetSelectedSearchResult();
                if (selectedSearchResult != null)
                {
                    try
                    {
                        Clipboard.SetText(selectedSearchResult.FileAndLine);
                    }
                    catch { }
                }
            }
        }

        private SearchResult GetSelectedSearchResult()
        {
            if(selectedSearchResult > -1 && selectedSearchResult < searchResults.Count)
            {
                return searchResults[selectedSearchResult];
            }

            return null;
        }

        private void SelectSearchResult(SearchResult newSelectedSearchResult)
        {
            newSelectedSearchResult.IsSelected = true;
            selectedSearchResult = searchResults.IndexOf(newSelectedSearchResult);

            if (selectedSearchResult >= 0)
            {
                VirtualizingStackPanel virtualizingStackPanel = UIHelper.GetChildOfType<VirtualizingStackPanel>(SearchItemsListBox);
                if (virtualizingStackPanel != null)
                {
                    virtualizingStackPanel.BringIndexIntoViewPublic(selectedSearchResult);
                    ListBoxItem listBoxItem = SearchItemsListBox.ItemContainerGenerator.ContainerFromItem(newSelectedSearchResult) as ListBoxItem;

                    listBoxItem?.BringIntoView();
                }
            }
        }

        private void OpenSearchResult(SearchResult result)
        {
            MainWindow mainWindow = UIHelper.FindAncestor<MainWindow>(this);
            if (mainWindow != null)
            {
                mainWindow.Close();
            }

            WrapperApp.OpenFile(result.FileAndLine, "0");
        }

        private void MenuGoTo_Click(object sender, RoutedEventArgs e)
        {
            SearchResult searchResult = qgrepSearchWindowControl.GetSearchResultFromMenuItem(sender);
            if (searchResult != null)
            {
                OpenSearchResult(searchResult);
            }
        }

        private void MenuCopyFullPath_Click(object sender, RoutedEventArgs e)
        {
            SearchResult searchResult = qgrepSearchWindowControl.GetSearchResultFromMenuItem(sender);
            if (searchResult != null)
            {
                try
                {
                    Clipboard.SetText(searchResult.FileAndLine);
                }
                catch { }
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
                    SelectSearchResult(searchResult);

                    if (e.ClickCount == 2)
                    {
                        OpenSearchResult(searchResult);
                        e.Handled = true;
                    }
                }
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
                    InitProgress.Value = 0;
                    InitButton.IsEnabled = false;
                    CleanButton.IsEnabled = false;
                }
            });
        }

        private void HandleUpdateFinish(DatabaseUpdate databaseUpdate)
        {
            TaskRunner.RunOnUIThread(() =>
            {
                if (databaseUpdate == null || !databaseUpdate.IsSilent)
                {
                    InitProgress.Visibility = Visibility.Collapsed;
                    InitButton.IsEnabled = true;
                    CleanButton.IsEnabled = true;
                    InitInfo.Text = lastMessage;

                    infoUpdateStopWatch.Stop();
                    progressUpdateStopWatch.Stop();
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
                    InitInfo.Text = message;
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
                        InitInfo.Text = message;
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

        private void InitButton_Click(object sender, RoutedEventArgs e)
        {
            SearchEngine.Instance.UpdateDatabaseAsync(new DatabaseUpdate() { ConfigPaths = ConfigParser.Instance.ConfigProjects.Select(x => x.Path).ToList() });
            Find();
        }

        private void CleanButton_Click(object sender, RoutedEventArgs e)
        {
            CleanDatabase();
            SearchEngine.Instance.UpdateDatabaseAsync(new DatabaseUpdate() { ConfigPaths = ConfigParser.Instance.ConfigProjects.Select(x => x.Path).ToList() });
            Find();
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
                    HandleErrorMessage(string.Format(Properties.Resources.CannotCleanIndex, ex.Message), null);
                });
            }
        }

        private void FiltersComboBox_ItemSelectionChanged(object sender, Xceed.Wpf.Toolkit.Primitives.ItemSelectionChangedEventArgs e)
        {
            Find();
        }

        private List<string> GetSelectedConfigProjects()
        {
            if (ConfigParser.Instance.ConfigProjects.Count == 1)
            {
                return new List<string>() { ConfigParser.ToUtf8(ConfigParser.Instance.ConfigProjects[0].Path) };
            }
            else
            {
                return FiltersComboBox.SelectedItems.Cast<ConfigProject>().Select(x => ConfigParser.ToUtf8(x.Path)).ToList();
            }
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

        public void OnErrorEvent(string message, SearchOptions searchOptions)
        {
            TaskRunner.RunOnUIThread(() =>
            {
                ErrorLabel.Text = message;
            });
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            UIHelper.CreateWindow(new qgrepControls.SearchWindow.FilesSettingsWindow(this), Properties.Resources.Settings, WrapperApp, this).ShowDialog();
        }

        private void MenuIncludeFile_Click(object sender, RoutedEventArgs e)
        {
            SearchResult searchResult = qgrepSearchWindowControl.GetSearchResultFromMenuItem(sender);
            if(searchResult != null)
            {
                IncludeSearchResult(searchResult);
            }
        }

        private void IncludeSearchResult(SearchResult searchResult)
        {
            MainWindow mainWindow = UIHelper.FindAncestor<MainWindow>(this);
            if (mainWindow != null)
            {
                mainWindow.Close();
            }

            WrapperApp.IncludeFile(searchResult.FileAndLine);
        }
    }
}