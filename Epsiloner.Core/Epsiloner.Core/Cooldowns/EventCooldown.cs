using System;
using System.Timers;

namespace Epsiloner.Cooldowns
{
    /// <summary>
    /// Postpones event execution after certain time.
    /// It waits for silence gap after last event has been raised.
    /// </summary>
    public class EventCooldown : DisposableObject
    {
        private readonly TimeSpan _accumulateAfter;
        private readonly Action _action;
        private readonly object _padlock = new object();
        private bool _timerIsDisposed;

        private Timer _timer;

        private bool _keepLastStackTrace;
        private string _lastStackTrace;

        /// <summary>
        /// Creates event cooldown.  
        /// </summary>
        /// <param name="accumulateAfter">Timespan after last event execute action.</param>
        /// <param name="action">Action to invoke.</param>
        public EventCooldown(TimeSpan accumulateAfter, Action action)
        {
            _accumulateAfter = accumulateAfter;
            _action = action;
            _timer = NewTimer();
            IsNow = false;
        }

        /// <summary>
        /// For debugging purposes keeps last stack trace of execution. 
        /// Effective only in DEBUG release mode. 
        /// </summary>
        public bool KeepLastStackTrace
        {
            get { return _keepLastStackTrace; }
            set
            {
#if !DEBUG
                return;
#endif
                _keepLastStackTrace = value;
            }
        }

        /// <summary>
        /// Was last cooldown called by now?
        /// </summary>
        public bool IsNow { get; private set; }

        /// <summary>
        /// Executes event with no cooldown. 
        /// </summary>
        public void Now()
        {
            IsNow = true;
            if (IsDisposing)
                return;

            lock (_padlock)
            {
                if (_timer == null)
                    return;

                _timer.Stop();
            }
            InvokeAction();
        }

        /// <summary>
        /// Puts event in cooldown. 
        /// In case no more events comes then OnElapsed will be called.
        /// </summary>
        public void Accumulate()
        {
            IsNow = false;
            if (IsDisposing)
                return;

            lock (_padlock)
            {
                if (_timer == null)
                    return;

#if DEBUG
                if (_keepLastStackTrace)
                    _lastStackTrace = Environment.StackTrace;
#endif

                StopStart();
            }
        }

        /// <summary>
        /// Cancels accumulation.
        /// </summary>
        public void Cancel()
        {
            lock (_padlock)
            {
                try
                {
                    _timer?.Stop();
                }
                catch (ObjectDisposedException)
                {
                    _timer = null;
                    // ignore, we know this
                }
            }
        }

        private void StopStart()
        {
            try
            {
                _timer?.Stop();
                if (!_timerIsDisposed)
                    _timer?.Start();
            }
            catch (ObjectDisposedException)
            {
                _timer = null;
                // ignore, we know this
            }
        }

        private Timer NewTimer()
        {
            var timer = new Timer(_accumulateAfter.TotalMilliseconds)
            {
                AutoReset = false
            };

            timer.Elapsed += OnElapsed;
            timer.Disposed += TimerDisposed;

            return timer;
        }

        private void TimerDisposed(object sender, EventArgs e)
        {
            _timerIsDisposed = true;
        }

        private void OnElapsed(object sender, ElapsedEventArgs e)
        {
            InvokeAction();
        }

        private void InvokeAction()
        {
            if (IsDisposing || _timerIsDisposed)
                return;

            _action.Invoke();
        }

        /// <inheritdoc />
        protected override void DisposeManagedResources()
        {
            lock (_padlock)
            {
                if (_timer == null)
                    return;

                _timer.Close();
                _timer.Elapsed -= OnElapsed;
                _timer.Disposed -= TimerDisposed;
                _timer = null;
            }
        }
    }
}
