# 計畫：以環境變數設定測試資料庫連線

## 目標

讓 `tests/Define/DatabaseSettings.xml` 保持空白可安全提交至 GitHub，開發者透過設定單一環境變數 `BEE_TEST_DB_CONNSTR` 即可執行資料庫相關測試。

## 環境變數

| 變數名稱 | 說明 | 範例 |
|----------|------|------|
| `BEE_TEST_DB_CONNSTR` | 完整的資料庫連線字串 | `Server=localhost;Database=BeeTest;User Id=sa;Password=xxx;TrustServerCertificate=True` |

## 變更項目

### 1. 修改 `tests/Bee.Tests.Shared/GlobalFixture.cs`

在現有初始化邏輯（註冊 DbProvider 之後）加入環境變數偵測：

- 讀取 `BEE_TEST_DB_CONNSTR`
- 若有值，程式化新增一筆 `DatabaseItem`（Id = `"common"`），直接使用完整連線字串
- 不使用 `ServerId` 或佔位符機制
- 觸發 `GlobalEvents.RaiseDatabaseSettingsChanged()` 以清除 `DbConnectionManager` 快取

### 2. 新增連線驗證測試 `tests/Bee.Db.UnitTests/DbConnectionTest.cs`

- 使用 `[LocalOnlyFact]` 標記（CI 自動跳過）
- 測試方法：`OpenConnection_WithEnvConnStr_Succeeds`
- 驗證邏輯：透過 `DbFunc.CreateConnection("common")` 建立連線，呼叫 `Open()` 確認可成功連線，最後 `Dispose` 關閉

### 3. `tests/Define/DatabaseSettings.xml` — 不變更

維持現有的空根元素，繼續 tracked in git。

## 不變更範圍

- 不修改核心 library 程式碼（`Bee.Definition`、`Bee.ObjectCaching`、`Bee.Db`）
- 不修改 XML 序列化 / 反序列化流程
- 不修改 `.gitignore`
