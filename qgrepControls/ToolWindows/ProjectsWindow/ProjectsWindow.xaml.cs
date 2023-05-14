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
        public new qgrepSearchWindowControl Parent;
        private ObservableCollection<SearchConfig> SearchConfigs = new ObservableCollection<SearchConfig>();

        private ProjectRow SelectedProject = null;
        private GroupRow SelectedGroup = null;

        public ProjectsWindow(qgrepSearchWindowControl Parent)
        {
            this.Parent = Parent;
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
            SearchConfigs.CollectionChanged += ConfigProjects_CollectionChanged;

            GroupsListBox.Title.Text = "Groups";
            GroupsListBox.ItemEditType = ConfigListBox.EditType.Text;
            GroupsListBox.AddButton.ToolTip = "Add new group";
            GroupsListBox.EditButton.ToolTip = "Edit group name";
            GroupsListBox.RemoveButton.ToolTip = "Remove selected group(s)";
            GroupsListBox.RemoveAllButton.ToolTip = "Remove all groups";
            GroupsListBox.AddButton.Click += AddNewGroup_Click;
            GroupsListBox.InnerListBox.SelectionChanged += ConfigGroups_SelectionChanged;

            PathsListBox.Title.Text = "Folders";
            PathsListBox.ItemEditType = ConfigListBox.EditType.None;
            PathsListBox.AddButton.ToolTip = "Add new folder(s)";
            PathsListBox.EditButton.ToolTip = "Edit folder name";
            PathsListBox.RemoveButton.ToolTip = "Remove selected folder(s)";
            PathsListBox.RemoveAllButton.ToolTip = "Remove all folders";
            PathsListBox.AddButton.Click += AddNewFolder_Click;

            RulesListBox.Title.Text = "Filters";
            RulesListBox.ItemEditType = ConfigListBox.EditType.Custom;
            RulesListBox.AddButton.ToolTip = "Add new filter";
            RulesListBox.EditButton.ToolTip = "Edit filter";
            RulesListBox.RemoveButton.ToolTip = "Remove selected filter(s)";
            RulesListBox.RemoveAllButton.ToolTip = "Remove all filters";
            RulesListBox.AddButton.Click += AddNewRule_Click;
            RulesListBox.OnEditClicked += EditRule_Click;

            LoadFromConfig();

            Parent.LoadColorsFromResources(this);
            Parent.LoadColorsFromResources(ProjectsListBox);
            Parent.LoadColorsFromResources(GroupsListBox);
            Parent.LoadColorsFromResources(PathsListBox);
            Parent.LoadColorsFromResources(RulesListBox);

            if (Parent.ExtensionInterface.IsStandalone)
            {
                AutomaticPopulation.Visibility = Visibility.Collapsed;
            }
        }

        private void EditRule_Click()
        {
            SearchRule searchRule = RulesListBox.InnerListBox.SelectedItem as SearchRule;

            RuleWindow ruleWindow = new RuleWindow(this);
            ruleWindow.RuleType.SelectedIndex = searchRule.IsExclude ? 1 : 0;
            ruleWindow.RegExTextBox.Text = searchRule.RegEx;
            ruleWindow.RegExTextBox.SelectAll();
            ruleWindow.RegExTextBox.Focus();

            MainWindow ruleDialog = Parent.CreateWindow(ruleWindow, "Edit rule", this);
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

        private void AddNewFolder_Click(object sender, RoutedEventArgs e)
        {
            FolderSelectDialog folderSelectDialog = new FolderSelectDialog();
            folderSelectDialog.InitialDirectory = Parent.ConfigParser.Path;
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
            if (Parent.ConfigParser != null)
            {
                Parent.ConfigParser.LoadConfig();

                SearchConfigs.Clear();
                foreach (ConfigProject configProject in Parent.ConfigParser.ConfigProjects)
                {
                    SearchConfigs.Add(new SearchConfig(configProject));
                }

                UpdateVisibility();
            }
        }

        public void LoadProjectsFromConfig()
        {
            ProjectsPanel.Children.Clear();

            foreach (ConfigProject configProject in Parent.ConfigParser.ConfigProjects)
            {
                ProjectsPanel.Children.Add(new ProjectRow(this, new ProjectRow.ProjectRowData(configProject.Name)));
            }

            ProjectsPanel.Children.Add(new RowAdd(Parent, "Add new search config", new RowAdd.ClickCallbackFunction(AddProject)));
            CheckAddButtonVisibility();

            bool foundOldProject = false;
            if (SelectedProject != null)
            {
                foreach (UIElement child in ProjectsPanel.Children)
                {
                    ProjectRow row = child as ProjectRow;
                    if (row != null)
                    {
                        if (row.Data.ProjectName == SelectedProject.Data.ProjectName)
                        {
                            foundOldProject = true;
                            SelectProject(row);
                            break;
                        }
                    }
                }
            }

            if (!foundOldProject)
            {
                SelectedProject = null;
                if (ProjectsPanel.Children.Count > 1)
                {
                    SelectProject(ProjectsPanel.Children[0] as ProjectRow);
                }
            }
        }

        private void ConfigProjects_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if(e.OldItems != null)
            {
                foreach (SearchConfig oldItem in e.OldItems)
                {
                    Parent.ConfigParser.RemoveProject(oldItem.ConfigProject);
                }
            }

            if(ProjectsListBox.InnerListBox.SelectedItems.Count == 0 && ProjectsListBox.InnerListBox.Items.Count > 0)
            {
                ProjectsListBox.InnerListBox.SelectedItem = ProjectsListBox.InnerListBox.Items[0];
            }

            UpdateVisibility();
        }

        public void LoadGroupsFromConfig()
        {
            GroupsPanel.Children.Clear();

            if (SelectedProject != null)
            {
                foreach (ConfigProject configProject in Parent.ConfigParser.ConfigProjects)
                {
                    if (configProject.Name == SelectedProject.Data.ProjectName)
                    {
                        for(int i = 0; i < configProject.Groups.Count; i++)
                        {
                            GroupsPanel.Children.Add(new GroupRow(this, new GroupRow.GroupRowData(i == 0 ? "<root>" : "Group" + i, i == 0, i)));
                        }

                        GroupsPanel.Children.Add(new RowAdd(Parent, "Add new search group", new RowAdd.ClickCallbackFunction(AddGroup)));
                        CheckAddButtonVisibility();

                        bool foundOldGroup = false;
                        if (SelectedGroup != null)
                        {
                            foreach (UIElement child in GroupsPanel.Children)
                            {
                                GroupRow row = child as GroupRow;
                                if (row != null)
                                {
                                    if (row.Data.Index == SelectedGroup.Data.Index)
                                    {
                                        foundOldGroup = true;
                                        SelectGroup(row);
                                        break;
                                    }
                                }
                            }
                        }

                        if (!foundOldGroup)
                        {
                            SelectedGroup = null;
                            if (GroupsPanel.Children.Count > 1)
                            {
                                SelectGroup(GroupsPanel.Children[0] as GroupRow);
                            }
                        }

                        break;
                    }
                }
            }
        }

        public void SelectProject(ProjectRow project)
        {
            if (project != SelectedProject)
            {
                foreach (UIElement child in ProjectsPanel.Children)
                {
                    ProjectRow row = child as ProjectRow;
                    if (row != null)
                    {
                        bool isSelected = (row == project);

                        if (isSelected)
                        {
                            SelectedProject = row;
                        }

                        row.Select(isSelected);
                    }
                }

                LoadGroupsFromConfig();
                UpdateHints();
            }
        }

        public void AddProject()
        {
            SearchConfigs.Add(new SearchConfig(Parent.ConfigParser.AddNewProject()));
            ProjectsListBox.InnerListBox.SelectedItem = SearchConfigs.Last();
        }

        public void ChangeProjectName(ProjectRow project, string newName)
        {
            foreach (ConfigProject configProject in Parent.ConfigParser.ConfigProjects)
            {
                if (configProject.Name == project.Data.ProjectName)
                {
                    if (configProject.Rename(newName))
                    {
                        Parent.RenameFilter(project.Data.ProjectName, newName);
                        LoadFromConfig();
                    }
                    break;
                }
            }
        }

        public void SelectGroup(GroupRow group)
        {
            foreach (UIElement child in GroupsPanel.Children)
            {
                GroupRow row = child as GroupRow;
                if (row != null)
                {
                    bool isSelected = (row == group);

                    if (isSelected)
                    {
                        SelectedGroup = group;
                    }

                    row.Select(isSelected);
                }
            }

            LoadPathsFromConfig();
            LoadRulesFromConfig();
            UpdateHints();
        }

        public void AddGroup()
        {
            SearchConfig selectedConfig = ProjectsListBox.InnerListBox.SelectedItem as SearchConfig;
            if(selectedConfig != null)
            {
                selectedConfig.SearchGroups.Add(new SearchGroup(selectedConfig.ConfigProject.AddNewGroup()));

                UpdateVisibility();
            }
        }

        public void LoadPathsFromConfig()
        {
            PathsPanel.Children.Clear();

            if (SelectedProject != null && SelectedGroup != null)
            {
                foreach (ConfigProject configProject in Parent.ConfigParser.ConfigProjects)
                {
                    if (configProject.Name == SelectedProject.Data.ProjectName)
                    {
                        //foreach(string Path in configProject.Groups[SelectedGroup.Data.Index].Paths)
                        //{
                        //    PathsPanel.Children.Add(new PathRow(this, new PathRow.PathRowData(Path)));
                        //}

                        PathsPanel.Children.Add(new RowAdd(Parent, "Add new folder", new RowAdd.ClickCallbackFunction(AddPath)));
                        CheckAddButtonVisibility();
                        break;
                    }
                }
            }
        }

        public void AddPath()
        {
            if (SelectedProject != null && SelectedGroup != null)
            {
                FolderSelectDialog folderSelectDialog = new FolderSelectDialog();
                folderSelectDialog.InitialDirectory = Parent.ConfigParser.Path;
                folderSelectDialog.Multiselect = true;
                if (folderSelectDialog.ShowDialog())
                {
                    foreach (ConfigProject configProject in Parent.ConfigParser.ConfigProjects)
                    {
                        if (configProject.Name == SelectedProject.Data.ProjectName)
                        {
                            foreach (string filename in folderSelectDialog.FileNames)
                            {
                                //if (!configProject.Groups[SelectedGroup.Data.Index].Paths.Contains(filename))
                                //{
                                //    NothingChanged = false;
                                //    configProject.Groups[SelectedGroup.Data.Index].Paths.Add(filename);
                                //}
                            }

                            Parent.ConfigParser.SaveConfig();
                            LoadFromConfig();
                            break;
                        }
                    }
                }
            }
        }

        public void DeletePath(PathRow path)
        {
            if (SelectedProject != null && SelectedGroup != null)
            {
                foreach (ConfigProject configProject in Parent.ConfigParser.ConfigProjects)
                {
                    if (configProject.Name == SelectedProject.Data.ProjectName)
                    {
                        //configProject.Groups[SelectedGroup.Data.Index].Paths.Remove(path.Data.Path);
                        LoadFromConfig();
                        break;
                    }
                }
            }
        }

        public void LoadRulesFromConfig()
        {
            RulesPanel.Children.Clear();

            if (SelectedProject != null && SelectedGroup != null)
            {
                foreach (ConfigProject configProject in Parent.ConfigParser.ConfigProjects)
                {
                    if (configProject.Name == SelectedProject.Data.ProjectName)
                    {
                        int index = 0;
                        foreach (ConfigRule rule in configProject.Groups[SelectedGroup.Data.Index].Rules)
                        {
                            RulesPanel.Children.Add(new RuleRow(this, new RuleRow.RuleRowData(rule.IsExclude, rule.Rule, index++)));
                        }

                        RulesPanel.Children.Add(new RowAdd(Parent, "Add new filter", new RowAdd.ClickCallbackFunction(AddRule)));
                        CheckAddButtonVisibility();
                        break;
                    }
                }
            }
        }

        public void DeleteRule(RuleRow rule)
        {
            if (SelectedProject != null && SelectedGroup != null)
            {
                foreach (ConfigProject configProject in Parent.ConfigParser.ConfigProjects)
                {
                    if (configProject.Name == SelectedProject.Data.ProjectName)
                    {
                        configProject.Groups[SelectedGroup.Data.Index].Rules.RemoveAt(rule.Data.Index);
                        LoadFromConfig();
                        break;
                    }
                }
            }
        }

        public void AddRule()
        {
            RuleWindow ruleWindow = new RuleWindow(this);

            MainWindow ruleDialog = Parent.CreateWindow(ruleWindow, "Add rule", this);
            ruleWindow.Dialog = ruleDialog;
            ruleDialog.ShowDialog();

            if(ruleWindow.IsOK)
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

        public void EditRule(RuleRow rule)
        {
            if (SelectedProject != null && SelectedGroup != null)
            {
                foreach (ConfigProject configProject in Parent.ConfigParser.ConfigProjects)
                {
                    if (configProject.Name == SelectedProject.Data.ProjectName)
                    {
                        ConfigRule configRule = configProject.Groups[SelectedGroup.Data.Index].Rules[rule.Data.Index];

                        RuleWindow ruleWindow = new RuleWindow(this);
                        ruleWindow.RuleType.SelectedIndex = configRule.IsExclude ? 1 : 0;
                        ruleWindow.RegExTextBox.Text = configRule.Rule;
                        ruleWindow.RegExTextBox.SelectAll();
                        ruleWindow.RegExTextBox.Focus();

                        MainWindow ruleDialog = Parent.CreateWindow(ruleWindow, "Edit rule", this);
                        ruleWindow.Dialog = ruleDialog;
                        ruleDialog.ShowDialog();

                        if (ruleWindow.IsOK)
                        {
                            configRule.Rule = ruleWindow.RegExTextBox.Text;
                            configRule.IsExclude = ruleWindow.RuleType.SelectedIndex == 1;

                            Parent.ConfigParser.SaveConfig();
                            LoadFromConfig();
                        }

                        break;
                    }
                }
            }
        }

        private void GroupsPanel_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            ShowAddPanel(GroupsPanel);
        }

        private void GroupsPanel_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            HideAddPanel(GroupsPanel);
        }

        private void ProjectsPanel_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            ShowAddPanel(ProjectsPanel);
        }

        private void ProjectsPanel_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            HideAddPanel(ProjectsPanel);
        }

        private void PathsPanel_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            ShowAddPanel(PathsPanel);
        }

        private void PathsPanel_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            HideAddPanel(PathsPanel);
        }

        private void RulesPanel_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            ShowAddPanel(RulesPanel);
        }

        private void RulesPanel_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            HideAddPanel(RulesPanel);
        }

        private void CheckAddButtonVisibility()
        {
            if (ProjectsPanel.IsMouseOver)
            {
                ShowAddPanel(ProjectsPanel);
            }
            if (GroupsPanel.IsMouseOver)
            {
                ShowAddPanel(GroupsPanel);
            }
            if (PathsPanel.IsMouseOver)
            {
                ShowAddPanel(PathsPanel);
            }
            if (RulesPanel.IsMouseOver)
            {
                ShowAddPanel(RulesPanel);
            }
        }

        void HideAddPanel(StackPanel panel)
        {
            if (panel.Children.Count > 0)
            {
                panel.Children[panel.Children.Count - 1].Visibility = Visibility.Hidden;
            }
        }
        void ShowAddPanel(StackPanel panel)
        {
            if (panel.Children.Count > 0)
            {
                panel.Children[panel.Children.Count - 1].Visibility = Visibility.Visible;
            }
        }

        private void UpdateHints()
        {
            int pathsCount = 0;
            int rulesCount = 0;

            if (SelectedProject != null && SelectedGroup != null)
            {
                foreach (ConfigProject configProject in Parent.ConfigParser.ConfigProjects)
                {
                    if (configProject.Name == SelectedProject.Data.ProjectName)
                    {
                        pathsCount = configProject.Groups[SelectedGroup.Data.Index].Paths.Count;
                        rulesCount = configProject.Groups[SelectedGroup.Data.Index].Rules.Count;
                    }
                }
            }

            //PathsHint.Visibility = PathsListBox.InnerListBox.Items.Count > 0 ? Visibility.Collapsed : Visibility.Visible;
            //RulesHint.Visibility = rulesCount > 0 ? Visibility.Collapsed : Visibility.Visible;
            //ProjectsHint.Visibility = ProjectsListBox.InnerListBox.Items.Count > 0 ? Visibility.Collapsed : Visibility.Visible;
            //GroupsHint.Visibility = GroupsListBox.InnerListBox.Items.Count > 0 ? Visibility.Collapsed : Visibility.Visible;
        }

        private void UpdateVisibility()
        {
            int projectsCount = Parent.ConfigParser.ConfigProjects.Count;
            int groupsCount = projectsCount > 0 ? Parent.ConfigParser.ConfigProjects[0].Groups.Count : 0;

            bool canGoBasic = projectsCount <= 1 && groupsCount <= 1;

            bool isAdvanced = canGoBasic ? Settings.Default.AdvancedProjectSettings : true;
            AdvancedToggle.Content = isAdvanced ? "›› Basic" : "‹‹ Advanced";

            AdvancedToggle.IsEnabled = isAdvanced && canGoBasic || !isAdvanced;
            AdvancedToggle.ToolTip = !canGoBasic ? "Remove extra projects and groups to go back to basic settings" : null;

            GridLength gridLength = isAdvanced ? new GridLength(1, GridUnitType.Star) : new GridLength(0, GridUnitType.Pixel);
            AdvancedColumn.Width = gridLength;

            UpdateHints();
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
                FileName = Parent.ConfigParser.Path + Parent.ConfigParser.PathSuffix,
                UseShellExecute = true,
                Verb = "open"
            });
        }

        bool IsAutomaticPopulationBusy = false;
        ConfigGroup CurrentlyPopulatingGroup = null;

        private void AutomaticPopulation_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedProject != null && SelectedGroup != null)
            {
                foreach (ConfigProject configProject in Parent.ConfigParser.ConfigProjects)
                {
                    if (configProject.Name == SelectedProject.Data.ProjectName)
                    {
                        CurrentlyPopulatingGroup = configProject.Groups[SelectedGroup.Data.Index];
                        AutomaticPopulation.IsEnabled = false;
                        AutomaticProgress.Visibility = Visibility.Visible;

                        Task.Run(() =>
                        {
                            List<string> solutionFolders = new List<string>();
                            HashSet<string> extensionsList = new HashSet<string>();
                            Parent.ExtensionInterface.GatherAllFoldersAndExtensionsFromSolution(extensionsList, HandleAutomaticNewFolder);

                            Dispatcher.Invoke(() =>
                            {
                                var extensions = extensionsList.Select(ext => ext.TrimStart('.')).ToList();
                                string extensionsPattern = @"\." + $"({string.Join("|", extensions)})" + @"$";

                                if (!CurrentlyPopulatingGroup.Rules.Any(x => x.Rule == extensionsPattern && x.IsExclude == false))
                                {
                                    CurrentlyPopulatingGroup.Rules.Add(new ConfigRule()
                                    {
                                        IsExclude = false,
                                        Rule = extensionsPattern
                                    });
                                }

                                AutomaticPopulation.IsEnabled = true;
                                AutomaticProgress.Visibility = Visibility.Collapsed;

                                Parent.ConfigParser.SaveConfig();
                                LoadFromConfig();
                            });
                        });

                        break;
                    }
                }
            }
        }

        private void HandleAutomaticNewFolder(string newFolder)
        {
            Dispatcher.Invoke(() =>
            {
                //if (!CurrentlyPopulatingGroup.Paths.Contains(newFolder))
                //{
                //    NothingChanged = false;
                //    CurrentlyPopulatingGroup.Paths.Add(newFolder);

                //    PathsPanel.Children.Add(new PathRow(this, new PathRow.PathRowData(newFolder)));
                //}
            });
        }

        private void DeleteAllPaths_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedProject != null && SelectedGroup != null)
            {
                foreach (ConfigProject configProject in Parent.ConfigParser.ConfigProjects)
                {
                    if (configProject.Name == SelectedProject.Data.ProjectName)
                    {
                        configProject.Groups[SelectedGroup.Data.Index].Paths.Clear();
                        LoadFromConfig();
                        break;
                    }
                }
            }
        }

        private void AddNewProject_Click(object sender, RoutedEventArgs e)
        {
            AddProject();
        }

        private void AddNewPath_Click(object sender, RoutedEventArgs e)
        {
            AddPath();
        }

        private void AddNewRule_Click(object sender, RoutedEventArgs e)
        {
            AddRule();
        }

        private void DeleteAllRules_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedProject != null && SelectedGroup != null)
            {
                foreach (ConfigProject configProject in Parent.ConfigParser.ConfigProjects)
                {
                    if (configProject.Name == SelectedProject.Data.ProjectName)
                    {
                        configProject.Groups[SelectedGroup.Data.Index].Rules.Clear();
                        LoadFromConfig();
                        break;
                    }
                }
            }
        }

        private void AddNewGroup_Click(object sender, RoutedEventArgs e)
        {
            AddGroup();
        }

        private void DeleteAllGroups_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedProject != null)
            {
                foreach (ConfigProject configProject in Parent.ConfigParser.ConfigProjects)
                {
                    if (configProject.Name == SelectedProject.Data.ProjectName)
                    {
                        configProject.Groups.RemoveRange(1, configProject.Groups.Count - 1);
                        LoadFromConfig();
                        break;
                    }
                }
            }
        }
    }
}
