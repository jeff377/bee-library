# Bee.Connect 命名空間重構計畫

## 目標

將 Bee.Connect 所有 `.cs` 檔案的命名空間由統一的 `Bee.Connect` 調整為與資料夾結構對應，
同時移除語意不明的 `Common/`、`Interface/` 資料夾，
並將 `Connector/` 資料夾改名為 `Connectors/` 以符合命名空間慣例。

---

## 一、資料夾與命名空間對應

| 資料夾 | 命名空間 |
|--------|---------|
| 專案根目錄 | `Bee.Connect` |
| `ApiServiceProvider/` | `Bee.Connect.ApiServiceProvider` |
| `Connectors/` | `Bee.Connect.Connectors` |
| `DefineAccess/` | `Bee.Connect.DefineAccess` |

---

## 二、各資料夾異動明細

### 2.1 專案根目錄（`Bee.Connect`）

**從 `Common/` 移入：**

| 原始路徑 | 異動 |
|---------|------|
| `Common/ApiClientContext.cs` | 移至根目錄，namespace 不變 |
| `Common/ApiConnectValidator.cs` | 移至根目錄，namespace 不變 |
| `Common/ConnectFunc.cs` | 移至根目錄，namespace 不變 |
| `Common/SyncExecutor.cs` | 移至根目錄，namespace 不變 |

**從 `Common/Common.cs` 拆出（新建檔案）：**

| 型別 | 新檔名 |
|------|--------|
| `ConnectType` | `ConnectType.cs` |
| `SupportedConnectTypes` | `SupportedConnectTypes.cs` |

---

### 2.2 `ApiServiceProvider/`（`Bee.Connect.ApiServiceProvider`）

**從 `Interface/` 移入：**

| 原始路徑 | 異動 |
|---------|------|
| `Interface/IJsonRpcProvider.cs` | 移至 `ApiServiceProvider/`，namespace 改為 `Bee.Connect.ApiServiceProvider` |

**Namespace 更新：**

| 檔案 | 異動 |
|------|------|
| `ApiServiceProvider/LocalApiServiceProvider.cs` | namespace 改為 `Bee.Connect.ApiServiceProvider` |
| `ApiServiceProvider/RemoteApiServiceProvider.cs` | namespace 改為 `Bee.Connect.ApiServiceProvider` |

---

### 2.3 `Connectors/`（`Bee.Connect.Connectors`，資料夾從 `Connector/` 改名）

| 檔案 | 異動 |
|------|------|
| `Connectors/ApiConnector.cs` | namespace 改為 `Bee.Connect.Connectors` |
| `Connectors/FormApiConnector.cs` | namespace 改為 `Bee.Connect.Connectors` |
| `Connectors/SystemApiConnector.cs` | namespace 改為 `Bee.Connect.Connectors` |

---

### 2.4 `DefineAccess/`（`Bee.Connect.DefineAccess`）

| 檔案 | 異動 |
|------|------|
| `DefineAccess/RemoteDefineAccess.cs` | namespace 改為 `Bee.Connect.DefineAccess` |

---

## 三、移除的資料夾

| 資料夾 | 原因 |
|--------|------|
| `Common/` | 語意不明，內容已分散至根目錄 |
| `Interface/` | 非 .NET 慣例，介面已移至實作所在的子資料夾 |

**改名資料夾：**

| 原名 | 新名 | 原因 |
|------|------|------|
| `Connector/` | `Connectors/` | 與命名空間 `Bee.Connect.Connectors` 對齊（複數） |

---

## 四、影響範圍

### Bee.Connect 內部
- 修改命名空間宣告：7 個 `.cs`（ApiServiceProvider × 2、Connectors × 3、DefineAccess × 1、IJsonRpcProvider × 1）
- 新建檔案（從 `Common/Common.cs` 拆出）：2 個（`ConnectType.cs`、`SupportedConnectTypes.cs`）
- 移動檔案：5 個（Common/ → 根目錄 × 4、Interface/ → ApiServiceProvider/ × 1）
- 刪除檔案：`Common/Common.cs`
- 移除空資料夾：`Common/`、`Interface/`
- 改名資料夾：`Connector/` → `Connectors/`

### 下游專案（需補 `using`）
下列專案直接引用 Bee.Connect，需依使用的型別補上對應 `using`：

| 專案 | 類型 |
|------|------|
| `tests/Bee.Connect.UnitTests` | 測試 |
| `samples/` 相關專案 | 範例 |

---

## 五、執行順序

1. 修改 Bee.Connect 內部所有命名空間宣告與檔案位置
2. 執行 `dotnet build src/Bee.Connect/Bee.Connect.csproj` 確認專案本身無錯誤
3. 逐一更新下游專案的 `using` 陳述式
4. 執行 `dotnet build` 全方案建置確認
5. 執行 `dotnet test` 確認測試全數通過
