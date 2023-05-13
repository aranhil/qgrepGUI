using qgrepControls.Classes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Animation;
using System.Xml.Linq;

namespace qgrepControls.ModelViews
{
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
    public class SearchGroup : SelectableData
    {
        private int index;

        public int Index
        {
            get
            {
                return index;
            }
            set
            {
                index = value;
                OnPropertyChanged();
            }
        }

        public string Name
        {
            get
            {
                return index == 0 ? "<root>" : "Group " + index;
            }
        }

        public ObservableCollection<SearchPath> Paths { get; set; }
        public ObservableCollection<SearchRule> Rules { get; set; }

        public ConfigGroup ConfigGroup;

        public SearchGroup(ConfigGroup configGroup)
        {
            ConfigGroup = configGroup;
            Index = configGroup.Index;
            Paths = new ObservableCollection<SearchPath>();
            Rules = new ObservableCollection<SearchRule>();

            foreach(ConfigPath path in configGroup.Paths)
            {
                Paths.Add(new SearchPath(path));
            }

            foreach(ConfigRule rule in configGroup.Rules)
            {
                Rules.Add(new SearchRule(rule));
            }
        }
    }
    public class SearchConfig: SelectableData
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

        public ObservableCollection<SearchGroup> SearchGroups { get; set; }

        public ConfigProject ConfigProject;

        public SearchConfig(ConfigProject configProject)
        {
            Name = configProject.Name;
            ConfigProject = configProject;
            SearchGroups = new ObservableCollection<SearchGroup>();

            foreach(ConfigGroup configGroup in configProject.Groups)
            {
                SearchGroups.Add(new SearchGroup(configGroup));
            }
        }
    }
}
