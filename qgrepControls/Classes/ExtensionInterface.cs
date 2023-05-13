using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace qgrepControls.Classes
{
    public delegate void FolderCallback(string folder);

    public interface IExtensionInterface
    {
        string GetSolutionPath();
        void GatherAllFoldersAndExtensionsFromSolution(HashSet<string> extensionsList, FolderCallback folderCallback);
        void OpenFile(string path, string line);
        string GetSelectedText();
        System.Drawing.Color GetColor(string resourceKey);
        void RefreshResources(Dictionary<string, object> newResources);
        bool WindowOpened { get; set; }
        bool IsStandalone { get; }
        Dictionary<string, Hotkey> ReadKeyBindings();
        void ApplyKeyBindings(Dictionary<string, Hotkey> bindings);
        void SaveKeyBindings(Dictionary<string, Hotkey> bindings);
    }
}
