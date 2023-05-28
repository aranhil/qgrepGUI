using qgrepControls.ModelViews;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace qgrepControls.Classes
{
    public delegate void MessageCallback(string message);
    public interface IWrapperApp
    {
        string GetConfigPath(bool useGlobalPath);
        void GatherAllFoldersAndExtensionsFromSolution(MessageCallback extensionCallback, MessageCallback folderCallback);
        void OpenFile(string path, string line);
        string GetSelectedText();
        System.Drawing.Color GetColor(string resourceKey);
        string GetMonospaceFont();
        string GetNormalFont();
        void RefreshResources(Dictionary<string, object> newResources);
        bool SearchWindowOpened { get; set; }
        bool IsStandalone { get; }
        Dictionary<string, Hotkey> ReadKeyBindings();
        void ApplyKeyBindings(Dictionary<string, Hotkey> bindings);
        void SaveKeyBindings(Dictionary<string, Hotkey> bindings);
        System.Windows.Window GetMainWindow();
        void GetIcon(string filePath, uint background, SearchResult searchResult);
        void StartBackgroundTask(string title);
        void UpdateBackgroundTaskPercentage(int progress);
        void UpdateBackgroundTaskMessage(string message);
        void StopBackgroundTask();
        bool LoadConfigAtStartup();
    }
}
