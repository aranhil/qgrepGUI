using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows;
using System.Windows.Controls;

namespace qgrepControls.Classes
{
    public class UIHelper
    {
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
                SizeToContent = SizeToContent.Manual,
                ResizeMode = resizeable ? ResizeMode.CanResizeWithGrip : ResizeMode.NoResize,
                Width = userControl.Width + 37,
                Height = userControl.Height + 37,
                Owner = UIHelper.FindAncestor<Window>(owner) ?? WrapperApp.GetMainWindow(),
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
            };

            if (resizeable)
            {
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
    }
}
