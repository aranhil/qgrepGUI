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

namespace qgrepControls.ToolWindows
{
    partial class SearchResult: INotifyPropertyChanged
    {
        private bool isSelected;

        public int Index { get; set; }
        public string File { get; set; }
        public string BeginText { get; set; }
        public string EndText { get; set; }
        public string HighlightedText { get; set; }

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
        private System.Timers.Timer UpdateTimer = new System.Timers.Timer();
        private string LastResults = "";

        public qgrepSearchWindowControl(IExtensionInterface extensionInterface)
        {
            ExtensionInterface = extensionInterface;

            InitializeComponent();
            SearchItemsControl.DataContext = items;

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

            SolutionLoaded();

            string colorSchemesJson = System.Text.Encoding.Default.GetString(qgrepControls.Properties.Resources.colors_schemes);
            colorSchemes = JsonConvert.DeserializeObject<ColorScheme[]>(colorSchemesJson);

            UpdateTimer.Enabled = true;

            UpdateColorsFromSettings();
            UpdateFromSettings();
        }

        public System.Windows.Media.Color ConvertColor(System.Drawing.Color color)
        {
            return System.Windows.Media.Color.FromArgb(color.A, color.R, color.G, color.B);
        }
        public System.Drawing.Color ConvertColor(System.Windows.Media.Color color)
        {
            return System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B); ;
        }

        public void UpdateFilters()
        {
            Visibility visibility = Visibility.Collapsed;

            if (ConfigParser != null)
            {
                FiltersComboBox.ItemsSource = ConfigParser.ConfigProjects;

                if (ConfigParser.ConfigProjects.Count > 0)
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

        ObservableCollection<SearchResult> items = new ObservableCollection<SearchResult>();
        int selectedItem = -1;

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
                items.Clear();
                return;
            }

            List<string> arguments = new List<string>();

            arguments.Add("qgrep");
            arguments.Add("search");

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
                        string file = "", beginText = "", endText = "", highlightedText = "";

                        int currentIndex = currentLine.IndexOf('\xB0');
                        if (currentIndex >= 0)
                        {
                            file = currentLine.Substring(0, currentIndex);
                            ConfigParser.RemovePaths(ref file);

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
                            ConfigParser.RemovePaths(ref file);

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
                            BeginText = beginText,
                            EndText = endText,
                            HighlightedText = highlightedText,
                            IsSelected = false,
                        });

                        resultIndex++;
                        lastIndex = index + 1;
                    }
                }

                Dispatcher.Invoke(new Action(() =>
                {
                    Errors += errors;
                    SearchItemsControl.DataContext = items = newItems;

                    foreach(string error in errors.Split('\n'))
                    {
                        ProcessErrorMessage(error);
                    }

                    if (items.Count > 0)
                    {
                        items[0].IsSelected = true;
                        selectedItem = 0;
                    }
                    else
                    {
                        selectedItem = -1;
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
                if(selectedItem >= 0 && selectedItem < items.Count)
                {
                    items[selectedItem].IsSelected = false;
                }

                SearchResult newSelectedItem = stackPanel.DataContext as SearchResult;
                if (newSelectedItem != null)
                {
                    newSelectedItem.IsSelected = true;
                    selectedItem = newSelectedItem.Index;
                }

                if (e.ClickCount == 2)
                {
                    OpenSelectedStackPanel();
                }

            }
        }

        private void OpenSelectedStackPanel()
        {
            if (selectedItem >= 0 && selectedItem < items.Count)
            {
                SearchResult selectedResult = items[selectedItem];
                try
                {
                    int lastIndex = selectedResult.File.LastIndexOf('(');
                    string file = selectedResult.File.Substring(0, lastIndex);
                    string line = selectedResult.File.Substring(lastIndex + 1, selectedResult.File.Length - lastIndex - 2);

                    ExtensionInterface.OpenFile(file, line);
                }
                catch (Exception)
                {
                }
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
                if (ConfigPath.IndexOf('\\') >= 0)
                {
                    String configFolder = ConfigPath.Substring(0, ConfigPath.LastIndexOf('\\'));

                    File.Delete(configFolder + "\\searchdb.qgd");
                    File.Delete(configFolder + "\\searchdb.qgf");
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
                    List<string> parameters = new List<string>();
                    parameters.Add("qgrep");
                    parameters.Add("update");
                    parameters.Add(configProject.Path);
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
                if (selectedItem > 0 && selectedItem < items.Count)
                {
                    items[selectedItem].IsSelected = false;
                    selectedItem--;
                    items[selectedItem].IsSelected = true;
                }
            }
            else if (e.Key == System.Windows.Input.Key.Down)
            {
                if (selectedItem < items.Count - 1 && selectedItem >= 0)
                {
                    items[selectedItem].IsSelected = false;
                    selectedItem++;
                    items[selectedItem].IsSelected = true;
                }
            }
            else if(e.Key == System.Windows.Input.Key.PageUp)
            {
                if (selectedItem > 0 && selectedItem < items.Count)
                {
                    items[selectedItem].IsSelected = false;
                    selectedItem = Math.Max(0, selectedItem - (int)(Math.Floor(SearchItemsControl.ActualHeight / 16.0f)));
                    items[selectedItem].IsSelected = true;
                }
            }
            else if(e.Key == System.Windows.Input.Key.PageDown)
            {
                if (selectedItem < items.Count - 1 && selectedItem >= 0)
                {
                    items[selectedItem].IsSelected = false;
                    selectedItem = Math.Min(items.Count - 1, selectedItem + (int)(Math.Floor(SearchItemsControl.ActualHeight / 16.0f)));
                    items[selectedItem].IsSelected = true;
                }
            }
            else if (e.Key == System.Windows.Input.Key.Enter)
            {
                OpenSelectedStackPanel();
            }
            else if(e.Key == System.Windows.Input.Key.C && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                LastResults = LastResults.Replace("\xB0", "\t");
                LastResults = LastResults.Replace("\xB1", "");
                LastResults = LastResults.Replace("\xB2", "");
                Clipboard.SetText(LastResults);
            }

            if (VisualTreeHelper.GetChildrenCount(SearchItemsControl) > 0)
            {
                Border childBorder = VisualTreeHelper.GetChild(SearchItemsControl, 0) as Border;
                if (childBorder != null && VisualTreeHelper.GetChildrenCount(childBorder) > 0)
                {
                    ScrollViewer childScrollbar = VisualTreeHelper.GetChild(childBorder, 0) as ScrollViewer;
                    if (childScrollbar != null)
                    {
                        double currentOffset = (double)selectedItem;
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
            Find();
        }
    }
}