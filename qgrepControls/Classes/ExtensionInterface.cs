using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace qgrepControls.Classes
{
    public interface IExtensionWindow
    {
        void ShowModal();
        void Show();
        void Close();
    }

    public interface IExtensionInterface
    {
        string GetSolutionPath();
        List<string> GatherAllFoldersFromSolution();
        IExtensionWindow CreateWindow(UserControl userControl, string title, UserControl owner = null);
        void OpenFile(string path, string line);
        string GetSelectedText();
        System.Drawing.Color GetColor(string resourceKey);
        bool WindowOpened { get; set; }
        bool IsStandalone { get; }
    }
}
