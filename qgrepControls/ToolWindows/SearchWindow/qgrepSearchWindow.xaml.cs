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

namespace qgrepControls.ToolWindows
{
    partial class SearchResult: INotifyPropertyChanged
    {
        private bool isSelected;

        public int Index { get; set; }
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

    public class ColorScheme
    {
        public string Name = "";
        public ColorEntry[] ColorEntries = new ColorEntry[] { };
    }

    public class Customer
    {
        public string ContentData { get; set; }
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
                InitInfo.Text = "Last updated: " + GetTimeAgoString(Settings.Default.LastUpdated);
            }));
        }

        public System.Windows.Media.Color ConvertColor(System.Drawing.Color color)
        {
            return System.Windows.Media.Color.FromArgb(color.A, color.R, color.G, color.B);
        }
        public System.Drawing.Color ConvertColor(System.Windows.Media.Color color)
        {
            return System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B); ;
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

                UpdateDatabase();
                UpdateFilters();

                WarningText.Visibility = Visibility.Hidden;
                InitButton.Visibility = Visibility.Visible;
                CleanButton.Visibility = Visibility.Visible;
            }
        }

        public void UpdateFromSettings()
        {
            Visibility visibility = Settings.Default.ShowIncludes == true ? Visibility.Visible : Visibility.Collapsed;
            IncludeFilesGrid.Visibility = visibility;

            visibility = Settings.Default.ShowExcludes == true ? Visibility.Visible : Visibility.Collapsed;
            ExcludeFilesGrid.Visibility = visibility;
        }

        public void UpdateColorsFromSettings()
        {
            if(Settings.Default.ColorScheme < colorSchemes.Length)
            {
                foreach(ColorEntry colorEntry in colorSchemes[Settings.Default.ColorScheme].ColorEntries)
                {
                    Resources[colorEntry.Name] = new SolidColorBrush(ConvertColor(colorEntry.Color));
                }
            }
        }

        public void UpdateColors(Dictionary<string, System.Windows.Media.Color> colors)
        {
            foreach (var color in colors)
            {
                Resources[color.Key] = new SolidColorBrush(color.Value);
            }
        }

        public Dictionary<string, System.Windows.Media.Color> GetColorsFromColorScheme()
        {
            Dictionary<string, System.Windows.Media.Color> results = new Dictionary<string, System.Windows.Media.Color>();

            if (Settings.Default.ColorScheme < colorSchemes.Length)
            {
                foreach (ColorEntry colorEntry in colorSchemes[Settings.Default.ColorScheme].ColorEntries)
                {
                    results[colorEntry.Name] = ConvertColor(colorEntry.Color);
                }
            }

            return results;
        }

        private void SaveOptions()
        {
            Settings.Default.CaseSensitive = SearchCaseSensitive.IsChecked == true;
            Settings.Default.RegEx = SearchRegEx.IsChecked == true;
            Settings.Default.WholeWord = SearchWholeWord.IsChecked == true;
            Settings.Default.IncludesRegEx = IncludeRegEx.IsChecked == true;
            Settings.Default.ShowExcludes = ExcludeRegEx.IsChecked == true;

            Settings.Default.Save();
        }

        private void Find()
        {
            if (EngineBusy)
            {
                QueueFind = true;
                return;
            }

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

            if(SearchInput.Text.Length == 0)
            {
                searchResults.Clear();
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
                arguments.Add("L10000");
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

            Task.Run(() =>
            {
                ObservableCollection<SearchResult> newItems = new ObservableCollection<SearchResult>();
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
                        string file = "", beginText = "", endText = "", highlightedText = "", fullFile = "", fullResult = "";

                        fullResult = currentLine.Replace("\xB0", "");
                        fullResult = fullResult.Replace("\xB1", "");
                        fullResult = fullResult.Replace("\xB2", "");

                        int currentIndex = currentLine.IndexOf('\xB0');
                        if (currentIndex >= 0)
                        {
                            fullFile = currentLine.Substring(0, currentIndex);
                            file = ConfigParser.RemovePaths(fullFile);

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

                        newItems.Add(new SearchResult()
                        {
                            Index = resultIndex,
                            File = file,
                            FullFile = fullFile,
                            BeginText = beginText,
                            EndText = endText,
                            HighlightedText = highlightedText,
                            FullResult = fullResult,
                            IsSelected = false,
                        });

                        resultIndex++;
                        lastIndex = index + 1;
                    }
                }

                Dispatcher.Invoke(new Action(() =>
                {
                    Errors += errors;
                    SearchItemsControl.DataContext = searchResults = newItems;

                    foreach(string error in errors.Split('\n'))
                    {
                        ProcessErrorMessage(error);
                    }

                    if (searchResults.Count > 0)
                    {
                        searchResults[0].IsSelected = true;
                        selectedSearchResult = 0;
                    }
                    else
                    {
                        selectedSearchResult = -1;
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

        private void VirtualizingStackPanel_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            VirtualizingStackPanel stackPanel = sender as VirtualizingStackPanel;
            if (stackPanel != null)
            {
                if(selectedSearchResult >= 0 && selectedSearchResult < searchResults.Count)
                {
                    searchResults[selectedSearchResult].IsSelected = false;
                }

                SearchResult newSelectedItem = stackPanel.DataContext as SearchResult;
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
        }

        private void OpenSelectedStackPanel()
        {
            if (selectedSearchResult >= 0 && selectedSearchResult < searchResults.Count)
            {
                SearchResult selectedResult = searchResults[selectedSearchResult];
                OpenResult(selectedResult);
            }
        }

        private void OpenResult(SearchResult result)
        {
            try
            {
                int lastIndex = result.File.LastIndexOf('(');
                string file = result.File.Substring(0, lastIndex);
                string line = result.File.Substring(lastIndex + 1, result.File.Length - lastIndex - 2);

                ExtensionInterface.OpenFile(file, line);
            }
            catch (Exception)
            {
            }
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
                InitInfo.Text = message;
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
                    InitInfo.Text = message;
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

            ExtensionInterface.CreateWindow(new qgrepControls.ToolWindows.ProjectsWindow(this), "Projects configuration").ShowModal();
        }

        private void AdvancedButton_Click(object sender, RoutedEventArgs e)
        {
            ExtensionInterface.CreateWindow(new qgrepControls.ToolWindows.SettingsWindow(this), "Advanced settings").ShowModal();
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
            ExtensionInterface.CreateWindow(new qgrepControls.ToolWindows.ColorsWindow(this), "Color settings").ShowModal();
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

        private void UserControl_LostFocus(object sender, RoutedEventArgs e)
        {
            ProcessQueue();
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

        private SearchResult GetResultFromMenuItem(object sender)
        {
            MenuItem menuItem = sender as MenuItem;
            if (menuItem != null)
            {
                VirtualizingStackPanel resultPanel = (menuItem.Parent as ContextMenu)?.PlacementTarget as VirtualizingStackPanel;
                if (resultPanel != null)
                {
                    return resultPanel.DataContext as SearchResult;
                }
            }

            return null;
        }

        private void MenuGoTo_Click(object sender, RoutedEventArgs e)
        {
            SearchResult searchResult = GetResultFromMenuItem(sender);
            if (searchResult != null)
            {
                OpenResult(searchResult);
            }
        }

        private void MenuCopyText_Click(object sender, RoutedEventArgs e)
        {
            SearchResult searchResult = GetResultFromMenuItem(sender);
            if(searchResult != null)
            {
                string text = searchResult.BeginText + searchResult.HighlightedText + searchResult.EndText;
                Clipboard.SetText(text);
            }
        }

        private void MenuCopyFullPath_Click(object sender, RoutedEventArgs e)
        {
            SearchResult searchResult = GetResultFromMenuItem(sender);
            if (searchResult != null)
            {
                int lastIndex = searchResult.FullFile.LastIndexOf('(');
                string text = searchResult.FullFile.Substring(0, lastIndex);

                Clipboard.SetText(text);
            }
        }

        private void MenuCopyResult_Click(object sender, RoutedEventArgs e)
        {
            SearchResult searchResult = GetResultFromMenuItem(sender);
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
    }
}