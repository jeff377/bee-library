# ADR-012：Session 公司情境模型（兩階段 session lifecycle）

## 狀態

已採納（2026-05-15）

## 背景

Bee.NET 採三類邏輯 DB（[ADR-010](adr-010-logical-database-category.md)），其中 `company` 分類的實際資料庫由「使用者目前操作的是哪家公司」決定。要支援這個多公司能力，session 必須帶上「當前公司」資訊；但「使用者帳密驗證」與「綁定操作公司」是兩個獨立的關注點：

- 使用者通常先輸入帳密通過認證，**之後才知道自己可以進哪些公司**
- 同一使用者可能擁有多家公司的存取權，需要在 session 中途切換
- 部分跨公司操作（Login、Logout、Ping、寫 audit log 等）發生在「進公司之前」或「離開公司之後」

如果把這兩件事塞進單一 `Login(account, password, companyId)`，會碰到結構性問題：

1. **使用者體驗反向**：要先告訴系統「我要進哪家公司」才能開始驗證，但「我能進哪些公司」要驗證後才知道
2. **切換成本**：切換公司 = 完整 logout + 重新 login，重發 token、重建加密金鑰
3. **跨公司流程被卡死**：「列出我可以進的公司」這種操作沒有合適的執行階段
4. **狀態語意混亂**：「已登入但未進公司」這個 in-between state 無處表達

需要一個明確的兩階段 session lifecycle：先完成身份驗證，再綁定公司情境。

## 決策

引入**兩階段 session 模型**，由四個對稱方法構成完整 lifecycle：

```
Login(account, password)   ←→  Logout()
EnterCompany(companyId)    ←→  LeaveCompany()
```

`SessionInfo` 加 `CompanyId`（`string?`，nullable）欄位表達 in-between state；`null` = 已登入但未進公司，非 null = 當前綁定該公司。

### 四項核心要點

1. **`Login` 只負責身份驗證**，不接受 `companyId`

   - 成功後建立 `SessionInfo`（含 `AccessToken`、`UserId`、`UserName` 等），`SessionInfo.CompanyId == null`
   - 回傳 `AccessToken`，client 端用此 token 呼叫後續所有方法
   - 之後可呼叫「列出可進入公司」等跨公司方法（共用 `common` DB）

2. **`EnterCompany` 負責綁定公司情境**，直接覆寫即為切換

   - 驗證 `CompanyId` 存在於 `ICompanyInfoService`（未來會加入使用者-公司權限驗證）
   - 寫入 `SessionInfo.CompanyId`，覆寫舊值（首次進入與切換用同一方法，無 `LeaveCompany` + `EnterCompany` 兩步驟）
   - 切換是原子操作：要嘛切換成功並指向新公司，要嘛拋例外維持原 `CompanyId`
   - 回傳完整 `CompanyInfo` 物件給 client（顯示用）

3. **`LeaveCompany` 是顯式「離開公司但保留 session」**

   - 清掉 `SessionInfo.CompanyId`，保留其他 session 狀態
   - Idempotent：對未進公司的 session 呼叫不報錯，方便前端不必先檢查狀態
   - 切換公司**不**透過 `LeaveCompany` + `EnterCompany` 兩步驟（避免非原子操作中間狀態）；`LeaveCompany` 的主要用途是「回到公司選擇頁」之類顯式 UX 動作

4. **`Logout` 隱含 `LeaveCompany` 清理，再銷毀整個 session**

   - 內部流程：清 `SessionInfo.CompanyId`（若非空）→ 移除整個 session entry
   - Idempotent：對不存在的 token 不報錯（避免被攻擊者用來探測 token 存在性）
   - 呼叫端不需要先 `LeaveCompany` 再 `Logout`，一個方法 cover

## 理由

### 為何 `SessionInfo.CompanyId` 是 nullable（而非預設空字串）

`null` 與「真實的空字串 CompanyId」語意不同。`null` 明確表達「未綁定公司」，空字串可能被誤判為合法值。本專案已全 enable nullable reference types，加 `?` annotation 不產生額外維護成本。

### 為何 `Logout` 隱含 `LeaveCompany`

兩種 API 設計方案：

| 方案 | 評估 |
|------|------|
| **`Logout` 內部隱含清理 ✅** | 一個動作搞定，呼叫端不需要操心順序；server 端能保證「session 銷毀時必清乾淨」 |
| `Logout` 要求呼叫端先 `LeaveCompany` | 呼叫端容易漏；若忘記，會在 audit log 看到「session 有 CompanyId 但已 expired」的奇怪狀態 |

採前者，符合 idempotent 設計原則：呼叫端不需要記得多步驟。

### 為何 `CompanyAccessDenied` 合併「無權限」與「不存在」

`EnterCompany` 可能失敗的兩種情境：

- 該 `CompanyId` 不存在
- 該使用者對該 `CompanyId` 無存取權

若兩者用不同錯誤碼，攻擊者可以透過反覆嘗試判斷哪些 `CompanyId` 存在於系統中（user enumeration attack）。合併為單一 `CompanyAccessDenied` (-32003, HTTP 403) 後，無權限的使用者無法區分「真的不存在」與「存在但我不能進」。

### 為何 `EnterCompany` 直接覆寫而非要求先 `LeaveCompany`

切換公司若拆兩步（`LeaveCompany` + `EnterCompany(newId)`），存在三個風險：

1. **非原子**：兩步驟之間若有錯誤或網路斷線，session 卡在「未進公司」狀態
2. **權限失效視窗**：兩步驟之間 UI 可能短暫嘗試呼叫某 company-bound BO method，會被拒
3. **語意混亂**：「我要切到 B 公司」本意是 atomic intent，不應被表達成兩個獨立動作

採直接覆寫後，切換是單一 RPC，要嘛成功切換要嘛失敗回滾，無中間狀態。

## 替代方案（已評估後不採納）

1. **單階段 `Login(account, password, companyId)`**
   - 拒絕原因：使用者通常需先驗證身份才能查詢可進入的公司清單；強制先選公司違反實務流程

2. **`Login` 後自動綁定第一家可存取公司**
   - 拒絕原因：使用者需要顯式選擇（避免誤入；多家公司情境下「第一家」語意不明確）

3. **省略 `LeaveCompany`，切換完全靠 `EnterCompany` 覆寫**
   - 拒絕原因：失去「顯式離開」的能力；UI 要回公司選擇頁時無對應 API
   - 採折衷：保留 `LeaveCompany` 但**不**讓它成為切換的必要前置步驟

4. **省略 `Logout`，靠 token 過期自然失效**
   - 拒絕原因：被動式登出延遲長（token 通常 1 小時 TTL），無法支援「使用者明確登出」UX；audit log 也需要顯式 logout 事件

5. **`CompanyId` 用 enum 替代 string**
   - 拒絕原因：companyId 是部署設定產生的字串值（可能含租戶代碼、年份等），無法在程式碼層 enum 化

## 結果

### Session 狀態轉移

```text
              Login()
   (none) ───────────────→ Logged-in
                          (CompanyId = null)
                              │
                              │ EnterCompany(A)
                              ↓
                          In Company A
                          (CompanyId = "A")
                              │
                       ┌──────┼──────┬──────────┐
                       │      │      │          │
              EnterCompany(B) │      │ Logout() │ LeaveCompany()
                       │      │      │          │
                       ↓      │      ↓          ↓
                  In Company B│   (none)    Logged-in
                              │
                              │ Logout()
                              ↓
                            (none)
```

### 合法 / 非法呼叫路徑

| 路徑 | 結果 |
|------|------|
| `Login → EnterCompany(A) → [業務] → LeaveCompany → EnterCompany(B) → Logout` | ✅ |
| `Login → EnterCompany(A) → Logout`（隱含 LeaveCompany） | ✅ |
| `Login → Logout`（未進公司直接登出） | ✅ |
| `Login → LeaveCompany`（未進公司直接 Leave） | ✅ idempotent |
| 未 `Login` → 任何方法（除 `Login` / `Ping` 等 Anonymous） | ❌ `Unauthorized` (-32001) |
| 已 `Login` 但未 `EnterCompany` → company 類 BO 方法 | ❌ `CompanyNotEntered` (-32002) |
| `EnterCompany(不存在或無權限)` | ❌ `CompanyAccessDenied` (-32003) |

### 新增錯誤碼

| 錯誤碼 | 數值 | HTTP 對應 | 用途 |
|--------|------|----------|------|
| `Unauthorized`（既有） | -32001 | 401 | session 無效或過期 |
| `CompanyNotEntered`（新增） | -32002 | 409 | 已登入但未進公司，呼叫了需要公司情境的 method |
| `CompanyAccessDenied`（新增） | -32003 | 403 | `EnterCompany` 失敗（公司不存在 / 無權限，刻意合併） |

### 對外 API 變更

| 範圍 | 變更 |
|------|------|
| `SystemBusinessObject` | 新增 `EnterCompany` / `LeaveCompany` / `Logout` 三個 public method |
| `ISystemBusinessObject` | 加 `Login` / `EnterCompany` / `LeaveCompany` / `Logout` 介面宣告（給跨 BO 呼叫用） |
| `SessionInfo` | 加 `CompanyId` (string?) 欄位 |
| `Bee.Definition.Identity.CompanyInfo` | **新增**類別（3 欄位：`CompanyId` / `CompanyName` / `CompanyDatabaseId`） |
| `ICompanyInfoService` / `CompanyInfoCache` | **新增**：類比 `ISessionInfoService` / `SessionInfoCache` 模式 |
| `SystemActions` | 加 `EnterCompany` / `LeaveCompany` / `Logout` 常數 |
| `JsonRpcErrorCode` | 加 `CompanyNotEntered` / `CompanyAccessDenied` |
| `SystemApiConnector` | 加對應 client wrapper（async + sync 各一） |

## 取捨

### 公司權限驗證延後

`EnterCompany` 目前只做「公司是否存在」檢查，**不做使用者-公司權限驗證**。完整權限模型（user-company 對映 schema、角色 / 可見性規則）由後續 ADR + plan 接手。`EnterCompany` 內部已留 TODO 註解標明擴充點，未來在「存在性檢查」與「寫入 SessionInfo」之間插入權限驗證即可。權限失敗的錯誤碼與「公司不存在」一致（`CompanyAccessDenied`），未來新增權限驗證不需改錯誤碼結構。

### `CompanyInfo` 從 cache miss 不洩漏 `CompanyId`

`EnterCompany` 寫入 `SessionInfo.CompanyId` 前已驗證 `CompanyInfo` 存在，理論上後續查 `CompanyInfo` cache 不應 miss。若發生（譬如 cache 被 invalidate），錯誤訊息不洩漏 `CompanyId`，避免 attacker 用 cache invalidation 配合錯誤訊息差異探測 ID。

### `LeaveCompany` 在切換場景不必要

`LeaveCompany` 不參與「切換公司」流程（`EnterCompany` 直接覆寫），可能讓部分開發者覺得它「沒什麼用」。設計上保留是為了：
- 顯式「離開公司」UX 動作（回公司選擇頁）
- 與 `Logout` 對稱（`Logout` 內部走 `LeaveCompany` 邏輯）
- 未來若需要「session timeout 但保留登入」之類降級行為，已有清掉公司 context 的 API

## 影響範圍

| 範圍 | 影響 |
|------|------|
| `src/Bee.Definition.Identity` | 新增 `CompanyInfo` 類別、`ICompanyInfoService` 介面；`SessionInfo` 加 `CompanyId` 欄位 |
| `src/Bee.ObjectCaching` | 新增 `CompanyInfoCache`、`CompanyInfoService`；`ICacheContainer` 加 `CompanyInfo` |
| `src/Bee.Business/System` | `SystemBusinessObject` 加 3 個方法；`ISystemBusinessObject` 介面更新 |
| `src/Bee.Api.Core` | 新增 `EnterCompany` / `LeaveCompany` / `Logout` 的 wire DTO 與 contract 介面；`JsonRpcErrorCode` 加 2 個值 |
| `src/Bee.Api.Client` | `SystemApiConnector` 加 3 組 async + sync wrapper |
| `src/Bee.Definition.SystemActions` | 加 3 個常數 |
| 測試 | 11 個 P3 EnterCompany 測試 + 6 個 P4 LeaveCompany 測試 + 6 個 P5 Logout 測試 + 4 個 P6 lifecycle 整合測試 |

## 相關文件

- [ADR-010：邏輯資料庫分類（DbCategory）](adr-010-logical-database-category.md) — `company` 類 DB 是本 ADR 的主要消費者
- [計畫：SystemBO Session 生命週期方法](../plans/plan-system-bo-session-lifecycle.md) — 實作細節與 phase 拆分
- [計畫：bo repo 三類資料庫存取路由](../plans/plan-bo-repo-db-routing.md) — Repository 層消費 `SessionInfo.CompanyId` 的路由機制
