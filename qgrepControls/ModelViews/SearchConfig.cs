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
    public class EditableData : SelectableData
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

        private Visibility normalTextVisibility = Visibility.Visible;
        public Visibility NormalTextVisibility
        {
            get
            {
                return normalTextVisibility;
            }
            set
            {
                normalTextVisibility = value;
                OnPropertyChanged();
            }
        }

        public bool InEditMode
        {
            get
            {
                return EditTextBoxVisibility != Visibility.Collapsed;
            }
            set
            {
                EditTextBoxVisibility = value ? Visibility.Visible : Visibility.Collapsed;
                NormalTextVisibility = value ? Visibility.Collapsed: Visibility.Visible;
            }
        }
    }

    public class SearchRule : EditableData
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
    public class SearchGroup : EditableData
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

        public ObservableCollection<SearchPath> Paths { get; set; }
        public ObservableCollection<SearchRule> Rules { get; set; }

        public ConfigGroup ConfigGroup;

        public SearchGroup(ConfigGroup configGroup)
        {
            ConfigGroup = configGroup;
            Name = configGroup.Name;
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
    public class SearchConfig : EditableData
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
                if(ConfigProject.Rename(value))
                {
                    name = value;
                    OnPropertyChanged();
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
