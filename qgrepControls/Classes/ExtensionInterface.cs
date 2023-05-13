using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace qgrepControls.Classes
{
<<<<<<< Updated upstream
    public interface IExtensionWindow
    {
        void ShowModal();
        void Show();
        void Close();
    }
=======
    public delegate void FolderCallback(string folder);
>>>>>>> Stashed changes

    public interface IExtensionInterface
    {
        string GetSolutionPath();
<<<<<<< Updated upstream
        List<string> GatherAllFoldersFromSolution();
        IExtensionWindow CreateWindow(UserControl userControl, string title, UserControl owner = null);
=======
        void GatherAllFoldersAndExtensionsFromSolution(HashSet<string> extensionsList, FolderCallback folderCallback);
>>>>>>> Stashed changes
        void OpenFile(string path, string line);
        string GetSelectedText();
        System.Drawing.Color GetColor(string resourceKey);
        void RefreshResources(Dictionary<string, object> newResources);
        bool WindowOpened { get; set; }
        bool IsStandalone { get; }
    }
}
