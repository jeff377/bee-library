# Northwind 案例走進客製覆蓋層

**狀態：✅ 已完成（2026-08-18）**

案例（`apps/Bee.Northwind`）從頭到尾沒有走進客製覆蓋層一次。本 plan 把案例這一端的接線補上：
一份客製語系資源檔，把訂單表單的客戶欄標題換成不一樣的字。

**框架端不動** —— `CustomizeOverlay` / `CustomizeDefineReader` / `CustomizeOnlyPathOptions`
機制本身完好，缺的只有案例的接線。

> 本輪只做 `bee-library` 的 `apps/Bee.Northwind`。獨立 repo `bee-northwind-avalonia` 之後另一輪同步。

---

## 動筆前複驗的結果（2026-08-18）

原始勘查列出四處要改，複驗全部成立，但**還缺第五處**，否則桌面畫面看不到任何差別：

| # | 檔案 | 內容 |
|---|------|------|
| 1 | `Bee.Northwind.Server/NorthwindCredentials.cs` | 新增 `CustomizeId` 常數 |
| 2 | `Bee.Northwind.Server/NorthwindCompanyInfoService.cs` | 硬編 company 補 `CustomizeId` |
| 3 | `Bee.Northwind.Server/NorthwindSystemBusinessObject.cs` | `Login` 覆寫一併蓋 `session.CustomizeId`（繞過了 `SessionCompanyBinder`） |
| 4 | `Bee.Northwind.Server/NorthwindBackend.cs` | `PathOptions.CustomizePath` |
| 5 | **`Bee.Northwind.UI/Controls/FormWorkspace.cs`** | **`FormView` / `ListView` 都沒有接 `FormDefinitionLoader`** |
| 6 | `Define/Language/zh-TW/Order.Language.xml` | 套裝語系資源（新增） |
| 7 | `Customize/northwind-demo/Language/zh-TW/Order.Language.xml` | 客製語系資源（新增） |

### 第 5 處：為什麼原本的四處不夠

`FormView.ResolveSchemaAsync` 只有在 `DefinitionLoader` 有值時才走
`GetLocalizedSchemaAsync`；`null` 時直接拿未在地化的 schema。案例的
`FormWorkspace` 是 `new FormView { ProgId = _progId }`，**沒有設 `DefinitionLoader`**，
所以整條「兩層語系 → `FormSchemaLocalizer` → 兩層 layout → `CustomizeOverlay`」在案例裡
從未執行 —— 客製語系檔就算放對位置也不會被讀。

`ListView` 同理（`ResolveSchemaAsync` 硬走 `ClientInfo.DefineAccess.GetFormSchemaAsync`），
但它沒有 `DefinitionLoader` 屬性，只有 `protected virtual` 的解析鉤子，所以案例端要**子類覆寫**。

---

## 決策：選 B（同時補一份套裝語系資源）

`Define/Language/` 目前是空的，欄位標題來自 `FormSchema` 的 `Caption`。兩個選項：

- **A. 只加客製那一份** —— `TryGetLangText` 在套裝為 `null` 時仍回客製值，覆寫會生效，
  但那示範的是「客製層是唯一來源」，不是**key 級疊加**。
- **B. 套裝 + 客製兩份** —— 客製只覆寫其中兩個 key，其餘照樣來自套裝。

**採 B**，理由三點：

1. 客製層的重點就是 key 級優先序。A 做不出「其餘 key 來自套裝」這件事，
   等於示範不到這一層真正的價值。
2. `Define/Language/{zh-TW,en-US}/` 兩個空資料夾本來就是預留位置，案例卻一份語系資源都沒有 ——
   補上是完成既有結構，不是額外發明範圍。
3. 一次改動同時讓「客製層」與「多語系」兩題都有真檔可引用。

**不補 en-US**：`FormSchema` 的 `Caption` 本身就是英文，另寫一份 en-US 資源等於複寫
（違反 `single-source.md`）。`FormSchemaLocalizer` 對缺 key 是「原字串不動」，
所以 en-US 語系下自然落回 schema 的英文標題。

### 客製哪個 key

客製 `Field.customer_rowid.Caption` 與 `Field.ref_customer_name.Caption`，
套裝的「客戶 / 客戶名稱」→ 客製的「經銷商 / 經銷商名稱」。

兩個而非一個：`customer_rowid` 在單身表單、`ref_customer_name` 在列表
（`ListFields`），只改一個會出現「表單寫經銷商、列表寫客戶名稱」的錯亂觀感。
兩個 key 對一個業務概念，仍然清楚是 key 級（24 個 key 只換 2 個）。

---

## 驗收

server 起在 `http://localhost:5100/api`（SQLite），所有請求帶 `X-Api-Key`。

| 呼叫 | 改動前 | 改動後 |
|---|---|---|
| `System.GetCustomizeLanguage`（`zh-TW` / `Order`） | `{"xml":""}` | 客製資源的 XML |
| `System.GetCustomizeFormLayout` | 空字串 | 空字串（本輪不做版面客製） |
| 桌面端訂單表單 | 英文標題（無在地化） | 中文標題，客戶欄為「經銷商」 |

另驗**短路沒被弄壞**：清掉 `CustomizeId` 或拿掉 `CustomizePath`，
應回到完全走套裝層、不探任何客製檔。

### 實測結果（2026-08-18）

三個 API 面向全部符合：`GetCustomizeLanguage`(zh-TW/Order) 回得出兩個 key 的客製資源、
同一支在 en-US 回空字串、`GetCustomizeFormLayout` 回空字串、`GetLanguage` 回套裝資源。
兩個短路開關**分別**清掉後 `GetCustomizeLanguage` 都回空字串，且套裝層不受影響。

桌面畫面改以**跑一次 `FormView` 的同一條 client 程式路徑**確認
（`FormDefinitionLoader.GetLocalizedSchemaAsync` + `GetRuntimeLayoutAsync`，
對真的 server 走真的 HTTP）：

| 欄位 | zh-TW | en-US |
|------|-------|-------|
| `customer_rowid` | **經銷商**（客製層） | Customer（schema） |
| `employee_rowid` | 業務員（套裝層） | Employee（schema） |
| `freight` | 運費（套裝層） | Freight（schema） |

—— 兩層來源在同一張表單上同時成立，正是 key 級疊加要示範的事。

**沒有取到畫素級截圖**：桌面 head 是 `dotnet <dll>` 起的 Avalonia 視窗、沒有 `.app` bundle，
螢幕控制的允許清單解析不到它。

---

## 連動：鐵人賽兩篇的事實敘述會過期

`docs/blogs/ithome-2026-ironman/` 的 Day 29 §83 與 Day 30 §105 明文斷言案例
「一次都沒有走進去」客製層。本改動讓那句話變成錯的。

**本 plan 不動那兩篇**（在另一個 git repo、由另一個 session 處理），只列出這筆連動供裁定。
判斷參考：加一個客製化代碼**不會生出第二個租戶** —— 案例仍然只有一家公司，
只是那家公司指了一份客製，所以兩篇的論點核心仍成立，變的只是那句事實敘述。
