# Plan：`FieldDbType.Time` 純時刻型別（討論稿）

**狀態：📝 擬定中（2026-07-25）**

> **這是討論稿，不是可執行計畫。** 目的是把
> [plan-datetime-timezone.md](plan-datetime-timezone.md) 討論過程中推導出的約束記下來，
> 避免日後動工時重新推導或誤踩。有實際需求時再展開為正式 plan。

---

## 1. 現況

`FieldDbType`（`src/Bee.Base/Data/FieldDbType.cs`）目前為：
`String` / `Text` / `Boolean` / `AutoIncrement` / `Int16` / `Int32` / `Int64` /
`Decimal` / `Currency` / `Date` / `DateTime` / `Guid` / `Binary` / `Unknown`。

**無 `Time`**。純時刻值（上班時間 08:30、營業時間、班別起訖）目前只能以
`String` 或 `DateTime` 勉強表達。

## 2. 為何現在不做

時區 plan 只需要「日曆日 vs 時間點」這條界線，`Date` / `DateTime` 兩個現有值已足夠。
`Time` 是獨立議題，與時區設計無互鎖。

## 3. 已確立的約束（本次討論的產出，動工時直接沿用）

### 3.1 新值必須加在列舉尾端

`FieldDbType` 目前**未顯式指定數值**（隱含 `0..N`），而它會上 MessagePack wire。
在中間插入 `Time`（例如排在 `Date` 旁邊求語意相鄰）會讓其後所有值的數值位移，
**打斷既有 payload 與既有定義檔的相容性**。

→ 一律 append 至尾端；或此次順帶改為顯式指定數值後再新增。

### 3.2 `Time` 屬於「絕不轉時區」

純時刻值與日曆日同為牆上時間，套用時區位移會得到無意義的結果。
在時區 plan 的 Connector 判斷表中，`Time` 與 `Date` 同列（絕不轉）。

→ 此結論已載入時區 plan 的 ADR，`Time` plan 不需重新推導。

## 4. 待討論議題

| 議題 | 說明 |
|------|------|
| CLR 承載型別 | `TimeOnly` / `TimeSpan` / 字串？參考前置 plan 的教訓——`Date` 曾試「以 `DateOnly` CLR 型別承載語意」後否決、改為顯式標記（見 [plan-date-semantics.md](plan-date-semantics.md)），`Time` 是否重蹈需先評估 |
| 各 provider 型別對應 | SQL Server `time`、PostgreSQL `time`、MySQL `TIME`、SQLite `TEXT` 皆有對應；**Oracle 無 `TIME` 型別**（只有 `DATE` / `TIMESTAMP` / `INTERVAL DAY TO SECOND`），需先定 Oracle 的落地方式，這是可行性的關鍵前提 |
| 取值層 | `ValueUtilities` 是否新增 `CTime`，與 `CDate` / `CDateTime` 家族對稱 |
| 三棲序列化 | XML / JSON / MessagePack 對承載型別的支援；若採 `TimeOnly`，MessagePack 是否有內建 formatter 需確認 |
| typeless 白名單 | 若承載型別會進 `FilterCondition.Value`，需補 `SafeTypelessFormatter` 白名單（`src/Bee.Definition/Serialization/SafeTypelessFormatter.cs`）——`DateOnly` 已有同樣缺口可為前車之鑑 |
| 標記 helper | 前置 plan 的 `ResolveFieldDbType` / `ApplyFieldDbType` / `GetDeclaredFieldDbType` 需納入新值 |
| UI 層 | `FormField` 與各 UI 端（Avalonia / MAUI / Blazor）的時刻編輯控件 |

## 5. 展開時機

**這是延後、不是擱置**——`Time` 是確定要補的表達力缺口，只是排在時區 plan 之後。
待有實際業務需求（排班、營業時間、班別定義等）牽動優先序時，將本文展開為正式 plan。
在此之前不動 `FieldDbType`。
