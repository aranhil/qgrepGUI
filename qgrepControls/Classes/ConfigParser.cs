using qgrepControls.Properties;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Timers;

namespace qgrepControls.Classes
{
    public class ConfigPath
    {
        public ConfigGroup Parent;

        private string path = "";
        public string Path
        {
            get
            {
                return path;
            }
            set
            {
                path = System.IO.Path.GetFullPath(value);
            }
        }

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
            if (Paths.Any(x => x.Path == path))
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

                    if (File.Exists(directory + "\\" + Name + ".qgd"))
                    {
                        System.IO.File.Move(directory + "\\" + Name + ".qgd", directory + "\\" + newName + ".qgd");
                    }

                    if (File.Exists(directory + "\\" + Name + ".qgf"))
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
            string newName = Groups.Count > 0 ? string.Format(Properties.Resources.GroupFormat, index++) : Properties.Resources.RootContent;
            while (Groups.Any(x => x.Name == newName))
            {
                newName = string.Format(Properties.Resources.GroupFormat, index++);
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
            try
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
            catch { }
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
                string processedRule = rule.Rule;
                processedRule = processedRule.Replace("\\\\", "\\/");

                if (rule.IsExclude)
                {
                    streamWriter.WriteLine(ExcludePrefix + processedRule);
                }
                else
                {
                    streamWriter.WriteLine(IncludePrefix + processedRule);
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

        public event Action<List<string>> FilesChanged;
        public event Action FilesAddedOrRemoved;
        private List<FileSystemWatcher> Watchers = new List<FileSystemWatcher>();

        public ConfigParser()
        {
        }

        public static string ToUtf8(string path)
        {
            byte[] utf8Bytes = Encoding.UTF8.GetBytes(path);
            return Encoding.GetEncoding("Windows-1252").GetString(utf8Bytes);
        }

        public static string FromUtf8(string path)
        {
            byte[] windows1252Bytes = Encoding.GetEncoding("Windows-1252").GetBytes(path);
            return Encoding.UTF8.GetString(windows1252Bytes);
        }

        public static void Initialize(string Path)
        {
            if (Instance.Path != Path)
            {
                UnloadConfig();
                Instance.Path = Path;
                LoadConfig();
            }
        }

        public static bool IsInitialized()
        {
            return Instance.Path.Length > 0;
        }

        public static void LoadConfig()
        {
            Instance.ConfigProjects = new ObservableCollection<ConfigProject>();

            if (!Directory.Exists(Instance.Path + Instance.PathSuffix))
            {
                Directory.CreateDirectory(Instance.Path + Instance.PathSuffix);
            }

            string[] configs = Directory.GetFiles(Instance.Path + Instance.PathSuffix, "*.cfg");
            if (configs.Length == 0)
            {
                AddNewProject();
            }
            else
            {
                foreach (string config in configs)
                {
                    ConfigProject configProject = new ConfigProject(config);
                    configProject.LoadConfig();
                    Instance.ConfigProjects.Add(configProject);
                }
            }

            if (Settings.Default.UpdateIndexAutomatically)
            {
                RemoveWatchers();
                AddWatchers();
            }
        }

        public static void AddWatchers()
        {
            List<string> uniqueFolders = new List<string>();
            foreach (ConfigProject configProject in Instance.ConfigProjects)
            {
                foreach (ConfigGroup configGroup in configProject.Groups)
                {
                    foreach (ConfigPath configPath in configGroup.Paths)
                    {
                        bool foundCommonPath = false;
                        for (int i = 0; i < uniqueFolders.Count; i++)
                        {
                            string commonPath = GetCommonPathPrefix(uniqueFolders[i], configPath.Path);
                            if (commonPath.Length > 0)
                            {
                                foundCommonPath = true;
                                uniqueFolders[i] = commonPath;
                                break;
                            }
                        }

                        if (!foundCommonPath)
                        {
                            uniqueFolders.Add(configPath.Path);
                        }
                    }
                }
            }

            foreach (string uniqueFolder in uniqueFolders)
            {
                if (Directory.Exists(uniqueFolder))
                {
                    try
                    {
                        FileSystemWatcher watcher = new FileSystemWatcher(uniqueFolder);
                        watcher.NotifyFilter = NotifyFilters.Attributes
                                 | NotifyFilters.CreationTime
                                 | NotifyFilters.DirectoryName
                                 | NotifyFilters.FileName
                                 | NotifyFilters.LastAccess
                                 | NotifyFilters.LastWrite
                                 | NotifyFilters.Security
                                 | NotifyFilters.Size;
                        watcher.Changed += OnChanged;
                        watcher.Created += OnCreated;
                        watcher.Deleted += OnDeleted;
                        watcher.Renamed += OnRenamed;
                        watcher.EnableRaisingEvents = true;
                        watcher.IncludeSubdirectories = true;
                        Instance.Watchers.Add(watcher);
                    }
                    catch { }
                }
            }
        }

        public static void UnloadConfig()
        {
            Instance.Path = "";
            Instance.ConfigProjects.Clear();

            RemoveWatchers();
        }

        public static void RemoveWatchers()
        {
            foreach (var watcher in Instance.Watchers)
            {
                watcher.Dispose();
            }

            Instance.Watchers.Clear();
        }

        public static void SaveConfig()
        {
            foreach (ConfigProject configProject in Instance.ConfigProjects)
            {
                configProject.SaveConfig();
            }
        }
        public static ConfigProject AddNewProject()
        {
            int index = 1;
            string newPath;

            do
            {
                newPath = Instance.Path + Instance.PathSuffix + string.Format(Properties.Resources.ConfigFormat, index) + ".cfg";
                index++;
            }
            while (File.Exists(newPath));

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

                if (File.Exists(fileToCheck))
                {
                    DateTime lastModified = File.GetLastWriteTime(fileToCheck);
                    if (lastModified < lastUpdated)
                    {
                        lastUpdated = lastModified;
                    }
                }

                fileToCheck = directory + "\\" + configProject.Name + ".qgc";
                if (File.Exists(fileToCheck))
                {
                    DateTime lastModified = File.GetLastWriteTime(fileToCheck);
                    if (lastModified < lastUpdated)
                    {
                        lastUpdated = lastModified;
                    }
                }
            }

            return lastUpdated;
        }

        public static string GetCommonPathPrefix(string path1, string path2)
        {
            var parts1 = path1.Split('\\');
            var parts2 = path2.Split('\\');

            int minLength = Math.Min(parts1.Length, parts2.Length);
            int commonLength = 0;

            for (int i = 0; i < minLength; i++)
            {
                if (parts1[i] == parts2[i])
                {
                    commonLength++;
                }
                else
                {
                    break;
                }
            }

            var commonParts = parts1.Take(commonLength);
            string commonPathPrefix = string.Join("\\", commonParts);

            // Ensure the common path prefix ends with a backslash if it's not the root
            if (commonPathPrefix.Length > 0 && !commonPathPrefix.EndsWith("\\"))
            {
                commonPathPrefix += "\\";
            }

            return commonPathPrefix;
        }

        public static string RemovePaths(string file, int pathStyle)
        {
            if (pathStyle == 1)
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
            else if (pathStyle == 2)
            {
                file = System.IO.Path.GetFileName(file);
            }

            return file;
        }

        public static string GetPathToRemove(int pathStyle)
        {
            if (pathStyle == 1)
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
            else if (pathStyle == 2)
            {
                return "*";
            }

            return "";
        }

        public static bool HasAnyPaths()
        {
            foreach (ConfigProject configProject in Instance.ConfigProjects)
            {
                foreach (ConfigGroup configGroup in configProject.Groups)
                {
                    if (configGroup.Paths.Count > 0)
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
            if (Instance.ConfigProjects.Count != Instance.OldConfigProjects.Count)
            {
                return true;
            }

            for (int i = 0; i < Instance.ConfigProjects.Count; i++)
            {
                if (!instance.ConfigProjects[i].HasGeneratedFiles())
                {
                    return true;
                }

                if (Instance.ConfigProjects[i].Groups.Count != Instance.OldConfigProjects[i].Groups.Count)
                {
                    return true;
                }

                for (int j = 0; j < Instance.ConfigProjects[i].Groups.Count; j++)
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

                    for (int k = 0; k < Instance.ConfigProjects[i].Groups[j].Rules.Count; k++)
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

        private static bool IsFileRelevant(string fullPath)
        {
            foreach (ConfigProject configProject in Instance.ConfigProjects)
            {
                foreach (ConfigGroup configGroup in configProject.Groups)
                {
                    bool matchesPath = false;
                    foreach (ConfigPath configPath in configGroup.Paths)
                    {
                        if (GetCommonPathPrefix(configPath.Path, fullPath).Length > 0)
                        {
                            matchesPath = true;
                            break;
                        }
                    }

                    if (matchesPath)
                    {
                        bool matchesIncludes = false;
                        bool matchesExcludes = false;

                        foreach (ConfigRule configRule in configGroup.Rules)
                        {
                            var match = Regex.Match(fullPath, configRule.Rule);
                            if (match.Success)
                            {
                                if (!configRule.IsExclude)
                                {
                                    matchesIncludes = true;
                                }
                                else
                                {
                                    matchesExcludes = true;
                                }
                            }
                        }

                        if (matchesIncludes && !matchesExcludes)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private class DelayedEventsHandler
        {
            private Timer timer;
            private object lockObject = new object();

            public List<RenamedEventArgs> renamedEvents = new List<RenamedEventArgs>();
            public List<FileSystemEventArgs> fileSystemEvents = new List<FileSystemEventArgs>();

            public DelayedEventsHandler()
            {
                timer = new Timer(500);
                timer.AutoReset = false;
                timer.Elapsed += TimerElapsed;
            }

            public void Reset()
            {
                timer.Stop();
                timer.Start();
            }

            public void AddRenameEvent(RenamedEventArgs renamedEventArgs)
            {
                lock (lockObject)
                {
                    renamedEvents.Add(renamedEventArgs);
                    Reset();
                }
            }

            public void AddFileSystemEvent(FileSystemEventArgs renamedEventArgs)
            {
                lock (lockObject)
                {
                    fileSystemEvents.Add(renamedEventArgs);
                    Reset();
                }
            }

            private void TimerElapsed(object sender, ElapsedEventArgs e)
            {
                lock (lockObject)
                {
                    timer.Stop();

                    HashSet<string> changedFiles = new HashSet<string>();

                    foreach (FileSystemEventArgs fileSystemEvent in fileSystemEvents)
                    {
                        if (!IsFileRelevant(fileSystemEvent.FullPath))
                        {
                            continue;
                        }

                        if (fileSystemEvent.ChangeType == WatcherChangeTypes.Created ||
                            fileSystemEvent.ChangeType == WatcherChangeTypes.Deleted)
                        {
                            Cleanup();
                            Instance.FilesAddedOrRemoved?.Invoke();
                            return;
                        }
                        else if (fileSystemEvent.ChangeType == WatcherChangeTypes.Changed)
                        {
                            changedFiles.Add(fileSystemEvent.FullPath);
                        }
                    }

                    HashSet<string> oldFiles = new HashSet<string>();
                    HashSet<string> newFiles = new HashSet<string>();

                    foreach (RenamedEventArgs renamedEventArgs in renamedEvents)
                    {
                        oldFiles.Add(renamedEventArgs.OldFullPath);
                        newFiles.Add(renamedEventArgs.FullPath);
                    }

                    foreach (string newFile in newFiles)
                    {
                        if (!IsFileRelevant(newFile))
                        {
                            continue;
                        }

                        if (oldFiles.Contains(newFile))
                        {
                            changedFiles.Add(newFile);
                        }
                        else
                        {
                            Cleanup();
                            Instance.FilesAddedOrRemoved?.Invoke();
                            return;
                        }
                    }

                    Cleanup();

                    if (changedFiles.Count > 0)
                    {
                        Instance.FilesChanged?.Invoke(changedFiles.ToList());
                    }
                }
            }

            private void Cleanup()
            {
                renamedEvents.Clear();
                fileSystemEvents.Clear();
            }
        }

        DelayedEventsHandler delayedEventsHandler = new DelayedEventsHandler();

        private static void OnChanged(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType != WatcherChangeTypes.Changed)
            {
                return;
            }

            Instance.delayedEventsHandler.AddFileSystemEvent(e);
        }

        private static void OnCreated(object sender, FileSystemEventArgs e)
        {
            Instance.delayedEventsHandler.AddFileSystemEvent(e);
        }

        private static void OnDeleted(object sender, FileSystemEventArgs e)
        {
            Instance.delayedEventsHandler.AddFileSystemEvent(e);
        }

        private static void OnRenamed(object sender, RenamedEventArgs e)
        {
            Instance.delayedEventsHandler.AddRenameEvent(e);
        }
    }
}
