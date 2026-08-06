# Plan：語系客製化

> 狀態：✅ 已完成（G1–G4、G6 全數落地，G5 裁決不做）· 2026-08-01
> 範圍：**語系資源的租戶客製**——某些單據或行為，公司有慣用語（欄位標題、表單名稱、訊息、選項文字）。
> 前置：[客製化共同前置](plan-customization-foundation.md)（缺口 A、B 未補則本案無法生效）
> 相關：[Layout 客製](plan-customization-layout.md)｜[業務邏輯客製](plan-customization-business.md)｜[ADR-016](../../adr/adr-016-multitenant-customization-overlay.md)

---

## 0. 一句話結論

**三類中設計最正確、缺口最明確、也最好補的一類。**
`LanguageService` 的 **per-key 疊加**已正確實作，但兩個主要消費端
（`FormSchemaLocalizer` / `BeeStringLocalizer`）**沒有 customizeId 管道**——
導致最常見的客製需求「**欄位標題／表單名稱改成公司慣用語**」目前完全不支援。

> **2026-08-01 進度更正（G1–G3 落地後）**：上述缺口**已補完**——四個伺服端消費端都接上
> `SessionInfo.CustomizeId`，「欄位標題／表單名稱改成公司慣用語」在伺服端已支援（§1.2）。
> 仍未做的是 `GetLanguage` 的客製管道（G4）與端到端驗證（G6，卡 foundation F3）。
> 且**實務上還沒有部署會餵值進來**：至今沒有 head 走過 `EnterCompany`，`CustomizeId` 恆為空
> （foundation §2.C）。

> **2026-08-01 再更正（G4 + G6 落地後，本案結案）**：`GetLanguage` 的客製管道（G4）與
> 端到端驗證（G6，隨 foundation F3）都已完成。「帶 `CustomizeId` 的 session →
> 欄位 caption 取到客製值」已有整合測試把關，不再是紙上能力。
> 唯 head 端仍無部署會走 `EnterCompany`（foundation §2.C），屬「能力備妥、尚無使用者」。

---

## 1. 現況

### 1.1 已實作且設計正確：per-key 疊加

`LanguageService.cs:90-112`：
```csharp
public bool TryGetLangText(string customizeId, string lang, string @namespace, string subKey, out string text)
{
    if (TryGetCustomizeResource(customizeId, lang, @namespace, out var custResource)
        && custResource!.Items.Contains(subKey))   // ← per-key，客製檔只需放要改的 key
    { text = custResource.Items[subKey].Value; return true; }
    var resource = _defineAccess.GetLanguage(lang, @namespace);
    ...
}
```
**客製檔只需放要覆蓋的 key，其餘自動 fallback base。** 這正是「公司慣用語」需要的粒度。

介面已備妥：`ILanguageService.cs:67,106,139,161` — 4 個 customizeId-aware **default interface method**，
預設委派 base（fail-safe）。

### 1.2 缺口：消費端沒有 customizeId 管道（**G1–G3 已補完**）

| 消費端 | 位置 | 問題 | 影響 | 狀態 |
|--------|------|------|------|------|
| **`FormSchemaLocalizer`** ★ | `:73,89,101,136` | 只用 base overload | **欄位 caption / 表單 DisplayName 無法客製** | ✅ G1 |
| `BeeStringLocalizer<T>` | `:60,66` | 建構子只有 `Func<string> langProvider`，**連傳入管道都沒有** | 一般 UI 字串無法客製 | ✅ G2（無消費端，未端到端驗證） |
| `BusinessObject.GetLangText` | `:95-105` | 只傳 `GetCurrentLang()` | BO 訊息無法客製 | ✅ G3 |
| `GetLanguage` API | `SystemBusinessObject.Define.cs:150-165` | 直接 `DefineAccess.GetLanguage(lang, ns)` | client 拿到的整份資源是 base only | 📝 G4 未做——但**方向已由 A2 改為「回原始定義、需求端疊加」**，不是原本設想的 server 疊好 |

> `BusinessObject.GetCurrentLang()`（`:114-119`）讀的是 `SessionInfo.Culture`——
> **同一個 SessionInfo 就在手上，卻沒讀 `CustomizeId`**。接線成本極低。

> ★ `FormSchemaLocalizer` 是你提的「某些單據有公司慣用語」的**主要路徑**
> （sub-key 規範：`Schema.DisplayName` / `Table.X.DisplayName` / `Field.X.Caption`），
> 也是目前缺口最明確的一項。

### 1.3 Enum 客製粒度較粗（**非缺口，已裁決維持**）

`LanguageService.LookupEnum`（`:158-170`）是 **enum 級**（非 entry 級）——
cust 一旦有同名 enum 就**整組取代**，無法只改一個 code 的文字。

> ~~與文字的 key 級疊加不一致。客製只想改一個選項，就得複製整組 enum 的所有 entry。~~

> **2026-08-01 裁決：這是刻意的，不是缺口**（決策 G1-b）。per-key 疊加的適用對象是
> `LanguageItem` 的 Key；`LanguageEnum` 是有順序的選項集，與 FormLayout 同屬「整體才成立」
> 的一類，故整組取代。客製檔須列出該選項集要有的全部 entry。

### 1.4 測試

`tests/Bee.Definition.UnitTests/Language/LanguageServiceCustomizeTests.cs`（7 測試）：
per-key 命中/落空、enum、空 id 短路(`:58`)、無 reader(`:73`)。
**皆為手動傳 customizeId 的元件級測試**；
**沒有** `BeeStringLocalizer` / `FormSchemaLocalizer` 的客製測試（因為它們根本沒有客製能力）。

> **2026-07-31（G1–G3 落地）補測**：新增 4 檔／25 測試，其中 10 個是**回歸防護**
> （未設 CustomizeId → reader 零呼叫、結果與舊多載逐位元一致）：
> `FormSchemaLocalizerCustomizeTests`、`BeeStringLocalizerCustomizeTests`（Bee.Definition.UnitTests）、
> `BusinessObjectLangCustomizeTests`、`BusinessObjectFactoryCustomizeTests`（Bee.Business.UnitTests）。
> 仍**全為元件級** —— 端到端（帶 CustomizeId 的 session → API）是 G6，卡在 foundation F3。
>
> **2026-08-01（G6 落地）補測**：`tests/Bee.Api.Client.UnitTests/Customization/TenantCustomizationEndToEndTests.cs`
> 補上端到端層——租戶一律由 session 決定，測試本身不傳 customizeId。

---

## 2. 設計決策

### 決策 G1：Enum 覆蓋粒度 — ✅ 已定案（2026-08-01）：採 G1-b，維持整組取代

- **選項 G1-a**：改為 **entry 級**覆蓋。
  優點：客製檔最小、base 新增 entry 自動傳播。
  缺點：需改 `LookupEnum` 疊加邏輯；順序與「刪除某個 entry」的語意都需另外表達。
- **選項 G1-b（採用）**：維持整組取代。cust 有同名 enum 就整組換掉 base 的。
  優點：零改動、語意單純。缺點：客製檔冗長、base 新增 entry 不會傳播到客製版。

> **定案理由**：per-key 疊加的適用對象是 **`LanguageItem` 的 Key**——文字 key 彼此獨立，
> 「這個標題叫法不同」不影響其餘任何一個 key。`LanguageEnum` 不是這種東西：它是一組
> **有順序的選項集**，逐 entry 合併會讓順序、以及「客製檔沒列到的 entry 是什麼意思」
> 兩件事都變曖昧。判別線是「一袋彼此獨立的值」vs「組合起來才成立的整體」——
> **enum 與 FormLayout 同屬後者**，客製了就完整擁有那一份。
>
> 這條分界已寫進公開文件 [`definition-files-overview`](../../definition-files-overview.md) §7（雙語），
> 免得日後有人把「enum 沒做 entry 級」當成缺口去「修」。

> **2026-08-01 一度誤實作為 G1-a 後已回退**（commit `9e3ce317` → `70d703aa`）。
> 誤判來源：把使用者對文字 key 的通則描述外推到 enum entry。
> `LookupEnum` 現況即最終樣貌：cust 有同名 enum 直接回 cust 實例，否則回 base 實例，
> 兩者都是快取實例、零複製、零配置。

### 決策 G2：customizeId 取得方式 — ✅ 已定案（2026-07-31，由 foundation 決策 A1 涵蓋）

**兩個 localizer 走不同路，不是同一種作法**——A1 依「消費端手上有沒有 session」二分：

| 型別 | 作法 | 理由 |
|------|------|------|
| `FormSchemaLocalizer` | **顯式傳參**（`Localize` 多載加 customizeId） | 它由 BO 在 `SystemBusinessObject.Define.cs:219` 直接 `new`，**session 就在手上**，繞一層委派沒有好處 |
| `BeeStringLocalizer<T>` | **委派**（建構子加 `Func<string> customizeIdProvider` 多載，與既有 `Func<string> langProvider` 對稱，`:46`） | 它是註冊進 DI 給 Blazor `@inject IStringLocalizer<T>` 用的 adapter，**沒有 session 概念** |

> 原 G2-a 提議「兩者都用委派」——**已由 A1 修正為上表的二分**。
> 未採用 G2-b（注入 `ISessionInfoService`）：會讓純函式風格的 localizer 加重相依。
> 完整理由與安全界線（伺服端永不採信 client 傳回的 customizeId）見
> [plan-customization-foundation.md](plan-customization-foundation.md) §2.A。

### 決策 G3：`GetLanguage` API 的疊加位置 — ✅ 已定案（2026-08-01）：採 G3-b

> 由 foundation [決策 A2](plan-customization-foundation.md) 定調：API 只供應**原始定義**，
> 套裝／客製的選用抽成**前後端通用的取用類別**（放 `Bee.Definition`），需求端拿到兩份後自行疊加。
> 套裝與客製**各取一次**，讓 connector 的方法合約不變。
>
> 這同時修正了 §1.2 表格最後一列與階段 G4 的方向：**不是**「server 疊好再回傳」，
> 而是「回原始定義，需求端用共用類別疊加」。伺服端自己的疊加（BO 訊息、schema 在地化）
> 也改用同一個類別，確保兩端演算法只有一份。
>
> 安全界線不變：client 取客製語系時**不得指定 customizeId**，要哪個租戶一律由
> `SessionInfo.CustomizeId` 決定。共用的是選用演算法，不是選擇權。

client（含**純 JS，無 .NET**）透過 `GetLanguage` 取整份語系資源，目前回 base only。

- **選項 G3-a（建議）**：**server 端依 session 的 CustomizeId 疊加好再回傳**。
  client 完全不需知道客製的存在。
  優點：純 JS client 零改動；wire 格式不變。
  缺點：同一 namespace 對不同租戶回不同內容 → **必須搭配 client 端快取失效**
  （foundation 缺口 C：`ClientInfo.ResetDefineCache()` 目前無呼叫端）。
- **選項 G3-b**：回傳 base + cust 兩份，client 自行疊加。
  優點：server 無狀態、可快取。缺點：每個 client（含 JS）都要實作疊加邏輯，違反「client 無感」。

> **G3-a 有前置相依**：不補 foundation 缺口 C，切換租戶後 client 會拿到前一租戶的客製文字。

---

## 3. 建議階段

| 階段 | 範圍 | 前置 | 狀態 |
|------|------|------|------|
| G0 | 決策定案（G1 enum 粒度、G2 傳遞方式、G3 API 疊加） | foundation F0 | ✅ 全數定案（2026-08-01）：G1-b、G2 由 A1 涵蓋、G3-b 由 A2 定調 |
| G1 | `FormSchemaLocalizer` 加 customizeId 管道 ★ | foundation F1、F2 | ✅ 已完成（2026-07-31）。`Localize(schema, customizeId, lang)` 多載；呼叫端 `SystemBusinessObject.Define.cs` `LoadAndLocalizeSchema` 傳 `GetCurrentCustomizeId()` |
| G2 | `BeeStringLocalizer<T>` 加 customizeId 管道 | 同上 | ✅ 已完成（2026-07-31）。3-arg ctor 加 `Func<string> customizeIdProvider`。**注意：repo 內無任何註冊點與消費端**，故只加得了 API，無法端到端驗證 |
| G3 | `BusinessObject.GetLangText` 接線（讀 `SessionInfo.CustomizeId`） | 同上 | ✅ 已完成（2026-07-31）。新增 `GetCurrentCustomizeId()`（比照 `GetCurrentLang()`）；`GetLangText(fullKey)` 改為就地切 key 後走 customizeId 多載 |
| G4 | `GetLanguage` 改回原始定義 + 補「取客製語系」的對應方法；疊加交給共用取用類別 | foundation 決策 A2 | ✅ 已完成（2026-08-01）。`GetLanguage` 回原始 XML（`ec94d0aa`）、新增 `System.GetCustomizeLanguage` 與 connector 方法（`bbd2fd2a`）、疊加走 `CustomizeOverlay`（`450ae846`） |
| G5 | Enum entry 級覆蓋（若選 G1-a） | 決策 G1 | ❌ 不做（2026-08-01）。決策 G1 定案 G1-b，維持整組取代；此階段取消 |
| G6 | 端到端測試：帶 CustomizeId 的 session → API → 拿到客製文字 | foundation F3 | ✅ 已完成（2026-08-01，隨 foundation F3）。`TenantCustomizationEndToEndTests`：進入帶 `customize_id` 的公司後，`FormDefinitionLoader` 取得的欄位 caption 為客製值、未覆寫的 key 仍為 base；離開公司即回 base；另一租戶（無客製檔）與未進公司的 session 皆與純 base 逐位元一致 |

> 回歸防護：**未設 CustomizeId 時，所有語系查找結果與現況逐位元一致**。

---

## 4. 給 review 的提問

1. ~~**客製 enum 是「改一兩個選項說法」還是「整組不同」？** → 決定 G1 是否要做 entry 級。~~
   ✅ 2026-08-01 定案：**整組取代**（G1-b，見 §2 決策 G1）。per-key 疊加只適用 `LanguageItem` 的 Key。
2. **客製語系的涵蓋面**：主要是欄位標題／表單名稱（`FormSchemaLocalizer`），
   還是也包含 BO 訊息、驗證錯誤文字、UI 一般字串？→ 決定 G1~G3 的優先序。
3. ~~**`GetLanguage` API 疊加**走 server 端疊好（G3-a）還是 client 自行疊加（G3-b）？~~
   ✅ 2026-08-01 定案：**G3-b**，由 foundation 決策 A2 定調（API 回原始定義、共用取用類別疊加）。
4. **客製語系檔的維護方式**？由誰產生、是否需要系統內編輯
   （目前 `SaveXxx` 全 throw，見 foundation §3）。
