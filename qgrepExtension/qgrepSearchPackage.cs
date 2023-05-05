using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;
using Microsoft.VisualStudio;
using qgrepControls.ToolWindows;
using Microsoft.VisualStudio.PlatformUI;

namespace qgrepSearch
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration("qgrep Search Tool", "Tool that uses qgrep to search in project files", "1.0")]
    [ProvideToolWindow(typeof(qgrepSearchWindow), Style = VsDockStyle.Tabbed, DockedWidth = 300, Window = "DocumentWell", Orientation = ToolWindowOrientation.Left)]
    [Guid("6e3b2e95-902b-4385-a966-30c06ab3c7a6")]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideBindingPath]
    public sealed class qgrepSearchPackage : AsyncPackage, IVsSolutionEvents
    {
        private EnvDTE80.DTE2 DTE;
        private uint SolutionEvents = uint.MaxValue;
        private int toolWindowId = -1;
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();
            await ShowToolWindow.InitializeAsync(this);

            DTE = await GetServiceAsync(typeof(EnvDTE.DTE)) as EnvDTE80.DTE2;

            IVsSolution solution = await GetServiceAsync(typeof(SVsSolution)) as IVsSolution;
            solution?.AdviseSolutionEvents(this, out SolutionEvents);

            IVsShell shell = await GetServiceAsync(typeof(SVsShell)) as IVsShell;
            VSColorTheme.ThemeChanged += VSColorTheme_ThemeChanged;
        }

        private void VSColorTheme_ThemeChanged(ThemeChangedEventArgs e)
        {
            qgrepSearchWindow searchWindow = FindToolWindow(typeof(qgrepSearchWindow), toolWindowId, false) as qgrepSearchWindow;
            if (searchWindow != null)
            {
                qgrepSearchWindowControl searchWindowControl = searchWindow.Content as qgrepSearchWindowControl;
                if (searchWindowControl != null)
                {
                    searchWindowControl.UpdateColorsFromSettings();
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            IVsSolution solution = GetServiceAsync(typeof(SVsSolution)) as IVsSolution;
            if (solution != null)
            {
                solution.UnadviseSolutionEvents(SolutionEvents);
            }
            SolutionEvents = uint.MaxValue;

            IVsShell shell = GetServiceAsync(typeof(SVsShell)) as IVsShell;
            VSColorTheme.ThemeChanged -= VSColorTheme_ThemeChanged;
        }

        public override IVsAsyncToolWindowFactory GetAsyncToolWindowFactory(Guid toolWindowType)
        {
            return toolWindowType.Equals(Guid.Parse(qgrepSearchWindow.SearchWindowGuid)) ? this : null;
        }

        protected override string GetToolWindowTitle(Type toolWindowType, int id)
        {
            return toolWindowType == typeof(qgrepSearchWindow) ? qgrepSearchWindow.Title : base.GetToolWindowTitle(toolWindowType, id);
        }

        protected override async Task<object> InitializeToolWindowAsync(Type toolWindowType, int id, CancellationToken cancellationToken)
        {
            toolWindowId = id;

            return new qgrepSearchWindowState
            {
                DTE = DTE,
                Package = this
            };
        }

        public bool WindowOpened = false;

        int IVsSolutionEvents.OnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded)
        {
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnQueryCloseProject(IVsHierarchy pHierarchy, int fRemoving, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnBeforeCloseProject(IVsHierarchy pHierarchy, int fRemoved)
        {
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy)
        {
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnQueryUnloadProject(IVsHierarchy pRealHierarchy, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnBeforeUnloadProject(IVsHierarchy pRealHierarchy, IVsHierarchy pStubHierarchy)
        {
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnAfterOpenSolution(object pUnkReserved, int fNewSolution)
        {
            qgrepSearchWindow searchWindow = FindToolWindow(typeof(qgrepSearchWindow), toolWindowId, false) as qgrepSearchWindow;
            if (searchWindow != null)
            {
                qgrepSearchWindowControl searchWindowControl = searchWindow.Content as qgrepSearchWindowControl;
                if (searchWindowControl != null)
                {
                    searchWindowControl.SolutionLoaded();
                }
            }

            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnQueryCloseSolution(object pUnkReserved, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnBeforeCloseSolution(object pUnkReserved)
        {
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnAfterCloseSolution(object pUnkReserved)
        {
            return VSConstants.S_OK;
        }
    }
}
