using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Epsiloner
{
    /// <summary>
    /// Provides functionality to run action only once at time and in case the same action
    /// during execution is invoked multiple times - it will put invocations into queue and will run them in async.
    /// </summary>
    public class RunQueue : IDisposable
    {
        private readonly int _queueMax;
        private static readonly object[] Params = new object[0];

        private readonly object _lock = new object();
        private readonly SemaphoreSlim _semaphore;

        /// <summary>
        /// Indicates if object already disposed.
        /// </summary>
        public bool IsDisposed { get; private set; }

        /// <summary>
        /// Weak reference to <see cref="Method"/> invocation target.
        /// </summary>
        protected WeakReference WeakReference { get; set; }

        /// <summary>
        /// Method to invoke via <see cref="RunAsync"/>.
        /// </summary>
        protected MethodInfo Method { get; set; }

        /// <summary>
        /// Constructor for <see cref="RunQueue"/>.
        /// </summary>
        /// <param name="action">Action that will be invoked via <see cref="RunAsync"/>. Weak reference.</param>
        /// <param name="queueLimit">Queue limit.</param>
        public RunQueue(Action action, int queueLimit = 1)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));
            if (queueLimit < 1)
                throw new ArgumentException("Queue limit must be >= 1.");

            _queueMax = queueLimit + 1;
            _semaphore = new SemaphoreSlim(_queueMax, _queueMax);

            Method = action.Method;
            WeakReference = new WeakReference(action.Target);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (IsDisposed)
                return;

            IsDisposed = true;
            _semaphore.Dispose();
        }

        /// <summary>
        /// Runs if queue is empty or adds to queue if queue is not full. Otherwise nothing happens.
        /// </summary>
        public async Task RunAsync()
        {
            if (IsDisposed)
                return;

            lock (_lock)
            {
                if (_semaphore.CurrentCount == 0) // Currently running + full queue.
                    return;

                if (_semaphore.CurrentCount < _queueMax) // Currently running, queue is not full.
                {
                    _semaphore.Wait(); // Put 1 run into queue.
                    return;
                }

                _semaphore.Wait(); // Start run without queue.
            }

            var runOnceMore = false;
            try
            {
                if (!WeakReference.IsAlive)
                {
                    Dispose();
                    return;
                }

                Method.Invoke(WeakReference.Target, Params);
            }
            finally
            {
                lock (_lock)
                {
                    _semaphore.Release(); // Finish current run.

                    if (_semaphore.CurrentCount != _queueMax) // Check if anything left in queue.
                    {
                        _semaphore.Release();
                        runOnceMore = true;
                    }
                }

                if (runOnceMore)
                    await RunAsync();
            }
        }

        /// <summary>
        /// Synchronous version of <see cref="RunAsync"/>.
        /// </summary>
        public void Run() => RunAsync().Wait();
    }
}
