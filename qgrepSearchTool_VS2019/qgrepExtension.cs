using EnvDTE;
using Microsoft.VisualStudio.PlatformUI;
using qgrepControls.Classes;
using System;
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
    }
}
