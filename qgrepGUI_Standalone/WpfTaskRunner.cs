using qgrepControls.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace qgrepGUI
{
    public class WpfTaskRunner : ITaskRunner
    {
        private readonly Dispatcher _dispatcher;

        public WpfTaskRunner(Dispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        public async Task RunInBackgroundAsync(Action backgroundTask)
        {
            await Task.Run(() => backgroundTask?.Invoke());
        }

        public void RunInBackground(Action backgroundTask)
        {
            RunInBackgroundAsync(backgroundTask).Wait();
        }

        public async Task RunOnUIThreadAsync(Action uiTask)
        {
            if (_dispatcher.CheckAccess())
            {
                // If already on UI thread, execute the task directly.
                uiTask?.Invoke();
            }
            else
            {
                // If not on UI thread, use Dispatcher.InvokeAsync to post the task on the UI thread.
                await _dispatcher.InvokeAsync(() => uiTask?.Invoke());
            }
        }

        public void RunOnUIThread(Action uiTask)
        {
            RunOnUIThreadAsync(uiTask).Wait();
        }
    }
}
