# Bee.Api.Core 命名空間重構計畫

## 目標

將 Bee.Api.Core 所有 `.cs` 檔案的命名空間由統一的 `Bee.Api.Core` 調整為與資料夾結構對應，
同時移除語意不明的 `Common/`、`Interface/` 資料夾，
並將 `Common.cs` 多型別檔案拆分為獨立檔案。

---

## 一、資料夾與命名空間對應

| 資料夾 | 命名空間 |
|--------|---------|
| 專案根目錄 | `Bee.Api.Core` |
| `ApiAccessValidator/` | `Bee.Api.Core`（不建立子命名空間） |
| `Authorization/` | `Bee.Api.Core.Authorization` |
| `JsonRpc/` | `Bee.Api.Core.JsonRpc` |
| `MessagePack/` | `Bee.Api.Core.MessagePack` |
| `Transformer/` | `Bee.Api.Core.Transformer` |

---

## 二、各資料夾異動明細

### 2.1 專案根目錄（`Bee.Api.Core`）

**從 `Common/` 移入（namespace 不變）：**

| 原始路徑 | 異動 |
|---------|------|
| `Common/ApiServiceOptions.cs` | 移至根目錄，namespace 不變 |

**從 `Common/Common.cs` 拆出（新建檔案）：**

| 型別 | 新檔名 | namespace |
|------|--------|-----------|
| `ApiHeaders` | `ApiHeaders.cs` | `Bee.Api.Core` |
| `PayloadFormat` | `PayloadFormat.cs` | `Bee.Api.Core` |

---

### 2.2 `ApiAccessValidator/`（`Bee.Api.Core`，namespace 不變）

| 檔案 | 異動 |
|------|------|
| `ApiAccessValidator/ApiAccessValidator.cs` | namespace 不變（`Bee.Api.Core`） |
| `ApiAccessValidator/ApiCallContext.cs` | namespace 不變（`Bee.Api.Core`） |

---

### 2.3 `Authorization/`（`Bee.Api.Core.Authorization`）

| 檔案 | 異動 |
|------|------|
| `Authorization/ApiAuthorizationContex.cs` | namespace 改為 `Bee.Api.Core.Authorization` |
| `Authorization/ApiAuthorizationResult.cs` | namespace 改為 `Bee.Api.Core.Authorization` |
| `Authorization/ApiAuthorizationValidator.cs` | namespace 改為 `Bee.Api.Core.Authorization` |

**從 `Interface/` 移入：**

| 原始路徑 | 異動 |
|---------|------|
| `Interface/IApiAuthorizationValidator.cs` | 移至 `Authorization/`，namespace 改為 `Bee.Api.Core.Authorization` |

---

### 2.4 `JsonRpc/`（`Bee.Api.Core.JsonRpc`）

| 檔案 | 異動 |
|------|------|
| `JsonRpc/ApiPayload.cs` | namespace 改為 `Bee.Api.Core.JsonRpc` |
| `JsonRpc/ApiPayloadConverter.cs` | namespace 改為 `Bee.Api.Core.JsonRpc` |
| `JsonRpc/JsonRpcError.cs` | namespace 改為 `Bee.Api.Core.JsonRpc` |
| `JsonRpc/JsonRpcExecutor.cs` | namespace 改為 `Bee.Api.Core.JsonRpc` |
| `JsonRpc/JsonRpcParams.cs` | namespace 改為 `Bee.Api.Core.JsonRpc` |
| `JsonRpc/JsonRpcRequest.cs` | namespace 改為 `Bee.Api.Core.JsonRpc` |
| `JsonRpc/JsonRpcResponse.cs` | namespace 改為 `Bee.Api.Core.JsonRpc` |
| `JsonRpc/JsonRpcResult.cs` | namespace 改為 `Bee.Api.Core.JsonRpc` |

**從 `Common/` 移入：**

| 原始路徑 | 異動 |
|---------|------|
| `Common/JsonRpcException.cs` | 移至 `JsonRpc/`，namespace 改為 `Bee.Api.Core.JsonRpc` |

**從 `Common/Common.cs` 拆出（新建檔案）：**

| 型別 | 新檔名 | namespace |
|------|--------|-----------|
| `JsonRpcErrorCode` | `JsonRpc/JsonRpcErrorCode.cs` | `Bee.Api.Core.JsonRpc` |

---

### 2.5 `MessagePack/`（`Bee.Api.Core.MessagePack`）

| 檔案 | 異動 |
|------|------|
| `MessagePack/CollectionBaseFormatter.cs` | namespace 改為 `Bee.Api.Core.MessagePack` |
| `MessagePack/DataSetFormatter.cs` | namespace 改為 `Bee.Api.Core.MessagePack` |
| `MessagePack/DataTableFormatter.cs` | namespace 改為 `Bee.Api.Core.MessagePack` |
| `MessagePack/FormatterResolver.cs` | namespace 改為 `Bee.Api.Core.MessagePack` |
| `MessagePack/MessagePackHelper.cs` | namespace 改為 `Bee.Api.Core.MessagePack` |
| `MessagePack/SerializableDataColumn.cs` | namespace 改為 `Bee.Api.Core.MessagePack` |
| `MessagePack/SerializableDataRelation.cs` | namespace 改為 `Bee.Api.Core.MessagePack` |
| `MessagePack/SerializableDataRow.cs` | namespace 改為 `Bee.Api.Core.MessagePack` |
| `MessagePack/SerializableDataSet.cs` | namespace 改為 `Bee.Api.Core.MessagePack` |
| `MessagePack/SerializableDataTable.cs` | namespace 改為 `Bee.Api.Core.MessagePack` |

---

### 2.6 `Transformer/`（`Bee.Api.Core.Transformer`）

| 檔案 | 異動 |
|------|------|
| `Transformer/AesPayloadEncryptor.cs` | namespace 改為 `Bee.Api.Core.Transformer` |
| `Transformer/ApiPayloadOptionsFactory.cs` | namespace 改為 `Bee.Api.Core.Transformer` |
| `Transformer/ApiPayloadTransformer.cs` | namespace 改為 `Bee.Api.Core.Transformer` |
| `Transformer/BinaryFormatterPayloadSerializer.cs` | namespace 改為 `Bee.Api.Core.Transformer` |
| `Transformer/GZipPayloadCompressor.cs` | namespace 改為 `Bee.Api.Core.Transformer` |
| `Transformer/MessagePackPayloadSerializer.cs` | namespace 改為 `Bee.Api.Core.Transformer` |
| `Transformer/NoCompressionCompressor.cs` | namespace 改為 `Bee.Api.Core.Transformer` |
| `Transformer/NoEncryptionEncryptor.cs` | namespace 改為 `Bee.Api.Core.Transformer` |

**從 `Interface/` 移入：**

| 原始路徑 | 異動 |
|---------|------|
| `Interface/IApiPayloadCompressor.cs` | 移至 `Transformer/`，namespace 改為 `Bee.Api.Core.Transformer` |
| `Interface/IApiPayloadEncryptor.cs` | 移至 `Transformer/`，namespace 改為 `Bee.Api.Core.Transformer` |
| `Interface/IApiPayloadSerializer.cs` | 移至 `Transformer/`，namespace 改為 `Bee.Api.Core.Transformer` |
| `Interface/IApiPayloadTransformer.cs` | 移至 `Transformer/`，namespace 改為 `Bee.Api.Core.Transformer` |

---

## 三、移除的資料夾

| 資料夾 | 原因 |
|--------|------|
| `Common/` | 語意不明，內容已分散至根目錄或對應子資料夾 |
| `Interface/` | 非 .NET 慣例，介面已移至實作所在的子資料夾 |

---

## 四、影響範圍

### Bee.Api.Core 內部
- 修改命名空間宣告：33 個 `.cs`（不含 `ApiAccessValidator/` 2 個及 `ApiServiceOptions.cs` 等 namespace 不變的檔案）
- 補充內部 `using`：子命名空間之間互相引用時，需在各檔案補上對應 `using`（例如 `JsonRpcExecutor` 使用 `Bee.Api.Core.Transformer` 的介面）
- 新建檔案（從 `Common.cs` 拆出）：3 個（`ApiHeaders.cs`, `PayloadFormat.cs`, `JsonRpcErrorCode.cs`）
- 刪除檔案：`Common/Common.cs`
- 移除空資料夾：`Common/`、`Interface/`

### 下游專案（需補 `using`）

下列 13 個檔案使用 `using Bee.Api.Core;`，需依使用的型別補上對應子命名空間：

| 專案 | 檔案 |
|------|------|
| `src/Bee.Api.AspNetCore` | `Controllers/ApiServiceController.cs` |
| `src/Bee.Api.AspNet` | `HttpModules/ApiServiceModule.cs` |
| `src/Bee.Connect` | `Connector/ApiConnector.cs`<br>`Connector/FormApiConnector.cs`<br>`Connector/SystemApiConnector.cs`<br>`ApiServiceProvider/LocalApiServiceProvider.cs`<br>`ApiServiceProvider/RemoteApiServiceProvider.cs`<br>`Interface/IJsonRpcProvider.cs` |
| `tests/Bee.Api.AspNetCore.UnitTests` | `ApiAspNetCoreTest.cs` |
| `samples/JsonRpcServerAspNet` | `Global.asax.cs` |
| `samples/JsonRpcServer` | `Extensions/BackendExtensions.cs` |
| `samples/JsonRpcClient` | `frmMainForm.cs`<br>`frmTraceViewer.cs` |

---

## 五、執行順序

1. 修改 Bee.Api.Core 內部所有命名空間宣告與檔案位置
2. 執行 `dotnet build src/Bee.Api.Core/Bee.Api.Core.csproj` 確認專案本身無錯誤
3. 逐一更新下游專案（Bee.Api.AspNetCore、Bee.Api.AspNet、Bee.Connect）的 `using` 陳述式
4. 更新 samples 的 `using` 陳述式
5. 執行 `dotnet build` 全方案建置確認
6. 執行 `dotnet test` 確認測試全數通過
