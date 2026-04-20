# 計畫：啟用 CA1305／CA1031／CA1725 編譯期檢查

**狀態：🚧 進行中（CA1725、CA1305 已完成 2026-04-20；CA1031 待處理）**

## 背景

2026-04-20 將 `sonarcloud.md` 列舉的 Sonar 規則改由 `.editorconfig` 硬性化時，多數 CA 對應規則零違規（CA1822、CA1052、IDE0044、IDE0051、IDE0052、CA3075、CA5350/5351、CA5394）直接啟用。但三條規則在啟用當下會讓 build 失敗：

- **CA1305**（`IFormatProvider`）：28 個違規
- **CA1031**（catch Exception）：22 個違規
- **CA1725**（override 參數名一致）：4 個違規

以上違規都是現有程式碼真實的規範問題，非誤報。修復涉及**語意判斷**（CA1305 須決定 invariant vs current culture、CA1031 須決定具體例外型別與是否 rethrow），不是機械替換，故獨立拉出此計畫分批處理。

本計畫目標：修復全部 54 處違規 → 啟用三條規則到 `.editorconfig` → 從 `.claude/rules/sonarcloud.md` 移除對應 S 規則段落（S6580、S927 的 catch Exception 部分見 `scanning.md`）。

## 違規清單

### CA1305 ✅ 已完成（2026-04-20）

執行時共修復 **28 處**違規，分佈於 Bee.Base（14 處）、Bee.Db（13 處）、Bee.Db.UnitTests（1 處）。原估「全在 Bee.Base」是因前次 build 未跑完成，實際上 Bee.Db 的 SQL builder 內插字串也都會觸發。

**Bee.Base**
- [BaseFunc.cs](src/Bee.Base/BaseFunc.cs)：`Convert.ToInt32/Double/Decimal/DateTime` 7 處
- [StrFunc.cs](src/Bee.Base/StrFunc.cs)：`string.Format`
- [IPValidator.cs](src/Bee.Base/IPValidator.cs)：`int.Parse`
- [PasswordHasher.cs](src/Bee.Base/Security/PasswordHasher.cs)：`int.Parse` 2 處
- [DataRowExtensions.cs](src/Bee.Base/Data/DataRowExtensions.cs)：`Convert.ChangeType` 2 處
- [DataTableJsonConverter.cs](src/Bee.Base/Serialization/DataTableJsonConverter.cs)：`Convert.ChangeType`

**Bee.Db**
- [DefaultParameterCollector.cs](src/Bee.Db/Query/DefaultParameterCollector.cs)：`int.ToString`
- [FromBuilder.cs](src/Bee.Db/Query/FromBuilder.cs)：`StringBuilder.Append` 內插字串 2 處
- [TableSchemaCommandBuilder.cs](src/Bee.Db/TableSchemaCommandBuilder.cs)：`StringBuilder.AppendLine/Append` 內插字串 6 處
- [SqlCreateTableCommandBuilder.cs](src/Bee.Db/Providers/SqlServer/SqlCreateTableCommandBuilder.cs)：`StringBuilder.Append` 內插字串 5 處（含 `ToUpper()` → `ToUpperInvariant()` 兩處）

**Bee.Db.UnitTests**
- [DbAccessTests.cs:167](tests/Bee.Db.UnitTests/DbAccessTests.cs:167)：`int.ToString`

**判斷原則**：本專案所有文化相依呼叫都是資料序列化／SQL 產生／轉換場景，應固定 `CultureInfo.InvariantCulture`，不受使用者地區影響。

### CA1031（22 處）

| 檔案 | 方法 | 建議 |
|------|------|------|
| [BackgroundService.cs](src/Bee.Base/BackgroundServices/BackgroundService.cs) `Initialize/Start/Stop/RunLoop` | 背景服務 lifecycle，需吞掉所有例外避免服務崩潰 | 保留 catch，但抽換為 `catch (Exception ex) when (LogAndSwallow(ex))` 模式，或加 `#pragma warning disable CA1031` + 註解說明 |
| [BaseFunc.cs](src/Bee.Base/BaseFunc.cs) `CInt/CDouble/CDecimal/CDateTime` | 型別轉換 fallback | 改 catch `FormatException` / `OverflowException` / `InvalidCastException` |
| [FileHashValidator.cs](src/Bee.Base/Security/FileHashValidator.cs) `HexToBytes` | 解析失敗回傳 null | 改 catch `FormatException` |
| [PasswordHasher.cs](src/Bee.Base/Security/PasswordHasher.cs) `VerifyPassword` | 雜湊驗證失敗回傳 false | 改 catch `FormatException` / `CryptographicException` |
| [DataTableJsonConverter.cs](src/Bee.Base/Serialization/DataTableJsonConverter.cs) `ConvertValue` | 轉型失敗 fallback | 改 catch `InvalidCastException` / `FormatException` |

**判斷原則**：能列舉具體例外型別者一律列舉；背景服務 lifecycle 這類必須吞例外的場景，用 `#pragma` 區域抑制並加說明。

### CA1725（4 處，全在 test fixture）✅ 已完成（2026-04-20）

[tests/Bee.Base.UnitTests/SerializeFuncTests.cs](tests/Bee.Base.UnitTests/SerializeFuncTests.cs) 的 `TestPayload` 類別 override `IObjectSerialize` / `IObjectSerializeProcess` 介面：

- Line 49: `SetSerializeState(SerializeState state)` → `serializeState`
- Line 59: `BeforeSerialize(SerializeFormat format)` → `serializeFormat`
- Line 60: `AfterSerialize(SerializeFormat format)` → `serializeFormat`
- Line 61: `AfterDeserialize(SerializeFormat format)` → `serializeFormat`

**修復**：純參數改名，測試邏輯不受影響。

## 執行步驟

1. **修復 CA1725（4 處）** — 最簡單，先做
2. **修復 CA1305（28 處）** — 加 `using System.Globalization;` 並傳入 `CultureInfo.InvariantCulture`
3. **修復 CA1031（22 處）** — 逐一判斷：
   - 可列舉具體例外型別 → 改 catch
   - 必須吞例外的 lifecycle → `#pragma warning disable CA1031` 並加說明註解（需符合 code-style.md「若需抑制單行，必須加上說明註解」原則）
4. **啟用 `.editorconfig` 三條規則**
5. **驗證**：`dotnet build --configuration Release` 零警告零錯誤
6. **執行測試**：`./test.sh`（Bee.Base 修改量大，CA1305 改 culture 可能影響轉型行為，需驗證）
7. **更新 `.claude/rules/sonarcloud.md`** — 移除：
   - 表 6「S6580」行（IFormatProvider，由 CA1305 取代）
   - 表 3「S927」行（override 參數名，由 CA1725 取代）
   - 並於頂部或對應段落加註「規則已由 `.editorconfig` 硬性化」
8. **同步更新 `.claude/rules/scanning.md`** — 「例外處理」段可加註 CA1031 已由 editorconfig 強制（但空 catch／throw ex; 仍靠 prompt）
9. **commit & push** — commit message 遵循 type(scope) 格式

## 不在本次範圍

- `Bee.Base` 以外專案的 CA1305 / CA1031 — 實測只有 Bee.Base 有違規，全部一次搞定
- Sonar S 規則中尚無 CA 對應者（S101、S2094、S3925、S2342 等）— 需 SonarAnalyzer NuGet 才能本地化
- `CA2100`（SQL 字串拼接）、`CA2007`（ConfigureAwait）— 可能誤報，另行評估

## 驗證條件

- `dotnet build --configuration Release` 零警告零錯誤
- `./test.sh` 全部 pass（特別留意 `BaseFunc` / `PasswordHasher` / `DataTableJsonConverter` 測試）
- `.editorconfig` 內含 CA1305、CA1031、CA1725 三條 warning
- `.claude/rules/sonarcloud.md` 對應 S 規則段落已移除或註記為「已硬性化」
