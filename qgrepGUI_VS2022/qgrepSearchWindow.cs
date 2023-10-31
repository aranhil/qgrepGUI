using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using qgrepControls.SearchWindow;
using System;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Media.Media3D;
using System.Windows.Media;

namespace qgrepSearch
{
    [Guid(SearchWindowGuid)]
    public class qgrepSearchWindow : ToolWindowPane, IVsWindowFrameNotify3
    {
        public const string SearchWindowGuid = "e4e2ba26-a455-4c53-adb3-8225fb696f8b";
        public const string Title = "qgrep Search Tool";

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
        private const uint WM_CLOSE = 0x0010;

        public qgrepSearchWindow(ExtensionData data) : base()
        {
            Caption = qgrepControls.Properties.Resources.Title;
            BitmapImageMoniker = KnownMonikers.ImageIcon;
            Content = new qgrepSearchWindowControl(new VisualStudioWrapper(data));
        }

        public IntPtr GetHwnd(DependencyObject dependencyObject)
        {
            if (dependencyObject != null)
            {
                var hwndSource = PresentationSource.FromDependencyObject(dependencyObject) as HwndSource;
                if (hwndSource != null)
                {
                    return hwndSource.Handle;
                }
            }
            return IntPtr.Zero;
        }

        public int OnClose(ref uint pgrfSaveOptions)
        {
            return Microsoft.VisualStudio.VSConstants.S_OK;
        }

        public int OnDockableChange(int fDockable, int x, int y, int w, int h)
        {
            return Microsoft.VisualStudio.VSConstants.S_OK;
        }

        public int OnMove(int x, int y, int w, int h)
        {
            return Microsoft.VisualStudio.VSConstants.S_OK;
        }

        public int OnShow(int fShow)
        {
            if(fShow == 0)
            {
                qgrepSearchWindowControl searchWindowControl = Content as qgrepSearchWindowControl;
                var hwnd = GetHwnd(searchWindowControl);
                if (hwnd != IntPtr.Zero)
                {
                    SendMessage(hwnd, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
                }

            }
            return Microsoft.VisualStudio.VSConstants.S_OK;
        }

        public int OnSize(int x, int y, int w, int h)
        {
            return Microsoft.VisualStudio.VSConstants.S_OK;
        }
    }
}
