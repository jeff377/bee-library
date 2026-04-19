# 計畫：Bee.Business 命名空間與資料夾重構

**狀態：✅ 已完成（2026-04-19）**

## 背景

`Bee.Business.BusinessObjects` 這個子命名空間有兩個問題：

1. **命名空間冗餘**：`Bee.Business.BusinessObjects.BusinessObject` 中「Business」連續出現三次，違反 `code-style.md` 的 CA1724 精神（命名空間最後一段與其中類別近似同名）。
2. **不利未來擴充**：現行扁平結構把 `FormBusinessObject` / `SystemBusinessObject` 及其 handler 全混放在同一層。未來當這兩種 BO 原型衍生大量子類別、輔助型別時，資料夾會失控。

本次重構**不改任何類別名稱**（詳見下方「命名決策」），只動資料夾結構與命名空間，消除冗餘、為未來擴充預留空間。

## 命名決策（先前討論結論）

### 保留 `BusinessObject` 後綴

- `BusinessObject` 是框架的語意核心（類似 EF 的 `DbContext`、ASP.NET 的 `Controller`），後綴承載「這是一個 BO」的明確訊號
- 業界慣例（`DbContext`、`ControllerBase`）都是「長名稱 + 平坦命名空間」
- 未來業務子類別本來就要帶後綴才可辨識（`CustomerBusinessObject`、`OrderBusinessObject`）

### 保留 `SystemBusinessObject` 不改為 `PlatformBusinessObject`

- 原設計意圖：「應用程式平台共用、非表單對應的 BO」= 系統層級 BO
- `System` 為刻意命名，不是隨手取名
- 中文語境「系統層 BO」比「平台層 BO」更自然對應團隊溝通
- 與 BCL `System` 撞名僅為理論疑慮，現狀 `Bee.Business.System` 已存在（16 個 Args/Result DTO）未造成問題

### 介面與實作同命名空間

- `IFormBusinessObject` / `ISystemBusinessObject` 從 `Bee.Business` 根搬入對應子命名空間
- 符合 `code-style.md` 的「介面就近放置」原則

## 目標結構

```
src/Bee.Business/
├── BusinessObject.cs              → namespace Bee.Business
├── BusinessObjectProvider.cs      → namespace Bee.Business
├── IBusinessObject.cs             → namespace Bee.Business（現狀保持）
├── IExecFuncHandler.cs            → namespace Bee.Business
├── Form/                          ← 新增資料夾
│   ├── IFormBusinessObject.cs     → namespace Bee.Business.Form（從根搬入）
│   ├── FormBusinessObject.cs      → namespace Bee.Business.Form
│   └── FormExecFuncHandler.cs     → namespace Bee.Business.Form
├── System/                        ← 現有資料夾擴充
│   ├── ISystemBusinessObject.cs   → namespace Bee.Business.System（從根搬入）
│   ├── SystemBusinessObject.cs    → namespace Bee.Business.System（從 BusinessObjects/ 搬入）
│   ├── SystemExecFuncHandler.cs   → namespace Bee.Business.System（從 BusinessObjects/ 搬入）
│   └── *Args.cs / *Result.cs      （16 個 DTO，現狀不動）
├── Provider/                      （不動）
├── Validator/                     （不動）
└── Attributes/                    （不動）
```

**`BusinessObjects/` 資料夾刪除。**

## 未來擴充模式（確立原則）

Bee.Business 框架層新增 BO 原型時，一律依此模式：

- 每個 **BO 原型**（有專屬 base class 繼承 `BusinessObject`、專屬 `ExecFuncHandler` 實作）= 一個子資料夾
- 子資料夾內含：介面、base class、handler、未來專屬輔助型別
- 命名空間為 `Bee.Business.<原型名>`

例如未來新增 Report BO 原型時：

```
Report/
├── IReportBusinessObject.cs       → namespace Bee.Business.Report
├── ReportBusinessObject.cs        → namespace Bee.Business.Report
└── ReportExecFuncHandler.cs       → namespace Bee.Business.Report
```

**業務領域 BO**（`CustomerBusinessObject`、`OrderBusinessObject` 等）在**消費端專案**繼承對應原型，不在本框架層開子資料夾。

## 影響範圍

### Bee.Library 內部

| 類型 | 檔案數 | 動作 |
|------|--------|------|
| src 原始檔（`BusinessObjects/` 內） | 7 | 移動 + 改 `namespace` 宣告 |
| src 根檔（`IFormBusinessObject.cs` / `ISystemBusinessObject.cs`） | 2 | 移動 + 改 `namespace` 宣告 |
| src using 更新 | 1 | `src/Bee.Business/BusinessFunc.cs` |
| src 字串常數更新 | 1 | `src/Bee.Definition/BackendDefaultTypes.cs:21`（AQN） |
| tests using 更新 | 11 | 見下方 |

**測試檔案 using 更新清單**：
- `tests/Bee.Business.UnitTests/SystemBusinessObjectTests.cs`
- `tests/Bee.Business.UnitTests/SystemBusinessObjectExtraTests.cs`
- `tests/Bee.Business.UnitTests/SystemBusinessObjectLoginTests.cs`
- `tests/Bee.Business.UnitTests/SystemBusinessObjectDefineTests.cs`
- `tests/Bee.Business.UnitTests/SystemBusinessObjectPureLogicTests.cs`
- `tests/Bee.Business.UnitTests/FormBusinessObjectTests.cs`
- `tests/Bee.Business.UnitTests/BusinessObjectProviderTests.cs`
- `tests/Bee.Business.UnitTests/Fakes/FakeExecFuncHandler.cs`
- `tests/Bee.Business.UnitTests/Fakes/TestableSystemBusinessObject.cs`
- `tests/Bee.Business.UnitTests/Fakes/TestableBusinessObject.cs`
- （若仍有遺漏，以 `using Bee.Business.BusinessObjects` 全搜為準）

### `BackendDefaultTypes.cs` 字串常數（關鍵）

```csharp
// 現狀
public const string BusinessObjectProvider = "Bee.Business.BusinessObjects.BusinessObjectProvider, Bee.Business";

// 調整後
public const string BusinessObjectProvider = "Bee.Business.BusinessObjectProvider, Bee.Business";
```

此為 Assembly-Qualified Name 字串，執行時透過 `Type.GetType()` 解析；**若未同步更新，系統啟動即失敗**。

### 外部消費端（破壞性變更）

本套件於 NuGet 公開發布，下列命名空間變動為 **breaking change**：

| 舊命名空間 | 新命名空間 | 範圍 |
|------------|------------|------|
| `Bee.Business.BusinessObjects` | `Bee.Business` | `BusinessObject`、`BusinessObjectProvider`、`IExecFuncHandler` |
| `Bee.Business.BusinessObjects` | `Bee.Business.Form` | `FormBusinessObject`、`FormExecFuncHandler` |
| `Bee.Business.BusinessObjects` | `Bee.Business.System` | `SystemBusinessObject`、`SystemExecFuncHandler` |
| `Bee.Business` | `Bee.Business.Form` | `IFormBusinessObject` |
| `Bee.Business` | `Bee.Business.System` | `ISystemBusinessObject` |

前端三個 repo（WinForms / Web / MAUI）升級本套件版本時需調整 `using`。

## 執行步驟

### 階段 1：搬動檔案與改命名空間

1. 建立 `src/Bee.Business/Form/` 資料夾
2. `BusinessObjects/FormBusinessObject.cs` → `Form/FormBusinessObject.cs`，`namespace` 改為 `Bee.Business.Form`
3. `BusinessObjects/FormExecFuncHandler.cs` → `Form/FormExecFuncHandler.cs`，`namespace` 改為 `Bee.Business.Form`
4. `IFormBusinessObject.cs`（根） → `Form/IFormBusinessObject.cs`，`namespace` 改為 `Bee.Business.Form`
5. `BusinessObjects/SystemBusinessObject.cs` → `System/SystemBusinessObject.cs`，`namespace` 改為 `Bee.Business.System`
6. `BusinessObjects/SystemExecFuncHandler.cs` → `System/SystemExecFuncHandler.cs`，`namespace` 改為 `Bee.Business.System`
7. `ISystemBusinessObject.cs`（根） → `System/ISystemBusinessObject.cs`，`namespace` 改為 `Bee.Business.System`
8. `BusinessObjects/BusinessObject.cs` → `BusinessObject.cs`（根），`namespace` 改為 `Bee.Business`
9. `BusinessObjects/BusinessObjectProvider.cs` → `BusinessObjectProvider.cs`（根），`namespace` 改為 `Bee.Business`
10. `BusinessObjects/IExecFuncHandler.cs` → `IExecFuncHandler.cs`（根），`namespace` 改為 `Bee.Business`
11. 刪除空資料夾 `BusinessObjects/`

### 階段 2：更新 using 與字串常數

12. `src/Bee.Business/BusinessFunc.cs`：移除 `using Bee.Business.BusinessObjects;`，加入必要的 `Bee.Business.Form` / `Bee.Business.System`（依實際引用判斷）
13. `src/Bee.Definition/BackendDefaultTypes.cs:21`：將字串 `"Bee.Business.BusinessObjects.BusinessObjectProvider, Bee.Business"` 改為 `"Bee.Business.BusinessObjectProvider, Bee.Business"`
14. 所有測試檔案：將 `using Bee.Business.BusinessObjects;` 替換為對應的新命名空間（`Bee.Business.Form` / `Bee.Business.System`；介面若直接引用，補對應命名空間）

### 階段 3：驗證

15. 執行 `dotnet build --configuration Release` 確認編譯通過
16. 執行 `dotnet test --configuration Release --settings .runsettings` 確認所有測試通過
17. 人工檢查：grep 全專案確認不再有 `Bee.Business.BusinessObjects` 字串殘留

### 階段 4：提交

18. 本機驗證通過後，commit 並 push 到 `main`（依 `pull-request.md`：桌面環境預設直接提交）
19. 觀察 `build-ci.yml` 在遠端跑一次確認

### 階段 5：文件

20. 更新 `src/Bee.Business/README.md` / `README.zh-TW.md`（若有提及 `BusinessObjects` 命名空間）
21. Plan 執行完畢，在文件頂部加上 `**狀態：✅ 已完成（YYYY-MM-DD）**`

## 風險與注意事項

- **AQN 字串常數（關鍵）**：`BackendDefaultTypes.cs` 的字串若漏改，執行期才會失敗（`Type.GetType()` 回傳 null），編譯階段偵測不到。必須在階段 3 以整合測試驗證。
- **前端 repo 升級**：本變更為 breaking change，建議搭配版本號跳 minor 或 major，並於 release notes 明列命名空間對照表。
- **`Bee.Business.System` 與 BCL `System` 的消歧**：現狀已存在此命名空間未見衝突，本次僅增加該命名空間下的類別數，不新增衝突風險。若搬入後出現 `DateTime`、`Guid` 等 BCL 型別解析模稜兩可，補 `global::System.` 限定即可。
- **不動項**：`Provider/`、`Validator/`、`Attributes/`、`BusinessArgs.cs`、`BusinessResult.cs`、`ExecFuncArgs.cs`、`ExecFuncResult.cs`、`BusinessFunc.cs` 本身位置不變，僅 `BusinessFunc.cs` 的 `using` 需更新。

## 後續工作（不在本 plan 範圍）

- Report BO 原型新增：依「未來擴充模式」章節實作，屬獨立任務
- 業務領域 BO 的 namespace 指引：由消費端（前端 repo）自行決定，本框架不強制
