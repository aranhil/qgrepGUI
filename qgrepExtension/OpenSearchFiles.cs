using Microsoft.VisualStudio.Shell;
using qgrepControls.Classes;
using qgrepControls.SearchWindow;
using System;
using System.ComponentModel.Design;
using Task = System.Threading.Tasks.Task;

namespace qgrepSearch
{
    internal sealed class OpenSearchFiles
    {
        public const int CommandId = 4141;
        public static readonly Guid CommandSet = new Guid("d480acd1-c9b7-45da-a687-4cacc45acf16");
        private readonly AsyncPackage package;
        private qgrepFilesWindowControl filesWindowControl = null;

        private OpenSearchFiles(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(this.Execute, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        public static OpenSearchFiles Instance
        {
            get;
            private set;
        }

        private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new OpenSearchFiles(package, commandService);
        }

        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            qgrepSearchPackage qgrepPackage = package as qgrepSearchPackage;
            if (qgrepPackage != null)
            {
                if(filesWindowControl == null)
                {
                    filesWindowControl = new qgrepFilesWindowControl(new VisualStudioWrapper(qgrepPackage.GetExtensionData()));
                }

                UIHelper.ShowDialog(filesWindowControl, qgrepControls.Properties.Resources.OpenFile, filesWindowControl.WrapperApp, null, true);
                filesWindowControl.OnParentWindowOpened();

                if (filesWindowControl.IncludeFile != null)
                {
                    filesWindowControl.WrapperApp.IncludeFile(filesWindowControl.IncludeFile);
                }
            }
        }
    }
}
