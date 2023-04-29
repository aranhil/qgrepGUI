using qgrepControls.Classes;
using qgrepControls.Properties;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media;

namespace qgrepControls.ToolWindows
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
        }

        public void LoadFromConfig()
        {
            if (Parent.ConfigParser != null)
            {
                Parent.ConfigParser.SaveConfig();
                Parent.ConfigParser.LoadConfig();
                Parent.UpdateFilters();
                LoadProjectsFromConfig();
                LoadGroupsFromConfig();
                UpdateVisibility();
            }
        }

        private void LoadColorsFromResources()
        {
            Dictionary<string, System.Windows.Media.Color> colors = Parent.GetColorsFromColorScheme();

            foreach (var color in colors)
            {
                Resources[color.Key] = new SolidColorBrush(color.Value);
            }
        }

        public void LoadProjectsFromConfig()
        {
            ProjectsPanel.Children.Clear();

            foreach (ConfigProject configProject in Parent.ConfigParser.ConfigProjects)
            {
                ProjectsPanel.Children.Add(new ProjectRow(this, new ProjectRow.ProjectRowData(configProject.Name)));
            }

            ProjectsPanel.Children.Add(new RowAdd(this, "Add new project", new RowAdd.ClickCallbackFunction(AddProject)));
            CheckAddButtonVisibility();

            if (SelectedProject != null)
            {
                bool foundOldProject = false;
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

                if(!foundOldProject)
                {
                    SelectedProject = null;
                }
            }
            else
            {
                if(ProjectsPanel.Children.Count > 1)
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

                        GroupsPanel.Children.Add(new RowAdd(this, "Add new group", new RowAdd.ClickCallbackFunction(AddGroup)));
                        CheckAddButtonVisibility();

                        if (SelectedGroup != null)
                        {
                            bool foundOldGroup = false;
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

                            if (!foundOldGroup)
                            {
                                SelectedGroup = null;
                            }
                        }
                        else
                        {
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
                    configProject.Rename(newName);
                    LoadFromConfig();
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

                        PathsPanel.Children.Add(new RowAdd(this, "Add new path", new RowAdd.ClickCallbackFunction(AddPath)));
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

                using (var fbd = new FolderBrowserDialog())
                {
                    fbd.Reset();
                    fbd.RootFolder = Environment.SpecialFolder.Desktop;
                    fbd.SelectedPath = Parent.ConfigParser.Path;

                    DialogResult result = FolderBrowserLauncher.ShowFolderBrowser(fbd);

                    if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                    {
                        if (Directory.Exists(fbd.SelectedPath))
                        {
                            foreach (ConfigProject configProject in Parent.ConfigParser.ConfigProjects)
                            {
                                if (configProject.Name == SelectedProject.Data.ProjectName)
                                {
                                    configProject.Groups[SelectedGroup.Data.Index].Paths.Add(fbd.SelectedPath);
                                    Parent.ConfigParser.SaveConfig();
                                    LoadFromConfig();
                                    break;
                                }
                            }

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

                        RulesPanel.Children.Add(new RowAdd(this, "Add new rule", new RowAdd.ClickCallbackFunction(AddRule)));
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

                IExtensionWindow ruleDialog = Parent.ExtensionInterface.CreateWindow(ruleWindow, "Add rule");
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
                            newRule.IsExclude = ruleWindow.GroupType.SelectedIndex == 1;

                            configProject.Groups[SelectedGroup.Data.Index].Rules.Add(newRule);
                            Parent.ConfigParser.SaveConfig();
                            LoadFromConfig();
                            break;
                        }
                    }
                }
            }
        }

        private void GroupsPanel_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            ShowPanel(GroupsPanel);
        }

        private void GroupsPanel_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            HidePanel(GroupsPanel);
        }

        private void ProjectsPanel_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            ShowPanel(ProjectsPanel);
        }

        private void ProjectsPanel_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            HidePanel(ProjectsPanel);
        }

        private void PathsPanel_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            ShowPanel(PathsPanel);
        }

        private void PathsPanel_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            HidePanel(PathsPanel);
        }

        private void RulesPanel_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            ShowPanel(RulesPanel);
        }

        private void RulesPanel_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            HidePanel(RulesPanel);
        }

        private void CheckAddButtonVisibility()
        {
            if (ProjectsPanel.IsMouseOver)
            {
                ShowPanel(ProjectsPanel);
            }
            if (GroupsPanel.IsMouseOver)
            {
                ShowPanel(GroupsPanel);
            }
            if (PathsPanel.IsMouseOver)
            {
                ShowPanel(PathsPanel);
            }
            if (RulesPanel.IsMouseOver)
            {
                ShowPanel(RulesPanel);
            }
        }

        void HidePanel(StackPanel panel)
        {
            if (panel.Children.Count > 0)
            {
                panel.Children[panel.Children.Count - 1].Visibility = Visibility.Hidden;
            }
        }
        void ShowPanel(StackPanel panel)
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
    }
}
