using qgrepControls.Properties;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace qgrepControls.Classes
{
    public class ConfigPath
    {
        public ConfigGroup Parent;
        public string Path = "";

        public ConfigPath DeepClone()
        {
            return new ConfigPath() { Path = Path };
        }
    }
    public class ConfigRule
    {
        public ConfigGroup Parent;
        public bool IsExclude = false;
        public string Rule = "";

        public ConfigRule DeepClone()
        {
            return new ConfigRule() { Rule = Rule, IsExclude = IsExclude };
        }
    }
    public class ConfigGroup
    {
        public ConfigProject Parent;
        public string Name;
        public List<ConfigPath> Paths = new List<ConfigPath>();
        public List<ConfigRule> Rules = new List<ConfigRule>();

        public ConfigPath AddNewPath(string path)
        {
            if(Paths.Any(x => x.Path == path))
            {
                return null;
            }

            ConfigPath configPath = new ConfigPath() { Path = path, Parent = this };
            Paths.Add(configPath);
            return configPath;
        }

        public ConfigRule AddNewRule(string regex, bool isExclude)
        {
            ConfigRule configRule = new ConfigRule() { Rule = regex, IsExclude = isExclude, Parent = this };
            Rules.Add(configRule);
            return configRule;
        }

        public ConfigGroup DeepClone()
        {
            ConfigGroup configGroup = new ConfigGroup();
            configGroup.Paths = Paths.Select(x => x.DeepClone()).ToList();
            configGroup.Rules = Rules.Select(x => x.DeepClone()).ToList();
            return configGroup;
        }
    }
    public class ConfigProject
    {
        private string PathPrefix = "path ";
        private string IncludePrefix = "include ";
        private string ExcludePrefix = "exclude ";
        private string GroupBegin = "group";
        private string GroupEnd = "endgroup";
        private string GroupName = "# group name:";
        private string _Path = "";

        public string Name { get; set; }
        public string Path
        {
            get
            {
                return _Path;
            }
            set
            {
                _Path = value;
                Name = System.IO.Path.GetFileNameWithoutExtension(value);
            }
        }

        public List<ConfigGroup> Groups = new List<ConfigGroup>();

        public ConfigProject(string Path)
        {
            this.Path = Path;
            this.Groups.Add(new ConfigGroup() { Name = GetNewGroupName(), Parent = this });
        }

        public void DeleteFiles()
        {
            string directory = System.IO.Path.GetDirectoryName(Path);
            File.Delete(directory + "\\" + Name + ".cfg");
            File.Delete(directory + "\\" + Name + ".qgd");
            File.Delete(directory + "\\" + Name + ".qgf");
        }

        public bool Rename(string newName)
        {
            string directory = System.IO.Path.GetDirectoryName(Path);
            string newPath = directory + "\\" + newName + ".cfg";

            if (!File.Exists(newPath))
            {
                DeleteFiles();
                Path = newPath;
                SaveConfig();

                return true;
            }

            return false;
        }

        public ConfigGroup AddNewGroup()
        {
            ConfigGroup configGroup = new ConfigGroup() { Name = GetNewGroupName(), Parent = this };
            Groups.Add(configGroup);
            return configGroup;
        }

        private string GetNewGroupName()
        {
            int index = 1;
            string newName = Groups.Count > 0 ? "Group " + index++ : "<root>";
            while (Groups.Any(x => x.Name == newName))
            {
                newName = "Group " + index++;
            }
            return newName;
        }

        public void LoadConfig()
        {
            Groups.Clear();
            Groups.Add(new ConfigGroup() { Parent = this });

            bool insideGroup = false;

            using (StreamReader sr = new StreamReader(Path))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    if (line.StartsWith(PathPrefix))
                    {
                        ConfigPath path = new ConfigPath() { Parent = GetGroup(insideGroup) };
                        path.Path = line.Substring(PathPrefix.Length);
                        GetGroup(insideGroup).Paths.Add(path);
                    }
                    else if (line.StartsWith(IncludePrefix))
                    {
                        ConfigRule rule = new ConfigRule() { Parent = GetGroup(insideGroup) };
                        rule.Rule = line.Substring(IncludePrefix.Length);
                        GetGroup(insideGroup).Rules.Add(rule);
                    }
                    else if (line.StartsWith(ExcludePrefix))
                    {
                        ConfigRule rule = new ConfigRule() { Parent = GetGroup(insideGroup) };
                        rule.IsExclude = true;
                        rule.Rule = line.Substring(IncludePrefix.Length);
                        GetGroup(insideGroup).Rules.Add(rule);
                    }
                    else if (line.StartsWith(GroupBegin))
                    {
                        insideGroup = true;
                        Groups.Add(new ConfigGroup() { Parent = this });
                    }
                    else if (line.StartsWith(GroupEnd))
                    {
                        insideGroup = false;
                    }
                    else if (line.StartsWith(GroupName))
                    {
                        string groupName = line.Substring(GroupName.Length);
                        GetGroup(insideGroup).Name = groupName;
                    }
                }
            }
        }

        public void SaveConfig()
        {
            using (StreamWriter streamWriter = new StreamWriter(Path))
            {
                SaveGroup(streamWriter, Groups.First());

                for (int i = 1; i < Groups.Count; i++)
                {
                    streamWriter.WriteLine(GroupBegin);
                    SaveGroup(streamWriter, Groups[i]);
                    streamWriter.WriteLine(GroupEnd);
                }
            }
        }

        public void SaveGroup(StreamWriter streamWriter, ConfigGroup configGroup)
        {
            streamWriter.WriteLine(GroupName + configGroup.Name);
            foreach (ConfigPath path in configGroup.Paths)
            {
                streamWriter.WriteLine(PathPrefix + path.Path);
            }
            foreach (ConfigRule rule in configGroup.Rules)
            {
                if (rule.IsExclude)
                {
                    streamWriter.WriteLine(ExcludePrefix + rule.Rule);
                }
                else
                {
                    streamWriter.WriteLine(IncludePrefix + rule.Rule);
                }
            }
        }

        private ConfigGroup GetGroup(bool insideGroup)
        {
            if (insideGroup)
            {
                return Groups.Last();
            }
            else
            {
                return Groups.First();
            }
        }

        public ConfigProject DeepClone()
        {
            ConfigProject configProject = new ConfigProject(Path);
            configProject.Groups = Groups.Select(x => x.DeepClone()).ToList();
            return configProject;
        }
    }
    public class ConfigParser
    {
        public string Path = "";
        public string PathSuffix = @"\.qgrep\";

        public ObservableCollection<ConfigProject> ConfigProjects = new ObservableCollection<ConfigProject>();
        public ObservableCollection<ConfigProject> OldConfigProjects = new ObservableCollection<ConfigProject>();

        public ConfigParser(string Path)
        {
            this.Path = Path;
        }

        public void LoadConfig()
        {
            ConfigProjects.Clear();

            if (!Directory.Exists(Path + PathSuffix))
            {
                Directory.CreateDirectory(Path + PathSuffix);
            }

            string[] configs = Directory.GetFiles(Path + PathSuffix, "*.cfg");
            if(configs.Length == 0)
            {
                AddNewProject();
            }
            else
            {
                foreach(string config in configs)
                {
                    ConfigProject configProject = new ConfigProject(config);
                    configProject.LoadConfig();
                    ConfigProjects.Add(configProject);
                }
            }
        }
        public void SaveConfig()
        {
            foreach(ConfigProject configProject in ConfigProjects)
            {
                configProject.SaveConfig();
            }
        }
        public ConfigProject AddNewProject()
        {
            int index = 1;
            string newPath = "";

            do
            {
                newPath = Path + PathSuffix + "Config" + index + ".cfg";
                index++;
            }
            while(File.Exists(newPath));

            ConfigProject newConfigProject = new ConfigProject(newPath);
            ConfigProjects.Add(newConfigProject);
            newConfigProject.SaveConfig();
            return newConfigProject;
        }

        public void RemoveProject(ConfigProject configProject)
        {
            configProject.DeleteFiles();
            ConfigProjects.Remove(configProject);
        }

        public void CleanProjects()
        {
            foreach (ConfigProject configProject in ConfigProjects)
            {
                string directory = System.IO.Path.GetDirectoryName(configProject.Path);
                File.Delete(directory + "\\" + configProject.Name + ".qgd");
                File.Delete(directory + "\\" + configProject.Name + ".qgf");
            }
        }

        public static string GetCommonPathPrefix(string path1, string path2)
        {
            int minLength = Math.Min(path1.Length, path2.Length);
            int commonLength = 0;

            for (int i = 0; i < minLength; i++)
            {
                if (path1[i] == path2[i])
                {
                    commonLength++;
                }
                else
                {
                    break;
                }
            }

            string commonPathPrefix = path1.Substring(0, commonLength);

            return commonPathPrefix;
        }

        public string RemovePaths(string file)
        {
            if (Settings.Default.PathStyleIndex == 1)
            {
                foreach (ConfigProject configProject in ConfigProjects)
                {
                    bool foundFirstPath = false;
                    string commonPathPrefix = "";

                    foreach (ConfigGroup configGroup in configProject.Groups)
                    {
                        foreach (ConfigPath path in configGroup.Paths)
                        {
                            if (!foundFirstPath)
                            {
                                foundFirstPath = true;
                                commonPathPrefix = path.Path;
                            }
                            else
                            {
                                commonPathPrefix = GetCommonPathPrefix(commonPathPrefix, path.Path);
                            }
                        }
                    }

                    if (commonPathPrefix.Length > 0)
                    {
                        file = file.Replace(commonPathPrefix, "");
                    }
                }
            }
            else if(Settings.Default.PathStyleIndex == 2)
            {
                file = System.IO.Path.GetFileName(file);
            }

            return file;
        }

        public bool HasAnyPaths()
        {
            foreach(ConfigProject configProject in ConfigProjects)
            {
                foreach(ConfigGroup configGroup in configProject.Groups)
                {
                    if(configGroup.Paths.Count > 0)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public void SaveOldCopy()
        {
            OldConfigProjects = new ObservableCollection<ConfigProject>(ConfigProjects.Select(x => x.DeepClone()));
        }

        public bool IsConfigChanged()
        {
            if(ConfigProjects.Count != OldConfigProjects.Count)
            {
                return true;
            }

            for(int i = 0; i < ConfigProjects.Count; i++)
            {
                if (ConfigProjects[i].Groups.Count != OldConfigProjects[i].Groups.Count)
                {
                    return true;
                }

                for(int j = 0; j < ConfigProjects[i].Groups.Count; j++)
                {
                    if (ConfigProjects[i].Groups[j].Paths.Count != OldConfigProjects[i].Groups[j].Paths.Count)
                    {
                        return true;
                    }

                    if (ConfigProjects[i].Groups[j].Rules.Count != OldConfigProjects[i].Groups[j].Rules.Count)
                    {
                        return true;
                    }

                    for (int k = 0; k < ConfigProjects[i].Groups[j].Paths.Count; k++)
                    {
                        if (!ConfigProjects[i].Groups[j].Paths[k].Path.Equals(OldConfigProjects[i].Groups[j].Paths[k].Path))
                        {
                            return true;
                        }
                    }

                    for(int k = 0; k < ConfigProjects[i].Groups[j].Rules.Count; k++)
                    {
                        if (!ConfigProjects[i].Groups[j].Rules[k].Rule.Equals(OldConfigProjects[i].Groups[j].Rules[k].Rule) ||
                            ConfigProjects[i].Groups[j].Rules[k].IsExclude != OldConfigProjects[i].Groups[j].Rules[k].IsExclude)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }
    }
}
