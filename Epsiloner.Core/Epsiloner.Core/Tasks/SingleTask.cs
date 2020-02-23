using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Epsiloner.Tasks
{
    /*
     Reason:
     - Sometimes we need to have only 1 running task at moment.
     -> It means that when new task appears, previous should be canceled.
     -> Try to provide to public Task that will be completed even in this scenario:
     - 1. Set task A.
     - 2. wait for Task from outside.
     - 3. Set task to B.
     - 4. task A cancels.
     - 5. outside - still waits for Task completion.
     - 6. task B completes
     - 7. outside - Task (B) completed.

     */

    /// <summary>
    /// 
    /// </summary>
    [Obsolete("Work in progress. Not ready for final use.")]
    public class SingleTask : IDisposable
    {
        private readonly Func<CancellationToken> _tokenResolver;
        private CancellationTokenSource _tokenSource;

        public Task Task { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tokenResolver">Method to resolve token for each function executed via <see cref="Next"/>.</param>
        public SingleTask(Func<CancellationToken> tokenResolver)
        {
            _tokenResolver = tokenResolver ?? (() => CancellationToken.None);
        }

        public void Dispose()
        {
            _tokenSource?.Dispose();
        }

        public Task Next(Func<CancellationToken, Task> func)
        {
            if (_tokenSource != null)
            {
                _tokenSource.Cancel();
                _tokenSource.Dispose();
            }

            var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(_tokenResolver());
            var token = linkedSource.Token;
            _tokenSource = linkedSource;

            var task = func(token);
            Task = task;
            return task;
        }

    }
}
