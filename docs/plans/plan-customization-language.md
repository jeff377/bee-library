# Plan：語系客製化（討論稿）

> 狀態：🚧 進行中（G1–G3 已交接實作；G4、G5 待 G1 / G3 決策）· 2026-07-31
> 範圍：**語系資源的租戶客製**——某些單據或行為，公司有慣用語（欄位標題、表單名稱、訊息、選項文字）。
> 前置：[客製化共同前置](plan-customization-foundation.md)（缺口 A、B 未補則本案無法生效）
> 相關：[Layout 客製](plan-customization-layout.md)｜[業務邏輯客製](plan-customization-business.md)｜[ADR-016](../adr/adr-016-multitenant-customization-overlay.md)

---

## 0. 一句話結論

**三類中設計最正確、缺口最明確、也最好補的一類。**
`LanguageService` 的 **per-key 疊加**已正確實作，但兩個主要消費端
（`FormSchemaLocalizer` / `BeeStringLocalizer`）**沒有 customizeId 管道**——
導致最常見的客製需求「**欄位標題／表單名稱改成公司慣用語**」目前完全不支援。

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

### 1.2 缺口：消費端沒有 customizeId 管道

| 消費端 | 位置 | 問題 | 影響 |
|--------|------|------|------|
| **`FormSchemaLocalizer`** ★ | `:73,89,101,136` | 只用 base overload | **欄位 caption / 表單 DisplayName 無法客製** |
| `BeeStringLocalizer<T>` | `:60,66` | 建構子只有 `Func<string> langProvider`，**連傳入管道都沒有** | 一般 UI 字串無法客製 |
| `BusinessObject.GetLangText` | `:95-105` | 只傳 `GetCurrentLang()` | BO 訊息無法客製 |
| `GetLanguage` API | `SystemBusinessObject.Define.cs:150-165` | 直接 `DefineAccess.GetLanguage(lang, ns)` | **client（含純 JS）拿到的整份資源是 base only** |

> `BusinessObject.GetCurrentLang()`（`:114-119`）讀的是 `SessionInfo.Culture`——
> **同一個 SessionInfo 就在手上，卻沒讀 `CustomizeId`**。接線成本極低。

> ★ `FormSchemaLocalizer` 是你提的「某些單據有公司慣用語」的**主要路徑**
> （sub-key 規範：`Schema.DisplayName` / `Table.X.DisplayName` / `Field.X.Caption`），
> 也是目前缺口最明確的一項。

### 1.3 Enum 客製粒度較粗

`LanguageService.LookupEnum`（`:158-170`）是 **enum 級**（非 entry 級）——
cust 一旦有同名 enum 就**整組取代**，無法只改一個 code 的文字。

> 與文字的 key 級疊加不一致。客製只想改一個選項，就得複製整組 enum 的所有 entry。

### 1.4 測試

`tests/Bee.Definition.UnitTests/Language/LanguageServiceCustomizeTests.cs`（7 測試）：
per-key 命中/落空、enum、空 id 短路(`:58`)、無 reader(`:73`)。
**皆為手動傳 customizeId 的元件級測試**；
**沒有** `BeeStringLocalizer` / `FormSchemaLocalizer` 的客製測試（因為它們根本沒有客製能力）。

---

## 2. 設計決策

### 決策 G1：Enum 覆蓋粒度

- **選項 G1-a（建議）**：改為 **entry 級**覆蓋，與文字 key 級一致。
  客製檔只放要改的 entry，其餘 fallback base。
  優點：一致、客製檔最小、base 新增 entry 自動傳播。
  缺點：需改 `LookupEnum` 疊加邏輯；「刪除某個 entry」的語意需另外表達（若有此需求）。
- **選項 G1-b**：維持整組取代。
  優點：零改動、語意單純。缺點：客製檔冗長、base 新增 entry 不會傳播到客製版。

> 待討論：實務上客製 enum 是「改一兩個選項的說法」還是「整組選項都不同」？
> 前者強烈指向 G1-a。

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

### 決策 G3：`GetLanguage` API 的疊加位置

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
| G0 | 決策定案（G1 enum 粒度、G2 傳遞方式、G3 API 疊加） | foundation F0 | 🚧 G2 ✅ 已定案（由 A1 涵蓋）；**G1、G3 仍未決** |
| G1 | `FormSchemaLocalizer` 加 customizeId 管道 ★ | foundation F1、F2 | 🚧 已交接實作（2026-07-31） |
| G2 | `BeeStringLocalizer<T>` 加 customizeId 管道 | 同上 | 🚧 已交接實作（2026-07-31） |
| G3 | `BusinessObject.GetLangText` 接線（讀 `SessionInfo.CustomizeId`） | 同上 | 🚧 已交接實作（2026-07-31） |
| G4 | `GetLanguage` API 依 G3 決策疊加 | 決策 G3 + **foundation F3**（client 快取失效） | 📝 待做（決策未定，**不在本次交接範圍**） |
| G5 | Enum entry 級覆蓋（若選 G1-a） | 決策 G1 | 📝 待做（決策未定，**不在本次交接範圍**） |
| G6 | 端到端測試：帶 CustomizeId 的 session → API → 拿到客製文字 | foundation F3 | 📝 待做（**不在本次交接範圍**） |

> 回歸防護：**未設 CustomizeId 時，所有語系查找結果與現況逐位元一致**。

---

## 4. 給 review 的提問

1. **客製 enum 是「改一兩個選項說法」還是「整組不同」？** → 決定 G1 是否要做 entry 級。
2. **客製語系的涵蓋面**：主要是欄位標題／表單名稱（`FormSchemaLocalizer`），
   還是也包含 BO 訊息、驗證錯誤文字、UI 一般字串？→ 決定 G1~G3 的優先序。
3. **`GetLanguage` API 疊加**走 server 端疊好（G3-a）還是 client 自行疊加（G3-b）？
   若走 G3-a，foundation 缺口 C（client 快取失效）必須一起補。
4. **客製語系檔的維護方式**？由誰產生、是否需要系統內編輯
   （目前 `SaveXxx` 全 throw，見 foundation §3）。
