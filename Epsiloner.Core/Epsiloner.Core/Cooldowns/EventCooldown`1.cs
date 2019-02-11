using System;
using System.Timers;

namespace Epsiloner.Cooldowns
{
    /// <summary>
    /// Postpones event execution after certain time.
    /// It waits for silence gap after last event has been raised.
    /// </summary>
    public class EventCooldown<T> : DisposableObject, IEventCooldown<T>
    {
        private readonly TimeSpan _accumulateAfter;
        private readonly Action<T> _action;
        private readonly object _padlock = new object();
        private bool _timerIsDisposed;

        private T _value;
        private Timer _timer;
        private bool _keepLastStackTrace;
        private string _lastStackTrace;


        /// <summary>
        /// Creates event cooldown.  
        /// </summary>
        /// <param name="accumulateAfter">Timespan after last event execute action.</param>
        /// <param name="action">Action to invoke.</param>
        public EventCooldown(TimeSpan accumulateAfter, Action<T> action)
        {
            _accumulateAfter = accumulateAfter;
            _action = action;
            _timer = NewTimer();
            IsNow = false;
        }

        /// <inheritdoc />
        public bool IsNow { get; private set; }

        /// <inheritdoc />
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

        /// <inheritdoc />
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

        /// <inheritdoc />
        public void Now(T value)
        {
            IsNow = true;
            if (IsDisposing)
                return;

            lock (_padlock)
            {
                if (_timer == null)
                    return;

                _timer.Stop();
                _value = value;
            }
            InvokeAction();
        }

        /// <inheritdoc />
        public void Cancel()
        {
            try
            {
                _timer.Stop();
            }
            catch (ObjectDisposedException)
            {
                _timer = null;
                // ignore, we know this
            }
        }

        /// <inheritdoc />
        public void Accumulate(T value)
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
                {
                    _lastStackTrace = Environment.StackTrace;
                }
#endif
                StopStart(value);
            }
        }

        public bool Any()
        {
            lock (_padlock)
            {
                return !_timerIsDisposed && _timer?.Enabled == true;
            }
        }

        protected virtual void StopStart(T value)
        {
            try
            {
                _timer.Stop();
                _value = value;
                if (!_timerIsDisposed)
                    _timer.Start();
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

            //lock (_padlock)
            _action.Invoke(_value);
        }

        protected override void DisposeManagedResources()
        {
            lock (_padlock)
            {
                if (_timer == null)
                    return;

                _timer.Close();
                _timer.Disposed -= TimerDisposed;
                _timer = null;
            }
        }
    }
}
