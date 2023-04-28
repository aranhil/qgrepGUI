using System;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace qgrepSearch
{
    internal sealed class ShowToolWindow
    {
        public static async Task InitializeAsync(AsyncPackage package)
        {
            var commandService = (IMenuCommandService)await package.GetServiceAsync(typeof(IMenuCommandService));

            var cmdId = new CommandID(Guid.Parse("9cc1062b-4c82-46d2-adcb-f5c17d55fb85"), 0x0100);
            var cmd = new MenuCommand((s, e) => Execute(package), cmdId);
            commandService.AddCommand(cmd);
        }

        private static void Execute(AsyncPackage package)
        {
            qgrepSearchPackage grepPackage = package as qgrepSearchPackage;
            if(grepPackage != null)
            {
                grepPackage.WindowOpened = true;
            }

            package.JoinableTaskFactory.RunAsync(async () =>
            {
                ToolWindowPane window = await package.ShowToolWindowAsync(
                    typeof(qgrepSearchWindow),
                    0,
                    create: true,
                    cancellationToken: package.DisposalToken);
            });
        }
    }
}
