# Bee.Db 資安與效能稽核計畫

## 稽核範圍

完整審查 `src/Bee.Db/` 專案 — 資料庫存取模式、SQL 產生方式、連線管理、錯誤處理。

---

## 發現事項

### 資安問題

#### S1. `SqlCreateTableCommandBuilder` 存在 SQL Injection 風險 (HIGH)

**檔案：**
- `src/Bee.Db/Providers/SqlServer/SqlCreateTableCommandBuilder.cs`

**說明：**

多個方法將表名與索引名直接串接進原始 SQL 字串，未做參數化或跳脫處理：

| 方法 | 行號 | 問題 |
|------|------|------|
| `GetDropTableCommandText` | 97-98 | `tableName` 直接串接進 `N'{tableName}'` 及 `EXEC('DROP TABLE {tableName}')` |
| `GetRenameTableCommandText` | 138-141 | 表名/索引名直接串接進 `sp_rename` 呼叫 |
| `GetInsertTableCommandText` | 120-121 | 使用 `[{tableName}]` 但未跳脫名稱中的 `]` |
| `GetCreateTableCommandText` | 162 | `[{dbTableName}]` 未跳脫 `]` |
| `GetPrimaryKeyCommandText` | 309, 312 | `[{field.FieldName}]` 未跳脫 `]` |
| `GetIndexCommandText` | 345, 349 | 同上 |
| `GetFieldCommandText` | 212-214 | `[{field.FieldName}]` 未跳脫 `]` |
| `GetCommandText` | 55-57 | `this.TableName` 出現在 SQL 註解中 — 風險較低但不一致 |

**風險評估：** 這些值來自 `TableSchema`（內部設定），並非直接的使用者輸入。但 Schema 定義可能來自表單設定，有被管理者控制的可能。`GetDropTableCommandText` 中的 `EXEC('DROP TABLE ...')` 模式尤其危險，因為它建構了動態 SQL。

**修復方式：**
- 建立 `SanitizeIdentifier` 輔助方法，跳脫 `]` → `]]`（SQL Server）。
- 在所有識別符號串接點統一套用。
- `GetDropTableCommandText` 應改用 SQL 的 `QUOTENAME()` 函式或將表名檢查參數化。

---

#### S2. `QuoteIdentifier` 未跳脫分隔符號 (HIGH)

**檔案：** `src/Bee.Db/DbFunc.cs:57`

**說明：**

```csharp
{ DatabaseType.SQLServer, s => $"[{s}]" },
{ DatabaseType.MySQL, s => $"`{s}`" },
{ DatabaseType.SQLite, s => $"\"{s}\"" },
{ DatabaseType.Oracle, s => $"\"{s}\"" }
```

若識別符號本身包含分隔符號（例如 SQL Server 的 `]`、MySQL 的 `` ` ``、SQLite/Oracle 的 `"`），quoting 會被突破，可能導致 SQL Injection。正確的跳脫規則：
- SQL Server：`]` → `]]`
- MySQL：`` ` `` → ``` `` ```
- SQLite/Oracle：`"` → `""`

**影響範圍：** 所有使用 `DbFunc.QuoteIdentifier` 的查詢建構器（WhereBuilder、SelectBuilder、FromBuilder、TableSchemaCommandBuilder）皆受影響。

**修復方式：** 更新 `QuoteIdentifiers` 字典，加入跳脫邏輯：
```csharp
{ DatabaseType.SQLServer, s => $"[{s.Replace("]", "]]")}]" },
{ DatabaseType.MySQL, s => $"`{s.Replace("`", "``")}`" },
{ DatabaseType.SQLite, s => $"\"{s.Replace("\"", "\"\"")}\"" },
{ DatabaseType.Oracle, s => $"\"{s.Replace("\"", "\"\"")}\"" },
```

---

#### S3. `SqlTableSchemaProvider` 的 RowFilter 注入 (MEDIUM)

**檔案：** `src/Bee.Db/Providers/SqlServer/SqlTableSchemaProvider.cs:151`

**說明：**

```csharp
table.DefaultView.RowFilter = $"Name='{name}'";
```

`name` 變數來自同一 DataTable 的資料列（資料庫查詢結果），並非直接由使用者控制。但若資料庫中的索引名稱包含單引號（`'`），RowFilter 表達式會中斷，可能拋出例外或產生錯誤的篩選結果。

**修復方式：** 將單引號加倍跳脫：
```csharp
table.DefaultView.RowFilter = $"Name='{name.Replace("'", "''")}'";
```

---

#### S4. 錯誤日誌洩漏敏感資訊 (MEDIUM)

**檔案：** `src/Bee.Db/Logging/DbAccessLogger.cs:74-76`

**說明：**

```csharp
sb.Append("Message=").Append(exception.Message).Append("; ");
sb.Append("CommandText=").Append(context.CommandText);
```

完整的 SQL 命令文字與例外訊息被記入錯誤日誌。資料庫提供者的例外（如 SqlException）可能包含伺服器名稱、連線資訊或 Schema 細節。CommandText 會暴露資料表結構與查詢模式。

**修復方式：**
- 將日誌中的 `CommandText` 截斷至可設定的最大長度。
- 避免記錄完整的 `exception.Message`；僅記錄 `exception.GetType().Name` 及錯誤代碼。
- `WriteWarning`（行 102）也有同樣的 `ctx.CommandText` 記錄問題。

---

#### S5. 連線字串以明文快取於記憶體中 (LOW)

**檔案：** `src/Bee.Db/Manager/DbConnectionInfo.cs:33`

**說明：**

`DbConnectionInfo.ConnectionString` 是一個包含完整連線字串（含嵌入憑證）的純 `string` 屬性，被永久快取在 `DbConnectionManager._cache`（靜態 `ConcurrentDictionary`）中。這代表憑證在應用程式的整個生命週期內都留存於程序記憶體中。

**風險：** 在記憶體傾印或偵錯器情境下，憑證可能被擷取。這是標準 ADO.NET 行為，通常可接受，但值得記錄。

**建議：** 記錄為已知取捨。若需更高安全性，可考慮使用 `SecureString` 或利用連線字串建構器的 `PersistSecurityInfo=false` 選項。

---

### 效能問題

#### P1. `ILMapper` 快取無上限 (MEDIUM)

**檔案：** `src/Bee.Db/ILMapper.cs:17`

**說明：**

```csharp
private static readonly ConcurrentDictionary<(Type, string), Delegate> _cache = ...
```

快取為靜態且永不清除。若應用程式使用大量不同的查詢形式（不同的欄位順序或子集），快取會無限增長。每個項目持有一個已編譯的 `DynamicMethod` 委派。

**修復方式：**
- 新增 `ClearCache()` 方法供明確清理。
- 對長時間執行的應用程式，考慮使用有上限的 LRU 快取。
- 至少記錄快取行為的說明文件。

---

#### P2. `SqlTableSchemaProvider` 重複建立 `DbAccess` 實例 (LOW)

**檔案：** `src/Bee.Db/Providers/SqlServer/SqlTableSchemaProvider.cs:72-73, 93-94, 183-184`

**說明：**

三個方法（`TableExists`、`GetTableIndexes`、`GetColumns`）各自建立新的 `DbAccessObject(DatabaseId)` 實例。雖然底層連線池機制緩解了連線開銷，但重複的物件建立與連線開啟/關閉循環並無必要。

**修復方式：** 在 `GetTableSchema` 中建立單一 `DbAccess` 實例並向下傳遞，或設為類別欄位。

---

#### P3. 迴圈中使用 `+=` 串接字串 (LOW)

**檔案：** `src/Bee.Db/Providers/SqlServer/SqlCreateTableCommandBuilder.cs:110-118, 303-308, 340-345`

**說明：**

多個方法在迴圈中使用 `string += "..."` 而非 `StringBuilder` 來組建 SQL 片段：

```csharp
// GetInsertTableCommandText, 行 116
fields += $"[{field.FieldName}]";

// GetPrimaryKeyCommandText, 行 308
fields += $"[{field.FieldName}] ...";
```

對多欄位的資料表，會產生 O(n^2) 的字串配置。

**修復方式：** 改用 `StringBuilder`（同一類別的其他方法已使用此模式）。

---

#### P4. DataTable 將整個結果集載入記憶體 (LOW — 設計使然)

**檔案：** `src/Bee.Db/DbAccess/DbAccess.cs:277-278, 617`

**說明：**

`ExecuteDataTableCore` 使用 `adapter.Fill(table)` 載入所有資料列。對大型結果集會造成記憶體壓力。`Query<T>()` 方法透過 `DbDataReader` 提供了串流替代方案，設計良好。

**建議：** 此為設計使然（DataTable 本質為記憶體內操作）。記錄建議對大型結果集使用 `Query<T>()` 即可。

---

## 修復優先順序

| 優先序 | 問題 | 工作量 |
|--------|------|--------|
| 1 | S2：`QuoteIdentifier` 跳脫修復 | 小 |
| 2 | S1：`SqlCreateTableCommandBuilder` SQL Injection | 中 |
| 3 | S3：RowFilter 注入跳脫 | 小 |
| 4 | S4：錯誤日誌敏感資訊 | 小 |
| 5 | P1：ILMapper 快取清理方法 | 小 |
| 6 | P2：重複 DbAccess 實例 | 小 |
| 7 | P3：字串串接優化 | 小 |
| 8 | S5：記憶體中的連線字串（僅記錄） | 極小 |
| 9 | P4：DataTable 記憶體（僅記錄） | 極小 |

## 實作備註

- S2 為最高優先，因為 `QuoteIdentifier` 被所有查詢建構器使用。
- S1 的修復應盡量複用修復後的 `QuoteIdentifier`，並針對 `EXEC()` 模式加入 SQL 層級的防護。
- 所有修復應依照 `.claude/rules/testing.md` 的規範撰寫對應的單元測試。
