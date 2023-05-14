using qgrepControls.Classes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;
using System.Xml.Linq;

namespace qgrepControls.ModelViews
{
    public abstract class IEditableData : SelectableData
    {
        private Visibility editTextBoxVisibility = Visibility.Collapsed;
        public Visibility EditTextBoxVisibility
        {
            get
            {
                return editTextBoxVisibility;
            }
            set
            {
                editTextBoxVisibility = value;
                OnPropertyChanged();
            }
        }

        public abstract bool InEditMode { get; set; }
    }

    public class SearchRule : SelectableData
    {
        private bool isExclude;
        private string regEx;

        public bool IsExclude
        {
            get
            {
                return isExclude;
            }
            set
            {
                isExclude = value;
                IsExcludeText = isExclude ? "Exclude" : "Include";
                OnPropertyChanged();
            }
        }

        private string isExcludeText;
        public string IsExcludeText
        {
            get
            {
                return isExcludeText;
            }
            set
            {
                isExcludeText = value;
                OnPropertyChanged();
            }
        }

        public string RegEx
        {
            get
            {
                return regEx;
            }
            set
            {
                regEx = value;
                OnPropertyChanged();
            }
        }

        public ConfigRule ConfigRule;

        public SearchRule(ConfigRule configRule)
        {
            ConfigRule = configRule;
            IsExclude = configRule.IsExclude;
            RegEx = configRule.Rule;
        }

        internal void UpdateConfig()
        {
            ConfigRule.Rule = RegEx;
            ConfigRule.IsExclude = IsExclude;
        }
    }
    public class SearchPath : SelectableData
    {
        private string path;

        public string Path
        {
            get
            {
                return path;
            }
            set
            {
                path = value;
                OnPropertyChanged();
            }
        }

        public ConfigPath ConfigPath;

        public SearchPath(ConfigPath configPath)
        {
            ConfigPath = configPath;
            Path = configPath.Path;
        }
    }
    public class SearchGroup : IEditableData
    {
        private string name;

        public string Name
        {
            get
            {
                return name;
            }
            set
            {
                name = value;
                OnPropertyChanged();
            }
        }

        private bool inEditMode = false;
        public override bool InEditMode
        {
            get
            {
                return inEditMode;
            }
            set
            {
                inEditMode = value;
                OnPropertyChanged();

                EditTextBoxVisibility = inEditMode ? Visibility.Visible : Visibility.Collapsed;

                if (!inEditMode && !name.Equals(ConfigGroup.Name))
                {
                    ConfigGroup.Name = name;
                }
            }
        }

        public ObservableCollection<SearchPath> Paths { get; set; }
        public ObservableCollection<SearchRule> Rules { get; set; }

        public ConfigGroup ConfigGroup;

        public SearchGroup(ConfigGroup configGroup)
        {
            name = configGroup.Name;
            ConfigGroup = configGroup;
            Paths = new ObservableCollection<SearchPath>();
            Rules = new ObservableCollection<SearchRule>();

            foreach (ConfigPath path in configGroup.Paths)
            {
                Paths.Add(new SearchPath(path));
            }

            foreach (ConfigRule rule in configGroup.Rules)
            {
                Rules.Add(new SearchRule(rule));
            }
        }
    }
    public class SearchConfig : IEditableData
    {
        private string name;

        public string Name
        {
            get
            {
                return name;
            }
            set
            {
                name = value;
                OnPropertyChanged();
            }
        }

        private bool inEditMode = false;
        public override bool InEditMode
        {
            get
            {
                return inEditMode;
            }
            set
            {
                inEditMode = value;
                OnPropertyChanged();

                EditTextBoxVisibility = inEditMode ? Visibility.Visible : Visibility.Collapsed;

                if (!inEditMode && !name.Equals(ConfigProject.Name))
                {
                    if(!ConfigProject.Rename(name))
                    {
                        Name = ConfigProject.Name;
                    }
                }
            }
        }

        public ObservableCollection<SearchGroup> SearchGroups { get; set; }

        public ConfigProject ConfigProject;

        public SearchConfig(ConfigProject configProject)
        {
            name = configProject.Name;
            ConfigProject = configProject;
            SearchGroups = new ObservableCollection<SearchGroup>();

            foreach (ConfigGroup configGroup in configProject.Groups)
            {
                SearchGroups.Add(new SearchGroup(configGroup));
            }
        }
    }
}
