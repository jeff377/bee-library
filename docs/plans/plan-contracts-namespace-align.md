# 計畫:Bee.Api.Contracts 合約介面命名空間按 BO 軸對齊

**狀態:🚧 進行中(2026-07-23)**

| 階段 | 範圍 | 狀態 |
|------|------|------|
| 1 | System 軸:介面 + DTO 移入 `System/`,修消費端 using | ✅ 已完成(2026-07-23) |
| 2 | Form 軸:介面移入 `Form/`,修消費端 using | ✅ 已完成(2026-07-23) |
| 3 | AuditLog 軸:介面 + DTO 移入 `AuditLog/`,修消費端 using | ✅ 已完成(2026-07-23) |
| 4 | 文件:Contracts README(雙語)✅;CHANGELOG 併發版 draft;ADR 待定 | 🚧 進行中 |

## 背景

三層 DTO 命名空間組織現況:

| 層 | 專案 | 組織方式 |
|----|------|---------|
| BO | `Bee.Business` | ✅ 依軸分:`.System` / `.Form` / `.AuditLog` / `.Permission` / `.Security` |
| 合約**實作** | `Bee.Api.Core.Messages` | ✅ 依軸分:`.System` / `.Form` / `.AuditLog`(+ 根層放跨軸與基礎型別) |
| 合約**介面** | `Bee.Api.Contracts` | ❌ **全平**:60 檔全擠在 `Bee.Api.Contracts` 單一命名空間 |

`Bee.Api.Core.Messages.System.LoginRequest`(實作)implements `Bee.Api.Contracts.ILoginRequest`(介面)—— 實作已進 `.System` 子命名空間,介面卻留在平的根命名空間。**此不對稱只存在於介面層**,BO 與實作兩層都已按軸分好。本計畫消除此不對稱,讓合約介面與其實作、對應 BO 三者命名空間軸一致。

### 為何現在做

- **無實際外部消費者**,破壞性 namespace 變更成本最低,不需相容 shim。
- 綁下一個 **major** 版本發佈(對齊 ADR-030 breaking-change 綁 major 慣例)。
- 變更**純為 source-level**(介面命名空間),**零 wire 影響**(見下節)。

### 零 wire 影響(重要前提)

- 移動的是**介面**;實際序列化的是 `Bee.Api.Core.Messages.*` 的**實作類別**,其命名空間**不變**。
- MessagePack 採 property-name key(keyAsPropertyName),key 來自屬性名而非型別/命名空間 → 不受影響。
- XML 持久化不涉及這些 API 介面。
- 結論:對 client/server wire round-trip **完全無影響**,只是 `using` 換位置。

## 目標命名空間對應(權威表)

軸歸屬以**實作層 `Bee.Api.Core.Messages/{軸}/` 現有分法**為權威來源反查而得。

### System 軸 → `Bee.Api.Contracts.System`(32 檔)

介面(29):`ICheckPackageUpdateRequest`/`Response`、`ICreateSessionRequest`/`Response`、`IEnterCompanyRequest`/`Response`、`IGetCommonConfigurationRequest`/`Response`、`IGetDefineRequest`/`Response`、`IGetDepartmentTreeResponse`、`IGetFormLayoutRequest`/`Response`、`IGetFormSchemaRequest`/`Response`、`IGetLanguageRequest`/`Response`、`IGetPackageRequest`/`Response`、`ILeaveCompanyRequest`/`Response`、`ILoginRequest`/`Response`、`ILogoutRequest`/`Response`、`IPingRequest`/`Response`、`ISaveDefineRequest`/`Response`

DTO(3):`PackageDelivery`(enum)、`PackageUpdateInfo`、`PackageUpdateQuery`

### Form 軸 → `Bee.Api.Contracts.Form`(12 檔)

介面(12):`IDeleteRequest`/`Response`、`IGetDataRequest`/`Response`、`IGetListRequest`/`Response`、`IGetLookupRequest`/`Response`、`IGetNewDataRequest`/`Response`、`ISaveRequest`/`Response`

### AuditLog 軸 → `Bee.Api.Contracts.AuditLog`(14 檔)

介面(13):`IGetAccessLogRequest`、`IGetApiAnomalyLogRequest`、`IGetApiAnomalySummaryRequest`、`IGetChangeDetailRequest`/`Response`、`IGetChangeLogRequest`/`Response`、`IGetDbAnomalyLogRequest`、`IGetDbAnomalySummaryRequest`、`IGetLoginLogRequest`、`IGetTopApiMethodsRequest`、`ILogAggregateResponse`、`ILogListResponse`

DTO(1):`RecordFieldChange`

> 註:多個 AuditLog `*Request` 無獨立 `*Response` 介面,其回應共用 `ILogListResponse` / `ILogAggregateResponse` —— 正常,仍全歸 AuditLog 軸。

### 保留在根 `Bee.Api.Contracts`(2 檔)

`IExecFuncRequest`、`IExecFuncResponse`

理由:其實作 `ExecFuncRequest` / `ExecFuncResponse` 位於 **Messages 根**命名空間 `Bee.Api.Core.Messages`(非任何子軸),因 ExecFunc 是跨 BO 的泛用 AnyCode 派發。為維持與實作層鏡像,介面同樣留根。

**合計:32 + 12 + 14 + 2 = 60 檔 ✓**

## 影響面

- **118 個檔案** `using Bee.Api.Contracts;`,分佈:`Bee.Api.Core.Messages`(56)、`Bee.Business`(56)、`Bee.Api.Core.UnitTests` / `Bee.Business.UnitTests`(5)、其他。
- 消費端已按軸自我分群(如 `Business.System/*` 只引 System 合約),故多數檔案只需補**單一**軸 using;跨軸檔(如 `ApiContractRegistry`、`MessagePackContractsTests`)需補多個。
- **靠編譯器定位**:`TreatWarningsAsErrors=true` 下,移檔後 build 會精準列出每個缺的 `using`,逐一補齊即可,不需事先人工判斷每檔引哪些型別。

## 執行策略

### 逐軸分階段(每階段結束 build 綠)

每個軸為一個獨立可交付階段,順序 System → Form → AuditLog。每階段:

1. 在 `src/Bee.Api.Contracts/` 建 `{軸}/` 子資料夾。
2. `git mv` 該軸介面 + DTO 檔進子資料夾。
3. 逐檔改 `namespace Bee.Api.Contracts` → `namespace Bee.Api.Contracts.{軸}`。
4. `dotnet build` → 依編譯錯誤,在消費端補 `using Bee.Api.Contracts.{軸};`。
5. 該階段 build + 受影響測試綠 → commit。

> **csproj 無需改**:`Bee.Api.Contracts.csproj` 為 SDK-style glob include,新增子資料夾自動納入。
>
> **IDE0130**:子資料夾對映子命名空間,符合資料夾=命名空間規範(與 `Messages.System` 等一致),不觸發告警。

### 跨軸消費端(每階段都可能碰)

- `ApiContractRegistry`(生產端,若有 method→型別對照)、`ApiContractRegistryTests`、`MessagePackContractsTests`、`AuditLogMessagePackTests` 等引用多軸型別的檔案,會在對應階段累加 using。
- 建議:跨軸檔在最後涉及的階段一次補齊,或每階段補該軸部分,擇一即可(編譯器把關,不會漏)。

## 驗證

- 每階段:`dotnet build --configuration Release` 全綠(`TreatWarningsAsErrors` 把關 using / IDE0130)。
- 全部完成後:`./test.sh` 或至少 `Bee.Api.Core.UnitTests` + `Bee.Business.UnitTests`,確認:
  - MessagePack / JSON round-trip 測試通過(佐證零 wire 影響)。
  - `ApiContractRegistry` 相關測試通過(型別對照正確)。
- 確認全 repo `grep -rn "namespace Bee.Api.Contracts$"` 僅剩 ExecFunc 2 檔在根命名空間。

## 階段 4:文件與發佈

- **Contracts README(雙語)**:✅ 已更新 —— 新增「軸分命名空間」設計慣例、重畫目錄結構為 System/Form/AuditLog 子資料夾 + ExecFunc 留根。
- **CHANGELOG**:**不現在起 major 段落**。目前 CHANGELOG 頂部為已發布的 4.14.0(minor);本破壞性變更綁下一個 major,而該 major 尚未起段落(adr-030 property-name key 那批亦未進)。依 releasing 慣例「累積到發版時用 `/changelog-draft` 掃 commit 統整」,三個 commit 皆帶 `refactor(contracts)!:` + `BREAKING CHANGE:` footer,發版 draft 時會被掃到。
- **ADR**:待使用者決定是否新增獨立 ADR。決議脈絡已存於本 plan 與 commit footer;獨立 ADR 對「命名空間重組」而言較既有 ADR 顆粒度重,故預設不建、由使用者拍板。
- **綁 major**:此變更隨下一個 major tag 發佈,不進 patch/minor。

## 尚未決策 / 待確認

1. **ExecFunc 留根**:本計畫採「鏡像實作層 → 留根」。若偏好「所有 API 介面一律入軸」(ExecFunc 另立 `.Execution` 之類),需先定;預設留根。
2. **是否同批合併 major 內其他 breaking change**:如與 ADR-030 property-name key 那批同一 major 視窗,可一次發。
