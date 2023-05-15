using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.PlatformUI;
using qgrepControls.SearchWindow;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.OLE.Interop;
using System.Net;
using Microsoft.Internal.VisualStudio.Shell;
using static System.Windows.Forms.AxHost;
using System.Windows.Input;

namespace qgrepSearch
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration("qgrep Search Tool", "Tool that uses qgrep to search in project files", "1.0")]
    [ProvideToolWindow(typeof(qgrepSearchWindow), Style = VsDockStyle.Tabbed, DockedWidth = 300, Window = "DocumentWell", Orientation = ToolWindowOrientation.Left)]
    [Guid("6e3b2e95-902b-4385-a966-30c06ab3c7a6")]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideKeyBindingTable(qgrepSearchWindow.SearchWindowGuid, 300)]
    [ProvideBindingPath]
    public sealed class qgrepSearchPackage : AsyncPackage, IVsSolutionEvents, IVsTextManagerEvents
    {
        private EnvDTE80.DTE2 DTE;
        private uint SolutionEvents = uint.MaxValue;
        private int toolWindowId = -1;
        private uint _cookie;
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();
            await ShowToolWindow.InitializeAsync(this);

            DTE = await GetServiceAsync(typeof(EnvDTE.DTE)) as EnvDTE80.DTE2;

            IVsSolution solution = await GetServiceAsync(typeof(SVsSolution)) as IVsSolution;
            solution?.AdviseSolutionEvents(this, out SolutionEvents);

            IVsShell shell = await GetServiceAsync(typeof(SVsShell)) as IVsShell;
            VSColorTheme.ThemeChanged += VSColorTheme_ThemeChanged;

            IVsTextManager textManager = await GetServiceAsync(typeof(SVsTextManager)) as IVsTextManager;
            ((IConnectionPointContainer)textManager).FindConnectionPoint(typeof(IVsTextManagerEvents).GUID, out IConnectionPoint connectionPoint);
            connectionPoint.Advise(this, out _cookie);
            await CaseSensitiveCommand.InitializeAsync(this);
            await WholeWordCommand.InitializeAsync(this);
            await RegExCommand.InitializeAsync(this);
            await IncludeFilesCommand.InitializeAsync(this);
            await ExcludeFilesCommand.InitializeAsync(this);
            await FilterResultsCommand.InitializeAsync(this);
            await ShowHistoryCommand.InitializeAsync(this);
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

            if (_cookie != 0)
            {
                IVsTextManager textManager = GetService(typeof(SVsTextManager)) as IVsTextManager;
                if (textManager != null)
                {
                    ((IConnectionPointContainer)textManager).FindConnectionPoint(typeof(IVsTextManagerEvents).GUID, out IConnectionPoint connectionPoint);
                    connectionPoint.Unadvise(_cookie);
                }
            }
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
            qgrepSearchWindow searchWindow = FindToolWindow(typeof(qgrepSearchWindow), toolWindowId, false) as qgrepSearchWindow;
            if (searchWindow != null)
            {
                qgrepSearchWindowControl searchWindowControl = searchWindow.Content as qgrepSearchWindowControl;
                if (searchWindowControl != null)
                {
                    searchWindowControl.SolutionUnloaded();
                }
            }

            return VSConstants.S_OK;
        }

        public void OnRegisterMarkerType(int iMarkerType)
        {
        }

        public void OnRegisterView(IVsTextView pView)
        {
        }

        public void OnUnregisterView(IVsTextView pView)
        {
        }

        public void OnUserPreferencesChanged(VIEWPREFERENCES[] pViewPrefs, FRAMEPREFERENCES[] pFramePrefs, LANGPREFERENCES[] pLangPrefs, FONTCOLORPREFERENCES[] pColorPrefs)
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

        public qgrepSearchWindowControl GetSearchWindowControl()
        {
            qgrepSearchWindow searchWindow = FindToolWindow(typeof(qgrepSearchWindow), toolWindowId, false) as qgrepSearchWindow;
            if (searchWindow != null)
            {
                return searchWindow.Content as qgrepSearchWindowControl;
            }

            return null;
        }

        public void ToggleCaseSensitive()
        {
            qgrepSearchWindowControl searchWindowControl = GetSearchWindowControl();
            searchWindowControl.ToggleCaseSensitive();
        }

        public void ToggleWholeWord()
        {
            qgrepSearchWindowControl searchWindowControl = GetSearchWindowControl();
            searchWindowControl.ToggleWholeWord();
        }

        public void ToggleRegEx()
        {
            qgrepSearchWindowControl searchWindowControl = GetSearchWindowControl();
            searchWindowControl.ToggleRegEx();
        }

        public void ToggleIncludeFiles()
        {
            qgrepSearchWindowControl searchWindowControl = GetSearchWindowControl();
            searchWindowControl.ToggleIncludeFiles();
        }

        public void ToggleExcludeFiles()
        {
            qgrepSearchWindowControl searchWindowControl = GetSearchWindowControl();
            searchWindowControl.ToggleExcludeFiles();
        }

        public void ToggleFilterResults()
        {
            qgrepSearchWindowControl searchWindowControl = GetSearchWindowControl();
            searchWindowControl.ToggleFilterResults();
        }
        public void ShowHistory()
        {
            qgrepSearchWindowControl searchWindowControl = GetSearchWindowControl();
            searchWindowControl.ShowHistory();
        }
    }
}
