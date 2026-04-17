# 計畫：新增 SonarCloud 規則指引至 `.claude/rules/`

## 背景

`docs/plans/plan-sonarcloud-codesmells.md` 已整理出 129 個 code smells 的修正清單，其中涵蓋約 30 條 SonarCloud 規則。目前 `.claude/rules/` 僅有 `scanning.md`（純 SAST 安全規則），缺乏對應的**程式碼品質／可維護性**規則指引。

新增 `rules/sonarcloud.md`，讓未來撰寫新程式碼時能主動遵守，減少事後修正成本，並避免重複觸發相同的 code smells。

## 目標

1. 建立 `.claude/rules/sonarcloud.md`，收錄專案歷來觸發過的 SonarCloud 規則
2. 依主題分類（非依規則編號），便於記憶與查詢
3. 每條規則含：**規則編號**、**一句話原則**、**✅ 正確／❌ 錯誤範例**（必要時）
4. 在 `.claude/CLAUDE.md` 底部 `@rules` 區塊加入 `@rules/sonarcloud.md`
5. 避免與現有 `code-style.md` / `scanning.md` 重複

## 檔案範圍

### 新增
- `.claude/rules/sonarcloud.md`

### 修改
- `.claude/CLAUDE.md`（加入 `@rules/sonarcloud.md`）

## 規則分類與內容大綱

以下為 `sonarcloud.md` 擬定章節，每節內列出對應規則。

### 1. 類別與型別設計
- **S1118** — 純 static 成員的 utility class 應加 `private` 建構子
- **S3442** — 只含 static 成員但可實例化的 class 應標為 `static class`
- **S3925** — 名稱含 `Exception` 的類別應繼承 `System.Exception`
- **S2094** — 不應存在空 class（移除或改為 interface）
- **S3260** — 非被繼承的 `private` class 應標為 `sealed`
- **S2344** — `enum` 不應明確指定 `int` 作為 underlying type
- **S2342** — `enum` 命名若表達「集合」語意，需符合末尾加 `s` 的 regex
- **S101** — 類別名須 Pascal case（如 `Utf8StringWriter` 而非 `UTF8StringWriter`）

### 2. 方法與成員修飾詞
- **S2325** — 未存取 instance 成員的方法應改為 `static`
- **S2933** — 不被重新賦值的欄位應標為 `readonly`
- **S4487** — 未被讀取的 `private` field 應移除

### 3. 介面與 override 一致性
- **S1006** — override／implementation 方法須保留與基底相同的 default 參數值
- **S927** — override／implementation 方法的參數名須與介面完全一致
- **S4144** — 實作完全相同的多個方法應合併或重構

### 4. 控制流與語法
- **S1066** — 可合併的巢狀 `if` 應合併
- **S127** — `for` loop 不應在 body 中修改停止條件變數
- **S4023** — 用 pattern matching 取代 `is`+cast 的慣用寫法
- **S1116** — 移除空 statement（多餘的分號）

### 5. 欄位與初始化
- **S3604** — 欄位若於建構子中賦值，不應再有 inline initializer（如 `= null`）
- **S3963** — 可 inline 初始化的靜態欄位，不應放入 static constructor
- **S3877** — static constructor 不應 throw
- **S2743** — generic type 中的 static field 不會跨 close constructed types 共享，避免誤用

### 6. DateTime 與文化相依 API
- **S6562** — 建立 `DateTime` 需明確指定 `DateTimeKind`
- **S6580** — `DateTime.Parse` 等文化相依方法須指定 `IFormatProvider`

### 7. 集合與 LINQ
- **S3267** — 可用 `.Where()` 取代 `foreach` + `if` 的過濾模式

### 8. 字串與陣列
- **S6580**（字串面向）— 重複字面字串應提取為 `const`
- **S3878** — `params` 參數呼叫時不需明確建立 array

### 9. 例外處理
- **S112** — 不應 throw `System.ApplicationException`，改用自訂例外或 `InvalidOperationException`
- （現有 `scanning.md` 已涵蓋 catch `Exception`、空 catch、`throw ex` 等 — 此處不重複）

### 10. 死碼與已廢棄程式碼
- **S125** — 移除被註解掉的程式碼
- **S1133** — 移除標記為 `[Obsolete]` 且無必要保留的程式碼

### 11. Reflection 與 Assembly
- **S3885** — 優先使用 `Assembly.Load` 而非 `Assembly.LoadFrom`

### 12. 測試
- **S2701** — `Assert` 第一參數不應為字面值（如 `Assert.True(true)`）

## 與既有規則的關係

| 新規則章節 | 既有檔案涵蓋 | 處理方式 |
|-----------|-----------|--------|
| 命名 Pascal case（S101） | `code-style.md` 已有一般命名 | sonarcloud.md 僅補 SonarCloud 具體 regex 要求，並交叉引用 |
| 例外處理（S112） | `scanning.md` 已涵蓋 `catch Exception` / 空 catch | S112 為新規則，補充在 sonarcloud.md |
| 命名空間一致性（CA1724/IDE0130） | `code-style.md` 已有 | 不重複 |

## 撰寫風格

- 語言：繁體中文
- 每條規則以**表格或簡短段落**呈現，維持 ≤ 3 行說明
- 僅在原則不明顯時補 `✅`/`❌` 範例
- 末尾附「不納入之規則」說明（避免讀者疑問）

## 不納入之規則

- **Cognitive Complexity（S3776）** — 計畫中已列為「不納入修正」，屬重構判斷題，非通用原則
- **一次性的專案特定修正**（如特定欄位名改名）— 非通用規則
- **CA 系列**（Roslyn Analyzer）— 已由編譯器警告把關，不需重複列入

## 驗證方式

- 文件撰寫完成後，由使用者審閱章節是否有遺漏或重複
- 確認 `.claude/CLAUDE.md` 正確 `@import`
- 未來 SonarCloud 掃描出新規則時，再追加至對應章節

## 預估規模

- `sonarcloud.md`：約 150–200 行（含範例）
- `CLAUDE.md`：新增 1 行
