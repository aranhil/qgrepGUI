using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
using qgrepControls.Classes;
using System;
using System.Threading.Tasks;

namespace qgrepExtension
{

    public class VsTaskRunner : ITaskRunner
    {
        public async Task RunInBackgroundAsync(Action backgroundTask)
        {
            await ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                await TaskScheduler.Default;

                backgroundTask?.Invoke();
            });
        }

        public void RunInBackground(Action backgroundTask)
        {
            ThreadHelper.JoinableTaskFactory.Run(() => RunInBackgroundAsync(backgroundTask));
        }

        public async Task RunOnUIThreadAsync(Action uiTask)
        {
            await ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                uiTask?.Invoke();
            });
        }

        public void RunOnUIThread(Action uiTask)
        {
            ThreadHelper.JoinableTaskFactory.Run(() => RunOnUIThreadAsync(uiTask));
        }
    }
}
