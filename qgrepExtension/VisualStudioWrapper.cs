using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TaskStatusCenter;
using qgrepControls.Classes;
using qgrepControls.ModelViews;
using qgrepControls.Properties;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Xml.Linq;
using Xceed.Wpf.Toolkit.Core.Input;

namespace qgrepSearch
{
    public class VisualStudioWrapper : IWrapperApp
    {
        private ExtensionData Data;
        ResourceManager ResourceManager = new ResourceManager("Properties.VSPackage.en-US.resx", Assembly.GetExecutingAssembly());
        public VisualStudioWrapper(ExtensionData windowState)
        {
            Data = windowState;
        }

        public bool SearchWindowOpened
        {
            get
            {
                if (Data.Package.SearchWindowOpened)
                {
                    Data.Package.SearchWindowOpened = false;
                    return true;
                }

                return false;
            }
            set
            {
            }
        }

        bool IWrapperApp.IsStandalone
        {
            get
            {
                return false;
            }
        }

        public string GetSelectedText()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            string currentlySelectedText = (Data.DTE?.ActiveDocument?.Selection as EnvDTE.TextSelection)?.Text ?? "";

            try
            {
                if (currentlySelectedText.Length == 0)
                {
                    EnvDTE.TextDocument textDoc = (EnvDTE.TextDocument)Data.DTE?.ActiveDocument?.Object("TextDocument");
                    EnvDTE.VirtualPoint activePoint = textDoc?.Selection?.ActivePoint;

                    if (activePoint != null)
                    {
                        EnvDTE.EditPoint startPoint = activePoint.CreateEditPoint();
                        EnvDTE.EditPoint endPoint = activePoint.CreateEditPoint();

                        if (startPoint == null || endPoint == null)
                        {
                            return "";
                        }

                        int startLine = startPoint.Line;
                        int endLine = endPoint.Line;

                        int letterCounter = 0;
                        int wordLetterLimit = 256;

                        while (!startPoint.AtStartOfDocument && letterCounter < wordLetterLimit)
                        {
                            startPoint.CharLeft(1);
                            string text = startPoint.GetText(1);

                            if (string.IsNullOrEmpty(text) || !(char.IsLetterOrDigit(text[0]) || text[0] == '_') || startPoint.Line != startLine)
                            {
                                startPoint.CharRight(1);
                                break;
                            }

                            letterCounter++;
                        }

                        string textEnd = endPoint.GetText(1);
                        while (!string.IsNullOrEmpty(textEnd) && (char.IsLetterOrDigit(textEnd[0]) || textEnd[0] == '_') && letterCounter < wordLetterLimit)
                        {
                            if (!endPoint.AtEndOfDocument)
                            {
                                endPoint.CharRight(1);
                                textEnd = endPoint.GetText(1);
                            }
                            else
                            {
                                break;
                            }

                            if (endPoint.Line != endLine)
                            {
                                endPoint.CharLeft(1);
                                break;
                            }

                            letterCounter++;
                        }

                        string word = startPoint.GetText(endPoint);

                        if (word.Any(x => char.IsLetterOrDigit(x)))
                        {
                            return word;
                        }
                        else
                        {
                            return "";
                        }
                    }
                }
            }
            catch { }

            return currentlySelectedText.Replace("\n", "").Replace("\r", "");
        }

        public string GetConfigPath(bool useGlobalPath)
        {
            try
            {
                if (useGlobalPath)
                {
                    string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                    string appFolderPath = Path.Combine(appDataPath, "qgrepSearch");

                    if (!Directory.Exists(appFolderPath))
                    {
                        Directory.CreateDirectory(appFolderPath);
                    }

                    return appFolderPath;
                }
                else
                {
                    return System.IO.Path.GetDirectoryName(Data.DTE?.Solution?.FullName ?? "");
                }
            }
            catch { }

            return "";
        }

        public void OpenFile(string path, string line)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                Data.DTE?.ItemOperations?.OpenFile(path);

                if (line != "0")
                {
                    (Data.DTE?.ActiveDocument?.Selection as EnvDTE.TextSelection)?.MoveToLineAndOffset(Int32.Parse(line), 1);
                }
            }
            catch { }
        }
        public static string GetRelativePath(string fromPath, string toPath)
        {
            if (string.IsNullOrEmpty(fromPath))
            {
                throw new ArgumentNullException("fromPath");
            }

            if (string.IsNullOrEmpty(toPath))
            {
                throw new ArgumentNullException("toPath");
            }

            Uri fromUri = new Uri(AppendDirectorySeparatorChar(fromPath));
            Uri toUri = new Uri(AppendDirectorySeparatorChar(toPath));

            if (fromUri.Scheme != toUri.Scheme)
            {
                return toPath;
            }

            Uri relativeUri = fromUri.MakeRelativeUri(toUri);
            string relativePath = Uri.UnescapeDataString(relativeUri.ToString());

            if (string.Equals(toUri.Scheme, Uri.UriSchemeFile, StringComparison.OrdinalIgnoreCase))
            {
                relativePath = relativePath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            }

            return relativePath;
        }

        private static string AppendDirectorySeparatorChar(string path)
        {
            // Append a slash only if the path is a directory and does not have a slash.
            if (!Path.HasExtension(path) &&
                !path.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                return path + Path.DirectorySeparatorChar;
            }

            return path;
        }

        public void IncludeFile(string path)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                EnvDTE.Document activeDocument = Data.DTE.ActiveDocument;

                string activeDocumentDirectory = Path.GetDirectoryName(activeDocument.FullName);
                string relativePath = "\"" + GetRelativePath(activeDocumentDirectory, path) + "\"";

                TextDocument textDocument = (TextDocument)activeDocument.Object("TextDocument");
                TextSelection selection = textDocument.Selection;
                selection.StartOfDocument();

                string lastInclude = null;
                int currentBlockStart = -1;
                int currentBlockEnd = -1;
                int currentBlockSize = 0;
                int largestBlockStart = -1;
                int largestBlockEnd = -1;
                int largestBlockSize = 0;
                int lastIncludeLine = -1;

                bool containsForwardSlashes = false;
                bool containsBackslashes = false;

                for (int i = 1; i <= textDocument.EndPoint.Line; i++)
                {
                    string line = textDocument.CreateEditPoint(textDocument.StartPoint).GetLines(i, i + 1).Trim();

                    bool isRelevantInclude = false;
                    string includeFile = "";

                    if (line.StartsWith("#include"))
                    {
                        lastIncludeLine = i;

                        includeFile = line.Substring(8).Trim(new char[] { ' ', '\t' });
                        if (!includeFile.StartsWith("<"))
                        {
                            isRelevantInclude = true;
                        }
                    }

                    if (isRelevantInclude)
                    {
                        containsForwardSlashes |= includeFile.Contains("/");
                        containsBackslashes |= includeFile.Contains("\\");

                        if (currentBlockStart == -1)
                        {
                            currentBlockStart = i;
                            currentBlockEnd = i;
                            currentBlockSize = 1;
                            lastInclude = includeFile;
                        }
                        else if (includeFile.CompareTo(lastInclude) >= 0)
                        {
                            currentBlockEnd = i;
                            currentBlockSize++;
                            lastInclude = includeFile;
                        }
                        else
                        {
                            if (largestBlockStart == -1 || currentBlockSize > largestBlockSize)
                            {
                                largestBlockStart = currentBlockStart;
                                largestBlockEnd = currentBlockEnd;
                                largestBlockSize = currentBlockSize;
                            }

                            currentBlockStart = i;
                            currentBlockEnd = i;
                            currentBlockSize = 1;
                            lastInclude = includeFile;
                        }
                    }
                    else if (!string.IsNullOrWhiteSpace(line))
                    {
                        if (currentBlockStart != -1)
                        {
                            if (largestBlockStart == -1 || currentBlockSize > largestBlockSize)
                            {
                                largestBlockStart = currentBlockStart;
                                largestBlockEnd = currentBlockEnd;
                                largestBlockSize = currentBlockSize;
                            }

                            currentBlockStart = -1;
                            currentBlockEnd = -1;
                            currentBlockSize = 0;
                            lastInclude = null;
                        }
                    }
                }

                if (currentBlockStart != -1 && (largestBlockStart == -1 || (currentBlockEnd - currentBlockStart) > (largestBlockEnd - largestBlockStart)))
                {
                    largestBlockStart = currentBlockStart;
                    largestBlockEnd = currentBlockEnd;
                    largestBlockSize = currentBlockSize;
                }

                if (!containsBackslashes)
                {
                    relativePath = relativePath.Replace('\\', '/');
                }

                if (largestBlockStart != -1 && largestBlockSize >= 3)
                {
                    int insertLine = largestBlockStart;

                    for (int i = largestBlockStart; i <= largestBlockEnd; i++)
                    {
                        string line = textDocument.CreateEditPoint(textDocument.StartPoint).GetLines(i, i + 1).Trim();
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            string includeFile = line.Substring(8).Trim(new char[] { ' ', '\t' });

                            if (includeFile.CompareTo(relativePath) > 0)
                            {
                                break;
                            }
                        }

                        insertLine++;
                    }

                    selection.MoveToLineAndOffset(insertLine, 1);
                    selection.Insert($"#include {relativePath}\n");
                }
                else if (lastIncludeLine >= 0)
                {
                    selection.MoveToLineAndOffset(lastIncludeLine + 1, 1);
                    selection.Insert($"#include {relativePath}\n");
                }
            }
            catch { }
        }
        public void GatherAllFoldersAndExtensionsFromSolution(MessageCallback extensionCallback, MessageCallback folderCallback)
        {
            try
            {
                EnvDTE80.DTE2 dte = Data?.DTE;
                Solution solution = dte?.Solution;

                foreach (EnvDTE.Project project in solution?.Projects)
                {
                    ProcessProject(project, extensionCallback, folderCallback);

                    GetAllFoldersFromProject(project?.ProjectItems, extensionCallback, folderCallback);
                }
            }
            catch { }
        }

        private static void ProcessProject(EnvDTE.Project project, MessageCallback extensionCallback, MessageCallback folderCallback)
        {
            string projectFullName = null;
            ThreadHelper.JoinableTaskFactory.Run(async delegate
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                projectFullName = project?.FullName;
            });

            if (project == null || string.IsNullOrEmpty(projectFullName)) return;

            var msbuildProject = new Microsoft.Build.Evaluation.Project(projectFullName);
            string projectDirectory = Path.GetDirectoryName(projectFullName);

            foreach (var projectItem in msbuildProject.Items)
            {
                if (projectItem.IsImported)
                {
                    continue;
                }

                string relativePath = projectItem.EvaluatedInclude;
                string fullPath = "";

                try
                {
                    fullPath = Path.Combine(projectDirectory, relativePath);
                }
                catch { }

                string extension = Path.GetExtension(fullPath);

                if (extension.Equals(".vcproj", StringComparison.OrdinalIgnoreCase) ||
                    extension.Equals(".vcxproj", StringComparison.OrdinalIgnoreCase) ||
                    extension.Equals(".csproj", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (File.Exists(fullPath))
                {
                    string directoryPath = Path.GetDirectoryName(fullPath);
                    folderCallback(directoryPath);

                    if (!string.IsNullOrEmpty(extension))
                    {
                        extensionCallback(extension);
                    }
                }
            }

            msbuildProject.ProjectCollection.UnloadAllProjects();
        }

        private static void GetAllFoldersFromProject(ProjectItems projectItems, MessageCallback extensionCallback, MessageCallback folderCallback)
        {
            if (projectItems != null)
            {
                foreach (EnvDTE.ProjectItem item in projectItems)
                {
                    EnvDTE.Project subProject = null;
                    EnvDTE.ProjectItems subProjectItems = null;

                    ThreadHelper.JoinableTaskFactory.Run(async delegate
                    {
                        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                        subProject = item?.SubProject;
                        subProjectItems = subProject?.ProjectItems;
                    });

                    if (subProject != null)
                    {
                        ProcessProject(subProject, extensionCallback, folderCallback);
                    }

                    if (subProjectItems != null)
                    {
                        GetAllFoldersFromProject(subProjectItems, extensionCallback, folderCallback);
                    }
                }
            }
        }
        class FontsAndColorsItem
        {
            public string Name { get; set; }
            public string Description { get; set; }
            public uint Background { get; set; }
            public uint Foreground { get; set; }
        }

        public Color GetColor(string resourceKey)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                string className = resourceKey.Substring(0, resourceKey.IndexOf('.'));
                string propertyName = resourceKey.Substring(resourceKey.IndexOf('.') + 1);

                if (className == "FontsAndColors")
                {
                    string subPropertyName = propertyName.Substring(propertyName.IndexOf('.') + 1);
                    propertyName = propertyName.Substring(0, propertyName.IndexOf('.'));

                    var properties = Data.DTE.Properties["FontsAndColors", "TextEditor"];
                    var fontsAndColorsItems = (FontsAndColorsItems)properties.Item("FontsAndColorsItems").Object;
                    var desiredItem = fontsAndColorsItems.Item(propertyName);

                    if (desiredItem != null)
                    {
                        if (subPropertyName == "Background")
                        {
                            return ColorTranslator.FromOle((int)desiredItem.Background);
                        }
                        else
                        {
                            return ColorTranslator.FromOle((int)desiredItem.Foreground);
                        }
                    }

                    return new Color();
                }
                else
                {
                    var type = FindType("Microsoft.VisualStudio.PlatformUI." + className + "Colors");
                    var property = type.GetProperty(propertyName + "ColorKey");
                    var themedResourceKey = property.GetValue(null);
                    return VSColorTheme.GetThemedColor(themedResourceKey as ThemeResourceKey);
                }
            }
            catch (Exception)
            {
                return new Color();
            }
        }

        private static Type FindType(string qualifiedTypeName)
        {
            Type t = Type.GetType(qualifiedTypeName);

            if (t != null)
            {
                return t;
            }
            else
            {
                foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    t = asm.GetType(qualifiedTypeName);
                    if (t != null)
                        return t;
                }
                return null;
            }
        }

        public void RefreshResources(Dictionary<string, object> newResources)
        {
        }

        private string GetBinding(string binding)
        {
            int position = binding.IndexOf("::");

            if (position >= 0)
            {
                string scope = binding.Substring(0, position);
                return binding.Substring(position + 2);
            }

            return "";
        }

        private string GetBindingAndScope(string binding, out string scope)
        {
            scope = null;
            int position = binding.IndexOf("::");

            if (position >= 0)
            {
                scope = binding.Substring(0, position);
                return binding.Substring(position + 2);
            }

            return "";
        }

        private string GetBindingForCommand(string commandString, List<Command> commands)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                Command command = commands.Find(x =>
                {
                    ThreadHelper.ThrowIfNotOnUIThread();
                    return x.Name.Contains(commandString);
                });

                if (command != null && command.Bindings is object[] bindings && bindings.Length > 0 && bindings[0] is string binding)
                {
                    return GetBinding(binding);
                }
            }
            catch { }

            return "";
        }

        private string GetGlobalBinding(string binding)
        {
            int position = binding.IndexOf("::");

            if (position >= 0)
            {
                string scope = binding.Substring(0, position);
                if(scope == Resources.Global)
                {
                    return binding.Substring(position + 2);
                }
            }

            return "";
        }

        private bool GetBinding(string binding, ref Hotkey hotkey)
        {
            int position = binding.IndexOf("::");

            if (position >= 0)
            {
                string result = binding.Substring(position + 2);
                string scope = binding.Substring(0, position);
                bool isGlobal = (scope == Resources.Global);

                try
                {
                    hotkey = Hotkey.FromString(result);
                    hotkey.Scope = scope;
                    hotkey.IsGlobal = isGlobal;
                }
                catch
                {
                    return false;
                }

                return true;
            }

            return false;
        }

        private bool GetBindingForCommand(string commandString, List<Command> commands, ref Hotkey hotkey)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                Command command = commands.Find(x =>
                {
                    ThreadHelper.ThrowIfNotOnUIThread();
                    return x.Name.Contains(commandString);
                });

                if (command != null && command.Bindings is object[] bindings)
                {
                    if(bindings.Length > 0 && bindings[0] is string binding)
                    {
                        GetBinding(binding, ref hotkey);
                    }
                    else
                    {
                        hotkey = new Hotkey();
                    }
                    return true;
                }
            }
            catch { }

            return false;
        }

        private List<Hotkey> GetBindingsForCommand(string commandString)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            List<Hotkey> hotkeys = new List<Hotkey>();

            try
            {
                EnvDTE.Command command = Data?.DTE?.Commands?.Item(commandString);
                if (command != null && command.Bindings is object[] bindings)
                {
                    foreach (var binding in bindings)
                    {
                        if (binding is string bindingString)
                        {
                            Hotkey hotkey = null;
                            if (GetBinding(bindingString, ref hotkey))
                            {
                                hotkeys.Add(hotkey);
                            }
                        }
                    }
                }
            }
            catch { }

            return hotkeys;
        }

        private List<string> GetCommandStrings()
        {
            return new List<string>
            {
                "ToggleCaseSensitive",
                "ToggleWholeWord",
                "ToggleRegEx",
                "ToggleIncludeFiles",
                "ToggleExcludeFiles",
                "ToggleFilterResults",
                "ShowHistory",
                "ToggleGroupBy",
                "ToggleGroupExpand",
                "ToggleSearchFilter1",
                "ToggleSearchFilter2",
                "ToggleSearchFilter3",
                "ToggleSearchFilter4",
                "ToggleSearchFilter5",
                "ToggleSearchFilter6",
                "ToggleSearchFilter7",
                "ToggleSearchFilter8",
                "ToggleSearchFilter9",
                "SelectSearchFilter1",
                "SelectSearchFilter2",
                "SelectSearchFilter3",
                "SelectSearchFilter4",
                "SelectSearchFilter5",
                "SelectSearchFilter6",
                "SelectSearchFilter7",
                "SelectSearchFilter8",
                "SelectSearchFilter9",
                "View.qgrepSearchFile",
                "View.qgrepSearchTool",
            };
        }

        public Dictionary<string, Hotkey> ReadKeyBindings()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            List<Command> commands = new List<Command>();

            foreach (EnvDTE.Command command in Data?.DTE?.Commands)
            {
                if (!string.IsNullOrEmpty(command.Name) && 
                    (command.Guid.ToLower() == "{d480acd1-c9b7-45da-a687-4cacc45acf16}" ||
                    command.Guid.ToLower() == "{9cc1062b-4c82-46d2-adcb-f5c17d55fb85}"))
                {
                    commands.Add(command);
                }
                if (!string.IsNullOrEmpty(command.Name))
                {
                    System.Diagnostics.Debug.WriteLine(command.Name);
                }
            }

            Dictionary<string, Hotkey> bindings = new Dictionary<string, Hotkey>();

            List<string> commandStrings = GetCommandStrings();
            foreach (string commandString in commandStrings)
            {
                Hotkey hotkey = null;
                if(GetBindingForCommand(commandString, commands, ref hotkey))
                {
                    bindings[commandString] = hotkey;
                }
                else
                {
                    return null;
                }
            }

            return bindings;
        }

        public Dictionary<string, string> ReadKeyBindingsReadOnly()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            List<Command> commands = new List<Command>();

            foreach (EnvDTE.Command command in Data?.DTE?.Commands)
            {
                if (!string.IsNullOrEmpty(command.Name) &&
                    (command.Guid.ToLower() == "{d480acd1-c9b7-45da-a687-4cacc45acf16}" ||
                    command.Guid.ToLower() == "{9cc1062b-4c82-46d2-adcb-f5c17d55fb85}"))
                {
                    commands.Add(command);
                }
            }

            Dictionary<string, string> bindings = new Dictionary<string, string>();

            List<string> commandStrings = GetCommandStrings();
            foreach (string commandString in commandStrings)
            {
                string binding = GetBindingForCommand(commandString, commands);
                bindings[commandString] = binding;
            }

            return bindings;
        }

        public void SaveKeyBindings(Dictionary<string, Hotkey> bindings)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                List<Tuple<string, string, string>> oldShortcuts = new List<Tuple<string, string, string>>();
                List<Tuple<string, string, bool>> newShortcuts = new List<Tuple<string, string, bool>>();

                foreach (KeyValuePair<string, Hotkey> binding in bindings)
                {
                    List<Hotkey> hotkeys = GetBindingsForCommand(binding.Key);

                    if(hotkeys.Count == 1 && 
                        hotkeys[0].ToUnlocalizedString() == binding.Value.ToUnlocalizedString() && 
                        hotkeys[0].IsGlobal == binding.Value.IsGlobal)
                    {
                        continue;
                    }

                    foreach (Hotkey hotkey in hotkeys)
                    {
                        oldShortcuts.Add(new Tuple<string, string, string>(binding.Key, hotkey.ToUnlocalizedString(), hotkey.Scope));
                    }

                    if(binding.Value.ToUnlocalizedString().Length > 0)
                    {
                        newShortcuts.Add(new Tuple<string, string, bool>(binding.Key, binding.Value.ToUnlocalizedString(), binding.Value.IsGlobal));
                    }
                }

                if(oldShortcuts.Count > 0 || newShortcuts.Count > 0)
                {
                    var doc = new XDocument(
                        new XElement("UserSettings",
                            new XElement("ApplicationIdentity", new XAttribute("version", Data?.DTE?.Version)),
                            new XElement("ToolsOptions",
                                new XElement("ToolsOptionsCategory", new XAttribute("name", "Environment"), new XAttribute("RegisteredName", "Environment"))
                            ),
                            new XElement("Category", new XAttribute("name", "Environment_Group"), new XAttribute("RegisteredName", "Environment_Group"),
                                new XElement("Category", new XAttribute("name", "Environment_KeyBindings"), 
                                                         new XAttribute("Category", "{F09035F1-80D2-4312-8EC4-4D354A4BCB4C}"), 
                                                         new XAttribute("Package", "{DA9FB551-C724-11d0-AE1F-00A0C90FFFC3}"), 
                                                         new XAttribute("RegisteredName", "Environment_KeyBindings"), 
                                                         new XAttribute("PackageName", "Visual Studio Environment Package"),
                                    new XElement("Version", $"{Data?.DTE?.Version}.0.0.0"),
                                    new XElement("KeyboardShortcuts",
                                        new XElement("ShortcutsScheme"),
                                        new XElement("UserShortcuts",
                                            oldShortcuts.ConvertAll(x => new XElement("RemoveShortcut", new XAttribute("Command", x.Item1), 
                                                new XAttribute("Scope", x.Item3), x.Item2)).ToArray(),
                                            newShortcuts.ConvertAll(x => new XElement("Shortcut", new XAttribute("Command", x.Item1), 
                                                new XAttribute("Scope", x.Item3 ? "{5EFC7975-14BC-11CF-9B2B-00AA00573819}" : "{E4E2BA26-A455-4C53-ADB3-8225FB696F8B}"), x.Item2)).ToArray()
                                        )
                                    )
                                )
                            )
                        )
                    );

                    string settingsPath = Path.Combine(GetConfigPath(false), ".qgrep", "qgrep-shortcuts.vssettings");

                    doc.Save(settingsPath);
                    Data?.DTE?.ExecuteCommand("Tools.ImportandExportSettings", $"/import:\"{settingsPath}\"");
                    File.Delete(settingsPath);
                }
            }
            catch { }
        }

        public List<string> GetConflictingCommandsForBinding(Dictionary<string, Hotkey> bindings)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            List<string> results = new List<string>();
            List<string> cachedHotkeys = bindings.Where(x => x.Value.IsGlobal).Select(x => x.Value.ToString()).ToList();

            try
            {
                foreach (EnvDTE.Command command in Data?.DTE?.Commands)
                {
                    if (!string.IsNullOrEmpty(command.Name) && command.Bindings is object[] commandBindings &&
                        (command.Guid.ToLower() != "{d480acd1-c9b7-45da-a687-4cacc45acf16}" &&
                        command.Guid.ToLower() != "{9cc1062b-4c82-46d2-adcb-f5c17d55fb85}"))
                    {
                        foreach (var binding in commandBindings)
                        {
                            if (binding is string bindingString)
                            {
                                string commandHotkey = GetBindingAndScope(bindingString, out string scope);
                                if(commandHotkey.Length > 0)
                                {
                                    System.Diagnostics.Debug.WriteLine(commandHotkey);

                                    if (cachedHotkeys.Contains(commandHotkey))
                                    {
                                        string conflictMessage = string.Format(Resources.CommandAndShortcut, command.LocalizedName, commandHotkey, scope);

                                        if (scope == Resources.Global)
                                        {
                                            results.Insert(0 , conflictMessage);
                                        }
                                        else
                                        {
                                            results.Add(conflictMessage);
                                        }
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch { }

            return results;
        }

        public bool CanEditKeyBindings()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                foreach (EnvDTE.Command command in Data?.DTE?.Commands)
                {
                    if (!string.IsNullOrEmpty(command.Name) && command.Bindings is object[] commandBindings &&
                        (command.Guid.ToLower() != "{d480acd1-c9b7-45da-a687-4cacc45acf16}" &&
                        command.Guid.ToLower() != "{9cc1062b-4c82-46d2-adcb-f5c17d55fb85}"))
                    {
                        foreach (var binding in commandBindings)
                        {
                            if (binding is string bindingString)
                            {
                                string commandHotkey = GetGlobalBinding(bindingString);
                                if (commandHotkey.Length > 0)
                                {
                                    try
                                    {
                                        int commaPos = commandHotkey.IndexOf(", ");
                                        if (commaPos >= 0)
                                        {
                                            commandHotkey = commandHotkey.Substring(0, commaPos);
                                        }

                                        Hotkey.FromString(commandHotkey);
                                    }
                                    catch
                                    {
                                        return false;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch { }

            return true;
        }

        public void ApplyKeyBindings()
        {
        }

        public System.Windows.Window GetMainWindow()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var mainWinHandle = (IntPtr)Data.DTE.MainWindow.HWnd;
            var mainWinSource = HwndSource.FromHwnd(mainWinHandle);
            return (System.Windows.Window)mainWinSource.RootVisual;
        }

        public string GetMonospaceFont()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                var properties = Data.DTE.Properties["FontsAndColors", "TextEditor"];
                var fontFamily = (string)properties.Item("FontFamily").Value;

                if (fontFamily != null)
                {
                    return fontFamily;
                }
            }
            catch { }

            return "Consolas";
        }

        public string GetNormalFont()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            IVsFontAndColorStorage Storage = Data.Package.GetService<IVsFontAndColorStorage, IVsFontAndColorStorage>();
            if (Storage != null)
            {
                var Guid = new Guid("1F987C00-E7C4-4869-8A17-23FD602268B0"); // GUID for Environment Font  
                if (Storage.OpenCategory(ref Guid, (uint)(__FCSTORAGEFLAGS.FCSF_READONLY | __FCSTORAGEFLAGS.FCSF_LOADDEFAULTS)) == 0)
                {
                    LOGFONTW[] Fnt = new LOGFONTW[] { new LOGFONTW() };
                    FontInfo[] Info = new FontInfo[] { new FontInfo() };

                    try
                    {
                        Storage.GetFont(Fnt, Info);
                        byte[] byteArray = new byte[Fnt[0].lfFaceName.Length * 2];
                        Buffer.BlockCopy(Fnt[0].lfFaceName, 0, byteArray, 0, byteArray.Length);
                        return System.Text.Encoding.Unicode.GetString(byteArray);
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show(e.ToString());
                    }
                }
            }

            return "Arial";
        }

        public Icon ExtractIconFromUIObject(IVsUIObject uiObject)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (uiObject.get_Type(out string type) == VSConstants.S_OK && type == "VsUI.Icon")
            {
                if (uiObject.get_Data(out object data) == VSConstants.S_OK)
                {
                    if (data is Icon icon)
                    {
                        return icon;
                    }
                }
            }

            return null;
        }

        private Dictionary<string, BitmapSource> iconCache = new Dictionary<string, BitmapSource>();

        public void GetIcon(uint background, SearchResult searchResult)
        {
            try
            {
                string fileExtension = Path.GetExtension(searchResult.FullResult);

                if (iconCache.ContainsKey(fileExtension))
                {
                    searchResult.ImageSource = iconCache[fileExtension];
                }
                else
                {
                    TaskRunner.RunOnUIThread(() =>
                    {
                        ThreadHelper.ThrowIfNotOnUIThread();

                        ImageMoniker imageMoniker = Data.Package.ImageService.GetImageMonikerForFile(searchResult.FullResult);

                        var atts = new ImageAttributes
                        {
                            StructSize = Marshal.SizeOf(typeof(ImageAttributes)),
                            Format = (uint)_UIDataFormat.DF_WPF,
                            LogicalHeight = 32,
                            LogicalWidth = 32,
                            Flags = (uint)_ImageAttributesFlags.IAF_RequiredFlags,
                            ImageType = (uint)_UIImageType.IT_Bitmap,
                            Background = background
                        };

                        unchecked
                        {
                            atts.Flags |= (uint)-2147483648;
                        }

                        var obj = Data.Package.ImageService.GetImage(imageMoniker, atts);
                        if (obj == null)
                            return;

                        obj.get_Data(out object data);
                        searchResult.ImageSource = iconCache[fileExtension] = (BitmapSource)data;
                    });
                }
            }
            catch { }
        }

        TaskCompletionSource<bool> FakeTask;
        ITaskHandler TaskHandler;
        TaskProgressData TaskProgressData;

        public void StartBackgroundTask(string title)
        {
            var options = default(TaskHandlerOptions);
            options.Title = title;

            //options.DisplayTaskDetails = task =>
            //{
            //    Data.Package.SearchWindowOpened = true;

            //    Microsoft.VisualStudio.Threading.JoinableTask joinableTask = Data.Package.JoinableTaskFactory.RunAsync(async () =>
            //    {
            //        ToolWindowPane window = await Data.Package.ShowToolWindowAsync(
            //            typeof(qgrepSearchWindow),
            //            0,
            //            create: true,
            //            cancellationToken: Data.Package.DisposalToken);
            //    });
            //};

            options.ActionsAfterCompletion = CompletionActions.None;

            TaskProgressData = default;
            TaskProgressData.CanBeCanceled = false;

            TaskHandler = Data.Package.TaskStatusCenterService.PreRegister(options, TaskProgressData);
            FakeTask = new TaskCompletionSource<bool>();
            TaskHandler.RegisterTask(FakeTask.Task);
        }

        public void UpdateBackgroundTaskPercentage(int progress)
        {
            TaskProgressData.PercentComplete = progress;
            TaskHandler?.Progress.Report(TaskProgressData);
        }

        public void UpdateBackgroundTaskMessage(string message)
        {
            TaskProgressData.ProgressText = message;
            TaskHandler?.Progress.Report(TaskProgressData);
        }

        public void StopBackgroundTask()
        {
            FakeTask.SetResult(true);
        }

        public bool LoadConfigAtStartup()
        {
            return Data.Package.SolutionAlreadyLoaded;
        }

        public bool IsActiveDocumentCpp()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                EnvDTE.Document activeDocument = Data?.DTE?.ActiveDocument;
                if(activeDocument != null)
                {
                    return activeDocument.Language == "C/C++";
                }
            }
            catch { }

            return false;
        }

        public void OpenKeyBindingSettings()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            Data?.DTE?.ExecuteCommand("Tools.CustomizeKeyboard");
        }
    }
}
