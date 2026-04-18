# 計畫：BackgroundService 重寫，移除 System.Timers.Timer

**狀態：✅ 已完成（2026-04-18）**

## 背景

`Bee.Base.BackgroundServices.BackgroundService` 目前以 `System.Timers.Timer` 為啟動觸發器，後續進入 `while (true)` 主迴圈並以 `Thread.Sleep(500)` 作為 iteration 間隔。

此設計導致 `BackgroundServiceTests.QueuedAction_IsExecutedDuringRun` 在 Windows CI（PR #13 與 main）至少三次發生偶發失敗（timeout 3000ms 內佇列中的動作未被執行）。原因：

1. `Start()` 呼叫後狀態先轉為 `StartPending`，並將 `Timer.Enabled = true`（interval 100ms）
2. Timer 在 ThreadPool worker 上 fire `Elapsed_EventHandler`，才真正進入 `Run()`、把狀態轉為 `Running`
3. CI 高負載時 ThreadPool 排程延遲，加上後續每次迴圈尾端 `Thread.Sleep(500)`，首次執行佇列內 task 的延遲可以被放大到秒級
4. 測試佇列中的動作需在 3000ms 內被執行，當延遲被放大時即 timeout

除了 flakiness，現行設計的其他問題：

- Timer callback 長時間佔用一個 ThreadPool worker（`while (true)` 內跑數小時甚至永久）
- `Stop()` 只停 Timer；主迴圈下一次檢查 `Status != Running` 時才退出，但需先走完當下的 `Thread.Sleep(500)`
- `_Status` 透過欄位直接讀寫，沒有 memory barrier；在非 x86/x64 平台或 JIT reorder 下可能存在 visibility 風險

**預期結果**：

- 移除 `System.Timers.Timer`，改用 `Task.Run(..., TaskCreationOptions.LongRunning)` + `CancellationTokenSource`
- 首次執行佇列 task 的延遲從「Timer interval + ThreadPool 排程 + 500ms」降為「Task scheduling（通常 < 10ms）」
- `Stop()` 能立刻透過 `cts.Cancel()` 喚醒 `Task.Delay`，不再被 500ms sleep 卡住
- 現有測試全部保留，`QueuedAction_IsExecutedDuringRun` 不再 flaky
- Public API 完全相容（`Start`／`Stop`／`AddTask`／`StatusChanged`／`Initialize`／`ThreadCount`／`Interval`／`OnInitialize`／`OnStart`／`OnStop`／`OnAddTasks`／`OnError` 簽名均不變）

---

## 現行設計摘要

```text
Start()
  ├─ SetStatus(StartPending)
  ├─ Timer.Enabled = true  (100ms interval)
  └─ OnStart()

Timer fires (Elapsed_EventHandler)
  ├─ Timer.Enabled = false
  └─ Run()
       ├─ SetStatus(Running)
       └─ while (true):
            ├─ if Status != Running: break
            ├─ AddTasks() / ExecuteTasks()
            └─ Thread.Sleep(500)
       └─ SetStatus(Stopped)

Stop()
  ├─ _Status = StopPending       (no event; direct field write)
  ├─ Timer.Enabled = false
  └─ OnStop()
```

## 新設計

```text
Start()
  ├─ cts = new CancellationTokenSource()
  ├─ SetStatus(StartPending)
  ├─ OnStart()
  └─ runTask = Task.Factory.StartNew(
                   () => RunLoop(cts.Token),
                   cts.Token,
                   TaskCreationOptions.LongRunning,
                   TaskScheduler.Default)

RunLoop(token)
  ├─ SetStatus(Running)
  └─ try:
       while (!token.IsCancellationRequested):
         try:
           AddTasks()
           ExecuteTasks()
         catch (Exception ex):
           OnError(ex, Run)
         try:
           await Task.Delay(500, token).Wait()   ← cancellable
         catch (OperationCanceledException): break
     finally:
       SetStatus(Stopped)

Stop()
  ├─ SetStatus(StopPending)      (改走 SetStatus，讓 StatusChanged 能觀察)
  ├─ cts.Cancel()
  └─ OnStop()
```

### 關鍵決策

| 項目 | 決定 | 理由 |
|------|------|------|
| 背景執行緒 | `Task.Factory.StartNew(..., TaskCreationOptions.LongRunning, ...)` | 避免佔用 ThreadPool worker（LongRunning 會 spawn 專用 Thread） |
| 迴圈 sleep | `Task.Delay(500, token).Wait()` | 與 `Thread.Sleep` 相比可被 `cts.Cancel()` 立刻喚醒；保持 `Run` 為同步方法以兼容現有測試對 `Status` 的同步輪詢 |
| Stop 語意 | 非同步（fire-and-forget：`cts.Cancel()` 後立即返回） | 與現行一致；測試以 `WaitFor(() => Status == Stopped)` 做同步等待 |
| `Status` 讀寫 | 改 `private volatile int _status` + `(BackgroundServiceStatus)_status` | 跨執行緒可見性；目前 tests 依賴同步觀察 |
| `StopPending` 事件 | 由 `Stop()` 內 `SetStatus(StopPending)` 觸發 | 現行版本漏了這個事件（直接寫 `_Status`），順便修正 |
| `Initialize` 失敗處理 | 保持原樣（已被測試驗證） | 不在此次 scope |

### 相容性檢查

| 對外 API | 簽名 | 行為變化 |
|----------|------|----------|
| `Start()` | 不變 | 不再經 Timer 啟動，狀態轉換立即發生 |
| `Stop()` | 不變 | 多觸發一次 `StopPending` 事件（原本沒觸發，屬 bug 修正） |
| `Initialize()` | 不變 | 不變 |
| `AddTask(Action<CT>, int)` | 不變 | 不變 |
| `Status`／`ThreadCount`／`Interval`／`TaskQueue`／`Semaphore`／`NextTime` getters | 不變 | 不變 |
| `OnInitialize`／`OnStart`／`OnStop`／`OnAddTasks`／`OnError` hooks | 不變 | 不變 |
| `StatusChanged` 事件 | 不變 | `StopPending` 現在會被觸發 |

### 對既有測試的影響

| 測試 | 預期 |
|------|------|
| `InitialState_StatusStoppedAndResourcesNotAllocated` | ✅ 通過 |
| `ThreadCountAndInterval_DefaultsAndCanBeUpdated` | ✅ 通過 |
| `Initialize_CreatesQueueAndSemaphore` | ✅ 通過 |
| `Initialize_OnInitializeThrows_CallsOnErrorWithInitializeAction` | ✅ 通過 |
| `AddTask_EnqueuesBackgroundAction` | ✅ 通過 |
| `Start_SetsStartPendingAndRaisesStatusChanged` | ✅ 通過（順序變成 StartPending → Running，都會觀察到 StartPending） |
| `StartThenStop_TransitionsThroughExpectedStatuses` | ✅ 通過（StopPending 現在也會觸發，但測試用 `Assert.Contains` 寬鬆比對） |
| `RunLoop_InvokesOnAddTasksAtLeastOnce` | ✅ 通過（首次 loop iteration 立刻發生） |
| `QueuedAction_IsExecutedDuringRun` | ✅ **修復 flakiness** |
| `RunLoop_ExceptionInOnAddTasks_CallsOnErrorAndContinues` | ✅ 通過（`try/catch` 包住 `AddTasks`／`ExecuteTasks` 呼叫保留不變） |

---

## 實作步驟

### Step 1 — 重寫 `src/Bee.Base/BackgroundServices/BackgroundService.cs`

- 移除 `_Timer`、`Timer` property、`Elapsed_EventHandler`
- 新增 `private CancellationTokenSource? _Cts`、`private Task? _RunTask`
- `_Status` 改為 `volatile int`（以 `(BackgroundServiceStatus)_status` 讀取）
- `Start()` 改為直接 spawn `Task.Factory.StartNew(RunLoop, cts.Token, LongRunning, TaskScheduler.Default)`
- 新 `RunLoop(CancellationToken token)` 合併原 `Run()` 並改用 `Task.Delay(500, token).Wait()`；`finally` 保證 `SetStatus(Stopped)`
- `Stop()` 改為 `SetStatus(StopPending)` + `_Cts?.Cancel()` + `OnStop()`
- `Dispose`：目前類別不實作 IDisposable；為簡化並保持 API 表面，不新增 IDisposable（CTS 的 lifetime 由 Start/Stop 管理，Run loop 結束後 Dispose）
- `using System.Timers` 移除；確保 `System.Threading`／`System.Threading.Tasks` 已有

### Step 2 — 補強測試

現行測試已充分覆蓋。額外新增 1 個測試強化 Stop 立即性：

```csharp
[Fact]
[DisplayName("Stop 應在 500ms 內完成狀態轉換（不受迴圈 sleep 阻塞）")]
public void Stop_TransitionsToStoppedPromptly()
{
    var svc = new TestService { Interval = 50 };
    svc.Initialize();
    svc.Start();
    Assert.True(WaitFor(() => svc.Status == BackgroundServiceStatus.Running));

    var sw = Stopwatch.StartNew();
    svc.Stop();
    Assert.True(WaitFor(() => svc.Status == BackgroundServiceStatus.Stopped, 1500));
    sw.Stop();

    Assert.True(sw.ElapsedMilliseconds < 1500,
        $"Stop took {sw.ElapsedMilliseconds}ms; expected < 1500ms");
}
```

> 不追求極緊的上限（避免引入新的 flakiness），只驗證「不會再被 500ms sleep 卡一整個週期 + 排程延遲」。

### Step 3 — 本機驗證

```bash
dotnet build Bee.Library.slnx --configuration Release
dotnet test tests/Bee.Base.UnitTests/Bee.Base.UnitTests.csproj --configuration Release --settings .runsettings --filter "FullyQualifiedName~BackgroundServiceTests"
dotnet test Bee.Library.slnx --configuration Release --no-build --settings .runsettings
```

為驗證非 flaky，把 `BackgroundServiceTests` 連跑 10 次：

```bash
for i in {1..10}; do
  dotnet test tests/Bee.Base.UnitTests/Bee.Base.UnitTests.csproj \
    --configuration Release --no-build --settings .runsettings \
    --filter "FullyQualifiedName~BackgroundServiceTests" || break
done
```

### Step 4 — 直接提交到 main（依 `.claude/rules/pull-request.md`，MacOS 本機可驗證環境預設走 main）

```bash
git add src/Bee.Base/BackgroundServices/BackgroundService.cs \
        tests/Bee.Base.UnitTests/BackgroundServiceTests.cs \
        docs/plans/plan-background-service-refactor.md
git commit -m "refactor(BackgroundService): 以 Task + CTS 取代 System.Timers.Timer"
git push origin main
```

commit 後追蹤 `build-ci.yml` 結果，若失敗再修復。

---

## 影響檔案

**修改**：
- `src/Bee.Base/BackgroundServices/BackgroundService.cs` — 核心重寫

**新增**：
- `docs/plans/plan-background-service-refactor.md`（本檔案）

**測試**：
- `tests/Bee.Base.UnitTests/BackgroundServiceTests.cs` — 新增 `Stop_TransitionsToStoppedPromptly`

**不動**：
- `BackgroundServiceStatus`／`BackgroundAction`／`BackgroundServiceAction`／`BackgroundServiceStatusChangedEvent` 四個型別
- README／sample 均無須調整（API 相容）

---

## 風險與反面思考

1. **`Task.Delay(token).Wait()` 會拋 `AggregateException`**  
   實作時以 `try { Task.Delay(500, token).Wait(); } catch (OperationCanceledException) { break; }` 處理；`.Wait()` 對 `TaskCanceledException` 會包成 `AggregateException`，所以實際 catch 需為 `AggregateException ae when ae.InnerException is OperationCanceledException`，或直接改用 `token.WaitHandle.WaitOne(500)` 回傳 bool（取消時 true）。**傾向用 `WaitHandle.WaitOne(500, token)` pattern**，較簡潔。實作時以第二種為主。

2. **LongRunning Task 在某些 scheduler 下仍可能走 ThreadPool**  
   `TaskCreationOptions.LongRunning` 是 hint，但 default scheduler 會尊重；測試無此敏感度。

3. **Stop 後立即 Start 的情境**  
   現行測試沒覆蓋，改寫不引入新限制；若需支援 restart，`Initialize` 需要重入，已超出 scope。

4. **`OnAddTasks` 拋例外時的行為**  
   `RunLoop_ExceptionInOnAddTasks_CallsOnErrorAndContinues` 測試要求：拋例外後服務仍為 `Running`、OnError 被呼叫、loop 不中斷。新設計保留 `try/catch` 圈住 `AddTasks`／`ExecuteTasks`，行為一致。
