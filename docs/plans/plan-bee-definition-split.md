# 計畫：Bee.Definition 職責拆分（Storage IO / Security 實作外移）

**狀態：✅ 已完成（2026-07-24）**

| 階段 | 範圍 | 狀態 |
|------|------|------|
| 1 | 消除 `is FileDefineStorage` 能力洩漏 → **已移交** [plan-cache-invalidation-model.md](plan-cache-invalidation-model.md) 階段 1 | ✅ 已完成（2026-07-24，於該計畫執行） |
| 2 | 檔案 IO 實作外移 | ✅ 裁決不執行（採 C：不搬，2026-07-24） |
| 3 | Security 實作歸屬 | ✅ 裁決維持現狀（2026-07-24） |

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

### 階段 2 — 檔案 IO 實作外移（✅ 裁決：不執行，採 C）

**裁決（2026-07-24）：採 C（不搬）。** 階段 1 完成後重新評估，依下列四點論證定案：

1. **階段 1 已零成本兌現拆分的主要目的。** 真正讓上層「被迫認識 IO」的具體洩漏（`Bee.ObjectCaching` 8 處 `is FileDefineStorage`）已於階段 1 消除且非 breaking。生產碼現對檔案 storage 具象型別 0 引用。剩下的「IO 實作實體位在 Bee.Definition 組件內」是**物理封裝位置**問題，對消費者不可見，不再是相依洩漏。
2. **搬移的 breaking 面比原估嚴重一級。** 實測發現 `FileDefineStorage` 的預設是透過**組件限定型別名字串**動態載入：`BackendDefaultTypes.DefineStorage = "Bee.Definition.Storage.FileDefineStorage, Bee.Definition"`。搬到新組件會使此字串在**執行期**失效（非編譯錯誤），且該字串可能已**序列化進既有部署的 `BackendConfiguration` 定義檔** —— 使搬移從「來源相容 breaking」升級為「二進位 + 資料相容 breaking」。
3. **效益/成本嚴重不對稱。** 真正搬走的是 `FileDefineStorage`(217) + `CustomizeOnlyStorage`(127) = 344 行，佔 Bee.Definition 15,003 行的 **2.3%**。用此換二進位+資料相容 breaking + 新套件維護，不划算。
4. **唯一殘餘耦合不在 Bee.Definition。** `CacheContainerProvider:41` 仍 `new CustomizeOnlyStorage`（1 處），若日後要再收斂應處理此點，但與「Bee.Definition 職責純度」無關、屬低優先。

處置：保留 IO 實作於 `Bee.Definition/Storage/`，以資料夾 + 文件標明「介面是 Domain Core 契約、實作是基礎設施」。若未來出現強驅動（例如要出不含檔案 IO 的精簡 Domain 套件），再以 major 版重啟，屆時連同組件限定字串的遷移一併規劃。

原三策略比較表（A 直接搬+major／B 型別轉送過渡／C 不搬）保留於 git 歷史。B 的陷阱備忘：type forwarding 要求舊組件參照新組件，與「把 IO 移出 Domain Core」方向相反，只在「延後 breaking」情境下有意義。

### 階段 3 — Security 實作歸屬（✅ 裁決：維持現狀）

**裁決（2026-07-24）：維持現狀，不外移。** `MasterKeyProvider` / `EncryptionKeyProtector` 是政策/金鑰層，`security.md`（P3-5 已定）明訂「政策與金鑰協定屬 `Bee.Definition`」——它們待在此處是正確的，非夾帶。體檢 M2「Security 實作下沉 Bee.Base」與此 convention 互斥，以 convention 為準。體檢 plan 的 M2 該項視為「由 convention 解決」。

## 結論

**整份計畫收斂完成**：階段 1 已執行（於快取計畫）；階段 2 裁決不執行（採 C）；階段 3 裁決維持現狀。Bee.Definition 職責純度的**實質目標**（消除相依洩漏）已由階段 1 達成；IO/Security 實作的物理位置維持不動，以文件與 convention 標明分層。
