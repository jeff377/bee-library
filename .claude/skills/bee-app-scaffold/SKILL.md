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

- 放 `samples/` 且可用 `Bee.Samples.Shared` 的 demo → 用 **`bee-sample-add`**（它選後端、auth、slnx、共用 Define）
- 純 MAUI / Avalonia 前端骨架 → **`maui-app-scaffold`** / `avalonia-*`（前端 head，不含後端接線）
- 只是要「加一張表單」到已接好的 app → **`bee-add-form`**
- 加一個跨層 BO 方法 → **`bee-add-bo-method`**

## 與相關 skill 的分工

| Skill | 處理 |
|-------|------|
| **`bee-app-scaffold`**（本 skill） | 獨立後端 host 接線 + DB scoping + company context + seeder |
| `bee-sample-add` | `samples/` 專案（可用 Bee.Samples.Shared）的前端/後端配對 |
| `bee-add-form` | 在已接好的 app 上加一張表單（FormSchema/TableSchema/註冊） |
| `bee-scaffold-from-formschema` | 從一份 FormSchema 產 layout/language/tableschema sidecar |
| `demo-smoke` | 接好後端到端冒煙 |

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

對照 `apps/Bee.Northwind/Bee.Northwind.Server/NorthwindBackend.cs`。順序固定：

1. `BEE_MASTER_KEY` demo fallback（沒設才塞固定 key，正式由部署注入）。
2. `PathOptions { DefinePath = ResolveDefinePath() }`：從 `AppContext.BaseDirectory` 往上走找 `Define/SystemSettings.xml`。
3. `Defaults.MaterializeTo(DefinePath, Filter: 只挑框架表)`：把 `st_cache_notify.TableSchema.xml` 等框架預設鋪進 `Define/`（skip-if-exists），讓 `IDefineAccess` 解析得到。
4. `DbProviderRegistry.Register` + `DbDialectRegistry.Register`（**顯式**註冊用到的 dialect，例 SQLite，不強迫拉全部 driver）。
5. `SystemSettingsLoader.Load(paths)` → `SysInfo.Initialize(...)` → `ApiServiceOptions.Initialize(...)`。
6. `builder.Services.AddBeeFramework(settings.BackendConfiguration, paths, autoCreateMasterKey: true)`。
7. **覆寫服務**（在 `AddBeeFramework` 之後 `AddSingleton`，後註冊者勝）：
   - `IBusinessObjectFactory` → 自訂 factory（接自訂 auth）。
   - `ICompanyInfoService` → 輕量 company（Part 3）。
8. `UseXxxBackend`：跑 seeder（Part 5）。**不要**設 `ApiClientInfo.LocalServiceProvider`（那是 in-process client bridge；遠端 head 走 HTTP 用不到，設了會逼 Server 依賴 `Bee.Api.Client` —— 見硬性規則）。

`ApiController.cs` 只要空殼繼承 `ApiServiceController`（框架已宣告 `[Route("api")]` + POST handler）。

## Part 3 — 輕量 company context

要 company scope 又不想建 `st_user` / `st_company` / `st_user_company` / 走完整 `EnterCompany`：

- **自訂 `ICompanyInfoService`**（`NorthwindCompanyInfoService.cs`）：`Get(companyId)` 回固定 demo 公司（`CompanyDatabaseId="company"`），`Set`/`Remove` no-op。DI 覆寫掉預設（讀 `st_company` 的版本）。
- **override `SystemBusinessObject.Login`**（在自訂 auth BO 內）：`base.Login` 後 `SessionInfoService.Get(token).CompanyId = 固定值; Set(...)`。`Get` 回**非 nullable** `SessionInfo`，勿加 null 檢查（CS8073）。
- 表單若無 `PermissionModelId`，免角色/員工快照（那是 `EnterCompany` 才需要）。

這是「hardcoded 登入」的 company 情境對應版 —— 最小可用、單公司。

## Part 4 — 自訂 auth（免 st_user）

`NorthwindAuthenticatingSystemBusinessObject : SystemBusinessObject`，override `AuthenticateUser(args, out userName)` 比對 hardcoded 帳密、回 true/userName，不碰 `st_user`。`Login` 順帶蓋 company（Part 3）。靠 Part 2 的 `IBusinessObjectFactory` 覆寫讓 system 呼叫（Login 等）派送到它。

## Part 5 — Seeder（建表 + 種子）

對照 `NorthwindSchemaSeeder.cs`。冪等（建表 create-if-not-exists、各表空才灌）。

- **建表資料驅動**：列舉 `DbCategorySettings` 各 category，`new TableSchemaBuilder(category.Id, ...)` + `Execute(category.Id, tableName)` —— `category.Id` 同時是**目標 db** 與 **TableSchema 資料夾**。框架表（`st_cache_notify`）另用 common builder 建進 common。**這讓「加一張表 = 純 XML（TableSchema + DbCategorySettings 一筆）」成立，seeder 不用改 C#。**
- **種子灌進 company db**（業務資料）：`dbAccessFactory.Create("company")`。
- **關連 seeding 用 `sys_id`**：JSON 關連欄填目標 `sys_id`（人類可讀），seeder 解析成 `sys_rowid`。**Forward**（目標已先建）inline 解析；**Deferred**（環狀，如 Department.manager↔Employee）第二輪 UPDATE。明細用同一 Forward 機制（`sys_master_rowid` → 主表 sys_id、`product_rowid` → 商品 sys_id），不需特殊 master-detail 邏輯。
- **SeedData 複製到輸出**：csproj `<Content Update="SeedData\**\*.json" CopyToOutputDirectory="PreserveNewest" />`。

## Part 6 — ProgramSettings 身兼兩職

`Define/ProgramSettings.xml` 一檔兩用：
1. **BO 綁定**：`ProgramItem.BusinessObject="Ns.Type, Asm"` → `ProgramSettingsFormBoTypeResolver` 載入自訂 `FormBusinessObject`。空 → 框架預設（純定義 CRUD）。
2. **導航選單來源**：前端從 `ClientInfo.DefineAccess.GetProgramSettings()` 列舉 category→header、item→表單連結（資料驅動，非硬編 `NavItems`）。`ProgramCategory` 做選單分組（與 DB 的 common/company 無關）。

> GetDefine 透過 `GetDefineResult.Xml`（XML-string）傳輸；定義型別 XML-serializable 即可遠端取（與 FormSchema 同路徑）。`SystemBusinessObject.GetDefine` 只擋遠端取 `SystemSettings`/`DatabaseSettings`，ProgramSettings 可遠端取。

---

## 硬性規則

1. **Server 不可 `ProjectReference Bee.Api.Client`**。後端是後端，client 是 client。唯一誘因是 `ApiClientInfo.LocalServiceProvider`（in-process client bridge）—— 遠端 head 走 HTTP 用不到，刪。
2. **CategoryId ∈ {common, company, log}**，業務資料 = company（Part 1）。
3. **TableSchema 資料夾名 = CategoryId**。
4. **slnx 不列舉 `Define/` 檔**：執行期資料、會過時；server 用 `PathOptions.DefinePath` 讀整個目錄。
5. **覆寫服務在 `AddBeeFramework` 之後**註冊（後者勝）。
6. **計算/伺服器衍生欄位標 `FormField.ReadOnly="true"`**（如 BO 算出的金額），免寫 FormLayout。

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
