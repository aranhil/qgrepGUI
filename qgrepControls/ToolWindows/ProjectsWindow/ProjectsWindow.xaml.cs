using qgrepControls.Classes;
using qgrepControls.ModelViews;
using qgrepControls.Properties;
using qgrepControls.UserControls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Shapes;

namespace qgrepControls.SearchWindow
{
    public partial class ProjectsWindow : System.Windows.Controls.UserControl
    {
        public qgrepSearchWindowControl SearchWindow;
        private ObservableCollection<SearchConfig> SearchConfigs = new ObservableCollection<SearchConfig>();

        public ProjectsWindow(qgrepSearchWindowControl SearchWindow)
        {
            this.SearchWindow = SearchWindow;
            InitializeComponent();

            ProjectsListBox.SetItemsSource(SearchConfigs);
            ProjectsListBox.ItemEditType = ConfigListBox.EditType.Text;
            ProjectsListBox.Title.Text = "Configs";
            ProjectsListBox.AddButton.ToolTip = "Add new config";
            ProjectsListBox.EditButton.ToolTip = "Edit config name";
            ProjectsListBox.RemoveButton.ToolTip = "Remove selected config(s)";
            ProjectsListBox.RemoveAllButton.ToolTip = "Remove all configs";
            ProjectsListBox.AddButton.Click += AddNewProject_Click;
            ProjectsListBox.InnerListBox.SelectionChanged += ConfigProjects_SelectionChanged;
            ProjectsListBox.IsPreviousSelectedOnRemove = true;
            SearchConfigs.CollectionChanged += ConfigProjects_CollectionChanged;

            GroupsListBox.Title.Text = "Groups";
            GroupsListBox.ItemEditType = ConfigListBox.EditType.Text;
            GroupsListBox.AddButton.ToolTip = "Add new group";
            GroupsListBox.EditButton.ToolTip = "Edit group name";
            GroupsListBox.RemoveButton.ToolTip = "Remove selected group(s)";
            GroupsListBox.RemoveAllButton.ToolTip = "Remove all groups";
            GroupsListBox.AddButton.Click += AddNewGroup_Click;
            GroupsListBox.InnerListBox.SelectionChanged += ConfigGroups_SelectionChanged;
            GroupsListBox.IsPreviousSelectedOnRemove = true;

            PathsListBox.Title.Text = "Folders";
            PathsListBox.ItemEditType = ConfigListBox.EditType.None;
            PathsListBox.AddButton.ToolTip = "Add new folder(s)";
            PathsListBox.EditButton.ToolTip = "Edit folder name";
            PathsListBox.RemoveButton.ToolTip = "Remove selected folder(s)";
            PathsListBox.RemoveAllButton.ToolTip = "Remove all folders";
            PathsListBox.AddButton.Click += AddNewPath_Click;
            PathsListBox.IsDeselectable = true;

            RulesListBox.Title.Text = "Filters";
            RulesListBox.ItemEditType = ConfigListBox.EditType.Custom;
            RulesListBox.AddButton.ToolTip = "Add new filter";
            RulesListBox.EditButton.ToolTip = "Edit filter";
            RulesListBox.RemoveButton.ToolTip = "Remove selected filter(s)";
            RulesListBox.RemoveAllButton.ToolTip = "Remove all filters";
            RulesListBox.AddButton.Click += AddNewRule_Click; ;
            RulesListBox.OnEditClicked += EditRule_Click;
            RulesListBox.IsDeselectable = true;

            LoadFromConfig();

            ThemeHelper.UpdateColorsFromSettings(this, SearchWindow.ExtensionInterface);
            ThemeHelper.UpdateColorsFromSettings(ProjectsListBox, SearchWindow.ExtensionInterface, false);
            ThemeHelper.UpdateColorsFromSettings(GroupsListBox, SearchWindow.ExtensionInterface, false);
            ThemeHelper.UpdateColorsFromSettings(PathsListBox, SearchWindow.ExtensionInterface, false);
            ThemeHelper.UpdateColorsFromSettings(RulesListBox, SearchWindow.ExtensionInterface, false);

            if (SearchWindow.ExtensionInterface.IsStandalone)
            {
                AutomaticPopulation.Visibility = Visibility.Collapsed;
            }

            UseGlobalPath.IsChecked = Settings.Default.UseGlobalPath;
        }

        private void AddNewRule_Click(object sender, RoutedEventArgs e)
        {
            RuleWindow ruleWindow = new RuleWindow(SearchWindow.ExtensionInterface);

            MainWindow ruleDialog = UIHelper.CreateWindow(ruleWindow, "Add filter", SearchWindow.ExtensionInterface, this);
            ruleWindow.Dialog = ruleDialog;
            ruleDialog.ShowDialog();

            if (ruleWindow.IsOK)
            {
                SearchGroup selectedGroup = GroupsListBox.InnerListBox.SelectedItem as SearchGroup;
                if (selectedGroup != null)
                {
                    ConfigRule configRule = selectedGroup.ConfigGroup.AddNewRule(ruleWindow.RegExTextBox.Text, ruleWindow.RuleType.SelectedIndex == 1);
                    if (configRule != null)
                    {
                        selectedGroup.Rules.Add(new SearchRule(configRule));
                    }

                    UpdateVisibility();
                }
            }
        }

        private void EditRule_Click()
        {
            SearchRule searchRule = RulesListBox.InnerListBox.SelectedItem as SearchRule;

            RuleWindow ruleWindow = new RuleWindow(SearchWindow.ExtensionInterface);
            ruleWindow.RuleType.SelectedIndex = searchRule.IsExclude ? 1 : 0;
            ruleWindow.RegExTextBox.Text = searchRule.RegEx;
            ruleWindow.RegExTextBox.SelectAll();
            ruleWindow.RegExTextBox.Focus();

            MainWindow ruleDialog = UIHelper.CreateWindow(ruleWindow, "Edit rule", SearchWindow.ExtensionInterface, this);
            ruleWindow.Dialog = ruleDialog;
            ruleDialog.ShowDialog();

            if (ruleWindow.IsOK)
            {
                searchRule.RegEx = ruleWindow.RegExTextBox.Text;
                searchRule.IsExclude = ruleWindow.RuleType.SelectedIndex == 1;
                searchRule.UpdateConfig();
            }

            UpdateVisibility();
        }

        private void AddNewPath_Click(object sender, RoutedEventArgs e)
        {
            FolderSelectDialog folderSelectDialog = new FolderSelectDialog();
            folderSelectDialog.InitialDirectory = ConfigParser.Instance.Path;
            folderSelectDialog.Multiselect = true;
            if (folderSelectDialog.ShowDialog())
            {
                SearchGroup selectedGroup = GroupsListBox.InnerListBox.SelectedItem as SearchGroup;
                if (selectedGroup != null)
                {
                    foreach (string filename in folderSelectDialog.FileNames)
                    {
                        ConfigPath configPath = selectedGroup.ConfigGroup.AddNewPath(filename);
                        if(configPath != null)
                        {
                            selectedGroup.Paths.Add(new SearchPath(configPath));
                        }
                    }

                    UpdateVisibility();
                }
            }
        }

        private void ConfigGroups_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.RemovedItems != null && e.RemovedItems.Count == 1)
            {
                (e.RemovedItems[0] as SearchGroup).Paths.CollectionChanged -= ConfigPaths_CollectionChanged;
                (e.RemovedItems[0] as SearchGroup).Rules.CollectionChanged -= ConfigRules_CollectionChanged;
            }

            if (GroupsListBox.InnerListBox.SelectedItems.Count == 1)
            {
                ObservableCollection<SearchPath> searchPaths = (GroupsListBox.InnerListBox.SelectedItem as SearchGroup).Paths;
                ObservableCollection<SearchRule> searchRules = (GroupsListBox.InnerListBox.SelectedItem as SearchGroup).Rules;

                PathsListBox.SetItemsSource(searchPaths);
                RulesListBox.SetItemsSource(searchRules);
                searchPaths.CollectionChanged += ConfigPaths_CollectionChanged;
                searchRules.CollectionChanged += ConfigRules_CollectionChanged;
            }
            else
            {
                PathsListBox.SetItemsSource(null);
                RulesListBox.SetItemsSource(null);
            }

            UpdateVisibility();
        }

        private void ConfigProjects_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(e.RemovedItems != null && e.RemovedItems.Count == 1)
            {
                (e.RemovedItems[0] as SearchConfig).SearchGroups.CollectionChanged -= ConfigGroups_CollectionChanged;
            }

            if(ProjectsListBox.InnerListBox.SelectedItems.Count == 1)
            {
                ObservableCollection<SearchGroup> searchGroups = (ProjectsListBox.InnerListBox.SelectedItem as SearchConfig).SearchGroups;

                GroupsListBox.SetItemsSource(searchGroups);
                searchGroups.CollectionChanged += ConfigGroups_CollectionChanged;

                if (GroupsListBox.InnerListBox.SelectedItems.Count == 0 && GroupsListBox.InnerListBox.Items.Count > 0)
                {
                    GroupsListBox.InnerListBox.SelectedItem = GroupsListBox.InnerListBox.Items[0];
                }
            }
            else
            {
                GroupsListBox.SetItemsSource(null);
            }

            UpdateVisibility();
        }

        private void ConfigPaths_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (SearchPath oldItem in e.OldItems)
                {
                    oldItem.ConfigPath.Parent.Paths.Remove(oldItem.ConfigPath);
                }
            }

            UpdateVisibility();
        }

        private void ConfigRules_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (SearchRule oldItem in e.OldItems)
                {
                    oldItem.ConfigRule.Parent.Rules.Remove(oldItem.ConfigRule);
                }
            }

            UpdateVisibility();
        }

        private void ConfigGroups_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (SearchGroup oldItem in e.OldItems)
                {
                    oldItem.ConfigGroup.Parent.Groups.Remove(oldItem.ConfigGroup);

                    if(IsAutomaticPopulationBusy && oldItem == CurrentlyPopulatingGroup)
                    {
                        IsAutomaticPopulationBusy = false;
                        AddAutomaticRules();
                    }
                }
            }

            if (GroupsListBox.InnerListBox.SelectedItems.Count == 0 && GroupsListBox.InnerListBox.Items.Count > 0)
            {
                GroupsListBox.InnerListBox.SelectedItem = GroupsListBox.InnerListBox.Items[0];
            }

            UpdateVisibility();
        }

        public void LoadFromConfig()
        {
            ConfigParser.LoadConfig();

            SearchConfigs.Clear();
            foreach (ConfigProject configProject in ConfigParser.Instance.ConfigProjects)
            {
                SearchConfigs.Add(new SearchConfig(configProject));
            }

            UpdateVisibility();
        }

        private void ConfigProjects_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if(e.OldItems != null)
            {
                foreach (SearchConfig oldItem in e.OldItems)
                {
                    ConfigParser.RemoveProject(oldItem.ConfigProject);

                    if (IsAutomaticPopulationBusy)
                    {
                        foreach(SearchGroup searchGroup in oldItem.SearchGroups)
                        {
                            if(searchGroup == CurrentlyPopulatingGroup)
                            {
                                IsAutomaticPopulationBusy = false;
                                AddAutomaticRules();
                            }
                        }
                    }
                }
            }

            UpdateVisibility();
        }

        private void UpdateVisibility()
        {
            int projectsCount = ConfigParser.Instance.ConfigProjects.Count;
            int groupsCount = projectsCount > 0 ? ConfigParser.Instance.ConfigProjects[0].Groups.Count : 0;

            bool canGoBasic = projectsCount <= 1 && groupsCount <= 1;

            bool isAdvanced = canGoBasic ? Settings.Default.AdvancedProjectSettings : true;
            AdvancedToggle.Content = isAdvanced ? "›› Basic" : "‹‹ Advanced";

            AdvancedToggle.IsEnabled = isAdvanced && canGoBasic || !isAdvanced;
            AdvancedToggle.ToolTip = !canGoBasic ? "Remove extra projects and groups to go back to basic settings" : null;

            GridLength advancedGridLength = isAdvanced ? new GridLength(1, GridUnitType.Star) : new GridLength(0, GridUnitType.Pixel);
            GridLength basicGridLength = new GridLength(3, GridUnitType.Star);
            AdvancedColumn.Width = advancedGridLength;
            BasicColumn.Width = basicGridLength;

            AutomaticPopulation.IsEnabled = !IsAutomaticPopulationBusy && GroupsListBox.InnerListBox.SelectedItems.Count == 1;
            AutomaticProgress.Visibility = IsAutomaticPopulationBusy ? Visibility.Visible : Visibility.Collapsed;
            StopButton.Visibility = IsAutomaticPopulationBusy ? Visibility.Visible : Visibility.Collapsed;
            UseGlobalPath.Visibility = !SearchWindow.ExtensionInterface.IsStandalone && isAdvanced ? Visibility.Visible : Visibility.Collapsed;
        }

        private void AdvancedToggle_Click(object sender, RoutedEventArgs e)
        {
            Settings.Default.AdvancedProjectSettings = !Settings.Default.AdvancedProjectSettings;
            Settings.Default.Save();

            UpdateVisibility();
        }

        private void ConfigOpen_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
            {
                FileName = ConfigParser.Instance.Path + ConfigParser.Instance.PathSuffix,
                UseShellExecute = true,
                Verb = "open"
            });
        }

        bool IsAutomaticPopulationBusy = false;
        SearchGroup CurrentlyPopulatingGroup = null;
        HashSet<string> CurrentlyPopulatingExtensions = null;

        private void AutomaticPopulation_Click(object sender, RoutedEventArgs e)
        {
            CurrentlyPopulatingGroup = GroupsListBox.InnerListBox.SelectedItem as SearchGroup;
            CurrentlyPopulatingExtensions = new HashSet<string>();
            IsAutomaticPopulationBusy = true;
            UpdateVisibility();

            Task.Run(() =>
            {
                SearchWindow.ExtensionInterface.GatherAllFoldersAndExtensionsFromSolution(HandleAutomaticNewExtension, HandleAutomaticNewFolder);

                Dispatcher.Invoke(() =>
                {
                    AddAutomaticRules();

                    IsAutomaticPopulationBusy = false;
                    UpdateVisibility();
                });
            });
        }

        private void AddAutomaticRules()
        {
            var extensions = CurrentlyPopulatingExtensions.Select(ext => ext.TrimStart('.')).ToList();
            string extensionsPattern = @"\." + $"({string.Join("|", extensions)})" + @"$";

            ConfigRule configRule = CurrentlyPopulatingGroup.ConfigGroup.AddNewRule(extensionsPattern, false);
            if (configRule != null)
            {
                CurrentlyPopulatingGroup.Rules.Add(new SearchRule(configRule));
            }
        }

        private void HandleAutomaticNewFolder(string newFolder)
        {
            if (!IsAutomaticPopulationBusy)
            {
                throw new Exception("Window closed.");
            }

            Dispatcher.Invoke(() =>
            {
                SearchGroup selectedGroup = GroupsListBox.InnerListBox.SelectedItem as SearchGroup;
                if (selectedGroup != null)
                {
                    ConfigPath configPath = CurrentlyPopulatingGroup.ConfigGroup.AddNewPath(newFolder);
                    if (configPath != null)
                    {
                        SearchPath newSearchPath = new SearchPath(configPath);
                        CurrentlyPopulatingGroup.Paths.Add(newSearchPath);
                        PathsListBox.InnerListBox.ScrollIntoView(newSearchPath);
                    }
                }
            });
        }

        private void HandleAutomaticNewExtension(string newExtension)
        {
            if(!IsAutomaticPopulationBusy)
            {
                throw new Exception("Window closed.");
            }

            Dispatcher.Invoke(() =>
            {
                if(!CurrentlyPopulatingExtensions.Contains(newExtension))
                {
                    CurrentlyPopulatingExtensions.Add(newExtension);
                }
            });
        }

        private void AddNewProject_Click(object sender, RoutedEventArgs e)
        {
            SearchConfigs.Add(new SearchConfig(ConfigParser.AddNewProject()));
            ProjectsListBox.InnerListBox.SelectedItem = SearchConfigs.Last();
        }

        private void AddNewGroup_Click(object sender, RoutedEventArgs e)
        {
            SearchConfig selectedConfig = ProjectsListBox.InnerListBox.SelectedItem as SearchConfig;
            if (selectedConfig != null)
            {
                selectedConfig.SearchGroups.Add(new SearchGroup(selectedConfig.ConfigProject.AddNewGroup()));
                UpdateVisibility();
            }
        }

        private void UserControl_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if(!(bool)e.NewValue)
            {
                if(IsAutomaticPopulationBusy)
                {
                    IsAutomaticPopulationBusy = false;
                    AddAutomaticRules();
                }
            }
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            IsAutomaticPopulationBusy = false;
            AddAutomaticRules();
        }

        private void UseGlobalPath_Click(object sender, RoutedEventArgs e)
        {
            Settings.Default.UseGlobalPath = UseGlobalPath.IsChecked ?? false;
            Settings.Default.Save();

            ConfigParser.Init(SearchWindow.ExtensionInterface.GetSolutionPath(Settings.Default.UseGlobalPath));
            LoadFromConfig();
        }
    }
}
