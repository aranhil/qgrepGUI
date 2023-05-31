using System;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
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
            commandService?.AddCommand(cmd);
        }

        private static void Execute(AsyncPackage package)
        {
            try
            {
                qgrepSearchPackage qgrepPackage = package as qgrepSearchPackage;
                if (qgrepPackage != null)
                {
                    qgrepPackage.SearchWindowOpened = true;
                }

                Microsoft.VisualStudio.Threading.JoinableTask joinableTask = package.JoinableTaskFactory.RunAsync(async () =>
                {
                    ToolWindowPane window = await package.ShowToolWindowAsync(
                        typeof(qgrepSearchWindow),
                        0,
                        create: true,
                        cancellationToken: package.DisposalToken);

                    await Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    IVsWindowFrame windowFrame = (IVsWindowFrame)window.Frame;
                    windowFrame.SetProperty((int)__VSFPROPID.VSFPROPID_CmdUIGuid, "6e3b2e95-902b-4385-a966-30c06ab3c7a6");
                });
            }
            catch { }
        }
    }
}
