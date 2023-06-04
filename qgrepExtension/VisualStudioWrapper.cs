using EnvDTE;
using EnvDTE80;
using Microsoft.Build.Evaluation;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TaskStatusCenter;
using qgrepControls;
using qgrepControls.Classes;
using qgrepControls.ModelViews;
using qgrepControls.SearchWindow;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media.Imaging;

namespace qgrepSearch
{
    public class VisualStudioWrapper: IWrapperApp
    {
        private ExtensionData Data;

        public VisualStudioWrapper(ExtensionData windowState)
        {
            Data = windowState;
        }

        public bool SearchWindowOpened
        {
            get
            {
                if(Data.Package.SearchWindowOpened)
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

                        if(startPoint == null || endPoint == null)
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
            if(useGlobalPath)
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

                        includeFile = line.Substring(8).Trim(new char[] { ' ', '\t'} );
                        if(!includeFile.StartsWith("<"))
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

                if(!containsBackslashes)
                {
                    relativePath = relativePath.Replace('\\', '/');
                }

                if (largestBlockStart != -1 && largestBlockSize >= 3)
                {
                    int insertLine = largestBlockStart;

                    for (int i = largestBlockStart; i <= largestBlockEnd; i++)
                    {
                        string line = textDocument.CreateEditPoint(textDocument.StartPoint).GetLines(i, i + 1).Trim();
                        string includeFile = line.Substring(8).Trim(new char[] { ' ', '\t' });

                        if (includeFile.CompareTo(relativePath) > 0)
                        {
                            break;
                        }

                        insertLine++;
                    }

                    selection.MoveToLineAndOffset(insertLine, 1);
                    selection.Insert($"#include {relativePath}\n");
                }
                else if(lastIncludeLine >= 0)
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

                    if(subProjectItems != null)
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

        private Hotkey GetBindingForCommand(string commandString)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            EnvDTE.Command command = Data.DTE.Commands.Item("qgrep." + commandString);

            if (command.Bindings is object[] bindings && bindings.Length > 0 && bindings[0] is string binding)
            {
                ModifierKeys modifiers = ModifierKeys.None;
                Key key = Key.None;

                int position = binding.IndexOf("::");

                if (position >= 0)
                {
                    string result = binding.Substring(position + 2);

                    string[] parts = result.Split('+');

                    foreach (string part in parts)
                    {
                        if (Enum.TryParse(part, true, out Key tempKey))
                        {
                            key = tempKey;
                        }
                        else if (Enum.TryParse(part, true, out ModifierKeys tempModifier))
                        {
                            modifiers |= tempModifier;
                        }
                    }

                    return new Hotkey(key, modifiers);
                }
            }

            return new Hotkey(Key.None, ModifierKeys.None);
        }


        public Dictionary<string, Hotkey> ReadKeyBindings()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            Dictionary<string, Hotkey> bindings = new Dictionary<string, Hotkey>();
            bindings["ToggleCaseSensitive"] = GetBindingForCommand("ToggleCaseSensitive");
            bindings["ToggleWholeWord"] = GetBindingForCommand("ToggleWholeWord");
            bindings["ToggleRegEx"] = GetBindingForCommand("ToggleRegEx");
            bindings["ToggleIncludeFiles"] = GetBindingForCommand("ToggleIncludeFiles");
            bindings["ToggleExcludeFiles"] = GetBindingForCommand("ToggleExcludeFiles");
            bindings["ToggleFilterResults"] = GetBindingForCommand("ToggleFilterResults");
            bindings["ShowHistory"] = GetBindingForCommand("ShowHistory");
            bindings["ToggleGroupBy"] = GetBindingForCommand("ToggleGroupBy");
            bindings["ToggleGroupExpand"] = GetBindingForCommand("ToggleGroupExpand");
            return bindings;
        }

        public void SaveKeyBindings(Dictionary<string, Hotkey> bindings)
        {
        }

        public void ApplyKeyBindings(Dictionary<string, Hotkey> bindings)
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

        public void GetIcon(string filePath, uint background, SearchResult searchResult)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                string fileExtension = Path.GetExtension(filePath);

                if (iconCache.ContainsKey(fileExtension))
                {
                    searchResult.ImageSource = iconCache[fileExtension];
                }
                else
                {
                    ImageMoniker imageMoniker = Data.Package.ImageService.GetImageMonikerForFile(filePath);

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
                    searchResult.ImageSource = (BitmapSource)data;
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
            EnvDTE.Document activeDocument = Data.DTE.ActiveDocument;
            return activeDocument.Language == "C/C++";
        }
    }
}
