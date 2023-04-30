using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace qgrepControls.Classes
{
    public interface IExtensionWindow
    {
        void ShowModal();
        void Close();
    }

    public interface IExtensionInterface
    {
        string GetSolutionPath();
        List<string> GatherAllFoldersFromSolution();
        IExtensionWindow CreateWindow(UserControl userControl, string title);
        void OpenFile(string path, string line);
        string GetSelectedText();
        bool WindowOpened { get; set; }
        bool IsStandalone { get; }
    }
}
