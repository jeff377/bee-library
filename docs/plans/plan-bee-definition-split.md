# 計畫：Bee.Definition 職責拆分（Storage IO / Security 實作外移）

**狀態：🚧 進行中（2026-07-24）**

| 階段 | 範圍 | 狀態 |
|------|------|------|
| 1 | 消除 `is FileDefineStorage` 能力洩漏 → **已移交** [plan-cache-invalidation-model.md](plan-cache-invalidation-model.md) 階段 1 | ✅ 已完成（2026-07-24，於該計畫執行） |
| 2 | 檔案 IO 實作外移至獨立套件（breaking，需版本規劃） | 📝 待裁決 |
| 3 | Security 實作歸屬重新確認（可能不動） | 📝 待裁決 |

## 背景

框架體檢（[plan-framework-review.md](plan-framework-review.md) P2-1）指出：`Bee.Definition` 是全圖最被依賴的專案（6 個直接下游），作為 Clean Architecture 的 Domain Core，卻同時承載檔案 IO（`Storage/`）與安全金鑰實作（`Security/`），弱化了「Domain Core 最純淨最穩定」的意圖。體檢當時建議「介面留在 Definition、實作外移」，並註明屬較大工程、應獨立立案。

本計畫即為該立案。**但實地調查後，實際規模與體檢的印象有落差，先如實陳述數據再談方案。**

## 現況實測（2026-07-24）

`Bee.Definition` 全專案 **233 檔 / 15,003 行**。兩個目標資料夾：

| 資料夾 | 行數 | 佔比 | 公開型別 |
|--------|-----:|-----:|---------|
| `Storage/` | 679 | 4.5% | `IDefineAccess`(182)、`IDefineStorage`(108)、`ICustomizeDefineReader`(45)、`FileDefineStorage`(217)、`CustomizeOnlyStorage`(127) |
| `Security/` | 337 | 2.2% | 3 個 enum + 3 個 interface（合計約 124 行）、`MasterKeyProvider`(168)、`EncryptionKeyProtector`(45) |

依「介面留下、實作外移」原則，**真正會搬走的只有 4 個型別、557 行，佔 Bee.Definition 的 3.7%**：

- Storage 實作：`FileDefineStorage`(217) + `CustomizeOnlyStorage`(127) = 344 行
- Security 實作：`MasterKeyProvider`(168) + `EncryptionKeyProtector`(45) = 213 行

**所有 `src/` 專案皆 `GeneratePackageOnBuild=True`**，即這 4 個型別都是已發布 NuGet 套件的公開 API —— 跨組件搬移對外部使用者是 **breaking change**。

## 關鍵發現

### 1. 真正的耦合問題不是「檔案放哪」，而是能力洩漏

`Bee.ObjectCaching` 有 **8 個 cache 類**以執行期具象型別判斷決定是否啟用檔案變更監控：

```csharp
if (_storage is FileDefineStorage)   // ProgramSettingsCache / FormSchemaCache / TableSchemaCache / ...
```

檔案：`ProgramSettingsCache`、`CurrencySettingsCache`、`DbCategorySettingsCache`、`FormLayoutCache`、`UnitSettingsCache`、`LanguageResourceCache`、`TableSchemaCache`、`FormSchemaCache`。

這是**對具象實作而非能力的相依**：上層必須知道「有一種叫 FileDefineStorage 的東西」才能運作。它同時是搬移的直接阻礙（型別搬走後這 8 處全斷），也是獨立於搬移本身就該修的設計問題。

### 2. Security 實作外移與已定 convention 相衝突

體檢 M2 建議「`Security/` 具體實作下沉至 `Bee.Base.Security` 旁」。但 P3-5 已在 `.claude/rules/security.md` 正式定下分界：

> **原語**（無狀態密碼學運算）放 `Bee.Base/Security/`；**政策 / 金鑰協定**（金鑰從哪來、誰能存取、如何驗證）放 `Bee.Definition/Security/`。

`MasterKeyProvider`（決定主金鑰來源）與 `EncryptionKeyProtector`（金鑰保護政策）**正是政策層**，依此 convention 它們待在 `Bee.Definition` 是正確的，不是夾帶。**兩份建議互相矛盾，需擇一。**

### 3. Security 實作的消費面極乾淨

`MasterKeyProvider` / `EncryptionKeyProtector` 在 repo 內**只被 `Bee.Hosting/BeeFrameworkServiceCollectionExtensions.cs` 的 DI 註冊消費**。若真要搬，技術成本極低——但這不改變第 2 點的歸屬爭議。

### 4. 成本效益需誠實面對

搬走 3.7% 的程式碼，換取「Domain Core 不含 IO 實作」的架構純度，代價是**已發布套件的 breaking change** + 下游相依圖變動。這不是「顯然該做」的重構，是一個取捨。

## 方案

### 階段 1 — 消除能力洩漏（✅ 已完成，於獨立計畫執行）

> 後續追查發現此項的根因不只是型別判斷，而是「`CacheItemPolicy` 只表達檔案相依、DB 相依散在快取類之外」的模型不完整。已獨立立案為
> **[plan-cache-invalidation-model.md](plan-cache-invalidation-model.md)** 並**全數執行完畢**（三階段皆 ✅）。
>
> **實際採用的做法與下方原始建議不同**：既非 (a) 的布林能力屬性、也非 (b) 的 opt-in 介面，而是在 `IDefineStorage` 加**預設介面方法**回傳中性描述子
> `DefineChangeSource`（`FilePaths` / `NotifyKey` 兩格，各實作只填自己那一格）。理由是 (b) 仍保留一個 `is` 判斷、且以「檔案」命名的能力介面會把檔案形狀寫進抽象，
> 導致 DB 實作永遠回報空值；描述子讓 `DbDefineStorage` 也有話可說（回報 cache-notify 鍵）。
>
> **結果**：`grep "is FileDefineStorage"` 為 0；`Bee.ObjectCaching` 不再認識任何 storage 具象型別，本計畫階段 2 的最大阻礙已消除。
>
> 以下保留原始分析供對照。

把 8 處 `is FileDefineStorage` 改為能力導向抽象。兩種寫法擇一：

- **(a) 在 `IDefineStorage` 加能力屬性**：如 `bool SupportsChangeMonitoring { get; }`（或回傳監控來源的 `string? MonitorRootPath`）。實作端自行宣告能力，上層只問能力不問型別。
- **(b) 抽 opt-in 能力介面**：如 `IFileChangeMonitorSource`，`FileDefineStorage` 實作之，上層改判 `_storage is IFileChangeMonitorSource`。對既有 `IDefineStorage` 實作零影響。

**建議 (b)** —— 不動 `IDefineStorage` 既有契約（它有外部實作者的可能），能力以獨立介面 opt-in，且語意更精確（「這個 storage 有可監控的檔案來源」）。

- 影響：8 個 cache 檔 + `FileDefineStorage` 加介面實作。
- **非 breaking**（只新增介面、不改既有簽章）。
- 完成後，`Bee.ObjectCaching` 不再認識 `FileDefineStorage` 具象型別 —— 階段 2 的最大阻礙消失，且即使階段 2 永不執行，這個修正本身仍是淨改善。

### 階段 2 — 檔案 IO 實作外移（需先裁決）

把 `FileDefineStorage` + `CustomizeOnlyStorage`（344 行）移至新套件（暫名 `Bee.Definition.Storage`）。介面（`IDefineAccess` / `IDefineStorage` / `ICustomizeDefineReader`）留在 `Bee.Definition`。

相容性策略三選一：

| 策略 | 作法 | 代價 |
|------|------|------|
| **A. 直接搬 + major 版號** | 搬走，CHANGELOG 標 breaking，發 5.0.0 | 外部使用者需改 `using` 並加套件參照 |
| **B. 型別轉送過渡** | 新套件承載實作，`Bee.Definition` 加 `[TypeForwardedTo]` 並參照新套件，下一個 major 移除轉送 | **`Bee.Definition` 反而多一條對新套件的相依**，Domain Core 在過渡期並未變純淨，等於延後而非解決 |
| **C. 不搬** | 維持現狀，以資料夾與文件標明職責分層 | 架構純度不變，但零成本零風險 |

> **策略 B 的陷阱值得特別註明**：type forwarding 要求舊組件參照新組件，這與「把 IO 移出 Domain Core」的目標方向相反。它只在「先讓外部使用者無痛、下一個 major 再斷」的情境下有意義。

### 階段 3 — Security 實作歸屬（需先裁決）

依「關鍵發現 2」，先在兩個互斥立場中擇一：

- **維持現狀**（依 `security.md` 已定 convention，政策層本就屬 `Bee.Definition`）→ 階段 3 直接關閉，並在體檢 plan 註記 M2 該項已由 convention 解決。
- **仍要外移** → 需同時修訂 `security.md` 的分界定義，否則規範與實作再度脫節。

## 建議執行順序

1. **先做階段 1**（非 breaking、獨立有價值、解除階段 2 阻礙）。
2. 階段 1 落地後，重新評估階段 2 的效益——屆時 `Bee.Definition` 對上層已無具象型別洩漏，「IO 實作留在同一組件」的實際危害會比現在更小，可能得出「不搬也可以」的結論。
3. 階段 3 屬 convention 裁決，與階段 1/2 無相依，可獨立決定。

## 待裁決事項

- **階段 2 相容性策略**：A（直接搬 + major）／B（型別轉送過渡）／C（不搬）。
- **階段 3 立場**：維持現狀（依現有 convention）／外移並同步修訂 `security.md`。
- 若採 A 或 B，需確認目標版本號與發版時程（現行 4.15.0）。
