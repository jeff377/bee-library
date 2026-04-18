# 計畫：SonarCloud MEDIUM 等級問題清理（共 140 筆）

**狀態：🚧 進行中**

## 背景

`https://sonarcloud.io/project/issues?impactSeverities=MEDIUM&issueStatuses=OPEN&id=jeff377_bee-library` 共 140 個開放 MEDIUM 問題。一次全修風險高，分 4 批處理，每批 push 後等 GitHub Actions 與 SonarCloud 重新掃描驗證。

## 批次規劃

### 第 1 批：流程驗證批（21 處，極低風險）— ✅ 已完成（2026-04-18，commit `ad84576`）

機械式修改，無功能變更風險，作為「修 → push → CI → SonarCloud 重掃」流程驗證。

- **CA1825（5 處）** `new T[0]` → `Array.Empty<T>()`
  - src/Bee.Api.Core/System/GetPackageResponse.cs:22
  - tests/Bee.Api.Core.UnitTests/AesPayloadEncryptorTests.cs:30, 48
  - src/Bee.Business/System/GetPackageResult.cs:18
  - src/Bee.Api.Client/DefineAccess/RemoteDefineAccess.cs:132
- **CA1510（12 處）** `if (x == null) throw new ArgumentNullException(nameof(x))` → `ArgumentNullException.ThrowIfNull(x)`
  - src/Bee.Definition/Database/TableSchemaGenerator.cs:20
  - src/Bee.Definition/Layouts/FormLayoutGenerator.cs:21
  - src/Bee.Base/BaseFunc.cs:760
  - src/Bee.Db/DbAccess.cs:100, 126, 127, 151, 294, 366, 468, 469, 494
- **CA2208 / S3928（4 處，同因）** `paramName` 字串大小寫對齊參數名（PascalCase → camelCase）
  - src/Bee.Business/BusinessObjects/SystemBusinessObject.cs:145（`ExpiresIn` → `expiresIn`）
  - src/Bee.Db/DbAccess.cs:152, 495（`Commands` → `commands`）
  - src/Bee.Db/DbAccess.cs:367（`DataTable` → `dataTable`）

### 第 2 批：測試批（33 處）— ✅ 已完成（2026-04-18，commit `e89bf4b`）

- CA1861（21 處）測試常數陣列改 `static readonly` 欄位
- CA1816（4 處）`Dispose()` 加 `GC.SuppressFinalize(this)`
- xUnit2032（8 處）`IsAssignableFrom` → `IsType(..., exactMatch: false)`

### 第 3 批：泛型與 API 批（45 處）— ✅ 已完成（2026-04-18，commit `afdc060`）

> 實際抓取（2026-04-18 接手時）：CA1822 34 處、CA2263 11 處，較 brief 預估多（brief 依早先快照寫為 25 處）。

- CA1822（34 處）方法可改 `static`（公開 API 需確認 ABI 影響）
  - src/：21 處全為 `private`（或公開於 `internal` 類別），ABI 安全
  - tests/：13 處測試輔助方法或 fake handler；反射呼叫的 handler 驗證 `method.Invoke(instance, args)` 對 static 仍可正常運作
- CA2263（11 處）改用泛型 overload
  - `JsonSerializer.Deserialize`、`MessagePackHelper`、`Enum.IsDefined` 等
  - 2 處測試刻意呼叫 non-generic overload 驗證行為，加 `#pragma warning disable CA2263` 並於註解說明原因

### 第 4 批：散落規則批（30 處）— ✅ 已完成（2026-04-18，commit `b552c60`）

一次處理多種低風險零散規則；不含 behavior/breaking 風險項目。

- CA1510（6）、CA1825（3）、CA1861（1）、CA1816（2）、S3971（1）、S1172（1）
- CA1822（3）private 方法改 static
- CA1507/1834/1836/1854/1866/1869/1872/1875、SYSLIB1045、ASP0015、S2589、CA1806、xUnit1042、xUnit1045
- CA1869（2）`JsonSerializerOptions` 抽為 `static readonly`

### 第 5 批：剩餘 12 處（行為/介面敏感，需個別評估）— 📝 擬定中

- **S2166 ApiException**（1）— 類別命名與繼承關係調整，屬 **breaking change**
- **S2925 測試 `Thread.Sleep`**（2）— 需導入 `TimeProvider`/`FakeTimeProvider` 或改寫時序測試
- **S6966 `using` → `await using`**（3）— 需審視 `DbAccess`/`DbConnectionScope` 呼叫鏈的 async 傳遞
- **CA1822 公開 API**（4）— `ApiConnectValidator.Validate`、`TableSchemaGenerator.Generate`、`SelectCommandBuilder.GetSelectContext`、`FormLayoutGenerator.AddLayoutGroups` 屬公開簽名，改 static 或改為 `static class` 均為 breaking change；需產品策略決策
- **xUnit1045 殘留**（2）— `TheoryData<object,...>` 的設計意圖保留，可加 `#pragma` 或採多個 `[Theory]` 拆分

## 紀錄

- 2026-04-18：擬定計畫，開始第 1 批
- 2026-04-18：第 1 批完成（commit `ad84576`），CI 通過、SonarCloud 從 140 → 115（-25，含同處重複計分的清除）
- 2026-04-18：第 2 批完成（commit `e89bf4b`），CI 通過、SonarCloud 從 115 → 82（-33）；xUnit2032 全清
- 2026-04-18：第 3 批完成（commit `afdc060`），CI 通過、SonarCloud 從 82 → 42（-40）；CA1822/CA2263 兩規則於此批大幅清除
- 2026-04-18：第 4 批完成（commit `b552c60`），CI 通過、SonarCloud 從 42 → 12（-30）；15+ 種零散規則一次清理，剩 12 筆均為 behavior/breaking 敏感項
