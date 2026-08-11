# 計畫：Northwind 案例補齊 common / log 兩個資料庫分類，登入改走 `st_user`

**狀態：🚧 進行中（2026-08-11）**

| 階段 | 範圍 | 狀態 |
|------|------|------|
| 0 | 框架缺陷：`st_user.password` 欄長不足以存放框架自己算出的雜湊 | ✅ 已完成（2026-08-11） |
| 1 | `common` 分類正式登錄，seeder 的旁路建表退役 | ✅ 已完成（2026-08-11） |
| 2 | `log` 分類接線 + 開啟稽核，登入事件落地 | ✅ 已完成（2026-08-11） |
| 3 | 框架補 `st_user` 預設認證（A 案），案例登入改走它 | ✅ 已完成（2026-08-11） |
| 4 | 連帶更新：README、鐵人賽已定稿篇章的案例數字 | 📝 待做 |

> ✅ **三個決策已於 2026-08-11 由使用者裁定，全部照建議**：
> A 案（框架補預設認證）／公司脈絡這次不做／`log` 與另兩個分類共用同一個 SQLite 檔。

---

## 背景

案例目前只登錄了一個資料庫分類（`company`），而框架的分類是三值
（`common` / `company` / `log`，權威在 `RepositoryFactory.ParseCategoryId`）。
這造成三件事：

1. **`common` 的表被一條旁路建出來。** `DbCategorySettings.xml` 沒有 `common` 分類，
   `NorthwindSchemaSeeder` 因此自己從 `Defaults.ListEmbedded()` 推導一份框架 common 表清單
   （`GetFrameworkCommonTables()`）再逐張建。`DatabaseSettings.xml` **已經有** `common` 這個
   `DatabaseItem`，缺的只是分類登錄。
2. **`log` 完全沒有接。** `Define/TableSchema/log/` 五份定義檔已在
   （`st_log_login` / `st_log_access` / `st_log_change` / `st_log_anomaly_api` / `st_log_anomaly_db`），
   但沒有 `DatabaseItem`、沒有 `DbCategory`、`SystemSettings.xml` 也沒有 `AuditLogOptions`，
   所以那五張表不會被建，也不會有任何一列寫進去。
3. **登入不走 `st_user`。** `NorthwindAuthenticatingSystemBusinessObject` 覆寫 `AuthenticateUser`，
   比對 `NorthwindCredentials` 的兩個常數，`st_user` 表建出來是空的。

## 目標

- 案例**三個分類都真的用到**，不再靠 seeder 的旁路建 common 表
- 登入走 `st_user`，密碼以 `PasswordHasher` 雜湊儲存
- 開啟稽核，**登入成功、失敗、鎖定三種事件自動落進 `st_log_login`**

第三項幾乎是零程式碼：`SystemBusinessObject.Login` 本身已經在三個分支各呼叫一次
`WriteLoginAudit`（`src/Bee.Business/System/SystemBusinessObject.Session.cs`），
只要 `AuditLogOptions.Enabled` 與 `LoginEnabled` 為真、且 `log` 分類解析得到資料庫，
記錄就會產生。

---

## 階段 0：先修一個框架缺陷（撰寫本 plan 時查出）

**`st_user.password` 存不下框架自己算出來的雜湊。**

- `src/Bee.Definition/Defaults/TableSchema/common/st_user.TableSchema.xml` 宣告
  `password` 為 `DbType="String" Length="40"`
- `PasswordHasher.HashPassword` 產出的格式是
  `v2.{iterations}.{saltBase64}.{hashBase64}`，實算長度為 **79 個字**
  （`v2.100000.` 十字 + 24 字 salt + 分隔點 + 44 字 hash）

這個缺陷至今沒有浮現，因為**框架沒有任何地方真的把雜湊寫進 `st_user`**
（預設 `AuthenticateUser` 永遠回 `false`，沒有內建的建立使用者流程）。
本 plan 的階段 3 會是第一次，所以必須先修。

⚠️ **在 SQLite 上不會報錯**（動態型別不強制長度），但在 SQL Server / PostgreSQL /
MySQL / Oracle 上會截斷或直接失敗，而**截斷之後 `VerifyPassword` 會永遠回 false**，
症狀是「密碼明明對卻登不進去」。案例跑 SQLite，所以光靠案例測不出來。

**修法**：把 `Length` 放寬到 `200`（留給日後換演算法或加大迭代數的餘裕）。
放寬欄位在升級管線上是同家族 ALTER、非破壞性（Day 6 那篇談過縮小才會被拒絕），
既有部署下次啟動比對時會自動跟上。

同步改 `apps/Bee.Northwind/Define/TableSchema/common/st_user.TableSchema.xml`
（案例是從 `Defaults/` 複製出去的，兩份各自維護）。

---

## 關鍵決策（✅ 已裁定，2026-08-11）

### 決策一：`st_user` 的認證要由誰實作 → **A 案：框架補上預設認證**

**框架目前沒有內建 `st_user` 認證。** 已查證：

- `SystemBusinessObject.AuthenticateUser` 的預設實作**永遠回 `false`**，
  XML doc 明寫這是「避免子類忘了覆寫就放行」的保護值
- `IUserRepository` 只有 `GetRowIdBySysId` / `GetLocale` / `GetName` /
  `IsDeploymentAdmin` / `SetDeploymentAdmin`，**沒有任何取密碼或驗密碼的方法**
- `st_user` 有 `password` 欄，`PasswordHasher.HashPassword` / `VerifyPassword` 在 `Bee.Base`

所以「直接採預設的 `st_user`」在目前的程式碼上還不成立，有兩條路：

| | A 案：框架補上預設認證 | B 案：只動應用 |
|---|---|---|
| 做法 | `IUserRepository` 加一個驗密碼的方法，`SystemBusinessObject` 提供一個以 `st_user` 為準的預設 `AuthenticateUser`（或一個可選的基底類別） | 應用自己寫一個讀 `st_user` 的 Repository，覆寫 `AuthenticateUser` |
| 案例端 | `NorthwindAuthenticatingSystemBusinessObject` 可**整個刪掉** | 該檔留著，內容從比對常數改成查表 |
| 影響面 | 動框架公開表面（`IUserRepository` 新增成員對外部實作者是 source-breaking，需進 `PublicAPI.Unshipped.txt` 並在 CHANGELOG 標明） | 零框架變更 |
| 對系列的意義 | 案例真的「零程式碼登入」，八張表單零行之外連登入也零行 | 案例仍有一支認證程式，只是換了資料來源 |

**建議 A 案。** 理由是這條線與系列一路的判別法一致：「這件事全世界的 ERP 做起來是不是都一樣？」
帳號密碼比對是典型的制式化行為，`st_user` 是框架自己的表、`password` 是框架自己的欄位、
雜湊器也是框架自己的，**唯獨把三者接起來那一段要求每個應用自己寫**，這個缺口不合理。

**採用的 API 形狀（2026-08-11 定）**：`IUserRepository` 新增
`bool VerifyPassword(string userId, string password)`，**雜湊值不離開 Repository**。
另一個選項是 `GetPasswordHash(userId)` 回傳雜湊由呼叫端自行比對，否決理由是
把雜湊值放進呼叫端的變數裡就多了一個會被寫進日誌或例外訊息的機會，
而 `scanning.md` 明文禁止那件事。找不到使用者與密碼錯誤一律回 `false`，
呼叫端無從分辨（避免帳號列舉）。

`SystemBusinessObject.AuthenticateUser` 的預設實作由「永遠回 false」改為走這個方法。
⚠️ **這是行為變更**：既有部署若「忘了覆寫」，原本任何人都登不進去，改版後 `st_user`
裡的帳號可以登入。判定為可接受 —— 沒有覆寫的部署本來就沒有能用的登入，
有能用登入的部署一定已經覆寫過、不受影響。仍須進 CHANGELOG 明列。

### 決策二：公司脈絡要不要一起走真流程 → **這次不做**

目前 `NorthwindCompanyInfoService` 是 `ICompanyInfoService` 的替身，
`NorthwindAuthenticatingSystemBusinessObject.Login` 直接把 `SessionInfo.CompanyId` 蓋上去，
**繞過 `EnterCompany`**（框架的 `EnterCompany` 會驗 `st_company` 存在與啟用、
驗 `st_user_company` 授權、快照角色與員工脈絡）。

`st_company` / `st_user_company` 的 TableSchema 都已在 `Define/TableSchema/common/`，建出來是空的。

| | 一併做 | 這次不做 |
|---|---|---|
| 內容 | seed 一家公司進 `st_company`、一列授權進 `st_user_company`，刪掉 `NorthwindCompanyInfoService` 與 `Login` 覆寫，走框架的 `EnterCompany` | 維持替身，只換認證來源 |
| 代價 | 範圍再擴大一圈，且要處理「前端要不要多一步選公司」 | `common` 分類仍有兩張表是建了不用 |

**建議「這次不做」**，理由是它會把這個 plan 變成三件事，且前端流程要跟著改。
但要在 README 誠實註明那兩張表為何是空的。

### 決策三：`log` 要不要獨立一個資料庫 → **與另兩個分類共用同一個 SQLite 檔**

`DatabaseSettings.xml` 現在兩個 `DatabaseItem`（`common` / `company`）指向**同一個 SQLite 檔**，
註解已說明這是單公司示範的刻意簡化。`log` 照辦（第三個 `DatabaseItem`、同一個檔）最省事，
且不損失示範價值 —— 分類與實體資料庫是兩件事，正是要示範的重點之一。

**建議同一個檔**，並沿用現有註解的寫法說明理由。

---

## 階段 1：`common` 分類正式登錄

**改動**

1. `Define/DbCategorySettings.xml` 新增 `common` 分類，把 `Define/TableSchema/common/` 現有七張表
   逐一登錄（`st_user` / `st_session` / `st_cache_notify` / `st_define` / `st_company` /
   `st_user_company` / `st_api_key`）
2. `NorthwindSchemaSeeder` 移除 `GetFrameworkCommonTables()` 與 `EnsureSchema` 尾端那段
   common 專用建表迴圈，改由既有的「逐分類建表」迴圈統一處理
3. `VerifyTablesExist` 的 `expected` 字典不再需要預先塞 common，改為純由 `DbCategorySettings` 推導

**⚠️ 這一步要正視一個既有的警告。** `GetFrameworkCommonTables()` 的 XML doc 明寫：
手寫清單「正是害登入壞掉過一次的原因」，框架兩次新增 common 表相依而清單沒跟上，
症狀是登入時一個含糊的 API 錯誤。改成 `DbCategorySettings` 登錄**同樣是手寫清單**，
等於把那個風險換一個位置放。

**兩個緩解方向，擇一**：

- (a) 保留 `VerifyTablesExist` 的啟動檢查（它本來就是為了把靜默失敗變成大聲失敗），
  並在其中額外比對「`Defaults` 內嵌的 common 表是否都被登錄」，缺漏就在啟動時報出名字。
  **這是建議做法**：清單搬到 XML（符合「加一張表是純 XML」的示範價值），
  同時保住原本那道防線。
- (b) 完全照 (a) 但不加比對，接受風險。不建議。

**驗收**：刪掉 `northwind.db` 重跑，七張 common 表都建出來；`dotnet build` 乾淨。

---

## 階段 2：`log` 分類接線 + 開啟稽核

**改動**

1. `Define/DatabaseSettings.xml` 新增 `log` 的 `DatabaseItem`（依決策三，指同一個 SQLite 檔）
2. `Define/DbCategorySettings.xml` 新增 `log` 分類，登錄 `Define/TableSchema/log/` 的五張表
3. `Define/SystemSettings.xml` 的 `BackendConfiguration` 新增 `AuditLogOptions`，
   `Enabled` 設 `true`
   - ⚠️ `UseBackgroundWriter` 要確認：ASP.NET Core host 有 `IHost`，預設 `true` 可用；
     但**若要在啟動後立刻看到登入記錄，同步寫入比較好觀察**，這一項起稿時實測後決定
   - ⚠️ 逐一確認 `AuditLogOptions` 還有哪些 `*Enabled` 子開關（至少有 `LoginEnabled`），
     只開登入還是全開，起稿時對照原始碼決定

**驗收**：登入一次，`st_log_login` 有一列成功事件；故意打錯密碼，多一列失敗事件。

---

## 階段 3：登入改走 `st_user`

依決策一的結果執行。共同部分：

1. `NorthwindSchemaSeeder` 新增一筆 `st_user` 的 seed（`sys_id` = `demo`，
   `password` = `PasswordHasher.HashPassword("demo")`，`sys_name` = `Demo User`，
   並帶 `time_zone` / `culture`，讓 `ApplyUserLocale` 有東西可讀）
   - ⚠️ seed 的密碼**不可寫死雜湊值**，要在 seeder 內呼叫 `HashPassword` 現算，
     否則換一次雜湊參數就對不上
2. `NorthwindCredentials` 保留 `UserId` / `Password` / 公司相關常數（桌面端登入畫面要顯示），
   移除已不再使用的項目

**A 案追加**：框架端 `IUserRepository` 加驗密碼方法 + `SystemBusinessObject` 提供預設實作，
案例刪掉 `NorthwindAuthenticatingSystemBusinessObject`。需同步 `PublicAPI.Unshipped.txt`
與 CHANGELOG（見 `~/.claude/rules/releasing.md` 的破壞性變更判定）。

**驗收**：以 `demo` / `demo` 登入成功；密碼打錯時 `st_log_login` 有失敗列；
`SessionInfo.TimeZone` / `Culture` 來自 `st_user` 那一列而非部署預設值。

---

## 階段 4：連帶更新

### `apps/Bee.Northwind/README.md`（雙語）

三個分類的說明、登入方式、稽核已開啟、以及決策二未做的部分要誠實註明。

### ⚠️ 鐵人賽已定稿篇章（`docs/blogs/`，另一個 repo）

**這一項是這個 plan 最容易被漏掉的連帶影響。** 已定稿九篇裡有多處依賴「案例沒有 common /
log、稽核是關著的、登入是硬編碼的」這個現況：

| 篇 | 受影響的敘述 | 處置 |
|---|---|---|
| Day 1 結尾 | 清單曾因案例無稽核而拿掉「稽核」二字 | 現在可以放回去，但**要評估是否值得動已定稿的文字** |
| Day 3 §2 | 「九張裡有兩張是框架提供的表」 | 補了 common 之後框架表變多，該句限定在 company 那九張，**字面仍為真**，確認即可 |
| Day 3 §6 | 「Server 端 12 個 `.cs`」這個底稿數字（記在 day-notes，Day 3 正文未寫出） | A 案會少一檔，B 案不變。**day-notes 底稿要更新** |
| Day 9 §5 | 案例「七比一」 | 不受影響（講的是八張表單） |
| Day 25（未寫） | day-notes 明記「不可宣稱案例正在產生稽核資料」 | **這條限制可以解除**，且案例會變成很好的落地素材 |
| Day 29（未寫） | 「對帳表不列稽核」 | 同上，可改為列入 |
| Day 10（未寫，即將動工） | 案例的資料庫分類落地 | **本 plan 完成後案例三個分類齊備，Day 10 的案例段會好寫得多** |

**排程建議**：這個 plan 若要做，**排在 Day 10 動筆之前**，否則 Day 10 寫完又要回頭改案例段。

---

## 不在本 plan 範圍

- 決策二的公司脈絡真流程（`EnterCompany` / `st_company` / `st_user_company`）
- 權限模型（案例目前 0 個 `PermissionModel`）
- 多公司部署示範（現在兩個 `DatabaseItem` 指同一個檔）


---

## 執行紀錄（2026-08-11）

### 已驗證

- **完整方案 Release 建置乾淨**（`--no-incremental`，0 警告 0 錯誤），含 analyzer 掃過案例的定義檔
- **啟動實測**：刪掉 `northwind.db` 重跑，seeder 全程走完，共 **21 張表**建出來
  （common 7 + company 9 + log 5），`VerifyCommonRegistration` 與 `VerifyTablesExist` 都通過
- **`st_user` 已植入**：`demo` / `Demo User` / `Asia/Taipei` / `en-US`，
  **密碼雜湊實測長度 79 字元** —— 直接證實階段 0 那個 `Length="40"` 非修不可
- **測試全綠**：`Bee.Api.Core` 759、`Bee.Business` 421、`Bee.Repository` 181、
  `Bee.ObjectCaching` 188，皆 0 失敗

### 尚未驗證

**登入的端到端往返與 `st_log_login` 實際落列。** 以 `curl` 送純 JSON 打不進去：
wire body 是 MessagePack + 壓縮 + 加密的封套，手工組的 JSON 不合形狀
（`System.Ping` 也同樣失敗，可證與認證改動無關）。要驗證得走桌面端或
`Bee.Api.Client`，留待階段 4 一併做。

### 過程中踩到、值得記下的兩個雷

1. **`SystemSettings.xml` 的元素順序必須與屬性宣告順序一致。**
   `AuditLogOptions` 在 `BackendConfiguration` 裡宣告為第 5 個，初版把它插在第 2 個位置
   （`SecurityKeySettings` 之前）。`XmlSerializer` 對序列順序敏感，順序錯**不會報錯**，
   而是把後面的元素靜默丟掉 —— 於是主金鑰來源沒被讀到，症狀是每一個 API 呼叫都 NRE，
   離肇因非常遠。**新增設定區塊時要先看類別的宣告順序。**
2. **`.gitignore` 的泛用 `[Ll]og/` 規則會吃掉 `TableSchema/log/`。**
   repo 內已有兩條同型例外（`src/Bee.Definition/Defaults/` 與 `tests/Define/`），
   本次補上第三條給 `apps/Bee.Northwind/`。沒補的話案例的五份 log 定義檔不會進版控，
   fresh clone 建不出稽核表，而且 analyzer 的 `BEE2002` 會在建置期就擋下來。

### 與 plan 原案的差異

- `NorthwindAuthenticatingSystemBusinessObject` **沒有整個刪掉，改名為
  `NorthwindSystemBusinessObject` 並瘦身**：認證那段確實移除了（改用框架預設），
  但依決策二仍要覆寫 `Login` 把 `SessionInfo.CompanyId` 蓋上去，所以類別本身留著。
  類別名不再帶 `Authenticating`，因為它已經不做認證。
- `NorthwindCredentials` 新增 `TimeZone` / `Culture` 兩個常數供 seed 使用。
