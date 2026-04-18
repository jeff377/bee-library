using System.ComponentModel;
using System.Diagnostics;
using Bee.Base.BackgroundServices;

namespace Bee.Base.UnitTests
{
    public class BackgroundServiceTests
    {
        private sealed class TestService : BackgroundService
        {
            public int InitializeCount;
            public int StartCount;
            public int StopCount;
            public int AddTasksCount;
            public Exception? LastError;
            public BackgroundServiceAction? LastErrorAction;
            public Exception? InitializeThrow;
            public Exception? StartThrow;
            public Exception? StopThrow;
            public Action? OnAddTasksHook;

            protected override void OnInitialize()
            {
                InitializeCount++;
                if (InitializeThrow != null)
                    throw InitializeThrow;
            }

            protected override void OnStart()
            {
                StartCount++;
                if (StartThrow != null)
                    throw StartThrow;
            }

            protected override void OnStop()
            {
                StopCount++;
                if (StopThrow != null)
                    throw StopThrow;
            }

            protected override void OnAddTasks()
            {
                AddTasksCount++;
                OnAddTasksHook?.Invoke();
            }

            protected override void OnError(Exception e, BackgroundServiceAction action)
            {
                LastError = e;
                LastErrorAction = action;
            }
        }

        private static bool WaitFor(Func<bool> condition, int timeoutMs = 3000)
        {
            return SpinWait.SpinUntil(condition, timeoutMs);
        }

        [Fact]
        [DisplayName("初始狀態應為 Stopped，且 TaskQueue 與 Semaphore 尚未建立")]
        public void InitialState_StatusStoppedAndResourcesNotAllocated()
        {
            var svc = new TestService();

            Assert.Equal(BackgroundServiceStatus.Stopped, svc.Status);
            Assert.Null(svc.TaskQueue);
            Assert.Null(svc.Semaphore);
            Assert.Equal(DateTime.MinValue, svc.NextTime);
        }

        [Fact]
        [DisplayName("ThreadCount 與 Interval 預設值並可被設定")]
        public void ThreadCountAndInterval_DefaultsAndCanBeUpdated()
        {
            var svc = new TestService();

            Assert.Equal(1, svc.ThreadCount);
            Assert.Equal(10000, svc.Interval);

            svc.ThreadCount = 4;
            svc.Interval = 250;

            Assert.Equal(4, svc.ThreadCount);
            Assert.Equal(250, svc.Interval);
        }

        [Fact]
        [DisplayName("Initialize 應呼叫 OnInitialize 並建立 TaskQueue 與 Semaphore")]
        public void Initialize_CreatesQueueAndSemaphore()
        {
            var svc = new TestService { ThreadCount = 3 };

            svc.Initialize();

            Assert.Equal(1, svc.InitializeCount);
            Assert.NotNull(svc.TaskQueue);
            Assert.Empty(svc.TaskQueue!);
            Assert.NotNull(svc.Semaphore);
            Assert.Equal(3, svc.Semaphore!.CurrentCount);
            Assert.Null(svc.LastError);
        }

        [Fact]
        [DisplayName("Initialize 內部拋例外時應呼叫 OnError 並傳入 Initialize 動作")]
        public void Initialize_OnInitializeThrows_CallsOnErrorWithInitializeAction()
        {
            var boom = new InvalidOperationException("boom");
            var svc = new TestService { InitializeThrow = boom };

            svc.Initialize();

            Assert.Same(boom, svc.LastError);
            Assert.Equal(BackgroundServiceAction.Initialize, svc.LastErrorAction);
            Assert.Null(svc.TaskQueue);
            Assert.Null(svc.Semaphore);
        }

        [Fact]
        [DisplayName("AddTask 應將動作加入 TaskQueue")]
        public void AddTask_EnqueuesBackgroundAction()
        {
            var svc = new TestService();
            svc.Initialize();

            svc.AddTask(_ => { }, 1000);
            svc.AddTask(_ => { }, 2000);

            Assert.Equal(2, svc.TaskQueue!.Count);
            Assert.True(svc.TaskQueue.TryDequeue(out var first));
            Assert.Equal(1000, first!.Timeout);
        }

        [Fact]
        [DisplayName("Start 應立即將狀態設為 StartPending 並觸發 StatusChanged 與 OnStart")]
        public void Start_SetsStartPendingAndRaisesStatusChanged()
        {
            var svc = new TestService();
            svc.Initialize();

            var statuses = new List<BackgroundServiceStatus>();
            svc.StatusChanged += (_, e) => statuses.Add(e.Status);

            try
            {
                svc.Start();

                Assert.Equal(1, svc.StartCount);
                Assert.Contains(BackgroundServiceStatus.StartPending, statuses);
            }
            finally
            {
                svc.Stop();
                WaitFor(() => svc.Status == BackgroundServiceStatus.Stopped);
            }
        }

        [Fact]
        [DisplayName("啟動後應進入 Running，停止後應回到 Stopped")]
        public void StartThenStop_TransitionsThroughExpectedStatuses()
        {
            var svc = new TestService { Interval = 50 };
            svc.Initialize();

            var statuses = new List<BackgroundServiceStatus>();
            svc.StatusChanged += (_, e) =>
            {
                lock (statuses) { statuses.Add(e.Status); }
            };

            svc.Start();
            Assert.True(WaitFor(() => svc.Status == BackgroundServiceStatus.Running),
                "Service did not reach Running state in time.");

            svc.Stop();
            Assert.Equal(1, svc.StopCount);

            Assert.True(WaitFor(() => svc.Status == BackgroundServiceStatus.Stopped),
                "Service did not reach Stopped state in time.");

            lock (statuses)
            {
                Assert.Contains(BackgroundServiceStatus.StartPending, statuses);
                Assert.Contains(BackgroundServiceStatus.Running, statuses);
                Assert.Contains(BackgroundServiceStatus.Stopped, statuses);
            }
        }

        [Fact]
        [DisplayName("執行期間應依 Interval 週期性呼叫 OnAddTasks")]
        public void RunLoop_InvokesOnAddTasksAtLeastOnce()
        {
            var svc = new TestService { Interval = 50 };
            svc.Initialize();

            try
            {
                svc.Start();
                Assert.True(WaitFor(() => svc.AddTasksCount >= 1, 3000),
                    "OnAddTasks was not invoked within timeout.");
            }
            finally
            {
                svc.Stop();
                WaitFor(() => svc.Status == BackgroundServiceStatus.Stopped);
            }
        }

        [Fact]
        [DisplayName("佇列中的動作應在執行期間被呼叫")]
        public void QueuedAction_IsExecutedDuringRun()
        {
            var svc = new TestService { Interval = 50 };
            svc.Initialize();

            using var executed = new ManualResetEventSlim(false);
            svc.AddTask(_ => executed.Set(), 1000);

            try
            {
                svc.Start();
                Assert.True(executed.Wait(3000),
                    "Queued action was not executed within timeout.");
            }
            finally
            {
                svc.Stop();
                WaitFor(() => svc.Status == BackgroundServiceStatus.Stopped);
            }
        }

        [Fact]
        [DisplayName("Stop 應在 1500ms 內完成狀態轉換，不受迴圈 sleep 阻塞")]
        public void Stop_TransitionsToStoppedPromptly()
        {
            var svc = new TestService { Interval = 50 };
            svc.Initialize();
            svc.Start();
            Assert.True(WaitFor(() => svc.Status == BackgroundServiceStatus.Running),
                "Service did not reach Running state in time.");

            var sw = Stopwatch.StartNew();
            svc.Stop();
            Assert.True(WaitFor(() => svc.Status == BackgroundServiceStatus.Stopped, 1500),
                "Service did not reach Stopped state within 1500ms of Stop().");
            sw.Stop();

            Assert.True(sw.ElapsedMilliseconds < 1500,
                $"Stop took {sw.ElapsedMilliseconds}ms; expected < 1500ms.");
        }

        [Fact]
        [DisplayName("Start 內部拋例外時應呼叫 OnError 並傳入 Start 動作")]
        public void Start_OnStartThrows_CallsOnErrorWithStartAction()
        {
            var boom = new InvalidOperationException("start failure");
            var svc = new TestService { StartThrow = boom };
            svc.Initialize();

            svc.Start();

            Assert.Same(boom, svc.LastError);
            Assert.Equal(BackgroundServiceAction.Start, svc.LastErrorAction);
        }

        [Fact]
        [DisplayName("Stop 內部拋例外時應呼叫 OnError 並傳入 Stop 動作")]
        public void Stop_OnStopThrows_CallsOnErrorWithStopAction()
        {
            var boom = new InvalidOperationException("stop failure");
            var svc = new TestService { Interval = 50, StopThrow = boom };
            svc.Initialize();
            svc.Start();
            Assert.True(WaitFor(() => svc.Status == BackgroundServiceStatus.Running),
                "Service did not reach Running state in time.");

            svc.Stop();

            Assert.Same(boom, svc.LastError);
            Assert.Equal(BackgroundServiceAction.Stop, svc.LastErrorAction);
            WaitFor(() => svc.Status == BackgroundServiceStatus.Stopped);
        }

        [Fact]
        [DisplayName("Stop 在 Start 前呼叫時不應拋例外（_Cts 為 null 分支）")]
        public void Stop_BeforeStart_DoesNotThrow()
        {
            var svc = new TestService();
            svc.Initialize();

            var ex = Record.Exception(() => svc.Stop());

            Assert.Null(ex);
            Assert.Null(svc.LastError);
            Assert.Equal(1, svc.StopCount);
        }

        [Fact]
        [DisplayName("執行期間發生例外應呼叫 OnError 並帶入 Run 動作且服務持續運行")]
        public void RunLoop_ExceptionInOnAddTasks_CallsOnErrorAndContinues()
        {
            var boom = new InvalidOperationException("loop failure");
            var svc = new TestService
            {
                Interval = 50,
                OnAddTasksHook = () => throw boom
            };
            svc.Initialize();

            try
            {
                svc.Start();
                Assert.True(WaitFor(() => svc.LastError != null, 3000),
                    "OnError was not invoked within timeout.");
                Assert.Same(boom, svc.LastError);
                Assert.Equal(BackgroundServiceAction.Run, svc.LastErrorAction);
                Assert.Equal(BackgroundServiceStatus.Running, svc.Status);
            }
            finally
            {
                svc.Stop();
                WaitFor(() => svc.Status == BackgroundServiceStatus.Stopped);
            }
        }
    }
}
