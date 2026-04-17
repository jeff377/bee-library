# 計畫：修正 SonarCloud Code Smells

## 背景

SonarCloud 掃描（main branch）Quality Gate **PASSED**，但仍有 **129 個 Code Smells**（Maintainability A 級）待改善。
本計畫依優先順序分批修正，每批 build 驗證後 commit。

---

## 批次一：Critical — S1006 override 方法缺少 default 參數值

SonarCloud 最新掃描（2026-04-17）新增的 Critical 問題，需在 override 方法中加入與 abstract/interface 相同的 default 值。

| 檔案 | 行號 | 問題 |
|------|------|------|
| `src/Bee.Db/Query/WhereBuilder.cs` | L31 | override `Build` 方法需加 `= null` default 值 |
| `src/Bee.Db/Query/SortBuilder.cs` | L31 | override `Build` 方法需加 `= null` default 值 |
| `src/Bee.Db/Providers/SqlServer/SqlFormCommandBuilder.cs` | L50 × 2 | override `BuildSelectCommand` 需加 default 值 |

**修正方式**：在 override 方法的參數宣告中，補上與父類別/介面相同的 `= null`（或對應預設值）。

---

## 批次二：Major — S927 參數名不符合介面宣告

| 檔案 | 行號 | 問題 |
|------|------|------|
| `src/Bee.Business/BusinessObjects/SystemBusinessObject.cs` | L145 | 參數名 `ExpiresIn` 不在介面宣告中 |
| `src/Bee.Db/DbAccess.cs` | L152 | 參數名 `Commands` |
| `src/Bee.Db/DbAccess.cs` | L318 | 參數名 `DataTable` |
| `src/Bee.Db/DbAccess.cs` | L487 | 參數名 `Commands` |

**修正方式**：將 override/implementation 方法的參數名稱改為與介面宣告完全一致。

---

## 批次三：Major — 其他 Major 問題

| 檔案 | 行號 | 規則 | 說明 |
|------|------|------|------|
| `src/Bee.Db/Logging/DbAccessLogger.cs` | L70, L90 | S2StringBuilder | StringBuilder 建立後 `.ToString()` 從未被呼叫，整個 StringBuilder 可移除 |
| `src/Bee.Base/BaseFunc.cs` | L512 | S1066 | 合併巢狀 if |
| `src/Bee.Base/BaseFunc.cs` | L95 | S6562 | 建立 DateTime 未指定 DateTimeKind，加上 `DateTimeKind.Unspecified` 或 `.Local` |
| `src/Bee.Base/BaseFunc.cs` | L702 | S127 | for loop 不應在 body 中修改停止條件變數 `i` |
| `src/Bee.Base/DateTimeFunc.cs` | L17, L27, L46 | S6562/S6580 | 建立 DateTime 未指定 DateTimeKind；DateTime.Parse 未指定 format provider |
| `src/Bee.Base/Serialization/DataTableJsonConverter.cs` | L354 | S6580 | DateTime.Parse 未指定 format provider |
| `src/Bee.Business/BusinessObjects/SystemBusinessObject.cs` | L194, L228 | S1066 | 合併巢狀 if |
| `src/Bee.Api.Client/Connectors/SystemApiConnector.cs` | L105 | S112 | 不應 throw `System.ApplicationException`，改用自訂例外或 `InvalidOperationException` |
| `src/Bee.Base/ApiException.cs` | L11 | S3925 | 類別名含 "Exception" 但未繼承 `Exception`，或繼承後應命名為 `ApiException` |
| `src/Bee.Base/StrFunc.cs` | L256 | S4144 | 方法實作與 `Append` 完全相同，合併或重新實作 |
| `src/Bee.Db/ILMapper.cs` | L17 | S2743 | Generic type 中的 static field 不共享於不同 close constructed types |

---

## 批次四：Minor — S3604 移除多餘的欄位初始化

欄位在建構函式中已明確賦值，不需要 `= null` / `= string.Empty` 等初始化。

| 檔案 | 行號 |
|------|------|
| `src/Bee.Api.Client/ApiServiceProvider/LocalApiServiceProvider.cs` | L24 |
| `src/Bee.Api.Client/DefineAccess/RemoteDefineAccess.cs` | L18, L19 |
| `src/Bee.Api.Core/JsonRpc/JsonRpcExecutor.cs` | L31 |
| `src/Bee.Db/Schema/TableSchemaComparer.cs` | L26 |

**修正方式**：刪除多餘的 `= null` 或 `= string.Empty` field initializer。

---

## 批次五：Minor — S2325 方法應改為 static

以下方法不存取 `this`，應加上 `static` 修飾詞。

| 檔案 | 方法 |
|------|------|
| `src/Bee.Api.AspNetCore/Controllers/ApiServiceController.cs` | `IsDevelopment` 屬性 |
| `src/Bee.Api.Client/ApiConnectValidator.cs` | `ValidateSystemSettings`, `ValidateRemote` |
| `src/Bee.Api.Client/Connectors/ApiConnector.cs` | `RestoreResponsePayload`, `TraceRequest`, `TraceResponse` |
| `src/Bee.Api.Core/JsonRpc/JsonRpcExecutor.cs` | `ParseMethod` |
| `src/Bee.Base/IPValidator.cs` | `IsWildcardMatch`, `IsInSubnet` |
| `src/Bee.Base/Security/PasswordHasher.cs` | `HashPassword`, `VerifyPassword` |
| `src/Bee.Business/BusinessObjects/FormExecFuncHandler.cs` | `Hello` |
| `src/Bee.Business/BusinessObjects/SystemBusinessObject.cs` | `GetDefineCore`, `SaveDefineCore` |
| `src/Bee.Business/BusinessObjects/SystemExecFuncHandler.cs` | `UpgradeTableSchema`, `TestConnection` |
| `src/Bee.Db/Providers/SqlServer/SqlCreateTableCommandBuilder.cs` | `GetDropTableCommandText`, `ConverDbType`, `GetDefaultValue`, `GetIndexCommandText` |
| `src/Bee.Db/Providers/SqlServer/SqlTableSchemaProvider.cs` | `ParseIndexes`, `ParseDBDefaultValue` |
| `src/Bee.Db/Query/SelectBuilder.cs` | `GetSelectFields` |
| `src/Bee.Db/Query/SelectContextBuilder.cs` | `GetSingleRelationFieldMappings` |
| `src/Bee.Definition/Database/TableSchemaGenerator.cs` | `AddIndexes` |

---

## 批次六：Minor — S3267 改用 LINQ `.Where()`

| 檔案 | 行號 |
|------|------|
| `src/Bee.Base/AssemblyLoader.cs` | L26 |
| `src/Bee.Base/Collections/KeyCollectionBase.cs` | L103 |
| `src/Bee.Base/IPValidator.cs` | L71, L88 |
| `src/Bee.Base/StrFunc.cs` | L117 |
| `src/Bee.Base/SysInfo.cs` | L73 |
| `src/Bee.Db/Providers/SelectCommandBuilder.cs` | L168 |
| `src/Bee.Db/Query/SelectContextBuilder.cs` | L175 |
| `src/Bee.Db/Query/TableJoinCollection.cs` | L19 |
| `src/Bee.Db/Schema/TableSchemaComparer.cs` | L121 |
| `src/Bee.Definition/Collections/MessagePackKeyCollectionBase.cs` | L107 |

---

## 批次七：其他 Minor

| 檔案 | 行號 | 規則 | 說明 |
|------|------|------|------|
| `src/Bee.Api.Client/ConnectFunc.cs` | L8 | S2094 | 空 class，移除或改為 interface |
| `src/Bee.Api.Client/ApiServiceProvider/IJsonRpcProvider.cs` | L20 | S1133 | 移除已廢棄的程式碼（`[Obsolete]`） |
| `src/Bee.Api.Core/JsonRpc/ApiPayloadJsonConverter.cs` | L90 | S125 | 移除空 `case` clause |
| `src/Bee.Api.Core/Validator/ApiAccessValidator.cs` | L52 | S125 | 移除空 `case` clause |
| `src/Bee.Api.Core/MessagePack/DataSetFormatter.cs` | L41 | S1116 | 移除空 statement（多餘的分號） |
| `src/Bee.Api.Core/MessagePack/DataTableFormatter.cs` | L41 | S1116 | 移除空 statement |
| `src/Bee.Api.Core/MessagePack/MessagePackHelper.cs` | L24 | S3963 | 靜態欄位改為 inline 初始化，移除 static constructor |
| `src/Bee.Api.Contracts/PackageDelivery.cs` | L6 | S2344 | enum 不應明確指定 `int` 作為 underlying type |
| `src/Bee.Base/Collections/KeyCollectionBase.cs` | L105 | S4023 | 改用 pattern matching 取代 type-check-and-cast |
| `src/Bee.Base/BaseFunc.cs` | L280, L465, L467 | S4023 | 改用 pattern matching |
| `src/Bee.Base/Serialization/DataTableJsonConverter.cs` | L83 | S6580 | 用常數取代重複字串 `"original"` |
| `src/Bee.Base/Serialization/UTF8StringWriter.cs` | L9 | S101 | 類別名改為 `Utf8StringWriter`（Pascal case） |
| `src/Bee.Base/SysInfo.cs` | L13 | S3963 | 靜態欄位改為 inline 初始化，移除 static constructor |
| `src/Bee.Base/StrFunc.cs` | L189, L464 | S3878 | 移除多餘的 array 建立，直接傳 elements |
| `src/Bee.Base/Tracing/TraceLayer.cs` | L10 | S2342 | enum 名稱須符合正規表達式 `^([A-Z]{1,3}[a-z0-9]+)*([A-Z]{2})?s$`（末尾加 `s`：`TraceLayers`） |
| `src/Bee.Base/AssemblyLoader.cs` | L67 | S3885 | 用 `Assembly.Load` 取代 `Assembly.LoadFrom` |
| `src/Bee.Business/Provider/LoginAttemptTracker.cs` | L137 | S3260 | 非繼承的 private class 加上 `sealed` |
| `src/Bee.Db/Query/InternalWhereBuilder.cs` | L67 | S6580 | 用常數取代重複字串 `" LIKE "` |
| `src/Bee.Db/Providers/SqlServer/SqlCreateTableCommandBuilder.cs` | L155 | S3267 | 改用 LINQ Select |
| `src/Bee.Definition/Collections/MessagePackKeyCollectionBase.cs` | L138 | S6580 | 屬性 `ItemsForSerialization` 不應 copy collection，改為 method |

---

## 不納入修正的 Critical（複雜度重構）

以下 Critical 問題需要大幅重構邏輯，風險較高，暫時保留觀察：

| 檔案 | Cognitive Complexity |
|------|------|
| `SerializableDataTable.cs:55` | 35 |
| `DataTableComparer.cs:16` | 35 |
| `SerializableDataTable.cs:133` | 25 |
| `DbCommandSpec.cs:167` | 24 |
| `ApiPayloadJsonConverter.cs:17` | 23 |
| `DbAccess.cs:315` | 21 |
| `InternalWhereBuilder.cs:42` | 20 |
| `DataTableJsonConverter.cs:183` | 18 |

---

## 驗證方式

每批修正後執行：

```bash
dotnet build --configuration Release 2>&1 | grep -E "warning|error"
dotnet test --configuration Release --settings .runsettings
```

確認 **0 warnings、0 errors**、所有測試通過後再 commit。
