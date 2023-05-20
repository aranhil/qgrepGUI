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
    public partial class qgrepFilesWindowControl : UserControl
    {
        public IExtensionInterface ExtensionInterface;
        private SearchEngine SearchEngine = new SearchEngine();

        public qgrepFilesWindowControl(IExtensionInterface extensionInterface)
        {
            InitializeComponent();

            IncludeFilesInput.Focus();

            ExtensionInterface = extensionInterface;
            ConfigParser.Init(System.IO.Path.GetDirectoryName(extensionInterface.GetSolutionPath()));

            SearchEngine.ResultCallback = HandleSearchResult;
            SearchEngine.StartSearchCallback = HandleSearchStart;
            SearchEngine.FinishSearchCallback = HandleSearchFinish;

            ThemeHelper.UpdateColorsFromSettings(this, ExtensionInterface, false);
            ThemeHelper.UpdateFontFromSettings(this, extensionInterface);

            PresentationTraceSources.DataBindingSource.Switch.Level = SourceLevels.Critical;
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

            if (IncludeFilesInput.Text.Length != 0)
            {
                SearchOptions searchOptions = new SearchOptions()
                {
                    Query = IncludeFilesInput.Text,
                    FilterResults = "",
                    IncludeFilesRegEx = false,
                    FilterResultsRegEx = false,
                    GroupingMode = 0,
                    Configs = ConfigParser.Instance.ConfigProjects.Select(x => x.Path).ToList(),
                    CacheUsageType = CacheUsageType.Normal,
                    BypassHighlight = true
                };

                SearchEngine.SearchFilesAsync(searchOptions);
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

        private void HandleSearchStart(SearchOptions searchOptions)
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

        private void HandleSearchResult(string file, string lineNumber, string beginText, string highlight, string endText, SearchOptions searchOptions)
        {
            if (!SearchEngine.IsSearchQueued)
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
                if (!SearchEngine.IsSearchQueued)
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
    }
}