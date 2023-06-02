using System;
using System.Resources;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Shell;
using qgrepControls.Properties;
using qgrepControls.SearchWindow;

namespace qgrepSearch
{
    [Guid(SearchWindowGuid)]
    public class qgrepSearchWindow : ToolWindowPane
    {
        public const string SearchWindowGuid = "e4e2ba26-a455-4c53-adb3-8225fb696f8b";
        public const string Title = "qgrep Search Tool";

        public qgrepSearchWindow(ExtensionData data) : base()
        {
            Caption = qgrepControls.Properties.Resources.Title;
            BitmapImageMoniker = KnownMonikers.ImageIcon;
            Content = new qgrepSearchWindowControl(new VisualStudioWrapper(data));
        }
    }
}
