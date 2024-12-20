﻿using qgrepControls.Classes;
using qgrepControls.ModelViews;
using qgrepControls.Properties;
using qgrepControls.UserControls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

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
            ProjectsListBox.Title.Text = Properties.Resources.ProjectsListBoxTitle;
            ProjectsListBox.AddButton.ToolTip = Properties.Resources.ProjectsListBoxAddButtonTooltip;
            ProjectsListBox.EditButton.ToolTip = Properties.Resources.ProjectsListBoxEditButtonTooltip;
            ProjectsListBox.RemoveButton.ToolTip = Properties.Resources.ProjectsListBoxRemoveButtonTooltip;
            ProjectsListBox.RemoveAllButton.ToolTip = Properties.Resources.ProjectsListBoxRemoveAllButtonTooltip;
            ProjectsListBox.AddButton.Click += AddNewProject_Click;
            ProjectsListBox.InnerListBox.SelectionChanged += ConfigProjects_SelectionChanged;
            ProjectsListBox.IsPreviousSelectedOnRemove = true;
            SearchConfigs.CollectionChanged += ConfigProjects_CollectionChanged;

            GroupsListBox.Title.Text = Properties.Resources.GroupsListBoxTitle;
            GroupsListBox.ItemEditType = ConfigListBox.EditType.Text;
            GroupsListBox.AddButton.ToolTip = Properties.Resources.GroupsListBoxAddButtonTooltip;
            GroupsListBox.EditButton.ToolTip = Properties.Resources.GroupsListBoxEditButtonTooltip;
            GroupsListBox.RemoveButton.ToolTip = Properties.Resources.GroupsListBoxRemoveButtonTooltip;
            GroupsListBox.RemoveAllButton.ToolTip = Properties.Resources.GroupsListBoxRemoveAllButtonTooltip;
            GroupsListBox.AddButton.Click += AddNewGroup_Click;
            GroupsListBox.InnerListBox.SelectionChanged += ConfigGroups_SelectionChanged;
            GroupsListBox.IsPreviousSelectedOnRemove = true;

            PathsListBox.Title.Text = Properties.Resources.PathsListBoxTitle;
            PathsListBox.ItemEditType = ConfigListBox.EditType.None;
            PathsListBox.AddButton.ToolTip = Properties.Resources.PathsListBoxAddButtonTooltip;
            PathsListBox.EditButton.ToolTip = Properties.Resources.PathsListBoxEditButtonTooltip;
            PathsListBox.RemoveButton.ToolTip = Properties.Resources.PathsListBoxRemoveButtonTooltip;
            PathsListBox.RemoveAllButton.ToolTip = Properties.Resources.PathsListBoxRemoveAllButtonTooltip;
            PathsListBox.AddButton.Click += AddNewPath_Click;
            PathsListBox.IsDeselectable = true;

            RulesListBox.Title.Text = Properties.Resources.RulesListBoxTitle;
            RulesListBox.ItemEditType = ConfigListBox.EditType.Custom;
            RulesListBox.AddButton.ToolTip = Properties.Resources.RulesListBoxAddButtonTooltip;
            RulesListBox.EditButton.ToolTip = Properties.Resources.EditFilterTitle;
            RulesListBox.RemoveButton.ToolTip = Properties.Resources.RulesListBoxRemoveButtonTooltip;
            RulesListBox.RemoveAllButton.ToolTip = Properties.Resources.RulesListBoxRemoveAllButtonTooltip;
            RulesListBox.AddButton.Click += AddNewRule_Click;
            RulesListBox.OnEditClicked += EditRule_Click;
            RulesListBox.IsDeselectable = true;

            LoadFromConfig();

            ThemeHelper.UpdateColorsFromSettings(this, SearchWindow.WrapperApp);
            ThemeHelper.UpdateColorsFromSettings(ProjectsListBox, SearchWindow.WrapperApp, false);
            ThemeHelper.UpdateColorsFromSettings(GroupsListBox, SearchWindow.WrapperApp, false);
            ThemeHelper.UpdateColorsFromSettings(PathsListBox, SearchWindow.WrapperApp, false);
            ThemeHelper.UpdateColorsFromSettings(RulesListBox, SearchWindow.WrapperApp, false);

            if (SearchWindow.WrapperApp.IsStandalone)
            {
                AutomaticPopulation.Visibility = Visibility.Collapsed;
            }

            UseGlobalPath.IsChecked = Settings.Default.UseGlobalPath;
            UseRelativePaths.IsChecked = ConfigParser.Instance.LastConfigPath.Length != 0;
        }

        private void AddNewRule_Click(object sender, RoutedEventArgs e)
        {
            RuleWindow ruleWindow = new RuleWindow(SearchWindow.WrapperApp);

            MainWindow ruleDialog = UIHelper.CreateWindow(ruleWindow, Properties.Resources.AddFilterTitle, SearchWindow.WrapperApp, this);
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

            RuleWindow ruleWindow = new RuleWindow(SearchWindow.WrapperApp);
            ruleWindow.RuleType.SelectedIndex = searchRule.IsExclude ? 1 : 0;
            ruleWindow.RegExTextBox.Text = searchRule.RegEx;
            ruleWindow.RegExTextBox.SelectAll();
            ruleWindow.RegExTextBox.Focus();

            MainWindow ruleDialog = UIHelper.CreateWindow(ruleWindow, Properties.Resources.EditFilterTitle, SearchWindow.WrapperApp, this);
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
            folderSelectDialog.Title = Properties.Resources.SelectFolderPrompt;
            if (folderSelectDialog.ShowDialog())
            {
                SearchGroup selectedGroup = GroupsListBox.InnerListBox.SelectedItem as SearchGroup;
                if (selectedGroup != null)
                {
                    foreach (string filename in folderSelectDialog.FileNames)
                    {
                        ConfigPath configPath = selectedGroup.ConfigGroup.AddNewPath(filename);
                        if (configPath != null)
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
            if (e.RemovedItems != null && e.RemovedItems.Count == 1)
            {
                (e.RemovedItems[0] as SearchConfig).SearchGroups.CollectionChanged -= ConfigGroups_CollectionChanged;
            }

            if (ProjectsListBox.InnerListBox.SelectedItems.Count == 1)
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

                    if (IsAutomaticPopulationBusy && oldItem == CurrentlyPopulatingGroup)
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
            ConfigParser.ApplyKeyBindings(SearchWindow.bindings);

            SearchConfigs.Clear();
            foreach (ConfigProject configProject in ConfigParser.Instance.ConfigProjects)
            {
                SearchConfigs.Add(new SearchConfig(configProject));
            }

            UpdateVisibility();
        }

        private void ConfigProjects_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (SearchConfig oldItem in e.OldItems)
                {
                    ConfigParser.RemoveProject(oldItem.ConfigProject);

                    if (IsAutomaticPopulationBusy)
                    {
                        foreach (SearchGroup searchGroup in oldItem.SearchGroups)
                        {
                            if (searchGroup == CurrentlyPopulatingGroup)
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

            bool canGoBasic = projectsCount == 1 && groupsCount == 1;

            bool isAdvanced = canGoBasic ? Settings.Default.AdvancedProjectSettings : true;
            AdvancedToggle.Content = isAdvanced ? Properties.Resources.BasicToggleContent : Properties.Resources.AdvancedToggleContent;

            AdvancedToggle.IsEnabled = isAdvanced && canGoBasic || !isAdvanced;
            AdvancedToggle.ToolTip = !canGoBasic ? Properties.Resources.RemoveExtraContent : null;

            GridLength advancedGridLength = isAdvanced ? new GridLength(1, GridUnitType.Star) : new GridLength(0, GridUnitType.Pixel);
            GridLength basicGridLength = new GridLength(3, GridUnitType.Star);
            AdvancedColumn.Width = advancedGridLength;
            BasicColumn.Width = basicGridLength;

            AutomaticPopulation.IsEnabled = !IsAutomaticPopulationBusy && GroupsListBox.InnerListBox.SelectedItems.Count == 1;
            AutomaticProgress.Visibility = IsAutomaticPopulationBusy ? Visibility.Visible : Visibility.Collapsed;
            StopButton.Visibility = IsAutomaticPopulationBusy ? Visibility.Visible : Visibility.Collapsed;
            UseGlobalPath.Visibility = !SearchWindow.WrapperApp.IsStandalone && isAdvanced ? Visibility.Visible : Visibility.Collapsed;
            UseRelativePaths.Visibility = !SearchWindow.WrapperApp.IsStandalone && isAdvanced ? Visibility.Visible : Visibility.Collapsed;
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

            TaskRunner.RunInBackgroundAsync(() =>
            {
                SearchWindow.WrapperApp.GatherAllFoldersAndExtensionsFromSolution(HandleAutomaticNewExtension, HandleAutomaticNewFolder);

                TaskRunner.RunOnUIThreadAsync(() =>
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

            TaskRunner.RunOnUIThread(() =>
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
            if (!IsAutomaticPopulationBusy)
            {
                throw new Exception("Window closed.");
            }

            TaskRunner.RunOnUIThread(() =>
            {
                if (!CurrentlyPopulatingExtensions.Contains(newExtension))
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
            if (!(bool)e.NewValue)
            {
                if (IsAutomaticPopulationBusy)
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

            ConfigParser.Initialize(SearchWindow.WrapperApp.GetConfigPath(Settings.Default.UseGlobalPath));
            LoadFromConfig();

            UseRelativePaths.IsChecked = ConfigParser.Instance.LastConfigPath.Length != 0;
        }

        private void UseRelativePaths_Click(object sender, RoutedEventArgs e)
        {
            ConfigParser.SaveConfig();
            ConfigParser.Instance.LastConfigPath = (UseRelativePaths.IsChecked ?? false) ? SearchWindow.WrapperApp.GetConfigPath(false) : "";
            ConfigParser.SaveSettings();

            ConfigParser.Initialize(SearchWindow.WrapperApp.GetConfigPath(Settings.Default.UseGlobalPath));
            LoadFromConfig();
        }
    }
}
