using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Shell;
using qgrepControls.SearchWindow;

namespace qgrepSearch
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
            Content = new qgrepSearchWindowControl(new qgrepExtension(state));
        }
    }
}
