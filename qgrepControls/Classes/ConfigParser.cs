using qgrepControls.Properties;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace qgrepControls.Classes
{
    public class ConfigRule
    {
        public bool IsExclude = false;
        public string Rule = "";
    }
    public class ConfigGroup
    {
        public List<string> Paths = new List<string>();
        public List<ConfigRule> Rules = new List<ConfigRule>();
    }
    public class ConfigProject
    {
        private string PathPrefix = "path ";
        private string IncludePrefix = "include ";
        private string ExcludePrefix = "exclude ";
        private string GroupBegin = "group";
        private string GroupEnd = "endgroup";
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
            this.Groups.Add(new ConfigGroup());
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

        public void LoadConfig()
        {
            Groups.Clear();
            Groups.Add(new ConfigGroup());

            bool insideGroup = false;

            using (StreamReader sr = new StreamReader(Path))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    if (line.StartsWith(PathPrefix))
                    {
                        GetGroup(insideGroup).Paths.Add(line.Substring(PathPrefix.Length));
                    }
                    else if(line.StartsWith(IncludePrefix))
                    {
                        ConfigRule rule = new ConfigRule();
                        rule.Rule = line.Substring(IncludePrefix.Length);
                        GetGroup(insideGroup).Rules.Add(rule);
                    }
                    else if(line.StartsWith(ExcludePrefix))
                    {
                        ConfigRule rule = new ConfigRule();
                        rule.IsExclude = true;
                        rule.Rule = line.Substring(IncludePrefix.Length);
                        GetGroup(insideGroup).Rules.Add(rule);
                    }
                    else if(line.StartsWith(GroupBegin))
                    {
                        insideGroup = true;
                        Groups.Add(new ConfigGroup());
                    }
                    else if(line.StartsWith(GroupEnd))
                    {
                        insideGroup = false;
                    }
                }
            }
        }

        public void SaveConfig()
        {
            using (StreamWriter streamWriter = new StreamWriter(Path))
            {
                SaveGroup(streamWriter, Groups.First());

                for(int i = 1; i < Groups.Count; i++)
                {
                    streamWriter.WriteLine(GroupBegin);
                    SaveGroup(streamWriter, Groups[i]);
                    streamWriter.WriteLine(GroupEnd);
                }
            }
        }

        public void SaveGroup(StreamWriter streamWriter, ConfigGroup configGroup)
        {
            foreach(string path in configGroup.Paths)
            {
                streamWriter.WriteLine(PathPrefix + path);
            }
            foreach (ConfigRule rule in configGroup.Rules)
            {
                if(rule.IsExclude)
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
    }
    public class ConfigParser
    {
        public string Path = "";
        public string PathSuffix = @"\.qgrep\";

        public ObservableCollection<ConfigProject> ConfigProjects = new ObservableCollection<ConfigProject>();

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
                SaveConfig();
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
        public void AddNewProject()
        {
            int index = 1;
            string newPath = "";

            do
            {
                newPath = Path + PathSuffix + "Config" + index + ".cfg";
                index++;
            }
            while(File.Exists(newPath));

            ConfigProjects.Add(new ConfigProject(newPath));
        }
        public void RemoveProject(string name)
        {
            foreach (ConfigProject configProject in ConfigProjects)
            {
                if (configProject.Name == name)
                {
                    configProject.DeleteFiles();
                    ConfigProjects.Remove(configProject);
                    break;
                }
            }
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
                        foreach (string path in configGroup.Paths)
                        {
                            if (!foundFirstPath)
                            {
                                foundFirstPath = true;
                                commonPathPrefix = path;
                            }
                            else
                            {
                                commonPathPrefix = GetCommonPathPrefix(commonPathPrefix, path);
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
    }
}
