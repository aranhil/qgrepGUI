using qgrepControls.Properties;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Shapes;
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
            if (Rules.Any(x => x.Rule == regex && x.IsExclude == isExclude))
            {
                return null;
            }

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

        public bool HasGeneratedFiles()
        {
            string directory = System.IO.Path.GetDirectoryName(Path);
            return File.Exists(directory + "\\" + Name + ".qgd") && File.Exists(directory + "\\" + Name + ".qgf");
        }

        public bool Rename(string newName)
        {
            if (newName.Length == 0)
            {
                return false;
            }

            string directory = System.IO.Path.GetDirectoryName(Path);
            string newPath = directory + "\\" + newName + ".cfg";

            if (!File.Exists(newPath))
            {
                try
                {
                    System.IO.File.Move(directory + "\\" + Name + ".cfg", directory + "\\" + newName + ".cfg");

                    if(File.Exists(directory + "\\" + Name + ".qgd"))
                    {
                        System.IO.File.Move(directory + "\\" + Name + ".qgd", directory + "\\" + newName + ".qgd");
                    }

                    if(File.Exists(directory + "\\" + Name + ".qgf"))
                    {
                        System.IO.File.Move(directory + "\\" + Name + ".qgf", directory + "\\" + newName + ".qgf");
                    }
                }
                catch
                {
                    return false;
                }

                try
                {
                    Settings.Default.SearchFilters = Settings.Default.SearchFilters.Replace(Name, newName);
                    Settings.Default.Save();
                }
                catch { }

                Path = newPath;
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
            Groups.Add(new ConfigGroup() { Parent = this, Name = GetNewGroupName() });

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
                        Groups.Add(new ConfigGroup() { Parent = this, Name = GetNewGroupName() });
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
        private static ConfigParser instance = null;
        private static readonly object padlock = new object();

        public static ConfigParser Instance
        {
            get
            {
                lock (padlock)
                {
                    if (instance == null)
                    {
                        instance = new ConfigParser();
                    }
                    return instance;
                }
            }
        }

        public string Path = "";
        public string PathSuffix = @"\.qgrep\";

        public ObservableCollection<ConfigProject> ConfigProjects = new ObservableCollection<ConfigProject>();
        public ObservableCollection<ConfigProject> OldConfigProjects = new ObservableCollection<ConfigProject>();

        public ConfigParser()
        {
        }

        public static void Init(string Path)
        {
            Instance.Path = Path;
        }

        public static void LoadConfig()
        {
            Instance.ConfigProjects.Clear();

            if (!Directory.Exists(Instance.Path + Instance.PathSuffix))
            {
                Directory.CreateDirectory(Instance.Path + Instance.PathSuffix);
            }

            string[] configs = Directory.GetFiles(Instance.Path + Instance.PathSuffix, "*.cfg");
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
                    Instance.ConfigProjects.Add(configProject);
                }
            }
        }

        public static void UnloadConfig()
        {
            Instance.ConfigProjects.Clear();
        }

        public static void SaveConfig()
        {
            foreach(ConfigProject configProject in Instance.ConfigProjects)
            {
                configProject.SaveConfig();
            }
        }
        public static ConfigProject AddNewProject()
        {
            int index = 1;
            string newPath = "";

            do
            {
                newPath = Instance.Path + Instance.PathSuffix + "Config" + index + ".cfg";
                index++;
            }
            while(File.Exists(newPath));

            ConfigProject newConfigProject = new ConfigProject(newPath);
            Instance.ConfigProjects.Add(newConfigProject);
            newConfigProject.SaveConfig();
            return newConfigProject;
        }

        public static void RemoveProject(ConfigProject configProject)
        {
            configProject.DeleteFiles();
            Instance.ConfigProjects.Remove(configProject);
        }

        public static void CleanProjects()
        {
            foreach (ConfigProject configProject in Instance.ConfigProjects)
            {
                string directory = System.IO.Path.GetDirectoryName(configProject.Path);
                File.Delete(directory + "\\" + configProject.Name + ".qgd");
                File.Delete(directory + "\\" + configProject.Name + ".qgf");
            }
        }

        public static DateTime GetLastUpdated()
        {
            DateTime lastUpdated = DateTime.MaxValue;
            foreach (ConfigProject configProject in Instance.ConfigProjects)
            {
                string directory = System.IO.Path.GetDirectoryName(configProject.Path);
                string fileToCheck = directory + "\\" + configProject.Name + ".qgd";

                if(File.Exists(fileToCheck))
                {
                    DateTime lastModified = File.GetLastWriteTime(fileToCheck);
                    if(lastModified < lastUpdated)
                    {
                        lastUpdated = lastModified;
                    }
                }
            }

            return lastUpdated;
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

        public static string RemovePaths(string file)
        {
            if (Settings.Default.PathStyleIndex == 1)
            {
                foreach (ConfigProject configProject in Instance.ConfigProjects)
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

        public static string GetPathToRemove()
        {
            if (Settings.Default.PathStyleIndex == 1)
            {
                foreach (ConfigProject configProject in Instance.ConfigProjects)
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
                        return commonPathPrefix;
                    }
                }
            }
            else if(Settings.Default.PathStyleIndex == 2)
            {
                return "*";
            }

            return "";
        }

        public static bool HasAnyPaths()
        {
            foreach(ConfigProject configProject in Instance.ConfigProjects)
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

        public static void SaveOldCopy()
        {
            Instance.OldConfigProjects = new ObservableCollection<ConfigProject>(Instance.ConfigProjects.Select(x => x.DeepClone()));
        }

        public static bool IsConfigChanged()
        {
            if(Instance.ConfigProjects.Count != Instance.OldConfigProjects.Count)
            {
                return true;
            }

            for(int i = 0; i < Instance.ConfigProjects.Count; i++)
            {
                if (!instance.ConfigProjects[i].HasGeneratedFiles())
                {
                    return true;
                }

                if (Instance.ConfigProjects[i].Groups.Count != Instance.OldConfigProjects[i].Groups.Count)
                {
                    return true;
                }

                for(int j = 0; j < Instance.ConfigProjects[i].Groups.Count; j++)
                {
                    if (Instance.ConfigProjects[i].Groups[j].Paths.Count != Instance.OldConfigProjects[i].Groups[j].Paths.Count)
                    {
                        return true;
                    }

                    if (Instance.ConfigProjects[i].Groups[j].Rules.Count != Instance.OldConfigProjects[i].Groups[j].Rules.Count)
                    {
                        return true;
                    }

                    for (int k = 0; k < Instance.ConfigProjects[i].Groups[j].Paths.Count; k++)
                    {
                        if (!Instance.ConfigProjects[i].Groups[j].Paths[k].Path.Equals(Instance.OldConfigProjects[i].Groups[j].Paths[k].Path))
                        {
                            return true;
                        }
                    }

                    for(int k = 0; k < Instance.ConfigProjects[i].Groups[j].Rules.Count; k++)
                    {
                        if (!Instance.ConfigProjects[i].Groups[j].Rules[k].Rule.Equals(Instance.OldConfigProjects[i].Groups[j].Rules[k].Rule) ||
                            Instance.ConfigProjects[i].Groups[j].Rules[k].IsExclude != Instance.OldConfigProjects[i].Groups[j].Rules[k].IsExclude)
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
