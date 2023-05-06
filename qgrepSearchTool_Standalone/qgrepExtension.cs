using ControlzEx;
using qgrepControls.Classes;
using qgrepControls.SearchWindow;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
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
        public Window window;
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

        public void Show()
        {
            window.Show();
        }
    }

    class qgrepExtension : IExtensionInterface
    {
        Window Window;
        public qgrepExtension(Window window)
        {
            Window = window;
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

        public IExtensionWindow CreateWindow(UserControl userControl, string title, UserControl owner)
        {
            MainWindow newWindow = new MainWindow
            {
                Title = title,
                Content = userControl,
                SizeToContent = SizeToContent.WidthAndHeight,
                ResizeMode = ResizeMode.NoResize,
                Owner = qgrepSearchWindowControl.FindAncestor<Window>(owner),
                WindowStartupLocation = WindowStartupLocation.CenterOwner, 
            };

            return new qgrepExtensionWindow(newWindow);
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
            try
            {
                System.Diagnostics.Process.Start(path);
            }
            catch { }
        }

        public List<string> GatherAllFoldersFromSolution()
        {
            return new List<string>();
        }

        public Color GetColor(string resourceKey)
        {
            Random random = new Random(DateTime.Now.Millisecond);
            return Color.FromArgb(random.Next(256), random.Next(256), random.Next(256));
        }

        public void RefreshResources(Dictionary<string, object> newResources)
        {
            foreach(var resource in newResources)
            {
                Window.Resources[resource.Key] = resource.Value;
                Application.Current.Resources[resource.Key] = resource.Value;
            }
        }
    }
}
