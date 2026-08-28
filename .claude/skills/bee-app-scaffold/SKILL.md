---
name: bee-app-scaffold
description: 在 bee-library（或畢業後的獨立 repo）搭一個「獨立 Bee.NET 後端應用/demo」的接線慣例 —— 不走 Bee.Samples.Shared，只依賴公開 Bee.* 套件或 ProjectReference。涵蓋最易踩錯的 DB scoping（CategoryId 是 common/company/log scope 選擇器、業務資料必須 company）、輕量 company context、自訂 auth（免 st_user）、seeder（DbCategorySettings 驅動建表 + sys_id 關連 seeding）、ProgramSettings 身兼 BO 綁定 + 選單來源、Server 不可依賴 Bee.Api.Client、以及反覆出現的操作避雷。當使用者要「起一個 Bee app / 後端」、「做一個會畢業到獨立 repo 的 demo」、「自己接 Bee 後端 host」、「設定 DatabaseSettings / DbCategorySettings / company 資料庫」、「Bee 的 common vs company 怎麼分」之類需求時使用，即使沒明講 scaffold 也要主動觸發。
---

# 獨立 Bee.NET 後端接線

搭一個不靠 `Bee.Samples.Shared` 的獨立 Bee 後端（典型：會「畢業」成獨立 repo 的 demo，或正式 app），要自己接十來件容易漏或接錯的事。這些不是隨機選擇，**是固定慣例**；本 skill 把它們寫死，並標出 `apps/Bee.Northwind/` 作為可對照的完整實作。

> **參考實作**：`apps/Bee.Northwind/`（Server / UI / Desktop + `Define/`）。每一段都可對著它的對應檔案看。

## 適用場景

- 起一個只依賴公開 `Bee.*` 套件（或 ProjectReference）的後端 / demo，**不引用 `Bee.Samples.Shared`**（例：將來搬獨立 repo 的範例）
- 需要自己寫 host bootstrap（`AddXxxBackend` / `UseXxxBackend`）
- 要正確設定多資料庫 scope（common / company）、company context、seeder

## 不適用

- 只要「一個能跑的 JSON-RPC server + client 往返」，不需要 company scope / seeder →
  **`bee-jsonrpc-backend`**（它也是本檔 bootstrap 與 auth 樣板的權威來源）
- 放 `samples/` 且可用 `Bee.Samples.Shared` 的 demo → 用 **`bee-sample-add`**（它選後端、auth、slnx、共用 Define）
- 純 MAUI / Avalonia 前端骨架 → **`maui-app-scaffold`** / `avalonia-*`（前端 head，不含後端接線）
- 只是要「加一張表單」到已接好的 app → **`bee-add-form`**
- 加一個跨層 BO 方法 → **`bee-add-bo-method`**

## 與相關 skill 的分工

| Skill | 處理 |
|-------|------|
| **`bee-app-scaffold`**（本 skill） | 在下列基礎上加 DB scoping + company context + seeder |
| `bee-jsonrpc-backend` | **host bootstrap / 空 controller / 登入三件套 / client 呼叫的權威樣板** |
| `bee-sample-add` | `samples/` 專案（可用 Bee.Samples.Shared）的前端/後端配對 |
| `bee-add-form` | 在已接好的 app 上加一張表單（FormSchema/TableSchema/註冊） |
| `bee-scaffold-from-formschema` | 從一份 FormSchema 產 layout/language/tableschema sidecar |
| `demo-smoke` | 接好後端到端冒煙 |

> 本 skill 的 BO 軸是 **`FormBusinessObject`**（ERP 定義驅動 CRUD）；
> `bee-jsonrpc-backend` 的是 **`BusinessObject`**（自訂 RPC action）。兩者可並存於同一 host。

---

## Part 1 — DB scoping（最關鍵、最易錯）

> **CategoryId 不是自由字串，是 DB scope 選擇器。** `FormRepositoryFactory.ParseCategoryId` 只認 `common` / `company` / `log`（`DbCategoryIds`），其餘丟 `Unknown schema.CategoryId`。把業務表掛 `common` 是錯的（這次 Bee.Northwind 的主要修正）。詳見 memory `categoryid-is-db-scope-selector`、`db-table-prefix-semantics`。

| Scope | 放什麼 | 解析方式 |
|-------|--------|---------|
| **`company`** | **業務資料**：`ft_*`，以及應用的組織表 `st_department` / `st_employee`（一家應用的員工屬於該公司） | router 走 `session.CompanyId → ICompanyInfoService.Get → CompanyInfo.CompanyDatabaseId` |
| **`common`** | 跨公司共享框架表（`st_session`、`st_cache_notify`）。**非應用資料** | 固定 databaseId `"common"`（框架強制 `DatabaseItem.Id == CategoryId == "common"`） |
| `log` | 稽核 / 操作 log | 固定 `"log"` |

落地三件事：
1. **FormSchema**：業務表 `CategoryId="company"`。
2. **TableSchema 資料夾 = CategoryId**：`TableSchema/company/*.xml`（框架表留 `TableSchema/common/`）。放錯資料夾 → seeder 找不到 / 建錯庫。
3. **DatabaseSettings**：保留 `common`（Id==CategoryId=="common"）+ 新增 `company` DatabaseItem。單公司 demo 兩者可指**同一個 SQLite 檔**保持單檔；真實多公司才各自分庫。

```xml
<!-- DbCategorySettings.xml：業務表掛 company 分類 -->
<DbCategory Id="company" DisplayName="...Company Database">
  <Tables><TableItem TableName="ft_xxx" DisplayName="..." /> ... </Tables>
</DbCategory>
```

## Part 2 — Host bootstrap（`AddXxxBackend` / `UseXxxBackend`）

> **完整順序與可貼用樣板見 `bee-jsonrpc-backend` skill 的
> `references/backend-bootstrap.md`**（master key fallback、`ResolveDefinePath` walk-up、
> `Defaults.MaterializeTo` 鋪框架表、provider/dialect 註冊、`SystemSettingsLoader` →
> `SysInfo` → `ApiServiceOptions` → `AddBeeFramework`、空 controller）。
> 那份是唯一權威，本檔不複寫。

在該樣板之上，**本情境（company scope + seeder）額外要做的只有兩件**：

1. **多覆寫一個 `ICompanyInfoService`** → 輕量 company（Part 3）。與 factory / resolver 一樣，
   必須在 `AddBeeFramework` **之後** `AddSingleton`（後註冊者勝）。
2. **`UseXxxBackend` 跑的是完整 seeder**（Part 5），不只建 `st_cache_notify` 一張框架表。

可對照的實作：`apps/Bee.Northwind/Bee.Northwind.Server/NorthwindBackend.cs`。

## Part 3 — company context：單公司也要走 `EnterCompany`

⛔ **不要用「硬編 `ICompanyInfoService` + 在覆寫的 `Login` 裡蓋 `SessionInfo.CompanyId`」這條捷徑。**
Northwind 走過，2026-08-28 移除，代價是兩個無聲的錯誤行為：

1. **以公司為鍵的查找全部失效。** `CacheDataSourceProvider` 的 `GetCompanyRolePermissions` /
   `GetDepartmentTree` / `GetCompanyAuditRules` 都先呼叫 `GetCompanyInfo`，而那一支走
   **`ICompanyRepository`（讀 `st_company`）**，不是被你替換掉的 `ICompanyInfoService`。
   `st_company` 沒有列 → 一律回 `null` → 那些機制靜默不生效。
2. **session 重建會掉公司，而且應用補不了。** `EnterCompany` 除了寫快取還會
   `SessionRepository.UpdateSession(CreateSeed(...))` 把公司寫進 `st_session` 種子；
   而 `CreateSeed` 是 `private static`、`SessionRepository` 是 `private`，**子類別拿不到**。
   快取一被逐出或伺服器一重啟，重建出來的 session 沒有公司，所有 `CategoryId="company"`
   表單擲 `CompanyNotEnteredException`。

正解就是照框架的兩步走，成本比捷徑低：

- **seed 三張表**：`st_user`、`st_company`（`customize_id` 別漏，客製層靠它）、`st_user_company`。
  三個 XML 欄（`number_formats_xml` / `cash_rounding_xml` / `allowed_currencies_xml`）是
  `DbType="Text"`，手寫 INSERT 必須顯式給 `''`（MySQL 的 TEXT 不能有 DEFAULT）。
- ⛔ **同時要註冊 `st_role` / `st_role_grant` / `st_user_role`**（company 類，空表即可）。
  `RolePermissionRepository` **沒有** schema 探測保護（`AuditRuleRepository` 有），
  只 seed `st_company` 而不建這三張，會把「靜默失效」升級成**登入後進不了公司**。
- **client 端登入後呼叫一次** `SystemApiConnector.EnterCompanyAsync(companyId)` +
  `ClientInfo.ApplyEnterCompanyResult(...)`。單公司就自動帶入，不必做選單。
  ⚠️ **connector 要重新取**：`ClientInfo.ApplyLoginResult` 會把快取的 connector 丟掉
  （它是用登入前的空 token 建的），沿用登入前的區域變數會得到
  「AccessToken is required or invalid」。
- 表單若無 `PermissionModelId`，角色空著無妨 —— 伺服端與前端都在 `modelId` 為空時短路。

可對照的實作：`apps/Bee.Northwind/Bee.Northwind.Server/NorthwindSchemaSeeder.cs`
（`SeedCommon`）與 `apps/Bee.Northwind/Bee.Northwind.UI/ViewModels/LoginViewModel.cs`。

## Part 4 — 自訂 auth（免 st_user）

> **登入三件套**（Credentials / 認證 System BO / factory）**的完整程式碼見
> `bee-jsonrpc-backend` skill 的 `references/business-object.md`**。本檔不複寫。

本情境唯一的差異：認證 BO 的 `Login` **順帶蓋 company**（Part 3），
`AuthenticateUser` 的部分與該樣板相同。可對照
`NorthwindAuthenticatingSystemBusinessObject`。

## Part 5 — Seeder（建表 + 種子）

對照 `NorthwindSchemaSeeder.cs`。冪等（建表 create-if-not-exists、各表空才灌）。

- **建表資料驅動**：列舉 `DbCategorySettings` 各 category，`new TableSchemaBuilder(category.Id, ...)` + `Execute(category.Id, tableName)` —— `category.Id` 同時是**目標 db** 與 **TableSchema 資料夾**。框架表（`st_cache_notify`）另用 common builder 建進 common。**這讓「加一張表 = 純 XML（TableSchema + DbCategorySettings 一筆）」成立，seeder 不用改 C#。**
- **種子灌進 company db**（業務資料）：`dbAccessFactory.Create("company")`。
- **關連 seeding 用 `sys_id`**：JSON 關連欄填目標 `sys_id`（人類可讀），seeder 解析成 `sys_rowid`。**Forward**（目標已先建）inline 解析；**Deferred**（環狀，如 Department.manager↔Employee）第二輪 UPDATE。明細用同一 Forward 機制（`sys_master_rowid` → 主表 sys_id、`product_rowid` → 商品 sys_id），不需特殊 master-detail 邏輯。
- **SeedData 複製到輸出**：csproj `<Content Update="SeedData\**\*.json" CopyToOutputDirectory="PreserveNewest" />`。

## Part 6 — ProgramSettings 身兼兩職

`Define/ProgramSettings.xml` 一檔兩用：
1. **BO 綁定**：`ProgramItem.BusinessObject="Ns.Type, Asm"` → `ProgramSettingsBoTypeResolver` 載入自訂 `FormBusinessObject`。空 → 框架預設（純定義 CRUD）。
2. **導航選單來源**：前端從 `ClientInfo.DefineAccess.GetProgramSettings()` 列舉 category→header、item→表單連結（資料驅動，非硬編 `NavItems`）。`ProgramCategory` 做選單分組（與 DB 的 common/company 無關）。

> GetDefine 透過 `GetDefineResult.Xml`（XML-string）傳輸；定義型別 XML-serializable 即可遠端取（與 FormSchema 同路徑）。`SystemBusinessObject.GetDefine` 只擋遠端取 `SystemSettings`/`DatabaseSettings`，ProgramSettings 可遠端取。

---

## 硬性規則

1. **Server 不可 `ProjectReference Bee.Api.Client`**。後端是後端，client 是 client。唯一誘因是 `ApiClientInfo.LocalServiceProvider`（in-process client bridge）—— 遠端 head 走 HTTP 用不到，刪。
2. **CategoryId ∈ {common, company, log}**，業務資料 = company（Part 1）。
3. **TableSchema 資料夾名 = CategoryId**。
4. **slnx 不列舉 `Define/` 檔**：執行期資料、會過時；server 用 `PathOptions.DefinePath` 讀整個目錄。
5. **覆寫服務在 `AddBeeFramework` 之後**註冊（後者勝）。
6. **計算/伺服器衍生欄位標 `FormField.ReadOnly="true"`**（如 BO 算出的金額）——產生 FormLayout 時會帶到 `LayoutField.ReadOnly`，不必在版面另外標一次。**FormLayout 本身仍必須落檔**（執行階段不會自動產生）。

## 避雷（反覆踩過）

- **port 占用**：重跑 server 前先 `lsof -ti :<port> | xargs kill -9`；舊 instance 沒收會 `address already in use`，新 instance 的 seeder 仍可能已跑（seeder 在 `app.Run()` 前），別誤判。
- **改 schema 要重建**：加欄位後刪 `*.db` 重跑讓 seeder 重建（create-if-not-exists 不會 ALTER 既有表加欄）。`.db` 應 gitignore。
- **apps/ 不在 CI**：`build-ci.yml` 只在 `src/ tests/ slnx props sonar yml` 觸發。後端正確性靠本機 build + `demo-smoke`；改 src 底層時 CI 仍建 src+tests。
- **Avalonia UI 自測交付使用者**：computer-use 跑 Avalonia 裸 dotnet 程序 `request_access` 認不得（要包 .app）；編譯過即交付使用者自測。
- **company 接線錯的徵兆很明確**：每張表單報 `CompanyNotEntered`（session 沒 CompanyId）或解析不到 db —— 先查 Part 3，不是表單問題。

## 完成檢查

- [ ] `dotnet build`（含前端 head）全綠
- [ ] 刪 db 重跑：seeder 建出所有表（company + 框架）、灌種子無錯、server `Now listening`
- [ ] Server csproj 無 `Bee.Api.Client`
- [ ] 業務 FormSchema 全 `CategoryId="company"`、TableSchema 在 `company/` 資料夾、DbCategorySettings 有對應 category
- [ ] 登入後表單 CRUD 可用（company 路由通）—— 交付使用者自測或 `demo-smoke`
