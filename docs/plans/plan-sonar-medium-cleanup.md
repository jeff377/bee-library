# 計畫：SonarCloud MEDIUM 等級問題清理（共 140 筆）

**狀態：🚧 進行中**

## 背景

`https://sonarcloud.io/project/issues?impactSeverities=MEDIUM&issueStatuses=OPEN&id=jeff377_bee-library` 共 140 個開放 MEDIUM 問題。一次全修風險高，分 4 批處理，每批 push 後等 GitHub Actions 與 SonarCloud 重新掃描驗證。

## 批次規劃

### 第 1 批：流程驗證批（21 處，極低風險）

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

### 第 2 批：測試批（33 處）

- CA1861（21 處）測試常數陣列改 `static readonly` 欄位
- CA1816（4 處）`Dispose()` 加 `GC.SuppressFinalize(this)`
- xUnit2032（8 處）`IsAssignableFrom` → `IsType(..., exactMatch: false)`

### 第 3 批：泛型與 API 批（25 處）

- CA1822（14 處）方法可改 `static`（公開 API 需確認 ABI 影響）
- CA2263（11 處）改用泛型 overload

### 第 4 批：行為敏感批（個別評估）

- S6966（3 處）`using` → `await using`
- CA1869（2 處）`JsonSerializerOptions` 快取
- S2925（2 處）測試 `Thread.Sleep` 改用其他機制
- 其他散落各 1-2 筆規則

### 補抓批

API 預設只回 100 筆，第 1 輪後再抓剩餘 40 筆，併入後續批次。

## 紀錄

- 2026-04-18：擬定計畫，開始第 1 批
