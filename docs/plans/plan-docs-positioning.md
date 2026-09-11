# 計畫：公開文件的定位由 ERP 改為以表單為基礎的企業資訊系統

**狀態：📝 擬定中**

## 背景

框架的消費群是 **ERP、CRM、HRM 這類以表單為基礎的資訊系統**，ERP 只是其中一類。

公開文件的現況（2026-09-11 查）：

- **已經正確的部分，作為錨點**：
  - 根目錄 README 開頭寫「企業資訊系統開發框架」，英文版是 "enterprise information systems"。
  - 架構總覽的「適用邊界」表列了「企業內部管理系統（HR、財務、採購、倉儲、CRM）」。
- **把 ERP 當成全部的部分**：
  - 架構總覽的副標題、「傳統 ERP 開發的痛點」一節，以及決策總表每一列的理由。
  - README 特色列表中的「針對 ERP 場景」。
  - 另有幾份文件和 XML doc 也這樣寫。

結果是：做 CRM、HRM 的讀者看完，會以為這個框架不是給他們用的。

**為什麼要排在翻譯之前**：繁中是翻譯的源頭。如果先翻譯再改定位，同一批修改就得在每種語言各做一次。
[plan-docs-multilingual-layout.md](plan-docs-multilingual-layout.md) 的「新增語言的程序」已經把本 plan 列為前置條件。

## 範圍

**納入**：

- 公開的 markdown：根目錄 README（中英兩份），以及 `docs/` 下的指引與參考文件（中英兩份）。
- `src/**/*.cs` 的 XML doc。它們隨 NuGet 套件發佈，屬於公開文件。

**不納入**：

- `docs/adr/`、`docs/changelogs/`、根目錄 `CHANGELOG`：這些是紀錄，記的是當時的決策與變更。
  依 `single-source.md` 的判讀原則，屬於「當時量到什麼」，保留原文。
- `.claude/`、`docs/repo-ops/`：不是公開文件。
- `docs/blogs/`：屬於另一個子 repo。
- **BPM／Workflow**：屬於未來的發展方向，這次不寫進文件。
  框架目前沒有流程引擎。[PermissionAction.cs](../../src/Bee.Definition/Settings/Permission/PermissionAction.cs) 的 XML doc 明寫，
  Approve、Post、Confirm 這類狀態轉換刻意不放在動作軸上，而是屬於另一層 workflow 權限，那一層還不存在。
  在那一層實作出來之前，公開文件都不提 BPM，免得寫出一個沒有機制支撐的宣稱。

## 判讀原則：逐處判斷，不整批替換

每一處 ERP 歸入以下三類之一：

| 類別 | 判別法 | 處理 |
|------|--------|------|
| **A 定位宣稱** | 在說「框架是給什麼系統用的」 | 改成目標系統的正式用語 |
| **B 以 ERP 當母集合的理由** | 把 ERP 換成 CRM 或 HRM，這句話仍然成立 | 改成通用說法；需要具體例子時才用 ERP 舉例 |
| **C 真正屬於 ERP 情境的內容** | 換成 CRM 或 HRM 就不成立，或讀起來很怪 | 保留 |

## 先定用語，再改文字

在 `terminology`（中英兩份）新增一個「目標系統」詞條，作為這次改寫與日後翻譯的共同錨點：

- **zh-TW**：見「待確認」。目前 README 用「企業資訊系統」，適用邊界表用「企業內部管理系統」，兩種說法並存，要統一成一種。
- **en**：見「待確認」。
- **ja、zh-CN**：翻譯時再決定，寫進同一個詞條。候選分別是「業務システム」和「企业管理系统／业务系统」。

## 逐處清單

2026-09-11 查的結果。行號以 zh-TW 源文件為準，實作時一律用 grep 重掃，不要照抄這裡的行號。
英文版的對應位置在同一個 commit 一起改。

### `docs/architecture-overview`

| 行 | 現在的寫法 | 類別 |
|----|-----------|------|
| 5 | 副標題「定義導向架構在 ERP 系統中的設計理念與實踐模式」 | A |
| 28 | 「解決傳統 ERP 開發中規格分散三層……」 | A |
| 36 | 標題「傳統 ERP 開發的痛點」 | A |
| 44–50 | 「適用邊界」表 | 已正確；統一用語，並明列 HRM |
| 56 | 「從各模式取用最適合 ERP 場景的概念」 | A |
| 68 | 「ERP 表單數量龐大（百張以上）」 | B |
| 75 | 「省去 ERP 場景中不必要的 Entity 建模成本」 | B |
| 143 | 「專為 ERP 制式表單設計」 | B |
| 148 | 標題「ERP 制式版面模式」 | B |
| 197 | 「ERP 表單幾乎都是這種形態」 | B |
| 274 | 「ERP 的 CRUD 高度同質化」 | B |
| 410–417 | 決策總表的理由欄 | B |

### 其他 markdown

| 位置 | 現在的寫法 | 類別 |
|------|-----------|------|
| `README` 特色列表 | 「針對 ERP 場景從各模式取用最適合的概念」 | A |
| `docs/formschema-data-access` 第 49 行 | 「ERP 系統的高頻變動會讓編譯期綁定的 ORM 付出沉重代價」 | B |
| `docs/formschema-data-access` 第 237 行 | 「ERP 動態欄位、客製化、多租戶」 | B |
| `docs/development-constraints` 第 328 行 | 「實務 ERP 場景下，BO 層已能完整表達業務規則」 | B |
| `docs/development-cookbook` 第 593 行 | 「Round-then-sum（ERP 不變量）」 | B：任何金額合計都適用，改成「金額合計不變量」之類的說法 |
| `docs/terminology` 第 333 行 | `ListView` 說明「ERP 畫面的清單側」 | B |
| `docs/permission-authorization` 第 171 行 | 「這貼合 ERP 實務——成本／……」 | C：以採購單隱藏成本欄為例，本身就是 ERP 情境 |

### XML doc

| 位置 | 現在的寫法 | 類別 |
|------|-----------|------|
| `src/Bee.Base/StringExtensions.cs` | "convention for ERP business logic" | B |
| `src/Bee.Base/ValueUtilities.cs` | "ERP-context defaults" | B |
| `src/Bee.Base/StringUtilities.cs`（三處） | "ERP-context defaults"、"ERP / database / serialization contexts"、"ERP identifiers" | B |
| `src/Bee.Db/Providers/Sqlite/SqliteSchemaSyntax.cs`（兩處） | "the ERP expectation"、"the ERP default" | B |
| `src/Bee.UI.Avalonia/Views/ListView.cs`、`FormView.cs` | "the ERP convention of keeping list browsing and…"、"the ERP list/record split" | B |
| `src/Bee.Base/Serialization/DataTableJsonConverter.cs` | "where an ERP's money actually travels" | C：舉例 |

## 步驟

1. 確認「待確認」表中的用語，寫進 `terminology` 的中英兩份。
2. 依清單修改 zh-TW 源文件，英文版的對應位置在同一個 commit 一起改。
3. **改標題之前**，先 grep 全 repo 指向該標題錨點的連結，一併更新。
   中文要查 `#傳統-erp-開發的痛點`、`#erp-制式版面模式`，英文版也有對應的 slug。
4. 修改 XML doc 後跑 Release build。
5. 驗證：
   - 在納入範圍內 grep `ERP`，剩下的命中只能是本 plan 列為 C 的項目。
   - 在納入範圍內 grep `BPM`、`workflow`，不應出現新增的能力宣稱。
   - `./check-public-docs.sh` 沒有新增輸出。
6. 下一版 changelog 的「文件」類別記一筆定位修正。

## 待確認

| 項目 | 建議 | 理由 |
|------|------|------|
| zh-TW 正式用語 | 「企業資訊系統」，詞條定義寫「以表單為基礎的企業資訊系統，例如 ERP、CRM、HRM」 | README 開頭已經在用。「管理系統」在簡中語境常指「後台管理系統」，範圍會被讀窄 |
| en 正式用語 | 沿用 README 的 "enterprise information systems"，把 "line-of-business (LOB) applications" 列為同義詞 | 與 README 一致；LOB 是 .NET 圈慣用的說法，放進詞條有助搜尋 |

## 與多語系 plan 的先後

- 本 plan 完成前，不開始翻譯 ja 與 zh-CN。
- 建議本 plan 排在 [plan-docs-multilingual-layout.md](plan-docs-multilingual-layout.md) 的階段 2（搬移）之前。
  本 plan 改的是內容、範圍比較小；先做完，搬移時的連結改寫就是在最終內容上進行，兩邊不會互相衝突。
