using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace qgrepGUI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            Window window = (sender as FrameworkElement)?.TemplatedParent as Window;
            if (window != null)
                window.WindowState = WindowState.Minimized;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Window window = (sender as FrameworkElement)?.TemplatedParent as Window;
            if (window != null)
                window.Close();
        }

        private void Maximize_Click(object sender, RoutedEventArgs e)
        {
            Window window = (sender as FrameworkElement)?.TemplatedParent as Window;
            if (window != null)
            {
                if (window.WindowState == WindowState.Maximized)
                {
                    window.WindowState = WindowState.Normal;
                }
                else
                {
                    window.WindowState = WindowState.Maximized;
                }
            }
        }
    }
}
