using Newtonsoft.Json;
using qgrepControls.Properties;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace qgrepControls.Classes
{
    public class UIHelper
    {
        private class WindowSize
        {
            public string Title { get; set; }
            public int Width { get; set; }
            public int Height { get; set; }
        }

        public static T FindAncestor<T>(DependencyObject current) where T : DependencyObject
        {
            while (current != null)
            {
                if (current is T ancestor)
                {
                    return ancestor;
                }

                current = VisualTreeHelper.GetParent(current);
            }

            return null;
        }
        public static T GetChildOfType<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj == null) return null;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                var child = VisualTreeHelper.GetChild(depObj, i);

                var result = (child as T) ?? GetChildOfType<T>(child);
                if (result != null) return result;
            }
            return null;
        }

        public static MainWindow CreateWindow(UserControl userControl, string title, IWrapperApp WrapperApp, Control owner, bool resizeable = false)
        {
            MainWindow newWindow = new MainWindow
            {
                Title = title,
                Content = userControl,
                SizeToContent = resizeable ? SizeToContent.Manual : SizeToContent.WidthAndHeight,
                ResizeMode = resizeable ? ResizeMode.CanResizeWithGrip : ResizeMode.NoResize,
                Owner = UIHelper.FindAncestor<Window>(owner) ?? WrapperApp.GetMainWindow(),
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
            };

            if (resizeable)
            {
                LoadWindowSize(newWindow);

                userControl.Width = double.NaN;
                userControl.Height = double.NaN;
            }

            Dictionary<string, object> resources = ThemeHelper.GetResourcesFromColorScheme(WrapperApp);
            foreach (var resource in resources)
            {
                userControl.Resources[resource.Key] = resource.Value;

                if (newWindow != null)
                {
                    newWindow.Resources[resource.Key] = resource.Value;
                }
            }

            return newWindow;
        }

        public static void ShowDialog(UserControl userControl, string title, IWrapperApp WrapperApp, Control owner, bool resizeable = false)
        {
            MainWindow window = CreateWindow(userControl, title, WrapperApp, owner, resizeable);
            window.ShowDialog();
        }

        public static void LoadWindowSize(MainWindow mainWindow)
        {
            try
            {
                List<WindowSize> windowSizes = null;
                windowSizes = JsonConvert.DeserializeObject<List<WindowSize>>(Settings.Default.WindowSizes);

                WindowSize windowSize = windowSizes.Find(x => x.Title == mainWindow.Title);
                if (windowSize != null)
                {
                    mainWindow.Width = windowSize.Width;
                    mainWindow.Height = windowSize.Height;
                }
            }
            catch { }
        }

        public static void SaveWindowSize(MainWindow mainWindow)
        {
            if (mainWindow.ResizeMode == ResizeMode.NoResize)
            {
                return;
            }

            List<WindowSize> windowSizes = null;

            try
            {
                windowSizes = JsonConvert.DeserializeObject<List<WindowSize>>(Settings.Default.WindowSizes);
            }
            catch { }

            if (windowSizes == null)
            {
                windowSizes = new List<WindowSize>();
            }

            windowSizes.RemoveAll(x => x.Title == mainWindow.Title);
            windowSizes.Add(new WindowSize { Title = mainWindow.Title, Width = (int)mainWindow.Width, Height = (int)mainWindow.Height });

            Settings.Default.WindowSizes = JsonConvert.SerializeObject(windowSizes);
            Settings.Default.Save();
        }
    }
}
