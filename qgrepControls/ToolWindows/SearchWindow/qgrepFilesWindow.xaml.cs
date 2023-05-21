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
    public partial class qgrepFilesWindowControl : UserControl, ISearchEngineEventsHandler
    {
        public IExtensionInterface ExtensionInterface;

        public qgrepFilesWindowControl(IExtensionInterface extensionInterface)
        {
            InitializeComponent();

            IncludeFilesInput.Focus();

            ExtensionInterface = extensionInterface;
            ConfigParser.Init(System.IO.Path.GetDirectoryName(extensionInterface.GetSolutionPath()));

            ThemeHelper.UpdateColorsFromSettings(this, ExtensionInterface, false);
            ThemeHelper.UpdateFontFromSettings(this, extensionInterface);

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
                    FilterResults = "",
                    IncludeFilesRegEx = false,
                    FilterResultsRegEx = false,
                    GroupingMode = 0,
                    Configs = GetSelectedConfigProjects(),
                    CacheUsageType = CacheUsageType.Normal,
                    BypassHighlight = true
                };

                SearchEngine.Instance.SearchFilesAsync(searchOptions);
            }
            else
            {
                searchResults.Clear();
            }
        }

        ObservableCollection<SearchResult> searchResults = new ObservableCollection<SearchResult>();
        ObservableCollection<SearchResult> newSearchResults = new ObservableCollection<SearchResult>();
        SearchResult selectedSearchResult = null;
        bool newSearch = false;

        public void OnStartSearchEvent(SearchOptions searchOptions)
        {
            Dispatcher.Invoke(() =>
            {
                newSearch = true;
                selectedSearchResult = null;
            });
        }

        private void AddResultsBatch(SearchOptions searchOptions)
        {
            if (newSearch)
            {
                SearchItemsListBox.ItemsSource = searchResults = newSearchResults;
                newSearchResults = new ObservableCollection<SearchResult>();
                newSearch = false;

                if (searchResults.Count > 0)
                {
                    searchResults[0].IsSelected = true;
                    selectedSearchResult = searchResults[0];
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

        public void OnResultEvent(string file, string lineNumber, string beginText, string highlight, string endText, SearchOptions searchOptions)
        {
            if (!SearchEngine.Instance.IsSearchQueued)
            {
                Dispatcher.Invoke(() =>
                {
                    if (IncludeFilesInput.Text.Length != 0)
                    {
                        string fileAndLine = "";
                        string trimmedFileAndLine = "";

                        if (file.Length > 0 && lineNumber.Length > 0)
                        {
                            fileAndLine = file + "(" + lineNumber + ")";
                            trimmedFileAndLine = ConfigParser.RemovePaths(fileAndLine);
                        }
                        else if(file.Length > 0)
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

                        if (newSearch && newSearchResults.Count >= 500)
                        {
                            AddResultsBatch(searchOptions);
                        }
                    }
                });
            }
        }

        public void OnFinishSearchEvent(SearchOptions searchOptions)
        {
            Dispatcher.Invoke(() =>
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
                    newSearch = false;
                    newSearchResults.Clear();
                }
            });
        }

        private void UserControl_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            FrameworkElement frameworkElement = e.OriginalSource as FrameworkElement;
            if (frameworkElement != null)
            {
                SearchResult searchResult = frameworkElement.DataContext as SearchResult;
                if (searchResult != null)
                {
                    selectedSearchResult = searchResult;
                }
            }
        }

        private void UserControl_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Down ||
                e.Key == System.Windows.Input.Key.Up ||
                e.Key == System.Windows.Input.Key.PageUp ||
                e.Key == System.Windows.Input.Key.PageDown)
            {
                if (IncludeFilesInput.IsFocused)
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
                    }
                }
            }

            if (e.Key == System.Windows.Input.Key.Enter)
            {
                OpenSearchResult(selectedSearchResult);
            }
            else if (e.Key == System.Windows.Input.Key.C && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                if (selectedSearchResult != null)
                {
                    Clipboard.SetText(selectedSearchResult.FileAndLine);
                }
            }
        }
        private void OpenSearchResult(SearchResult result)
        {
            MainWindow mainWindow = UIHelper.FindAncestor<MainWindow>(this);
            if(mainWindow != null)
            {
                mainWindow.Close();
            }

            ExtensionInterface.OpenFile(result.FileAndLine, "0");
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
                Clipboard.SetText(searchResult.FileAndLine);
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

        private Stopwatch infoUpdateStopWatch = new Stopwatch();
        private Stopwatch progressUpdateStopWatch = new Stopwatch();
        private string lastMessage = "";

        private void HandleUpdateStart()
        {
            Dispatcher.Invoke(() =>
            {
                InitProgress.Value = 0;
                InitButton.IsEnabled = false;
                CleanButton.IsEnabled = false;
            });
        }

        private void HandleUpdateFinish()
        {
            Dispatcher.Invoke(new Action(() =>
            {
                InitProgress.Visibility = Visibility.Collapsed;
                InitButton.IsEnabled = true;
                CleanButton.IsEnabled = true;
                InitInfo.Content = lastMessage;

                infoUpdateStopWatch.Stop();
                progressUpdateStopWatch.Stop();
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

        private void HandleProgress(double percentage)
        {
            if (!progressUpdateStopWatch.IsRunning || progressUpdateStopWatch.ElapsedMilliseconds > 20)
            {
                Dispatcher.Invoke(new Action(() =>
                {
                    InitProgress.Value = percentage;
                    InitProgress.Visibility = percentage >= 0 ? Visibility.Visible : Visibility.Collapsed;
                    progressUpdateStopWatch.Restart();
                }));
            }
        }

        private void InitButton_Click(object sender, RoutedEventArgs e)
        {
            SearchEngine.Instance.UpdateDatabaseAsync(ConfigParser.Instance.ConfigProjects.Select(x => x.Path).ToList());
            Find();
        }

        private void CleanButton_Click(object sender, RoutedEventArgs e)
        {
            CleanDatabase();
            SearchEngine.Instance.UpdateDatabaseAsync(ConfigParser.Instance.ConfigProjects.Select(x => x.Path).ToList());
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
                Dispatcher.Invoke(new Action(() =>
                {
                    HandleErrorMessage("Cannot clean indexes: " + ex.Message + "\n");
                }));
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
                return new List<string>() { ConfigParser.Instance.ConfigProjects[0].Path };
            }
            else
            {
                return FiltersComboBox.SelectedItems.Cast<ConfigProject>().Select(x => x.Path).ToList();
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
            Dispatcher.Invoke(() =>
            {
                ErrorLabel.Content = message;
            });
        }
    }
}