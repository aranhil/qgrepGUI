﻿using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TaskStatusCenter;
using Microsoft.VisualStudio.TextManager.Interop;
using qgrepControls.Classes;
using qgrepControls.SearchWindow;
using qgrepExtension;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace qgrepSearch
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration("qgrep Search Tool", "Instant search in files for large codebases", "1.0")]
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
        private IVsImageService2 _imageService;
        private IVsTaskStatusCenterService _taskStatusCenterService;
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();
            await ShowToolWindow.InitializeAsync(this);

            TaskRunner.Initialize(new VsTaskRunner());

            DTE = await GetServiceAsync(typeof(EnvDTE.DTE)) as EnvDTE80.DTE2;

            if (DTE != null)
            {
                DTE.Events.WindowEvents.WindowActivated += OnWindowActivated;
            }

            IVsSolution solution = await GetServiceAsync(typeof(SVsSolution)) as IVsSolution;
            solution?.AdviseSolutionEvents(this, out SolutionEvents);

            IVsShell shell = await GetServiceAsync(typeof(SVsShell)) as IVsShell;
            VSColorTheme.ThemeChanged += VSColorTheme_ThemeChanged;

            IVsTextManager textManager = await GetServiceAsync(typeof(SVsTextManager)) as IVsTextManager;
            ((IConnectionPointContainer)textManager).FindConnectionPoint(typeof(IVsTextManagerEvents).GUID, out IConnectionPoint connectionPoint);
            connectionPoint.Advise(this, out _cookie);

            _imageService = await GetServiceAsync(typeof(SVsImageService)) as IVsImageService2;
            _taskStatusCenterService = (IVsTaskStatusCenterService)(await GetServiceAsync(typeof(SVsTaskStatusCenterService)));

            await CaseSensitiveCommand.InitializeAsync(this);
            await WholeWordCommand.InitializeAsync(this);
            await RegExCommand.InitializeAsync(this);
            await IncludeFilesCommand.InitializeAsync(this);
            await ExcludeFilesCommand.InitializeAsync(this);
            await FilterResultsCommand.InitializeAsync(this);
            await ShowHistoryCommand.InitializeAsync(this);
            await OpenSearchFiles.InitializeAsync(this);
            await GroupingByCommand.InitializeAsync(this);
            await GroupExpandCommand.InitializeAsync(this);

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            System.Windows.Application.Current.DispatcherUnhandledException += App_DispatcherUnhandledException;
            await SearchFilterCommands.InitializeAsync(this);
        }
        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            CrashReportsHelper.WriteCrashReport(e.Exception);
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            CrashReportsHelper.WriteCrashReport((Exception)e.ExceptionObject);
        }

        public IVsImageService2 ImageService
        {
            get { return _imageService; }
        }

        public IVsTaskStatusCenterService TaskStatusCenterService
        {
            get { return _taskStatusCenterService; }
        }

        private void VSColorTheme_ThemeChanged(ThemeChangedEventArgs e)
        {
            qgrepSearchWindow searchWindow = FindToolWindow(typeof(qgrepSearchWindow), toolWindowId, false) as qgrepSearchWindow;
            if (searchWindow != null)
            {
                qgrepSearchWindowControl searchWindowControl = searchWindow.Content as qgrepSearchWindowControl;
                if (searchWindowControl != null)
                {
                    ThemeHelper.ClearCache();
                    ThemeHelper.UpdateColorsFromSettings(searchWindowControl, searchWindowControl.WrapperApp);
                    ThemeHelper.UpdateFontFromSettings(searchWindowControl, searchWindowControl.WrapperApp);
                }
            }
        }

        private void OnWindowActivated(Window gotFocus, Window lostFocus)
        {
            qgrepSearchWindow searchWindow = FindToolWindow(typeof(qgrepSearchWindow), toolWindowId, false) as qgrepSearchWindow;
            if (searchWindow != null)
            {
                qgrepSearchWindowControl searchWindowControl = searchWindow.Content as qgrepSearchWindowControl;
                if (searchWindowControl != null)
                {
                    searchWindowControl.ActiveDocumentChanged();
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

            if (DTE != null)
            {
                DTE.Events.WindowEvents.WindowActivated -= OnWindowActivated;
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

        public bool SolutionAlreadyLoaded = false;
        protected override async Task<object> InitializeToolWindowAsync(Type toolWindowType, int id, CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            IVsSolution solutionService = (IVsSolution)(await GetServiceAsync(typeof(SVsSolution)));

            // Check if a solution is open.
            int hr = solutionService.GetProperty((int)__VSPROPID.VSPROPID_IsSolutionOpen, out object isSolutionOpen);
            ErrorHandler.ThrowOnFailure(hr);

            toolWindowId = id;
            SolutionAlreadyLoaded = (bool)isSolutionOpen;
            return GetExtensionData();
        }

        public ExtensionData GetExtensionData()
        {
            return new ExtensionData
            {
                DTE = DTE,
                Package = this
            };
        }

        public bool SearchWindowOpened = false;

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
                    ThemeHelper.ClearCache();
                    ThemeHelper.UpdateColorsFromSettings(searchWindowControl, searchWindowControl.WrapperApp);
                    ThemeHelper.UpdateFontFromSettings(searchWindowControl, searchWindowControl.WrapperApp);
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

        public void ToggleGroupBy()
        {
            qgrepSearchWindowControl searchWindowControl = GetSearchWindowControl();
            searchWindowControl.ToggleGroupBy();
        }

        public void ToggleGroupExpand()
        {
            qgrepSearchWindowControl searchWindowControl = GetSearchWindowControl();
            searchWindowControl.ToggleGroupExpand();
        }

        public void ToggleSearchFilter(int index)
        {
            qgrepSearchWindowControl searchWindowControl = GetSearchWindowControl();
            searchWindowControl.ToggleSearchFilter(index);
        }

        public void SelectSearchFilter(int index)
        {
            qgrepSearchWindowControl searchWindowControl = GetSearchWindowControl();
            searchWindowControl.SelectSearchFilter(index);
        }
    }
}
