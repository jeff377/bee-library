---
name: bee-add-form
description: 在一個已接好的 Bee.NET app 上「加一張表單」的多檔流程與避雷 —— 一張可用的 CRUD 表單 = 4 處純定義修改（FormSchema + TableSchema + DbCategorySettings 註冊 + ProgramSettings 上選單），不寫 UI / CRUD 程式碼。涵蓋 4 檔 checklist（漏哪個會有什麼徵兆）、business 表用 company scope、TableSchema 資料夾必須 = CategoryId、FormSchema 慣例（lookup 的 RelationProgId+RelationFieldMappings+ref_* RelationField、master-detail 的 sys_master_rowid、DropDownEdit+ListItems、計算欄 FormField.ReadOnly、sys_name 可省）、何時才需要自訂 BO。當使用者要「加一張表單 / 主檔 / 單據」、「新增一個 ProgId / 畫面」、「為某張表做 CRUD」、「Bee 表單要怎麼定義 lookup / 明細 / 下拉 / 唯讀欄」之類需求時使用，即使沒明講「加表單」也要主動觸發。
---

# Bee app 加一張表單

在已接好的 Bee 後端（見 `bee-app-scaffold`）上加一張**可用的 CRUD 表單**，是 **4 處純定義修改**，不寫 UI 也不寫 CRUD code。FormLayout 由框架從 FormSchema 自動產生，不必寫。

> **參考實作**：`apps/Bee.Northwind/Define/`（純主檔、雙 lookup 的 Product、master-detail 的 Order）。對著抄最快。

## 適用場景

- 加一張新表單到既有 Bee app（主檔、含 lookup 的主檔、master-detail 單據）
- 想知道 FormSchema 怎麼表達 lookup / 明細 / 下拉 / 唯讀欄

## 不適用

- 還沒接好後端 host → 先做 **`bee-app-scaffold`**
- 表單需要框架無法以定義表達的業務邏輯（單號、狀態機、驗證、金額）→ 表單照本 skill 加，**業務碼**走 **`bee-add-bo-method`** 或直接 override `Save`/`GetNewData`（見「何時需要自訂 BO」）
- 從現成 FormSchema 反推 layout/language/tableschema sidecar → **`bee-scaffold-from-formschema`**

## 4 處修改（漏一個的徵兆）

| # | 檔案 | 作用 | 漏掉的徵兆 |
|---|------|------|-----------|
| 1 | `Define/FormSchema/<ProgId>.FormSchema.xml` | 表單欄位 + 清單欄 + lookup | 表單開不出來 |
| 2 | `Define/TableSchema/<categoryId>/<table>.TableSchema.xml` | DB 表結構 + 索引 | seeder 建不出表 / CRUD 失敗 |
| 3 | `Define/DbCategorySettings.xml` 對應 category 加 `<TableItem>` | 註冊表 → seeder 才會建、router 才知 table→db | 表沒建出來 |
| 4 | `Define/ProgramSettings.xml` 加 `<ProgramItem>` | 上資料驅動選單（+ 選配 BO 綁定） | 表單不出現在選單 |

> **業務表全用 `company` scope**：FormSchema `CategoryId="company"`、TableSchema 放 `TableSchema/company/`、DbCategorySettings 掛 company 分類。CategoryId 是 DB scope 選擇器（common/company/log），不是自由標籤——business 資料掛 common 是錯的（見 `bee-app-scaffold` Part 1 / memory `categoryid-is-db-scope-selector`）。**TableSchema 資料夾名必須 = CategoryId**。

加完 **重啟 server（建表）+ 重啟前端**，即得完整 list / new / edit / delete + `uk_` 唯一檢查。

## FormSchema 慣例

### 鍵與系統欄（每張表）
`sys_no`(AutoIncrement, Visible=false) / `sys_rowid`(Guid, Visible=false) / `sys_id`(String 業務代碼) / `sys_name`(名稱)。
**`sys_name` 可省**：只有「被 lookup 引用的來源表」需要它（lookup 顯示回退用）；單據類（如 Order）無自然名稱可不放。

### TableSchema 索引慣例
`pk_{0}`(sys_no, PrimaryKey) / `rx_{0}`(sys_rowid, Unique) / `uk_{0}`(sys_id, Unique) / 每個關連欄 `fk_{0}_<col>`。

### Lookup（跨表關連，零程式碼）
關連欄（Guid）放 `RelationProgId` + `RelationFieldMappings`，把目標的欄位寫回本表的 `ref_*` 顯示欄；`ref_*` 欄標 `Type="RelationField"`。框架自動渲染成 ButtonEdit 開窗、寫回顯示值、重載時由 server JOIN 重算。

```xml
<FormField FieldName="customer_rowid" Caption="Customer" DbType="Guid" RelationProgId="Customer">
  <RelationFieldMappings>
    <FieldMapping SourceField="sys_id" DestinationField="ref_customer_id" />
    <FieldMapping SourceField="sys_name" DestinationField="ref_customer_name" />
  </RelationFieldMappings>
</FormField>
<FormField FieldName="ref_customer_id" Caption="Customer Code" DbType="String" Type="RelationField" />
<FormField FieldName="ref_customer_name" Caption="Customer Name" DbType="String" Type="RelationField" />
```
- 來源表 FormSchema 要有 `LookupFields="sys_id,sys_name"`（複合顯示「編號 - 名稱」）。
- 業務表可指向框架表（如 Order.employee → `st_employee`），反之亦然。

### Master-detail（單據）
主表 `FormTable.TableName == ProgId`（框架不變式）。明細是第二個 `FormTable`，欄位含 `sys_master_rowid`(Guid, Visible=false) 指主表。明細的 lookup（每列選一個目標）寫法同上 —— 框架在 InCell grid 渲染開窗。整筆一次儲存/重載。

### 固定選項下拉
`ControlType="DropDownEdit"` + `<ListItems><ListItem Value=".." Text=".."/></ListItems>`；預設值用 `DefaultValue="..."`。

### 計算 / 唯讀欄
`FormField.ReadOnly="true"` —— 計算欄或伺服器衍生欄（如 BO 算出的金額）標唯讀，主檔欄與明細 InCell 格皆呈現唯讀。免寫 FormLayout。

### 清單欄
`ListFields="sys_id,sys_name,ref_xxx_name,..."` 控制清單檢視欄（顯示 `ref_*` 比 `*_rowid` 友善）。

## 何時需要自訂 BO

預設 `FormBusinessObject` 管 CRUD（純定義）。**只有**框架無法以定義表達的才寫業務碼：單號生成、狀態機 / 合法轉移、必填驗證、金額計算（不信任前端值）。做法：`ProgramSettings` 的 `ProgramItem.BusinessObject="Ns.OrderBO, Asm"` → override `Save` / `GetNewData`，純規則抽到 DB-free helper（可讀可測）。詳見參考實作 `OrderBO` / `bee-add-bo-method`。

## 種子（選配）

要預載資料：`Bee.Northwind.Server/SeedData/<Table>.json`，關連欄填目標 `sys_id`（seeder 解析成 `sys_rowid`）。新表只是要 CRUD、不必種子（使用者自己在 UI 建）。

## 完成檢查

- [ ] 4 檔都改了（FormSchema / TableSchema / DbCategorySettings / ProgramSettings）
- [ ] 業務表 `CategoryId="company"`、TableSchema 在 `company/` 資料夾
- [ ] lookup 來源表有 `LookupFields`；`ref_*` 欄標 `Type="RelationField"`
- [ ] 重啟 server + 前端，表單出現在選單、CRUD 可用、lookup 開窗正常
