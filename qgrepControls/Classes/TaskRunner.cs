using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace qgrepControls.Classes
{
    public static class TaskRunner
    {
        public static ITaskRunner Instance { get; private set; }

        public static void Initialize(ITaskRunner taskRunner)
        {
            Instance = taskRunner;
        }

        public static void RunInBackground(Action backgroundTask)
        {
            Instance.RunInBackground(backgroundTask);
        }

        public static void RunOnUIThread(Action uiTask)
        {
            Instance.RunOnUIThread(uiTask);
        }

        public static Task RunInBackgroundAsync(Action backgroundTask)
        {
            return Instance.RunInBackgroundAsync(backgroundTask);
        }

        public static Task RunOnUIThreadAsync(Action uiTask)
        {
            return Instance.RunOnUIThreadAsync(uiTask);
        }
    }

    public interface ITaskRunner
    {
        void RunInBackground(Action backgroundTask);
        void RunOnUIThread(Action uiTask);
        Task RunInBackgroundAsync(Action backgroundTask);
        Task RunOnUIThreadAsync(Action uiTask);
    }
}
