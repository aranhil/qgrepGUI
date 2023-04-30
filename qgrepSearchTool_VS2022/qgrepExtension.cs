using EnvDTE;
using Microsoft.VisualStudio.PlatformUI;
using qgrepControls.Classes;
using System;
using System.Collections.Generic;
using System.IO;
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
            State.DTE?.ItemOperations?.OpenFile(path);
            (State.DTE?.ActiveDocument?.Selection as EnvDTE.TextSelection)?.MoveToLineAndOffset(Int32.Parse(line), 1);
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
            DTE dte = State?.DTE;
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
    }
}
