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

namespace qgrepControls.SearchWindow
{
    partial class SearchResultGroup : INotifyPropertyChanged
    {
        private bool isSelected = false;
        private bool isExpanded = false;

        public int Index { get; set; }
        public string File { get; set; } = "";
        public string FullFile { get; set; } = "";
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
        public string Line { get; set; }
        public string File { get; set; }
        public string FullFile { get; set; }
        public string BeginText { get; set; }
        public string EndText { get; set; }
        public string HighlightedText { get; set; }
        public string FullResult { get; set; }

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

        public qgrepSearchWindowControl(IExtensionInterface extensionInterface)
        {
            ExtensionInterface = extensionInterface;

            InitializeComponent();
            SearchItemsControl.DataContext = searchResults;

            InitButton.Visibility = Visibility.Hidden;
            CleanButton.Visibility = Visibility.Hidden;
            InitProgress.Visibility = Visibility.Collapsed;
            InitInfo.Visibility = Visibility.Hidden;
            Overlay.Visibility = Visibility.Collapsed;

            SearchCaseSensitive.IsChecked = Settings.Default.CaseSensitive;
            SearchRegEx.IsChecked = Settings.Default.RegEx;
            SearchWholeWord.IsChecked = Settings.Default.WholeWord;

            IncludeRegEx.IsChecked = Settings.Default.IncludesRegEx;
            ExcludeRegEx.IsChecked = Settings.Default.ExcludesRegEx;
            FilterRegEx.IsChecked = Settings.Default.FilterRegEx;

            StartTimer();
            SolutionLoaded();

            string colorSchemesJson = System.Text.Encoding.Default.GetString(qgrepControls.Properties.Resources.colors_schemes);
            colorSchemes = JsonConvert.DeserializeObject<ColorScheme[]>(colorSchemesJson);

            UpdateColorsFromSettings();
            UpdateFromSettings();
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

        private void UpdateTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                if(!EngineBusy)
                {
                    if (Settings.Default["LastUpdated"] != null)
                    {
                        InitInfo.Content = "Last updated: " + GetTimeAgoString(Settings.Default.LastUpdated);
                    }
                }
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
                UpdateDatabase();
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

        private void Find()
        {
            if (EngineBusy)
            {
                QueueFind = true;
                return;
            }

            SearchItemsControl.Visibility = Settings.Default.GroupingIndex == 0 ? Visibility.Visible : Visibility.Collapsed;
            SearchItemsTreeView.Visibility = Settings.Default.GroupingIndex != 0 ? Visibility.Visible : Visibility.Collapsed;

            if(SearchInput.Text.Length == 0)
            {
                searchResults.Clear();
                searchResultsGroups.Clear();
                return;
            }

            List<string> arguments = new List<string>
            {
                "qgrep",
                "search"
            };

            string configs = "";

            for (int i = 0; i < FiltersComboBox.SelectedItems.Count; i++)
            {
                ConfigProject configProject = FiltersComboBox.SelectedItems[i] as ConfigProject;
                if(configProject != null)
                {
                    configs += configProject.Path;
                    if (i < FiltersComboBox.SelectedItems.Count - 1)
                    {
                        configs += ",";
                    }
                }

            }
            arguments.Add(configs);

            if (SearchCaseSensitive.IsChecked == false) arguments.Add("i");

            if (SearchRegEx.IsChecked == false && SearchWholeWord.IsChecked == false) arguments.Add("l");

            if (Settings.Default.ShowIncludes && IncludeFilesInput.Text.Length > 0)
            {
                arguments.Add("fi" + (IncludeRegEx.IsChecked == true ? IncludeFilesInput.Text : Regex.Escape(IncludeFilesInput.Text)));
            }

            if (Settings.Default.ShowExcludes && ExcludeFilesInput.Text.Length > 0)
            {
                arguments.Add("fe" + (ExcludeRegEx.IsChecked == true ? ExcludeFilesInput.Text : Regex.Escape(ExcludeFilesInput.Text)));
            }

            arguments.Add("HM");

            if (SearchInput.Text.Length == 1)
            {
                arguments.Add("L1000");
            }
            else if (SearchInput.Text.Length == 2)
            {
                arguments.Add("L2000");
            }
            else if (SearchInput.Text.Length == 3)
            {
                arguments.Add("L4000");
            }
            else
            {
                arguments.Add("L6000");
            }

            arguments.Add("V");

            if(SearchWholeWord.IsChecked == true)
            {
                arguments.Add("\\b" + (SearchRegEx.IsChecked == true ? SearchInput.Text : Regex.Escape(SearchInput.Text)) + "\\b");
            }
            else
            {
                arguments.Add(SearchInput.Text);
            }

            EngineBusy = true;

            string resultsFilterText = Settings.Default.ShowFilter ? FilterResultsInput.Text.ToLower() : "";
            bool resultsFilterRegEx = FilterRegEx.IsChecked ?? false;

            Task.Run(() =>
            {
                int groupIndex = 0;

                ObservableCollection<SearchResult> newItems = new ObservableCollection<SearchResult>();
                ObservableCollection<SearchResultGroup> newGroups = new ObservableCollection<SearchResultGroup>();
                SearchResultGroup lastGroup = new SearchResultGroup() { Index = groupIndex++ };
                string errors = "";

                string results = QGrepWrapper.CallQGrep(arguments, ref errors);
                LastResults = results;

                int resultIndex = 0;
                int lastIndex = 0;
                for (int index = 0; index < results.Length; index++)
                {
                    if (results[index] == '\n')
                    {
                        string currentLine = results.Substring(lastIndex, index - lastIndex);
                        string file = "", beginText = "", endText = "", highlightedText = "", fullFile = "", fullResult = "", lineNo = "";

                        fullResult = currentLine.Replace("\xB0", "");
                        fullResult = fullResult.Replace("\xB1", "");
                        fullResult = fullResult.Replace("\xB2", "");

                        int currentIndex = currentLine.IndexOf('\xB0');
                        if (currentIndex >= 0)
                        {
                            fullFile = currentLine.Substring(0, currentIndex);
                            file = ConfigParser.RemovePaths(fullFile);

                            int indexOfParanthesis = fullFile.LastIndexOf('(');
                            string filePath = ConfigParser.RemovePaths(fullFile.Substring(0, indexOfParanthesis));
                            lineNo = fullFile.Substring(indexOfParanthesis + 1, fullFile.Length - indexOfParanthesis - 2);

                            if (resultsFilterText.Length > 0)
                            {
                                string resultText = currentLine.Substring(currentIndex + 1).ToLower();
                                resultText = resultText.Replace("\xB0", "");
                                resultText = resultText.Replace("\xB1", "");
                                resultText = resultText.Replace("\xB2", "");

                                if (resultsFilterRegEx)
                                {
                                    if (!Regex.Match(filePath, resultsFilterText).Success && !Regex.Match(resultText, resultsFilterText).Success)
                                    {
                                        lastIndex = index + 1;
                                        continue;
                                    }
                                }
                                else
                                {
                                    if (!filePath.ToLower().Contains(resultsFilterText) && !resultText.Contains(resultsFilterText))
                                    {
                                        lastIndex = index + 1;
                                        continue;
                                    }
                                }
                            }

                            if (Settings.Default.GroupingIndex == 1)
                            {
                                if (lastGroup.File.Length == 0)
                                {
                                    lastGroup.File = filePath;
                                    lastGroup.FullFile = fullFile.Substring(0, indexOfParanthesis);
                                }
                                else if (lastGroup.File != filePath)
                                {
                                    newGroups.Add(lastGroup);
                                    lastGroup = new SearchResultGroup() { Index = groupIndex++ };
                                    lastGroup.File = filePath;
                                    lastGroup.FullFile = fullFile.Substring(0, indexOfParanthesis);
                                }
                            }

                            if (currentIndex >= 0 && currentIndex + 1 < currentLine.Length)
                            {
                                currentLine = currentLine.Substring(currentIndex + 1);
                            }
                            else
                            {
                                currentLine = "";
                            }
                        }
                        else
                        {
                        }

                        currentIndex = currentLine.IndexOf('\xB1');
                        if (currentIndex >= 0)
                        {
                            beginText = " " + currentLine.Substring(0, currentIndex);

                            if (currentIndex >= 0 && currentIndex + 1 < currentLine.Length)
                            {
                                currentLine = currentLine.Substring(currentIndex + 1);
                            }
                            else
                            {
                                currentLine = "";
                            }
                        }
                        else
                        {
                            beginText = " " + currentLine;
                            currentLine = "";
                        }

                        currentIndex = currentLine.IndexOf('\xB2');
                        if (currentIndex >= 0)
                        {
                            highlightedText = currentLine.Substring(0, currentIndex);

                            if (currentIndex >= 0 && currentIndex + 1 < currentLine.Length)
                            {
                                currentLine = currentLine.Substring(currentIndex + 1);
                            }
                            else
                            {
                                currentLine = "";
                            }
                        }
                        else
                        {
                        }

                        currentLine = currentLine.Replace("\xB1", "");
                        currentLine = currentLine.Replace("\xB2", "");
                        endText = currentLine;

                        SearchResult newSearchResult = new SearchResult()
                        {
                            Index = resultIndex,
                            File = file,
                            FullFile = fullFile,
                            BeginText = beginText,
                            EndText = endText,
                            HighlightedText = highlightedText,
                            FullResult = fullResult,
                            IsSelected = false,
                            Line = lineNo
                        };


                        if (Settings.Default.GroupingIndex == 0)
                        {
                            newItems.Add(newSearchResult);
                        }
                        else
                        {
                            lastGroup.SearchResults.Add(newSearchResult);
                        }

                        resultIndex++;
                        lastIndex = index + 1;
                    }
                }

                if (Settings.Default.GroupingIndex == 1)
                {
                    newGroups.Add(lastGroup);
                }

                Dispatcher.Invoke(new Action(() =>
                {
                    Errors += errors;
                    bool scrollToTop = false;

                    if(Settings.Default.GroupingIndex == 0)
                    {
                        if(!SearchResultsAreIdentical(searchResults, newItems))
                        {
                            SearchItemsControl.DataContext = searchResults = newItems;

                            if (searchResults.Count > 0)
                            {
                                searchResults[0].IsSelected = true;
                                selectedSearchResult = 0;
                            }
                            else
                            {
                                selectedSearchResult = -1;
                            }

                            scrollToTop = true;
                        }
                    }
                    else
                    {
                        if (!SearchGroupsAreIdentical(searchResultsGroups, newGroups))
                        {
                            if(Settings.Default.ExpandModeIndex == 1 || Settings.Default.ExpandModeIndex == 2)
                            {
                                bool expandAll = true;

                                if(Settings.Default.ExpandModeIndex == 1)
                                {
                                    int count = 0;
                                    foreach (var group in newGroups)
                                    {
                                        count += group.SearchResults.Count;
                                    }

                                    if(count > 500)
                                    {
                                        expandAll = false;
                                    }
                                }

                                if (expandAll)
                                {
                                    foreach (var group in newGroups)
                                    {
                                        group.IsExpanded = true;
                                    }
                                }
                            }

                            SearchItemsTreeView.ItemsSource = searchResultsGroups = newGroups;

                            if (searchResultsGroups.Count > 0)
                            {
                                searchResultsGroups[0].IsSelected = true;
                                selectedSearchResultGroup = 0;
                            }
                            else
                            {
                                selectedSearchResultGroup = 0;
                            }

                            scrollToTop = true;
                        }
                    }

                    if(scrollToTop)
                    {
                        if (VisualTreeHelper.GetChildrenCount(SearchItemsControl) > 0)
                        {
                            Border childBorder = VisualTreeHelper.GetChild(SearchItemsControl, 0) as Border;
                            if (childBorder != null && VisualTreeHelper.GetChildrenCount(childBorder) > 0)
                            {
                                ScrollViewer childScrollbar = VisualTreeHelper.GetChild(childBorder, 0) as ScrollViewer;
                                if (childScrollbar != null)
                                {
                                    childScrollbar.ScrollToTop();
                                }
                            }
                        }
                    }

                    foreach (string error in errors.Split('\n'))
                    {
                        ProcessErrorMessage(error);
                    }

                    if (System.Diagnostics.Debugger.IsAttached)
                    {
                        System.Threading.Thread.Sleep(1);
                    }

                    EngineBusy = false;
                    ProcessQueue();
                }));
            });
        }

        private bool ResultsAreIdentical(SearchResult oldResult, SearchResult newResult)
        {
            if (!oldResult.FullFile.Equals(newResult.FullFile))
            {
                return false;
            }
            if (!oldResult.BeginText.Equals(newResult.BeginText))
            {
                return false;
            }
            if (!oldResult.HighlightedText.Equals(newResult.HighlightedText))
            {
                return false;
            }
            if (!oldResult.EndText.Equals(newResult.EndText))
            {
                return false;
            }

            return true;
        }

        private bool SearchResultsAreIdentical(ObservableCollection<SearchResult> oldResults, ObservableCollection<SearchResult> newResults)
        {
            if(oldResults.Count != newResults.Count) 
            { 
                return false;
            }

            for(int i = 0; i <  oldResults.Count; i++)
            {
                if (!ResultsAreIdentical(oldResults[i], newResults[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private bool SearchGroupsAreIdentical(ObservableCollection<SearchResultGroup> oldResults, ObservableCollection<SearchResultGroup> newResults)
        {
            if (oldResults.Count != newResults.Count)
            {
                return false;
            }

            for (int i = 0; i < oldResults.Count; i++)
            {
                if (oldResults[i].SearchResults.Count != newResults[i].SearchResults.Count)
                {
                    return false;
                }

                for(int j = 0; j < oldResults[i].SearchResults.Count; j++)
                {
                    if (!ResultsAreIdentical(oldResults[i].SearchResults[j], newResults[i].SearchResults[j]))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private void SearchResult_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            DockPanel dockPanel = sender as DockPanel;
            if (dockPanel != null)
            {
                SearchResult selectedResult = GetSelectedSearchResult();
                if(selectedResult != null)
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
                }

                if (e.ClickCount == 2)
                {
                    OpenSelectedStackPanel();
                }

            }

            e.Handled = true;
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
            if(Settings.Default.GroupingIndex == 0)
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
            SearchResult selectedResult = GetSelectedSearchResult();
            if (selectedResult != null)
            {
                OpenSearchResult(selectedResult);
            }
        }

        private void OpenSearchResult(SearchResult result)
        {
            try
            {
                int lastIndex = result.FullFile.LastIndexOf('(');
                string file = result.FullFile.Substring(0, lastIndex);
                string line = result.FullFile.Substring(lastIndex + 1, result.FullFile.Length - lastIndex - 2);

                ExtensionInterface.OpenFile(file, line);
            }
            catch (Exception)
            {
            }
        }
        private void OpenSearchGroup(SearchResultGroup result)
        {
            ExtensionInterface.OpenFile(result.FullFile, "0");
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

        private void ProcessInitMessage(string message)
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
                    QGrepWrapper.Callback callback = new QGrepWrapper.Callback(ProcessInitMessage);
                    QGrepWrapper.Callback errorsCallback = new QGrepWrapper.Callback(ProcessErrorMessage);
                    List<string> parameters = new List<string>
                    {
                        "qgrep",
                        "update",
                        configProject.Path
                    };
                    QGrepWrapper.CallQGrepAsync(parameters, callback, errorsCallback);
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
            InitInfo.Visibility = Visibility.Visible;
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
            Find();
            SaveOptions();
        }

        private void SearchRegEx_Click(object sender, RoutedEventArgs e)
        {
            Find();
            SaveOptions();
        }

        private void SearchWholeWord_Click(object sender, RoutedEventArgs e)
        {
            Find();
            SaveOptions();
        }

        private void PathsButton_Click(object sender, RoutedEventArgs e)
        {
            if(EngineBusy)
            {
                return;
            }

            CreateWindow(new qgrepControls.SearchWindow.ProjectsWindow(this), "Search configurations", this).ShowDialog();
            UpdateWarning();
            UpdateDatabase();
        }

        private void AdvancedButton_Click(object sender, RoutedEventArgs e)
        {
            CreateWindow(new qgrepControls.SearchWindow.SettingsWindow(this), "Advanced settings", this).ShowDialog();
            Find();
        }

        private void UserControl_GotFocus(object sender, RoutedEventArgs e)
        {
            if (ExtensionInterface.WindowOpened)
            {
                SearchInput.Focus();

                string selectedText = ExtensionInterface.GetSelectedText();
                if(selectedText.Length > 0)
                {
                    SearchInput.Text = selectedText;
                    SearchInput.CaretIndex = SearchInput.Text.Length;

                    if(searchHistory.Count == 0 || searchHistory.Last() != selectedText)
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
        }

        private void SearchInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            Find();

            if(SearchInput.Text.Length > 0)
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
        }

        private void UserControl_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Up)
            {
                if (selectedSearchResult > 0 && selectedSearchResult < searchResults.Count)
                {
                    searchResults[selectedSearchResult].IsSelected = false;
                    selectedSearchResult--;
                    searchResults[selectedSearchResult].IsSelected = true;
                }
            }
            else if (e.Key == System.Windows.Input.Key.Down)
            {
                if (selectedSearchResult < searchResults.Count - 1 && selectedSearchResult >= 0)
                {
                    searchResults[selectedSearchResult].IsSelected = false;
                    selectedSearchResult++;
                    searchResults[selectedSearchResult].IsSelected = true;
                }
            }
            else if(e.Key == System.Windows.Input.Key.PageUp)
            {
                if (selectedSearchResult > 0 && selectedSearchResult < searchResults.Count)
                {
                    searchResults[selectedSearchResult].IsSelected = false;
                    selectedSearchResult = Math.Max(0, selectedSearchResult - (int)(Math.Floor(SearchItemsControl.ActualHeight / 16.0f)));
                    searchResults[selectedSearchResult].IsSelected = true;
                }
            }
            else if(e.Key == System.Windows.Input.Key.PageDown)
            {
                if (selectedSearchResult < searchResults.Count - 1 && selectedSearchResult >= 0)
                {
                    searchResults[selectedSearchResult].IsSelected = false;
                    selectedSearchResult = Math.Min(searchResults.Count - 1, selectedSearchResult + (int)(Math.Floor(SearchItemsControl.ActualHeight / 16.0f)));
                    searchResults[selectedSearchResult].IsSelected = true;
                }
            }
            else if (e.Key == System.Windows.Input.Key.Enter)
            {
                OpenSelectedStackPanel();
            }
            else if(e.Key == System.Windows.Input.Key.C && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                if (selectedSearchResult >= 0 && selectedSearchResult < searchResults.Count)
                {
                    SearchResult searchResult = searchResults[selectedSearchResult];
                    string text = searchResult.BeginText + searchResult.HighlightedText + searchResult.EndText;
                    Clipboard.SetText(text);
                }
            }

            if (VisualTreeHelper.GetChildrenCount(SearchItemsControl) > 0)
            {
                Border childBorder = VisualTreeHelper.GetChild(SearchItemsControl, 0) as Border;
                if (childBorder != null && VisualTreeHelper.GetChildrenCount(childBorder) > 0)
                {
                    ScrollViewer childScrollbar = VisualTreeHelper.GetChild(childBorder, 0) as ScrollViewer;
                    if (childScrollbar != null)
                    {
                        double currentOffset = (double)selectedSearchResult;
                        double verticalOffset = childScrollbar.VerticalOffset;
                        double totalOffset = Math.Floor(SearchItemsControl.ActualHeight / 16.0f) - 1.0f;
                        double maxVerticalOffset = verticalOffset + totalOffset;

                        if (currentOffset < verticalOffset)
                        {
                            childScrollbar.ScrollToVerticalOffset(currentOffset);
                        }
                        else if (currentOffset > maxVerticalOffset)
                        {
                            childScrollbar.ScrollToVerticalOffset(currentOffset - totalOffset);
                        }
                    }
                }
            }
        }

        private void Option_Click(object sender, RoutedEventArgs e)
        {
            Find();
            SaveOptions();
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
            Find();
            SaveOptions();
        }

        private void ExcludeRegEx_Click(object sender, RoutedEventArgs e)
        {
            Find();
            SaveOptions();
        }

        private void FilterRegEx_Click(object sender, RoutedEventArgs e)
        {
            Find();
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

            Find();
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

        private void MenuCopyText_Click(object sender, RoutedEventArgs e)
        {
            SearchResult searchResult = GetSearchResultFromMenuItem(sender);
            if(searchResult != null)
            {
                string text = searchResult.BeginText + searchResult.HighlightedText + searchResult.EndText;
                Clipboard.SetText(text);
            }
        }

        private void MenuCopyFullPath_Click(object sender, RoutedEventArgs e)
        {
            SearchResult searchResult = GetSearchResultFromMenuItem(sender);
            if (searchResult != null)
            {
                int lastIndex = searchResult.FullFile.LastIndexOf('(');
                string text = searchResult.FullFile.Substring(0, lastIndex);

                Clipboard.SetText(text);
            }

            SearchResultGroup searchGroup = GetSearchGroupFromMenuItem(sender);
            if (searchGroup != null)
            {
                Clipboard.SetText(searchGroup.FullFile);
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
                if (searchHistory.Count == 0 || searchHistory.Last() != SearchInput.Text)
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
            }

            HistoryPopup.IsOpen = false;
        }
    }
}