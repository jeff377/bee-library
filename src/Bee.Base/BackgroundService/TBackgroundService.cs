using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Bee.Base
{
    /// <summary>
    /// 背景服務基底類別。
    /// </summary>    
    public abstract class TBackgroundService
    {
        private System.Timers.Timer _Timer = null;
        private EBackgroundServiceStatus _Status = EBackgroundServiceStatus.Stopped;
        private int _ThreadCount = 1;
        private ConcurrentQueue<TBackgroundAction> _TaskQueue = null;
        private SemaphoreSlim _Semaphore = null;
        private DateTime _NextTime = DateTime.MinValue;
        private int _Interval = 10000;

        #region 建構函式

        /// <summary>
        /// 建構函式。
        /// </summary>
        public TBackgroundService()
        {
            _Timer = new System.Timers.Timer(100);
            _Timer.Elapsed += new System.Timers.ElapsedEventHandler(Elapsed_EventHandler);
        }

        #endregion

        #region StatusChanged 事件

        /// <summary>
        /// 背景服務狀態變更引發的事件。
        /// </summary>
        public event BackgroundServiceStatusChangedEventHandler StatusChanged;

        /// <summary>
        /// 引發 StatusChanged 事件。
        /// </summary>
        public void OnStatusChanged(BackgroundServiceStatusChangedEventArgs e)
        {
            StatusChanged?.Invoke(this, e);
        }

        #endregion

        /// <summary>
        /// 計時器。
        /// </summary>
        private System.Timers.Timer Timer
        {
            get { return _Timer; }
        }

        /// <summary>
        /// 背景服務狀態。
        /// </summary>
        public EBackgroundServiceStatus Status
        {
            get { return _Status; }
        }

        /// <summary>
        /// 設定背景服務狀態，並引發 StatusChanged 事件。
        /// </summary>
        /// <param name="status">背景服務狀態。</param>
        private void SetStatus(EBackgroundServiceStatus status)
        {
            BackgroundServiceStatusChangedEventArgs oArgs;

            _Status = status;
            oArgs = new BackgroundServiceStatusChangedEventArgs();
            oArgs.Status = status;
            OnStatusChanged(oArgs);
        }

        /// <summary>
        /// 使用線程數。
        /// </summary>
        public int ThreadCount
        {
            get { return _ThreadCount; }
            set { _ThreadCount = value; }
        }

        /// <summary>
        /// 工作佇列。
        /// </summary>
        public ConcurrentQueue<TBackgroundAction> TaskQueue
        {
            get { return _TaskQueue; }
        }

        /// <summary>
        /// 線程號誌。
        /// </summary>
        public SemaphoreSlim Semaphore
        {
            get { return _Semaphore; }
        }

        /// <summary>
        /// 處理工作加入佇列的時間標記。
        /// </summary>
        public DateTime NextTime
        {
            get { return _NextTime; }
        }

        /// <summary>
        /// 處理工作加入佇列的時間間隔。
        /// </summary>
        public int Interval
        {
            get { return _Interval; }
            set { _Interval = value; }
        }

        /// <summary>
        /// 初始化。
        /// </summary>
        public void Initialize()
        {
            try
            {
                // 初始化的實作方法
                OnInitialize();
                _TaskQueue = new ConcurrentQueue<TBackgroundAction>();
                _Semaphore = new SemaphoreSlim(this.ThreadCount);
            }
            catch (Exception ex)
            {
                OnError(ex, EBackgroundServiceAction.Initialize);
            }
        }

        /// <summary>
        /// 初始化的實作方法。
        /// </summary>
        protected virtual void OnInitialize()
        { }

        /// <summary>
        /// 啟動。
        /// </summary>
        public void Start()
        {
            try
            {
                // 狀態設為正在啟動
                SetStatus(EBackgroundServiceStatus.StartPending);
                // 啟動計時器
                this.Timer.Enabled = true;
                // 啟動的實作方法
                OnStart();
            }
            catch (Exception ex)
            {
                OnError(ex, EBackgroundServiceAction.Start);
            }
        }

        /// <summary>
        /// 啟動的實作方法。
        /// </summary>
        protected virtual void OnStart()
        { }

        /// <summary>
        /// 停止。
        /// </summary>
        public void Stop()
        {
            try
            {
            // 狀態設為正在停止
            _Status = EBackgroundServiceStatus.StopPending;
            // 停止計時器
            this.Timer.Enabled = false;
            // 停止的實作方法
            OnStop();
            }
            catch (Exception ex)
            {
                OnError(ex, EBackgroundServiceAction.Stop);
            }
        }

        /// <summary>
        /// 停止的實作方法。
        /// </summary>
        protected virtual void OnStop()
        { }

        /// <summary>
        /// 計時器時間到達事件處理方式。
        /// </summary>
        private void Elapsed_EventHandler(object sender, System.Timers.ElapsedEventArgs e)
        {
            // 停止計時器
            this.Timer.Enabled = false;
            // 執行服務
            Run();
        }

        /// <summary>
        /// 執行服務。
        /// </summary>
        private void Run()
        {
            // 狀態設為執行中
            SetStatus(EBackgroundServiceStatus.Running);
            // 執行無窮迴圈，以線程執行排程工作
            while (true)
            {
                // 若狀態非執行中，則跳出迴圈
                if (this.Status != EBackgroundServiceStatus.Running) { break; }
                try
                {
                    // 工作加入佇列
                    AddTasks();
                    // 執行佇列工作
                    ExecuteTasks();
                }
                catch (Exception ex)
                {
                    // 處理迴圈發生的例外錯誤，防止因例外錯誤跳出迴圈，造成服務無法正常運行
                    OnError(ex, EBackgroundServiceAction.Run);
                }
                // 迴圈暫停 500 豪秒，防止佔用過多 CPU 效能
                Thread.Sleep(500);
            }

            // 狀態設為停止
            SetStatus(EBackgroundServiceStatus.Stopped);
        }

        /// <summary>
        /// 工作加入佇列。
        /// </summary>
        protected virtual void AddTasks()
        {
            // 若佇列數目大於線程數，則離開
            if (this.TaskQueue.Count >= this.ThreadCount) { return; }
            // 還未到載入時間，則離開
            if (DateTime.Now < this.NextTime) { return; }
            // 載入待處理工作實作
            OnAddTasks();
            // 計算下次載入時間
            _NextTime = DateTime.Now.AddMilliseconds(this.Interval);
        }

        /// <summary>
        /// 工作加入佇列的實作方法。
        /// </summary>
        protected virtual void OnAddTasks()
        {

        }

        /// <summary>
        /// 執行佇列工作。
        /// </summary>
        protected void ExecuteTasks()
        {
            // 當服務狀態為執行中時，以多線程執行佇列中的工作
            while (this.Status == EBackgroundServiceStatus.Running && this.TaskQueue.Count > 0 && this.TaskQueue.TryDequeue(out TBackgroundAction backgroundAction))
            {
                Debug.WriteLine($"可用線程數 : {this.Semaphore.CurrentCount}");
                this.Semaphore.Wait(); // 控制最大並行任務數量
                // 為每個任務創建 CancellationTokenSource 來設置逾時時間
                CancellationTokenSource cts = new CancellationTokenSource(backgroundAction.Timeout);

                Task.Run(() =>
                {
                    try
                    {
                        // 以線程執行委派方法
                        backgroundAction.Action.Invoke(cts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        Debug.WriteLine("Task 逾時取消");
                    }
                    finally
                    {
                        this.Semaphore.Release();
                    }
                }, cts.Token).ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        Debug.WriteLine($"Task 發生例外錯誤: {t.Exception?.Message}");
                    }
                });
            }
        }

        /// <summary>
        /// 添加新工作到佇列。
        /// </summary>
        /// <param name="action">委派方法。</param>
        /// <param name="timeout">逾時取消工作的時間間隔，以毫秒為單位。</param>
        public void AddTask(Action<CancellationToken> action, int timeout)
        {
            this.TaskQueue.Enqueue(new TBackgroundAction(action, timeout));
        }

        /// <summary>
        /// 錯誤處理方法。
        /// </summary>
        /// <param name="e">例外錯誤。</param>
        /// <param name="action">背景服務執行動作。</param>
        protected virtual void OnError(Exception e, EBackgroundServiceAction action)
        { }
    }
}
