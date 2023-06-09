using qgrepControls.Classes;
using System;
using System.Windows;

namespace qgrepGUI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            Current.DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        }
        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            CrashReportsHelper.WriteCrashReport(e.Exception);
        }
        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            CrashReportsHelper.WriteCrashReport((Exception)e.ExceptionObject);
        }

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
