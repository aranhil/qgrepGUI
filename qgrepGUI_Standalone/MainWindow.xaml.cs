using ControlzEx;
using Octokit;
using qgrepControls.Classes;
using qgrepControls.SearchWindow;
using qgrepGUI.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace qgrepGUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : WindowChromeWindow
    {
        qgrepSearchWindowControl SearchWindow;
        string UpdateFilePath = "";
        bool UpdateQueued = false;

        public MainWindow()
        {
            InitializeComponent();
            TaskRunner.Initialize(new WpfTaskRunner(Dispatcher));

            SearchWindow = new qgrepSearchWindowControl(new StandaloneWrapper(this));
            WindowContent.Children.Add(SearchWindow);

            Task.Run(CheckForUpdates);
        }

        public async Task<string> GetLatestRelease()
        {
            var github = new GitHubClient(new ProductHeaderValue("qgrepGUI"));
            var releases = await github.Repository.Release.GetAll("aranhil", "qgrepGUI");
            return releases[0].TagName;
        }

        private async Task DownloadLatestReleaseAsync()
        {
            try
            {
                var client = new GitHubClient(new ProductHeaderValue("qgrepGUI"));

                var latestRelease = await client.Repository.Release.GetLatest("aranhil", "qgrepGUI");

                ReleaseAsset asset = null;
                foreach (var item in latestRelease.Assets)
                {
                    if (item.Name.EndsWith(".exe"))
                    {
                        asset = item;
                        break;
                    }
                }

                if (asset == null)
                {
                    throw new Exception("No executable found.");
                }

                var httpClient = new HttpClient();
                var downloadStream = await httpClient.GetStreamAsync(asset.BrowserDownloadUrl);

                UpdateFilePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), asset.Name);
                using (var fileStream = System.IO.File.Create(UpdateFilePath))
                {
                    downloadStream.CopyTo(fileStream);
                }

                Process.Start(UpdateFilePath);
            }
            catch { }

            Dispatcher.Invoke(() =>
            {
                System.Windows.Application.Current.Shutdown();
            });
        }

        public async Task CheckForUpdates()
        {
            try
            {
                //Settings.Default.SkippedVersion = "1.0";
                //Settings.Default.Save();

                var currentVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                var skippedVersionString = Settings.Default.SkippedVersion;

                var latestReleaseTag = await GetLatestRelease();
                if(latestReleaseTag.StartsWith("v"))
                {
                    latestReleaseTag = latestReleaseTag.Substring(1);
                }

                Version latestVersion = new Version(latestReleaseTag);
                Version skippedVersion = new Version(skippedVersionString);

                if (skippedVersion < latestVersion && currentVersion < latestVersion)
                {
                    Dispatcher.Invoke(() =>
                    {
                        InstallUpdateWindow installUpdateWindow = new InstallUpdateWindow();
                        qgrepControls.MainWindow installUpdateDialog = UIHelper.CreateWindow(installUpdateWindow, "Update available", new StandaloneWrapper(this), this);
                        installUpdateWindow.Dialog = installUpdateDialog;
                        installUpdateDialog.ShowDialog();

                        if(installUpdateWindow.IsOk)
                        {
                            UpdateQueued = true;
                        }
                        else if(installUpdateWindow.IsSkip)
                        {
                            Settings.Default.SkippedVersion = latestVersion.ToString();
                            Settings.Default.Save();
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred while checking for updates: " + ex.Message);
            }
        }

#pragma warning disable 618
        private void TitleBarGrid_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // if (e.ClickCount == 1)
            // {
            //     e.Handled = true;
            //
            //     // taken from DragMove internal code
            //     this.VerifyAccess();
            //
            //     // for the touch usage
            //     UnsafeNativeMethods.ReleaseCapture();
            //
            //     var criticalHandle = (IntPtr)criticalHandlePropertyInfo.GetValue(this, emptyObjectArray);
            //
            //     // these lines are from DragMove
            //     // NativeMethods.SendMessage(criticalHandle, WM.SYSCOMMAND, (IntPtr)SC.MOUSEMOVE, IntPtr.Zero);
            //     // NativeMethods.SendMessage(criticalHandle, WM.LBUTTONUP, IntPtr.Zero, IntPtr.Zero);
            //
            //     var wpfPoint = this.PointToScreen(Mouse.GetPosition(this));
            //     var x = (int)wpfPoint.X;
            //     var y = (int)wpfPoint.Y;
            //     NativeMethods.SendMessage(criticalHandle, WM.NCLBUTTONDOWN, (IntPtr)HT.CAPTION, new IntPtr(x | (y << 16)));
            // }
            // else if (e.ClickCount == 2
            //          && this.ResizeMode != ResizeMode.NoResize)
            // {
            //     e.Handled = true;
            //
            //     if (this.WindowState == WindowState.Normal
            //         && this.ResizeMode != ResizeMode.NoResize
            //         && this.ResizeMode != ResizeMode.CanMinimize)
            //     {
            //         SystemCommands.MaximizeWindow(this);
            //     }
            //     else
            //     {
            //         SystemCommands.RestoreWindow(this);
            //     }
            // }
        }

        private void TitleBarGrid_OnMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            // Windows.Shell.SystemCommands.ShowSystemMenu(this, e);
        }

        private void CommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void ToggleCaseSensitive_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            SearchWindow.ToggleCaseSensitive();
        }

        private void ToggleWholeWord_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            SearchWindow.ToggleWholeWord();
        }

        private void ToggleRegEx_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            SearchWindow.ToggleRegEx();
        }

        private void ToggleIncludeFiles_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            SearchWindow.ToggleIncludeFiles();
        }

        private void ToggleExcludeFiles_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            SearchWindow.ToggleExcludeFiles();
        }

        private void ToggleFilterResults_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            SearchWindow.ToggleFilterResults();
        }

        private void ShowHistory_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            SearchWindow.ShowHistory();
        }

        private void OpenFileSearch_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            qgrepFilesWindowControl filesWindowControl = new qgrepFilesWindowControl(new StandaloneWrapper(this));
            UIHelper.ShowDialog(filesWindowControl, qgrepControls.Properties.Resources.OpenFile, filesWindowControl.WrapperApp, SearchWindow, true);
        }

        private void ToggleGroupBy_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            SearchWindow.ToggleGroupBy();
        }

        private void ToggleGroupExpand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            SearchWindow.ToggleGroupExpand();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if(UpdateQueued)
            {
                e.Cancel = true;
                Hide();

                Task.Run(DownloadLatestReleaseAsync);
            }
        }

        private void ToggleSearchFilter1_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            SearchWindow.ToggleSearchFilter(0);
        }

        private void ToggleSearchFilter2_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            SearchWindow.ToggleSearchFilter(1);
        }

        private void ToggleSearchFilter3_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            SearchWindow.ToggleSearchFilter(2);
        }

        private void ToggleSearchFilter4_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            SearchWindow.ToggleSearchFilter(3);
        }

        private void ToggleSearchFilter5_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            SearchWindow.ToggleSearchFilter(4);
        }

        private void ToggleSearchFilter6_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            SearchWindow.ToggleSearchFilter(5);
        }

        private void ToggleSearchFilter7_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            SearchWindow.ToggleSearchFilter(6);
        }

        private void ToggleSearchFilter8_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            SearchWindow.ToggleSearchFilter(7);
        }

        private void ToggleSearchFilter9_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            SearchWindow.ToggleSearchFilter(8);
        }

        private void SelectSearchFilter1_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            SearchWindow.SelectSearchFilter(0);
        }

        private void SelectSearchFilter2_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            SearchWindow.SelectSearchFilter(1);
        }

        private void SelectSearchFilter3_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            SearchWindow.SelectSearchFilter(2);
        }

        private void SelectSearchFilter4_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            SearchWindow.SelectSearchFilter(3);
        }

        private void SelectSearchFilter5_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            SearchWindow.SelectSearchFilter(4);
        }

        private void SelectSearchFilter6_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            SearchWindow.SelectSearchFilter(5);
        }

        private void SelectSearchFilter7_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            SearchWindow.SelectSearchFilter(6);
        }

        private void SelectSearchFilter8_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            SearchWindow.SelectSearchFilter(7);
        }

        private void SelectSearchFilter9_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            SearchWindow.SelectSearchFilter(8);
        }
    }
}
