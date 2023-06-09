﻿using System.Threading;

namespace qgrepControls.Classes
{
    public class CountdownTimer
    {
        private Timer _timer;
        private bool _hasExpired = false;
        int LastUpdateInterval = 0;
        int FirstInterval = 150;
        int IntervalIncrement = 100;

        public void Start()
        {
            LastUpdateInterval = FirstInterval;
            StartInternal();
        }

        private void StartInternal()
        {
            _hasExpired = false;
            _timer = new Timer(TimerCallback, null, LastUpdateInterval, Timeout.Infinite);
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

        public void Reset()
        {
            LastUpdateInterval += IntervalIncrement;

            Stop();
            StartInternal();
        }

        public void Stop()
        {
            _timer?.Dispose();
            _timer = null;
        }
    }
}
