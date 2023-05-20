using ControlzEx;
using qgrepControls.Classes;
using qgrepControls.SearchWindow;
using System;
using System.Collections.Generic;
using System.Linq;
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

namespace qgrepSearchTool_Standalone
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : WindowChromeWindow
    {
        qgrepSearchWindowControl SearchWindow;

        public MainWindow()
        {
            InitializeComponent();
            SearchWindow = new qgrepSearchWindowControl(new qgrepExtension(this));
            WindowContent.Children.Add(SearchWindow);
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
            qgrepFilesWindowControl filesWindowControl = new qgrepFilesWindowControl(new qgrepExtension(this));
            UIHelper.ShowDialog(filesWindowControl, "Open file", filesWindowControl.ExtensionInterface, SearchWindow, true);
        }

        private void ToggleGroupingBy_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            SearchWindow.ToggleGroupingBy();
        }
    }
}
