using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Xceed.Wpf.Toolkit.Core.Input;

namespace qgrepControls.Classes
{
    public interface IExtensionInterface
    {
        string GetSolutionPath();
        List<string> GatherAllFoldersFromSolution();
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
