using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Bee.Base.BackgroundServices
{
    /// <summary>
    /// Base class for background services.
    /// </summary>
    public abstract class BackgroundService
    {
        private volatile int _Status = (int)BackgroundServiceStatus.Stopped;
        private int _ThreadCount = 1;
        private ConcurrentQueue<BackgroundAction>? _TaskQueue;
        private SemaphoreSlim? _Semaphore;
        private DateTime _NextTime = DateTime.MinValue;
        private int _Interval = 10000;
        private CancellationTokenSource? _Cts;

        #region StatusChanged Event

        /// <summary>
        /// Event raised when the background service status changes.
        /// </summary>
        public event BackgroundServiceStatusChangedEventHandler? StatusChanged;

        /// <summary>
        /// Raises the StatusChanged event.
        /// </summary>
        public void OnStatusChanged(BackgroundServiceStatusChangedEventArgs e)
        {
            StatusChanged?.Invoke(this, e);
        }

        #endregion

        /// <summary>
        /// Gets the current background service status.
        /// </summary>
        public BackgroundServiceStatus Status
        {
            get { return (BackgroundServiceStatus)_Status; }
        }

        /// <summary>
        /// Sets the background service status and raises the StatusChanged event.
        /// </summary>
        /// <param name="status">The new background service status.</param>
        private void SetStatus(BackgroundServiceStatus status)
        {
            _Status = (int)status;
            var oArgs = new BackgroundServiceStatusChangedEventArgs
            {
                Status = status
            };
            OnStatusChanged(oArgs);
        }

        /// <summary>
        /// Gets or sets the number of threads to use.
        /// </summary>
        public int ThreadCount
        {
            get { return _ThreadCount; }
            set { _ThreadCount = value; }
        }

        /// <summary>
        /// Gets the task queue.
        /// </summary>
        public ConcurrentQueue<BackgroundAction>? TaskQueue
        {
            get { return _TaskQueue; }
        }

        /// <summary>
        /// Gets the thread semaphore.
        /// </summary>
        public SemaphoreSlim? Semaphore
        {
            get { return _Semaphore; }
        }

        /// <summary>
        /// Gets the timestamp for the next task queue loading.
        /// </summary>
        public DateTime NextTime
        {
            get { return _NextTime; }
        }

        /// <summary>
        /// Gets or sets the interval (in milliseconds) between task queue loading cycles.
        /// </summary>
        public int Interval
        {
            get { return _Interval; }
            set { _Interval = value; }
        }

        /// <summary>
        /// Initializes the background service.
        /// </summary>
        public void Initialize()
        {
            try
            {
                // Call the initialization implementation
                OnInitialize();
                _TaskQueue = new ConcurrentQueue<BackgroundAction>();
                _Semaphore = new SemaphoreSlim(this.ThreadCount);
            }
            catch (Exception ex)
            {
                OnError(ex, BackgroundServiceAction.Initialize);
            }
        }

        /// <summary>
        /// Override to provide custom initialization logic.
        /// </summary>
        protected virtual void OnInitialize()
        { }

        /// <summary>
        /// Starts the background service.
        /// </summary>
        public void Start()
        {
            try
            {
                SetStatus(BackgroundServiceStatus.StartPending);
                OnStart();

                _Cts = new CancellationTokenSource();
                var token = _Cts.Token;
                _ = Task.Factory.StartNew(
                    () => RunLoop(token),
                    token,
                    TaskCreationOptions.LongRunning,
                    TaskScheduler.Default);
            }
            catch (Exception ex)
            {
                OnError(ex, BackgroundServiceAction.Start);
            }
        }

        /// <summary>
        /// Override to provide custom start logic.
        /// </summary>
        protected virtual void OnStart()
        { }

        /// <summary>
        /// Stops the background service.
        /// </summary>
        public void Stop()
        {
            try
            {
                SetStatus(BackgroundServiceStatus.StopPending);
                _Cts?.Cancel();
                OnStop();
            }
            catch (Exception ex)
            {
                OnError(ex, BackgroundServiceAction.Stop);
            }
        }

        /// <summary>
        /// Override to provide custom stop logic.
        /// </summary>
        protected virtual void OnStop()
        { }

        /// <summary>
        /// Runs the service loop on a dedicated long-running task.
        /// </summary>
        /// <param name="token">Cancellation token signaled by <see cref="Stop"/>.</param>
        private void RunLoop(CancellationToken token)
        {
            try
            {
                SetStatus(BackgroundServiceStatus.Running);

                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        AddTasks();
                        ExecuteTasks();
                    }
                    catch (Exception ex)
                    {
                        // Handle exceptions in the loop to prevent service interruption
                        OnError(ex, BackgroundServiceAction.Run);
                    }

                    // Cancellable wait; WaitOne returns true if the token is signaled,
                    // false if the 500 ms elapses with no cancellation.
                    if (token.WaitHandle.WaitOne(500)) { break; }
                }
            }
            finally
            {
                SetStatus(BackgroundServiceStatus.Stopped);
                _Cts?.Dispose();
                _Cts = null;
            }
        }

        /// <summary>
        /// Enqueues tasks for processing.
        /// </summary>
        protected virtual void AddTasks()
        {
            // Exit if the queue already has tasks equal to or exceeding the thread count
            if (this.TaskQueue!.Count >= this.ThreadCount) { return; }
            // Exit if it is not yet time to load tasks
            if (DateTime.Now < this.NextTime) { return; }
            // Call the task-loading implementation
            OnAddTasks();
            // Calculate the next load time
            _NextTime = DateTime.Now.AddMilliseconds(this.Interval);
        }

        /// <summary>
        /// Override to provide custom task-loading logic.
        /// </summary>
        protected virtual void OnAddTasks()
        {

        }

        /// <summary>
        /// Executes queued tasks.
        /// </summary>
        protected void ExecuteTasks()
        {
            // Execute queued tasks on multiple threads while the service is running
            while (this.Status == BackgroundServiceStatus.Running && this.TaskQueue!.Count > 0 && this.TaskQueue.TryDequeue(out BackgroundAction? backgroundAction))
            {
                Debug.WriteLine($"Available thread count: {this.Semaphore!.CurrentCount}");
                this.Semaphore.Wait(); // Limit maximum concurrency
                // Create a CancellationTokenSource for each task to set a timeout
                CancellationTokenSource cts = new CancellationTokenSource(backgroundAction.Timeout);

                Task.Run(() =>
                {
                    try
                    {
                        // Execute the delegate on a thread
                        backgroundAction.Action.Invoke(cts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        Debug.WriteLine("Task timed out and was cancelled");
                    }
                    finally
                    {
                        this.Semaphore.Release();
                    }
                }, cts.Token).ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        Debug.WriteLine($"Task faulted with exception: {t.Exception?.Message}");
                    }
                    cts.Dispose();
                });
            }
        }

        /// <summary>
        /// Adds a new task to the queue.
        /// </summary>
        /// <param name="action">The delegate to execute.</param>
        /// <param name="timeout">The timeout interval in milliseconds after which the task will be cancelled.</param>
        public void AddTask(Action<CancellationToken> action, int timeout)
        {
            this.TaskQueue!.Enqueue(new BackgroundAction(action, timeout));
        }

        /// <summary>
        /// Error handler method.
        /// </summary>
        /// <param name="e">The exception that occurred.</param>
        /// <param name="action">The background service action during which the error occurred.</param>
        protected virtual void OnError(Exception e, BackgroundServiceAction action)
        { }
    }
}
