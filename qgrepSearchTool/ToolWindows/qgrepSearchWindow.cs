using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Shell;

namespace qgrepSearch.ToolWindows
{
    [Guid(SearchWindowGuid)]
    public class qgrepSearchWindow : ToolWindowPane
    {
        public const string SearchWindowGuid = "e4e2ba26-a455-4c53-adb3-8225fb696f8b";
        public const string Title = "qgrep Search Tool";

        public qgrepSearchWindow(qgrepSearchWindowState state) : base()
        {
            Caption = Title;
            BitmapImageMoniker = KnownMonikers.ImageIcon;

            Content = new qgrepSearchWindowControl(state);
        }
    }

    [Guid(SearchFilesWindowGuid)]
    public class qgrepSearchFilesWindow : ToolWindowPane
    {
        public const string SearchFilesWindowGuid = "94e8f3c6-022a-400a-8682-399697b97e4b";
        public const string Title = "qgrep Search Files Tool";

        public qgrepSearchFilesWindow(qgrepSearchWindowState state) : base()
        {
            Caption = Title;
            BitmapImageMoniker = KnownMonikers.ImageIcon;

            Content = new qgrepSearchWindowControl(state);
        }
    }
}
