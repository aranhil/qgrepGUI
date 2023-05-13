using EnvDTE;
<<<<<<< Updated upstream
=======
using EnvDTE80;
using Microsoft.Build.Evaluation;
>>>>>>> Stashed changes
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using qgrepControls;
using qgrepControls.Classes;
using qgrepControls.SearchWindow;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace qgrepSearch
{
    //public class qgrepExtensionWindow : IExtensionWindow
    //{
    //    private MainWindow dialogWindow;
    //    public qgrepExtensionWindow(MainWindow dialogWindow)
    //    {
    //        this.dialogWindow = dialogWindow;
    //    }

    //    public void ShowModal()
    //    {
    //        dialogWindow.ShowDialog();
    //    }

    //    public void Close()
    //    {
    //        dialogWindow.Close();
    //    }

    //    public void Show()
    //    {
    //        dialogWindow.Show();
    //    }
    //}

    public class qgrepExtension : IExtensionInterface
    {
        private qgrepSearchWindowState State;

        public qgrepExtension(qgrepSearchWindowState windowState)
        {
            State = windowState;
        }

        public bool WindowOpened
        {
            get
            {
                return State.Package.WindowOpened;
            }
            set
            {
                State.Package.WindowOpened = value;
            }
        }

        bool IExtensionInterface.IsStandalone
        {
            get
            {
                return false;
            }
        }

        public string GetSelectedText()
        {
            return (State.DTE?.ActiveDocument?.Selection as EnvDTE.TextSelection)?.Text ?? "";
        }

        public string GetSolutionPath()
        {
            return State.DTE?.Solution?.FullName ?? "";
        }

        public void OpenFile(string path, string line)
        {
            try
            {
                State.DTE?.ItemOperations?.OpenFile(path);

                if (line != "0")
                {
                    (State.DTE?.ActiveDocument?.Selection as EnvDTE.TextSelection)?.MoveToLineAndOffset(Int32.Parse(line), 1);
                }
            }
            catch { }
        }
<<<<<<< Updated upstream

        public IExtensionWindow CreateWindow(UserControl userControl, string title, UserControl owner)
        {
            return null;

            //return new qgrepExtensionWindow(
            //    new MainWindow
            //    {
            //        Title = title,
            //        Content = userControl,
            //        SizeToContent = SizeToContent.WidthAndHeight,
            //        ResizeMode = ResizeMode.NoResize,
            //        //HasMinimizeButton = false,
            //        //HasMaximizeButton = false,
            //        //Owner = qgrepSearchWindowControl.FindAncestor<DialogWindow>(owner),
            //        //WindowStartupLocation = WindowStartupLocation.CenterOwner
            //    }
            //);
        }

        public List<string> GatherAllFoldersFromSolution()
=======
        public void GatherAllFoldersAndExtensionsFromSolution(HashSet<string> extensionsList, FolderCallback folderCallback)
>>>>>>> Stashed changes
        {
            try
            {
                EnvDTE80.DTE2 dte = State?.DTE;
                Solution solution = dte?.Solution;

                foreach (EnvDTE.Project project in solution?.Projects)
                {
                    ProcessProject(project, extensionsList, folderCallback);

                    GetAllFoldersFromProject(project?.ProjectItems, extensionsList, folderCallback);
                }
            }
            catch { }
        }

        private static void ProcessProject(EnvDTE.Project project, HashSet<string> extensionsList, FolderCallback folderCallback)
        {
            if (project == null || project.FullName.Length == 0) return;

            var msbuildProject = new Microsoft.Build.Evaluation.Project(project.FullName);
            string projectDirectory = Path.GetDirectoryName(project.FullName);

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
                    extension.Equals(".csproj", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (File.Exists(fullPath))
                {
                    string directoryPath = Path.GetDirectoryName(fullPath);
                    folderCallback(directoryPath);

                    if (!string.IsNullOrEmpty(extension) && !extensionsList.Contains(extension))
                    {
                        extensionsList.Add(extension);
                    }
                }
            }

            msbuildProject.ProjectCollection.UnloadAllProjects();
        }

        private static void GetAllFoldersFromProject(ProjectItems projectItems, HashSet<string> extensionsList, FolderCallback folderCallback)
        {
            if (projectItems != null)
            {
                foreach (EnvDTE.ProjectItem item in projectItems)
                {
                    if (item?.SubProject != null)
                    {
                        ProcessProject(item.SubProject, extensionsList, folderCallback);

                        GetAllFoldersFromProject(item.SubProject.ProjectItems, extensionsList, folderCallback);
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
            try
            {
                string className = resourceKey.Substring(0, resourceKey.IndexOf('.'));
                string propertyName = resourceKey.Substring(resourceKey.IndexOf('.') + 1);

                if (className == "FontsAndColors")
                {
                    string subPropertyName = propertyName.Substring(propertyName.IndexOf('.') + 1);
                    propertyName = propertyName.Substring(0, propertyName.IndexOf('.'));

                    var properties = State.DTE.Properties["FontsAndColors", "TextEditor"];
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
    }
}
