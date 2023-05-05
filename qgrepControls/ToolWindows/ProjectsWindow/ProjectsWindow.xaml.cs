using qgrepControls.Classes;
using qgrepControls.Properties;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Forms;
using System.Windows.Media;

namespace qgrepControls.SearchWindow
{
    public partial class ProjectsWindow : System.Windows.Controls.UserControl
    {
        public new qgrepSearchWindowControl Parent;

        private ProjectRow SelectedProject = null;
        private GroupRow SelectedGroup = null;

        public ProjectsWindow(qgrepSearchWindowControl Parent)
        {
            this.Parent = Parent;
            InitializeComponent();

            LoadFromConfig();
            LoadColorsFromResources();
            UpdateVisibility();

            if (Parent.ExtensionInterface.IsStandalone)
            {
                AutomaticPopulation.Visibility = Visibility.Collapsed;
            }
        }

        public void LoadFromConfig()
        {
            if (Parent.ConfigParser != null)
            {
                Parent.ConfigParser.SaveConfig();
                Parent.ConfigParser.LoadConfig();
                Parent.UpdateFilters();
                LoadProjectsFromConfig();
                UpdateVisibility();
            }
        }

        private void LoadColorsFromResources()
        {
            Dictionary<string, SolidColorBrush> colors = Parent.GetBrushesFromColorScheme();

            foreach (var color in colors)
            {
                Resources[color.Key] = color.Value;
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

        public void DeleteProject(ProjectRow project)
        {
            Parent.ConfigParser.RemoveProject(project.Data.ProjectName);
            LoadFromConfig();
        }

        public void AddProject()
        {
            Parent.ConfigParser.AddNewProject();
            LoadFromConfig();
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

        public void DeleteGroup(GroupRow group)
        {
            if(SelectedProject != null)
            {
                foreach (ConfigProject configProject in Parent.ConfigParser.ConfigProjects)
                {
                    if (configProject.Name == SelectedProject.Data.ProjectName)
                    {
                        configProject.Groups.RemoveAt(group.Data.Index);
                        break;
                    }
                }

                LoadFromConfig();
            }
        }

        public void AddGroup()
        {
            if (SelectedProject != null)
            {
                foreach (ConfigProject configProject in Parent.ConfigParser.ConfigProjects)
                {
                    if (configProject.Name == SelectedProject.Data.ProjectName)
                    {
                        configProject.Groups.Add(new ConfigGroup());
                        break;
                    }
                }

                Parent.ConfigParser.SaveConfig();
                LoadFromConfig();
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
                        foreach(string Path in configProject.Groups[SelectedGroup.Data.Index].Paths)
                        {
                            PathsPanel.Children.Add(new PathRow(this, new PathRow.PathRowData(Path)));
                        }

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
                                if (!configProject.Groups[SelectedGroup.Data.Index].Paths.Contains(filename))
                                {
                                    configProject.Groups[SelectedGroup.Data.Index].Paths.Add(filename);
                                }
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
                        configProject.Groups[SelectedGroup.Data.Index].Paths.Remove(path.Data.Path);
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
            if (SelectedProject != null && SelectedGroup != null)
            {
                RuleWindow ruleWindow = new RuleWindow(this);

                IExtensionWindow ruleDialog = Parent.ExtensionInterface.CreateWindow(ruleWindow, "Add rule", this);
                ruleWindow.Dialog = ruleDialog;
                ruleDialog.ShowModal();

                if(ruleWindow.IsOK)
                {
                    foreach (ConfigProject configProject in Parent.ConfigParser.ConfigProjects)
                    {
                        if (configProject.Name == SelectedProject.Data.ProjectName)
                        {
                            ConfigRule newRule = new ConfigRule();
                            newRule.Rule = ruleWindow.RegExTextBox.Text;
                            newRule.IsExclude = ruleWindow.RuleType.SelectedIndex == 1;

                            configProject.Groups[SelectedGroup.Data.Index].Rules.Add(newRule);
                            Parent.ConfigParser.SaveConfig();
                            LoadFromConfig();
                            break;
                        }
                    }
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

                        IExtensionWindow ruleDialog = Parent.ExtensionInterface.CreateWindow(ruleWindow, "Edit rule", this);
                        ruleWindow.Dialog = ruleDialog;
                        ruleDialog.ShowModal();

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

            PathsHint.Visibility = pathsCount > 0 ? Visibility.Collapsed : Visibility.Visible;
            RulesHint.Visibility = rulesCount > 0 ? Visibility.Collapsed : Visibility.Visible;
        }

        private void UpdateVisibility()
        {
            bool isAdvanced = Settings.Default.AdvancedProjectSettings;
            AdvancedToggle.Content = isAdvanced ? "›› Basic" : "‹‹ Advanced";

            int projectsCount = Parent.ConfigParser.ConfigProjects.Count;
            int groupsCount = projectsCount > 0 ? Parent.ConfigParser.ConfigProjects[0].Groups.Count : 0;

            bool canGoBasic = projectsCount <= 1 && groupsCount <= 1;

            AdvancedToggle.IsEnabled = isAdvanced && canGoBasic || !isAdvanced;
            AdvancedToggle.ToolTip = !canGoBasic ? "Remove extra projects and groups to go back to basic settings" : null;

            GridLength gridLength = isAdvanced ? new GridLength(1, GridUnitType.Star) : new GridLength(0, GridUnitType.Pixel);
            ProjectsColumn.Width = gridLength;
            GroupsColumn.Width = gridLength;

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

        private void AutomaticPopulation_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedProject != null && SelectedGroup != null)
            {
                foreach (ConfigProject configProject in Parent.ConfigParser.ConfigProjects)
                {
                    if (configProject.Name == SelectedProject.Data.ProjectName)
                    {
                        List<string> solutionFolders = Parent.ExtensionInterface.GatherAllFoldersFromSolution();
                        foreach (string solutionFolder in solutionFolders)
                        {
                            if (!configProject.Groups[SelectedGroup.Data.Index].Paths.Contains(solutionFolder))
                            {
                                configProject.Groups[SelectedGroup.Data.Index].Paths.Add(solutionFolder);
                            }
                        }

                        Parent.ConfigParser.SaveConfig();
                        LoadFromConfig();
                        break;
                    }
                }
            }
        }
    }
}
