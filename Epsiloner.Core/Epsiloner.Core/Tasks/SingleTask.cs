using System;
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
    public class SingleTask<TResult> : IDisposable
    {
        private readonly Func<CancellationToken> _tokenResolver;
        private CancellationTokenSource _tokenSource;
        private TaskCompletionSource<TResult> _completionSource = new TaskCompletionSource<TResult>();

        public Task<TResult> Task => _completionSource.Task;

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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="func"></param>
        /// <returns>Returns <see cref="System.Threading.Tasks.Task"/> retrieved from <paramref name="func"/>.</returns>
        public Task Next(Func<CancellationToken, Task<TResult>> func)
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

            //Check if new TaskCompletionSource should be created.
            if (_completionSource.Task.IsCanceled ||
                _completionSource.Task.IsCompleted ||
                _completionSource.Task.IsFaulted)
                _completionSource = new TaskCompletionSource<TResult>();

            task.ContinueWith(x =>
            {
                if (ReferenceEquals(linkedSource, _tokenSource))
                    _completionSource.SetResult(x.Result);
            }, token, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Current);
            task.ContinueWith(x =>
            {
                if (ReferenceEquals(linkedSource, _tokenSource))
                    _completionSource.SetException(x.Exception);
            }, token, TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Current);
            task.ContinueWith(x =>
            {
                if (ReferenceEquals(linkedSource, _tokenSource))
                    _completionSource.SetCanceled();
            }, token, TaskContinuationOptions.OnlyOnCanceled, TaskScheduler.Current);

            return task;
        }
    }
}
