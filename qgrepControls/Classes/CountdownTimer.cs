using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace qgrepControls.Classes
{
    public class CountdownTimer
    {
        private Timer _timer;
        private bool _hasExpired = false;

        public void Start(int intervalInMilliseconds)
        {
            _hasExpired = false;
            _timer = new Timer(TimerCallback, null, intervalInMilliseconds, Timeout.Infinite);
        }

        private void TimerCallback(object state)
        {
            _hasExpired = true;
        }

        public bool HasExpired()
        {
            return _hasExpired;
        }

        public bool IsStarted()
        {
            return _timer != null;
        }

        public void Reset(int intervalInMilliseconds)
        {
            Stop();
            Start(intervalInMilliseconds);
        }

        public void Stop()
        {
            _timer?.Dispose();
            _timer = null;
        }
    }
}
