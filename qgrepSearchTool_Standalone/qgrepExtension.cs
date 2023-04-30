using qgrepControls.Classes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace qgrepSearchTool_Standalone
{
    class qgrepExtensionWindow : IExtensionWindow
    {
        private Window window;
        public qgrepExtensionWindow(Window window)
        {
            this.window = window;
        }

        public void ShowModal()
        {
            window.ShowDialog();
        }

        public void Close()
        {
            window.Close();
        }
    }

    class qgrepExtension : IExtensionInterface
    {
        Window window = null;

        public qgrepExtension(Window window)
        {
            this.window = window;
        }

        public bool WindowOpened 
        {
            get 
            {
                return false;
            }
            set
            {
            }
        }

        public bool IsStandalone
        {
            get
            {
                return true;
            }
        }

        public IExtensionWindow CreateWindow(UserControl userControl, string title)
        {
            return new qgrepExtensionWindow(
                new Window
                {
                    Title = title,
                    Content = userControl,
                    SizeToContent = SizeToContent.WidthAndHeight,
                    ResizeMode = ResizeMode.NoResize,
                    Owner = window
                }
            );
        }

        public string GetSelectedText()
        {
            return "";
        }

        public string GetSolutionPath()
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string appFolderPath = Path.Combine(appDataPath, "qgrepSearch");

            if (!Directory.Exists(appFolderPath))
            {
                Directory.CreateDirectory(appFolderPath);
            }

            return appFolderPath;
        }

        public void OpenFile(string path, string line)
        {
        }

        public List<string> GatherAllFoldersFromSolution()
        {
            return new List<string>();
        }
    }
}
