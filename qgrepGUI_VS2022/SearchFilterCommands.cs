using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.ComponentModel.Design;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace qgrepSearch
{
    internal sealed class SearchFilterCommands
    {
        public static int[] ToggleCommandIds = new int[]
        {
            4144,
            4145,
            4146,
            4147,
            4148,
            4149,
            4150,
            4151,
            4152
        };

        public static int[] SelectCommandIds = new int[]
        {
            4153,
            4154,
            4155,
            4156,
            4157,
            4158,
            4159,
            4160,
            4161
        };

        public static readonly Guid CommandSet = new Guid("d480acd1-c9b7-45da-a687-4cacc45acf16");

        private readonly AsyncPackage package;

        private SearchFilterCommands(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            for(int i = 0; i < 9; i++)
            {
                var menuCommandID = new CommandID(CommandSet, ToggleCommandIds[i]);
                int index = i;

                var menuItem = new MenuCommand((object sender, EventArgs e) =>
                {
                    ThreadHelper.ThrowIfNotOnUIThread();
                    ExecuteToggle(sender, e, index);
                }, menuCommandID);

                commandService.AddCommand(menuItem);
            }

            for(int i = 0; i < 9; i++)
            {
                var menuCommandID = new CommandID(CommandSet, SelectCommandIds[i]);
                int index = i;

                var menuItem = new MenuCommand((object sender, EventArgs e) =>
                {
                    ThreadHelper.ThrowIfNotOnUIThread();
                    ExecuteSelect(sender, e, index);
                }, menuCommandID);

                commandService.AddCommand(menuItem);
            }
        }

        public static SearchFilterCommands Instance
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
            // Switch to the main thread - the call to AddCommand in SearchFilterToggleCommands's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new SearchFilterCommands(package, commandService);
        }

        private void ExecuteToggle(object sender, EventArgs e, int index)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            qgrepSearchPackage searchPackage = package as qgrepSearchPackage;
            if (searchPackage != null)
            {
                searchPackage.ToggleSearchFilter(index);
            }
        }
        private void ExecuteSelect(object sender, EventArgs e, int index)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            qgrepSearchPackage searchPackage = package as qgrepSearchPackage;
            if (searchPackage != null)
            {
                searchPackage.SelectSearchFilter(index);
            }
        }
    }
}
