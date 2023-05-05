using EnvDTE;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using qgrepControls.Classes;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace qgrepSearch
{
    public class qgrepExtensionWindow : IExtensionWindow
    {
        private DialogWindow dialogWindow;
        public qgrepExtensionWindow(DialogWindow dialogWindow)
        {
            this.dialogWindow = dialogWindow;
        }

        public void ShowModal()
        {
            dialogWindow.ShowModal();
        }

        public void Close()
        {
            dialogWindow.Close();
        }
    }

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

        public IExtensionWindow CreateWindow(UserControl userControl, string title)
        {
            return new qgrepExtensionWindow(
                new DialogWindow
                {
                    Title = title,
                    Content = userControl,
                    SizeToContent = SizeToContent.WidthAndHeight,
                    ResizeMode = ResizeMode.NoResize,
                    HasMinimizeButton = false,
                    HasMaximizeButton = false,
                }
            );
        }

        public List<string> GatherAllFoldersFromSolution()
        {
            EnvDTE80.DTE2 dte = State?.DTE;
            Solution solution = dte?.Solution;
            List<string> folderList = new List<string>();

            foreach (Project project in solution?.Projects)
            {
                GetAllFoldersFromProject(project?.ProjectItems, folderList);
            }

            return folderList;
        }

        private static void GetAllFoldersFromProject(ProjectItems projectItems, List<string> folderList)
        {
            if (projectItems != null)
            {
                foreach (ProjectItem item in projectItems)
                {
                    if (item?.SubProject != null)
                    {
                        GetAllFoldersFromProject(item.SubProject.ProjectItems, folderList);
                    }
                    else if (item.FileCount > 0)
                    {
                        for (short i = 1; i <= item.FileCount; i++)
                        {
                            string filePath = item.FileNames[i];
                            if (File.Exists(filePath))
                            {
                                string directoryPath = System.IO.Path.GetDirectoryName(filePath);
                                if (!folderList.Contains(directoryPath))
                                {
                                    folderList.Add(directoryPath);
                                }
                            }
                        }
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
    }
}
