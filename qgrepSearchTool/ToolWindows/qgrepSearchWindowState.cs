using Microsoft.VisualStudio.Shell;

namespace qgrepSearch.ToolWindows
{
    public class qgrepSearchWindowState
    {
        public EnvDTE80.DTE2 DTE { get; set; }
        public qgrepSearchPackage Package { get; set; }
        public bool SearchFiles { get; set; }
    }
}
