using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace qgrepControls.Classes
{
    public class MutexUtility
    {
        private static MutexUtility _instance;
        private readonly Mutex _mutex;

        private MutexUtility(string mutexName)
        {
            _mutex = new Mutex(false, mutexName);
        }

        public static MutexUtility Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new MutexUtility("qgrepGUI");
                }
                return _instance;
            }
        }

        public bool TryAcquireMutex()
        {
            try
            {
                if (_mutex.WaitOne(0))
                {
                    return true;
                }
            }
            catch (AbandonedMutexException)
            {
                return true;
            }

            return false;
        }

        public bool IsMutexFree()
        {
            bool isFree;

            try
            {
                isFree = _mutex.WaitOne(0);
                if (isFree)
                {
                    _mutex.ReleaseMutex();
                }
            }
            catch (AbandonedMutexException)
            {
                isFree = true;
            }

            return isFree;
        }

        public void WorkDone()
        {
            _mutex.ReleaseMutex();
        }
    }
}
