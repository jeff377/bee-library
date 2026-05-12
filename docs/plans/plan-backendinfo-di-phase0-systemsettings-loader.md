# 計畫：Phase 0 — 抽離 SystemSettings boot-time 讀檔職責（SystemSettingsLoader）

**狀態：✅ 已完成（2026-05-12）**

> 本文件為主計畫 [plan-backendinfo-to-di-migration.md](plan-backendinfo-to-di-migration.md) 的 **Phase 0** sub-plan，獨立可 ship。

## 背景

### 主計畫定位

Phase 0 是整個 BackendInfo → DI 遷移的前置清理。在動到 BackendInfo 任何屬性訪問點之前，先解掉一個結構性耦合：**`IDefineAccess` 同時負責「啟動期讀 `SystemSettings.xml`」與「runtime 服務」兩件事**。後續 Phase 2 要把 `IDefineAccess` 改成乾淨的 runtime DI 服務時，必須先讓「啟動期讀檔」不再依賴它。

### chicken-and-egg 現況

目前的啟動流程（以 `GlobalFixture` 為例，production 啟動路徑同形）：

```csharp
// 1. 設定 DefinePath（純設定字串，無服務依賴）
BackendInfo.DefinePath = Path.Combine(repoRoot, "tests", "Define");

// 2. 手動 new 一個臨時 DefineAccess 才能讀 XML
BackendInfo.DefineAccess = new LocalDefineAccess();

// 3. 透過 DefineAccess.GetSystemSettings() 走 SystemSettingsCache → XmlCodec 讀檔
var settings = BackendInfo.DefineAccess.GetSystemSettings();

// 4. 用讀到的 settings 初始化
SysInfo.Initialize(settings.CommonConfiguration);
BackendInfo.Initialize(settings.BackendConfiguration, autoCreateMasterKey: true);
//    ↑ InitializeComponents 內部會「再次」用反射建立真正的 DefineAccess，蓋掉步驟 2 的臨時實例
```

問題：

- **`IDefineAccess` 必須在 `BackendInfo.Initialize` 之前就存在**，但它「正式版」又是 `Initialize` 的產物 —— 邏輯閉環
- 步驟 2 是「為了讀檔而 new 一個臨時 service instance」的 workaround，所有啟動點都得照搬
- `SystemSettingsCache.CreateInstance` 內部直接 `XmlCodec.DeserializeFromFile<SystemSettings>(filePath)` —— 真正讀檔的程式碼其實**根本沒用到 `IDefineAccess` 的任何介面方法**，只是被包在 cache 包在 service 內

### 現有路徑分析

啟動期讀檔的實際 call chain：

```
BackendInfo.DefineAccess.GetSystemSettings()        // IDefineAccess 介面
  → CacheContainer.SystemSettings.Get()             // ObjectCache<SystemSettings>
    → SystemSettingsCache.CreateInstance()          // 第一次或 cache miss 時
      → XmlCodec.DeserializeFromFile<SystemSettings>(filePath)   // ← 真正讀檔的一行
```

中間三層（IDefineAccess、CacheContainer、SystemSettingsCache）為了 runtime 高頻存取做的快取設計，**對啟動期讀一次的需求是過度設計**。啟動期只需「給一個檔案路徑、回一個 SystemSettings 物件」。

## 目標

1. 新增 `SystemSettingsLoader`（純 static 類別，位於 `Bee.Definition`），提供 boot-time 直接讀 `SystemSettings.xml` 的能力
2. 修改啟動路徑（`GlobalFixture` 與所有現存 boot 點），改為先用 `SystemSettingsLoader.Load(...)` 取得 settings 再呼叫 `BackendInfo.Initialize`
3. **不刪除** `LocalDefineAccess.GetSystemSettings()` 與 `SystemSettingsCache` —— runtime 仍透過原路徑高頻存取（cache + 檔案變更監控）；本階段只做「啟動期分離」
4. 為 `SystemSettingsLoader` 補單元測試

## 非目標（本 phase 不做）

- 不改 `BackendInfo` 的任何屬性訪問點（含 `BackendInfo.DefinePath`、`BackendInfo.DefineAccess` 等）
- 不改 `IDefineAccess` 介面定義
- 不刪除 `SystemSettingsCache`（runtime path 仍需）
- 不改 `BackendInfo.Initialize` 的簽章（仍接收 `BackendConfiguration`，由呼叫端從 `SystemSettings.BackendConfiguration` 抽出）
- 不引入 DI 容器（DI 改造在 Phase 1 開始）

### 關於 `BackendInfo.DefinePath` 的後續安排

`BackendInfo.DefinePath` 是跨層使用的「資料目錄根」配置值（`DefinePathInfo`、`MasterKeyProvider` 與多個測試 fixture 都讀它），**Phase 0 完全不動**。`SystemSettingsLoader.Load()` 取得檔案路徑時透過既有的 `DefinePathInfo.GetSystemSettingsFilePath()` facade，等於「站在現有路徑解析機制之上」，不新增對 `BackendInfo.DefinePath` 的直接引用。

預計在 **Phase 2** 處理 `BackendInfo.DefinePath` 的移除（與 `IDefineAccess`、`MasterKeyProvider`、`DatabaseSettings` 一起，因為它們同屬 `Bee.Definition` 層 + Security 子層）。屆時引入 `PathOptions` 之類的注入式配置，一次轉完所有讀取點。Phase 0 期間 `BackendInfo.DefinePath` 行為與現況完全相同。

## 設計

### 新增類別

**檔案**：`src/Bee.Definition/SystemSettingsLoader.cs`

```csharp
namespace Bee.Definition
{
    /// <summary>
    /// Loads <see cref="SystemSettings"/> from disk during application bootstrap.
    /// Unlike runtime access (which goes through <c>IDefineAccess</c> + cache),
    /// this loader has no service dependencies and is safe to call before any
    /// other framework component is initialized.
    /// </summary>
    /// <remarks>
    /// Boot-time path:
    /// <code>
    /// BackendInfo.DefinePath = "...";
    /// var settings = SystemSettingsLoader.Load();
    /// BackendInfo.Initialize(settings.BackendConfiguration);
    /// </code>
    /// Runtime path (unchanged) continues to use
    /// <c>BackendInfo.DefineAccess.GetSystemSettings()</c> for cached access.
    /// </remarks>
    public static class SystemSettingsLoader
    {
        /// <summary>
        /// Loads <see cref="SystemSettings"/> from the file path derived from
        /// <see cref="BackendInfo.DefinePath"/> via <see cref="DefinePathInfo.GetSystemSettingsFilePath"/>.
        /// </summary>
        /// <exception cref="FileNotFoundException">
        /// Thrown when the resolved file does not exist.
        /// </exception>
        public static SystemSettings Load()
            => Load(DefinePathInfo.GetSystemSettingsFilePath());

        /// <summary>
        /// Loads <see cref="SystemSettings"/> from an explicit file path.
        /// </summary>
        /// <param name="filePath">Absolute or relative path to the XML file.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="filePath"/> is null or whitespace.
        /// </exception>
        /// <exception cref="FileNotFoundException">
        /// Thrown when the file does not exist.
        /// </exception>
        public static SystemSettings Load(string filePath)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

            if (!File.Exists(filePath))
                throw new FileNotFoundException(
                    $"SystemSettings file not found: {filePath}", filePath);

            return XmlCodec.DeserializeFromFile<SystemSettings>(filePath)!;
        }
    }
}
```

### 設計要點

- **純 static**：無建構式、無欄位、無 lifecycle —— 啟動最早期可呼叫
- **零服務依賴**：不引用 `IDefineAccess` / `CacheContainer` / 任何 BackendInfo.X 服務屬性
- **重用 `DefinePathInfo`**：路徑邏輯不重複（`DefinePathInfo.GetSystemSettingsFilePath()` 已是 static helper）
- **重用 `XmlCodec`**：序列化邏輯不重複（與 `SystemSettingsCache.CreateInstance` 同樣呼叫 `XmlCodec.DeserializeFromFile`）
- **無快取**：boot 期讀一次後資料就傳給 `BackendInfo.Initialize`，無重複讀檔需求
- **兩個 overload**：`Load()` 預設用 `BackendInfo.DefinePath`；`Load(filePath)` 給測試或特殊場景用

### 與 runtime 路徑共存

| 路徑 | 用途 | 經由 | 在 Phase 0 後狀態 |
|------|------|------|---------|
| `SystemSettingsLoader.Load()` | **boot 期讀一次**（取得初始化所需配置） | 直接讀檔 | **新增** |
| `BackendInfo.DefineAccess.GetSystemSettings()` | **runtime 高頻存取**（含 file watcher 觸發 cache 失效） | `IDefineAccess` → `CacheContainer` → `SystemSettingsCache` → 檔案 | **不變** |

`SystemSettings.xml` 同一份檔案，兩條讀法。boot 路徑只在 Initialize 前讀；runtime 路徑在 Initialize 之後（`IDefineAccess` 已備妥）才會被觸碰。沒有競爭、沒有 cache 一致性問題。

## 改動清單

### 新增

| 檔案 | 內容 |
|------|------|
| `src/Bee.Definition/SystemSettingsLoader.cs` | 新類別（上方設計） |
| `tests/Bee.Definition.UnitTests/SystemSettingsLoaderTests.cs` | 對應測試（見下節） |

### 修改

| 檔案 | 修改 |
|------|------|
| `tests/Bee.Tests.Shared/GlobalFixture.cs` | `InitializeOnce()` 中將 `var settings = BackendInfo.DefineAccess.GetSystemSettings();` 改為 `var settings = SystemSettingsLoader.Load();`，並加上中文註解說明 boot-time vs runtime 兩條路徑分工 |

**注意**：`BackendInfo.DefineAccess = new LocalDefineAccess();` 那行（line 59）**保留**，因為前面的 `RegisterSqlServer()`、`EnsureFallbackCommonDatabaseItem()` 等仍需透過 `DefineAccess.GetDatabaseSettings()` 寫入 `DatabaseSettings.Items`。這條依賴會在 Phase 2 處理（屆時 `DatabaseSettings.Items` 改為注入式 `IDatabaseSettingsProvider`）。Phase 0 只解 SystemSettings 那一條 boot-time chicken-and-egg。

### 既有測試影響

`tests/Bee.Definition.UnitTests/BackendInfoTests.cs` 與其他 fixture-based 測試應**無功能變動**，因為 `BackendInfo.Initialize` 簽章與行為不變，只是啟動前序步驟更乾淨。如有測試明確驗證「Initialize 前手動 set DefineAccess」的舊行為（不預期有），需順手調整。

### 不需改

- `src/Bee.Definition/BackendInfo.cs` — 不動
- `src/Bee.ObjectCaching/LocalDefineAccess.cs` — 不動（runtime 用）
- `src/Bee.ObjectCaching/Define/SystemSettingsCache.cs` — 不動（runtime 用）
- `src/Bee.Definition/DefinePathInfo.cs` — 不動（兩條路徑共用）

## 測試策略

### 新增測試（`SystemSettingsLoaderTests.cs`）

依測試規範（[testing.md](../../.claude/rules/testing.md)）撰寫，命名 `<方法>_<情境>_<預期>`：

1. `Load_ValidFile_ReturnsSettings` — 給有效檔案路徑，回傳非 null 且結構正確
2. `Load_FileNotFound_ThrowsFileNotFoundException` — 給不存在的路徑，丟 `FileNotFoundException`
3. `Load_NullPath_ThrowsArgumentException` — 給 null
4. `Load_WhitespacePath_ThrowsArgumentException` — 給空白字串
5. `Load_NoArgs_UsesDefinePathInfo` — 預設 overload 走 `BackendInfo.DefinePath` —— 用 `TempDefinePath` 暫時切到測試用目錄

### 測試 fixture 隔離

- 涉及檔案讀寫的測試用 `TempDefinePath`（[tests/Bee.Tests.Shared/TempDefinePath.cs](../../tests/Bee.Tests.Shared/TempDefinePath.cs)）暫時切換 `BackendInfo.DefinePath`，避免動到共享的 `tests/Define/SystemSettings.xml`
- 寫 fixture XML 內容用 `XmlCodec.SerializeToFile(new SystemSettings { ... }, path)` 在 temp 目錄產出，再呼叫 `Load(path)` 驗證 round-trip

## 驗收標準

- [ ] `SystemSettingsLoader` 類別存在於 `Bee.Definition`，含兩個 overload
- [ ] 5 個 unit test 全綠
- [ ] `GlobalFixture.InitializeOnce()` 改為使用 `SystemSettingsLoader.Load()`
- [ ] `./test.sh` 全套測試通過（含原有 159 個 BackendInfo 相關測試）
- [ ] `dotnet build --configuration Release` 通過（含 `TreatWarningsAsErrors`）
- [ ] 程式碼中保留至少一條中文註解（XML 文件）說明 boot vs runtime 兩條路徑的分工

## 風險與緩解

| 風險 | 緩解 |
|------|------|
| 既有測試依賴「先 set DefineAccess」的隱含順序 | grep `BackendInfo.DefineAccess = ` 全 repo 確認；目前找到的點都是 `GlobalFixture` 與測試專用，可一併調整 |
| 啟動期讀檔失敗的錯誤訊息變化 | 新類別與既有 `SystemSettingsCache.CreateInstance` 都丟 `FileNotFoundException`；訊息文字微調但型別一致，不破壞既有 `Assert.Throws<FileNotFoundException>` 測試 |
| 與後續 phase 的 XML 結構演進 | Phase 0 不動 XML 結構與 `SystemSettings` POCO；後續 phase 改 schema 時 `SystemSettingsLoader` 自然吃新結構（反序列化目標型別自動切換）；無相依衝突 |

## 提交策略

依使用者規範（[pull-request.md](../../.claude/rules/pull-request.md)），本機可驗證環境採直接提交 main：

1. 本機跑 `dotnet build --configuration Release` + `./test.sh` 通過
2. 單一 commit 包含新增類別 + 測試 + `GlobalFixture` 改動
3. commit message 格式遵循專案慣例（`feat(definition): 抽離 SystemSettings boot-time 讀檔...`）
4. push 後 CI `build-ci.yml` 仍會跑一次，若失敗依規範處理

預估 diff 量：< 200 lines（新增類別 ~50、新增測試 ~80、GlobalFixture 修改 ~5）。

## 完成後狀態

- 主計畫頂部「Sub-plan 進度」表 Phase 0 狀態更新為 ✅ 已完成
- Phase 2 動工時可確認：boot 期不再需要 `IDefineAccess`，後者可純粹作為 runtime 服務由 DI 管理
- `GlobalFixture` 啟動序列簡化一行，為 Phase 5 測試 fixture 重寫做暖身
