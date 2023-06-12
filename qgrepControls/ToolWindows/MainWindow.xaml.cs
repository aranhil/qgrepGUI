using ControlzEx;
using qgrepControls.Classes;
using System;
using System.Windows;
using System.Windows.Input;

namespace qgrepControls
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : WindowChromeWindow
    {
        public MainWindow()
        {
            InitializeComponent();
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

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();

                Window window = sender as Window;
                if (window != null && window.Owner != null)
                    window.Owner.Focus();
            }
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (ResizeMode == ResizeMode.NoResize)
            {
                Window window = sender as Window;

                if (window != null)
                {
                    double widthDifference = e.NewSize.Width - e.PreviousSize.Width;
                    double heightDifference = e.NewSize.Height - e.PreviousSize.Height;

                    window.Left -= widthDifference / 2;
                    window.Top -= heightDifference / 2;
                }
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            UIHelper.SaveWindowSize(this);
        }
    }
}
