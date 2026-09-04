# 框架全面體檢（2026-09-04）

**狀態：🚧 進行中（2026-09-04）** —— **P0 / P1 / P2 全數關閉**（1 + 11 + 12 項），
**P3 的 DOC 系列（19 條）亦全數關閉**。P3 其餘與 P4 進行中。

> 每一項修正都經**負向驗證**（刻意弄壞、確認閘門會紅）或**實測**（真資料庫、真環境）。
> 過程中查出報告本身數處有誤，逐筆記在對應條目 —— 那些更正也是本次產出的一部分。

> **MessagePack 標註的文件殘留已全部清除**（P1-10 + DOC-9 兩個 commit）：全公開文件對
> `[MessagePackObject` / `[Key(` / `keyAsPropertyName` / `[Union(` 命中歸零。共同根因是
> `plan-definition-messagepack-decoupling.md`（✅ 2026-08-09 完成）改完程式碼、文件沒跟 ——
> **plan 完成不等於文件完成**，這條值得寫進下輪的方法論。

> **A-5（把 1,121 行檔案 IO 搬離定義層）經使用者裁決維持遞延**（2026-09-04）。
> P1-10 的 README 敘述因此改為陳述現況並說明它為何還在，而不是承諾搬離。

對 17 個 `src/` 專案做十一面向唯讀體檢，產出分級重構計畫與評分。
方法：10 個平行唯讀子代理分面向全量掃描 → 交叉去重 → P0/P1 主代理複驗（含執行期 probe 與實測）。

- 基準版本：**v4.27.0**（`Version.props`），HEAD `93ef5713`
- 上輪體檢：2026-08-11（[plan-framework-review-2026-08-11.md](archive/plan-framework-review-2026-08-11.md)，基準 v4.19.0、HEAD `227daa70`）
- 期間變更：**165 commits**（自 `227daa70`；自 `v4.19.0` tag 起為 177，tag 與上輪體檢 HEAD 之間有 12 個），
  `src/` 312 檔異動（+8,487 / −4,107），新增 64 檔 / 刪除 15 檔，跨 8 個 minor，**11 筆破壞性變更**
- 佐證：clean Release build **0 警告 / 0 錯誤**；`./test.sh` 16 個測試專案、**5,827 通過 / 1 略過（RSA）**，
  四個 DB 容器全在。**但第二次執行出現 1 筆失敗 —— 見 T-1。**

---

## 評分總表

| # | 面向 | 上輪修正後 | 本輪 | 變化 | 主要扣分點 |
|---|------|-----------|------|------|-----------|
| 1 | 架構分層 | 9.0 | **8.8** | ▼0.2 | A-5 / A-3 遞延中；**ARCH-1**（README 主動否認 A-5）、**ARCH-2**（四條硬約束零可執行閘門）、**ARCH-3**（`HttpUtilities` 搬離最底層）、ARCH-4 |
| 2 | 相依分層 | 9.4 | **9.0** | ▼0.4 | **DEP-1**（README 雙語宣稱定義層相依 MessagePack）、**GATE-1**（`Bee.Api.Contracts` 閘門無 canary）。**相依事實本身 100% 乾淨** |
| 3 | 安全性 | 8.7 | **8.2** | ▼0.5 | **SEC-1**（記錄範圍可繞過）、**SEC-2**（空雜湊段恆真 + password 未受保護）、SEC-3（MySQL 逸出）、三項遞延未動 |
| 4 | 維護性 | 9.4 | **9.2** | ▼0.2 | M-1（XML doc 清點數字寫錯，真回歸）、M-2（中文註解 +5）、M-3（可變靜態命名） |
| 5 | 散落／不必要類別 | 9.3 | **9.2** | ▼0.1 | ~~**D-1**~~（已修）、D-2~D-4 |
| 6 | 序列化一致性 | 9.5 | **8.7** | ▼0.8 | **S-1**（DataTable 金額在 JSON codec 精度失真）、**S-2**（回應 codec 對稱零測試）、**S-3**（fixture 完整性閘門恆真）、S-4~S-6 文件 |
| 7 | 公開 API 表面 | 9.2 | **8.6** | ▼0.6 | **API-1**（`ReplayProtection` 無守門）、API-2（最高槓桿建議只落地一半）、**API-3**（NUL 讓 grep 閘門失明）、API-4（新閘門缺防空轉） |
| 8 | 測試品質與覆蓋 | 9.0 | **8.8** | ▼0.2 | **T-1**（實測 flaky：測試併發改寫共用快取）、T-2（`Bee.Hosting.UnitTests` 無並行保護）、T-3 |
| 9 | 文件漂移 | 9.2 | **7.6** | ▼1.6 | **DOC-1**（Northwind README 教一個必定失敗的步驟）、DOC-2~DOC-6（已移除／已更名型別以現在式呈現，20+ 處） |
| 10 | 效能／熱路徑 | 8.6 | **8.0** | ▼0.6 | **PERF-1**（每列一次 DB round trip，實測 14.3×）、**PERF-2**（>30 KB body 落磁碟換取零人使用的重繞） |
| 11 | 並行與全域狀態 | 9.1 | **8.8** | ▼0.3 | CON-1（稽核背景服務無例外護欄）、CON-2（檔案 fallback 無鎖並行 append）、CON-3（`ClientInfo` per-user static 無警語） |

**九面向平均 9.19 → 8.68**（▼0.51）；**八面向（不含文件）9.22 → 8.81**（▼0.41）；**十一面向 9.13 → 8.63**（▼0.50）。

> **評分方法沿用上輪：扣分只看「現在還開著什麼」，不獎勵「修了多少」。** 明示遞延的項目（A-5、A-3、SEC 啟動硬失敗、
> 帳號鎖定 IP 維度、建構子 `isLocalCall`）仍然照扣 —— 它們沒有被解決，只是被理解了。
>
> **⚠️ 相依分層的扣分與文件漂移重疊。** 相依面向的扣分（DEP-1、BEE9001 涵蓋範圍漂移、9 份 README 的相依區塊）
> 內容上全部是文件錯誤，也計入面向 9。我把它從代理原評的 8.9 上調為 **9.0**，理由是**相依關係的事實本身零缺陷**
> （28 條邊零循環、四條硬約束全綠、ADR-038 落實度全綠、mermaid 圖 28/28 吻合），扣的是「描述它的文件錯了」，
> 不該與文件面向等量各扣一次。

---

## 分數下降的歸因（方法論教訓 C：拆分才知道是修好了還是沒看到）

| 面向 | 變化 | 真回歸 | 既有問題首次掃出 | 掃描深度變化 |
|------|------|--------|-----------------|-------------|
| 安全性 | ▼0.5 | −0.3（**SEC-13/P-0 是 4.25.0 引入**） | −0.7（SEC-1 記錄範圍、SEC-2 空雜湊皆早於基準） | +0.5（白名單 parser 的修法**已擴散到 4.27.0 新程式碼**，是實質改善） |
| 文件漂移 | ▼1.6 | −1.2（D-1~D-6 全部源自 4.21.0–4.23.0 的改名／刪型別未同步 `.md`） | −0.3（DOC-1 的 apps README、Protection 圖例） | +0.5（機械性維度**全部滿分**：2,457 連結零壞、ADR 索引 44/44、analyzer 22/22、雙語 74 對、版號複寫 0） |
| 序列化 | ▼0.8 | −0.6（S-1 的曝險、S-2/S-3 的新閘門缺口皆隨 4.27.0 新機制而來） | −0.2（S-4~S-6 的公開文件早已失效） | 0（codec 協商的實作面經逐行審查與 probe，設計紮實） |
| 效能 | ▼0.6 | 0 | −0.6（PERF-1 自 `c6dc285f`、PERF-2 自 2025-05-06） | +0.3（**codec 協商零效能債**，JSON 比 MessagePack 略快、gzip 後 0.99×） |
| 並行 | ▼0.3 | −0.05（CON-D replay window TOCTOU，4.27.0） | −0.45（CON-1/2/3 全部早於基準，**上輪「無實質殘留」判定太寬**） | +0.2（上輪 9 項修正全部複驗有效且**無一引入新攻擊面**） |
| 公開 API | ▼0.6 | −0.3（API-1 `ReplayProtection`、API-4 新閘門、明細檔缺漏） | −0.3（NUL、236 處 `<c>`、契約集合型別、analyzer 基準從未 ship） | 0 |

**沒有任何一項是架構或相依的程式碼回歸。** 165 個 commit 的新功能全部落在正確的層，
28 條相依邊零循環，四條硬約束全綠。

---

## 執行階段

| 階段 | 範圍 | 項目數 | 狀態 |
|------|------|--------|------|
| P0 | 已出貨功能遠端不可用 | 1 | ✅ **已完成**（2026-09-04，P0-1 修正 + 新增保留字 progId 建構閘門，負向驗證通過） |
| P1 | 授權邊界、跨語言 wire 正確性、實測效能、閘門可靠性 | 11 | ✅ **已完成**（2026-09-04，11 項全數落地，皆經負向驗證或實測） |
| P2 | 結構、並行、一致性 | 12 | ✅ **已完成**（2026-09-04，含 D-1 的 helper 下沉；T-2 查證後不成立） |
| P3 | 文件漂移與低風險清理 | — | 🚧 **進行中**（DOC 系列 19 條 ✅、ARCH-2 / ARCH-3 ✅、T-5 ✅、T-7 ✅ + 衍生 T-8 的 A 類 ✅、T-6 部分完成、**CON-5 / SEC-4 / SEC-5 / T-3 ✅**、**S-4~S-10 ✅**、**D-2 / D-3 / M-1 / M-2 / M-3 / T-4 ✅**；**SEC-6 ✅**、**CON-6 ❌ 撤回**；**API-3 / D-4 ✅**；剩 API-4、API-5） |
| P4 | 觀察／待裁決 | — | 📝 擬定中（`A-5` / `A-3` 經使用者裁決維持遞延） |

> **階段的項目數不寫死**：條目在執行中會被拆分（T-7 → 衍生 T-8）或查證後撤銷（T-2、CON-6），
> 寫死的數字必漂。各級別的實際清單以下方對應段落為準。

---

## P0 — 已出貨功能遠端完全不可用

### P0-1　`AuditRule` progId 經 JSON-RPC 完全不可達（4.25.0 起，真回歸）　✅ 已修（2026-09-04）

`src/Bee.Business/AuditLog/AuditRuleBusinessObject.cs:28`

```csharp
public AuditRuleBusinessObject(IBeeContext ctx, Guid accessToken, string progId)   // 只有 3 參數
    : base(ctx, accessToken, progId)
```

`BusinessObjectFactory.CreateBusinessObject`（`src/Bee.Business/BusinessObjectFactory.cs:66`）固定以 **4 個引數**
`Activator.CreateInstance(type, ctx, accessToken, progId, isLocalCall)`。**C# 建構子不繼承**，此型別沒有 4 參數版本。

**兩次獨立複驗**：
- 子代理載入 `Bee.Business.dll` 重現工廠那一行 → `MissingMethodException: Constructor on type
  'Bee.Business.AuditLog.AuditRuleBusinessObject' not found.`
- 主代理以組件反射比對建構子簽章 → `AuditRuleBusinessObject` 4-arg ctor **False**，
  `LogBusinessObject` / `FormBusinessObject` 皆 **True**

`src/Bee.Business/ReservedProgIds.cs:23` 有登記 `SysProgIds.AuditRule`，**代表它本來就該被派發** ——
任何 `AuditRule.*` 呼叫都炸在 BO 建立階段，映射為 `InternalError`。
4.25.0 出貨的「每表單稽核規則」維護表單（ADR-041）因此遠端完全不可用。

**為何出貨時是綠的**：唯一的測試 `tests/Bee.Business.UnitTests/Form/AuditRuleFormTests.cs:129`
直接 `new AuditRuleBusinessObject(...)`，**從未走工廠**。

**修法**：建構子補第四個參數。**同時補閘門** —— 目前沒有任何測試把「每個註冊的保留 progId 都能經
`BusinessObjectFactory` 建立」釘住，一個測試（對 `ReservedProgIds.All` 逐項 `CreateBusinessObject`）即可關閉這一整類。

引入：`5eab83e1`（4.25.0）。

---

## P1

### P1-1　layer-2 記錄範圍在「明細-only 的 Save」下完全不執行　✅ 已修（2026-09-04）

`src/Bee.Business/Form/FormBusinessObject.Permission.cs:74`（`HasExistingMasterWrite`）、`:102`（`EnforceWriteScope`）、
`src/Bee.Repository/Form/DataFormRepository.cs:264`

兩個守門條件都以「主檔表存在於 DataSet 中」為前提：

```csharp
if (string.IsNullOrEmpty(masterTableName) || !dataSet.Tables.Contains(masterTableName)) { return; }
```

而 `DataFormRepository.Save` 對每張表獨立判斷是否在 DataSet 裡（`:264` `continue`），**主檔缺席時明細照樣寫入**。
送出一個只含明細表的 `Save`，`HasExistingMasterWrite` 回 `false` → `EnforceWriteScope` 根本沒被呼叫 →
對 scope 外的主檔記錄插入／修改／刪除明細列。`sys_master_rowid` 由 payload 原樣採用（不在 `ProtectedFields`）。

**與程式碼自己的宣稱直接矛盾**：`FormBusinessObject.Write.cs:56-59` 寫著
「Scope is master-only, so once the master passes the whole record persists with it」——
但主檔不在 payload 裡時，**沒有任何 master 通過過**。

前提：已認證且對該 form 具備寫入 grant、其 scope 被限縮。需知道目標 `sys_master_rowid`（GUID 猜不到，
但會經 lookup 欄位、報表、匯出、以及「曾在 scope 內後被移出」等管道外洩）。**靜態分析結論，未端到端實測。**

**修法（2026-09-04 已落地，兩道檢查都做）**：`EnforceWriteScope` 改為無條件呼叫，並加
(1) 宣告了主檔表卻只帶待寫明細列 → 拒絕；(2) 每個待寫明細列的 `sys_master_rowid` 必須是
本次 payload 帶著的主檔列之一（`Modified` 檢查 Original 與 Current 兩版）。

> **修正過程中發現同一破口的第二種走法，已一併關掉**：明細的 `sys_master_rowid` 同樣是
> payload 原樣採用，且 `Modified` 列會被完整欄位 UPDATE 改寫 —— 帶一個 in-scope 主檔、
> 明細卻指向另一筆，或把既有明細從別人的記錄改嫁出來，都能繞過。只修回報的那一種會
> 製造已經修好的錯覺。`HasExistingMasterWrite` 隨之移除：它的兩個 false 情形
> （純新增 vs 主檔表缺席）語意相反卻回同一個答案，正是這個 bug 的形狀。

引入：`2d217c11`（2026-06-05），早於上輪基準 → 既有問題首次掃出。

### P1-2　`PasswordHasher.VerifyPassword` 對空雜湊段恆真，且 `st_user.password` 未受保護　✅ 已修（2026-09-04）

`src/Bee.Base/Security/PasswordHasher.cs:53`、`:64`（v2 與 legacy 兩個分支同形）

`storedHash.Length == 0` → `Pbkdf2(..., outputBytes: 0)` 回空陣列 → `FixedTimeEquals(空, 空)` **回傳 true**。

**子代理 scratchpad 實測**（走公開 API `PasswordHasher.VerifyPassword`）：

```
stored='v2.100000..'       -> True     ← 任何密碼都通過
stored='v2.1..'            -> True
stored='1..'               -> True     ← legacy 分支同樣中
stored='v2.100000..AAAA'   -> False    ← 對照組：雜湊段非空即正確拒絕
```

**主代理補強可達性**：`src/Bee.Definition/ProtectedFields.cs:26` **只含 `st_user.deployment_admin` 一筆**，
而 `st_user.password` 是框架出貨 TableSchema 的可寫欄位
（`src/Bee.Definition/Defaults/TableSchema/common/st_user.TableSchema.xml:8`，`String/200`）。
任何在 `st_user` 上建了使用者維護表單的部署（ERP 常態），該欄即可經 FormSchema 資料路徑寫入
→ 把他人 password 寫成 `v2.100000..` → 以任意密碼登入該帳號。

`ProtectedFields` 的 XML doc 自稱「Each protected column has exactly one legitimate write path」——
password 同樣符合該描述卻不在清單內。

**另一併修**：`iterations` 直接讀自儲存字串、無下限，`v2.1.…` 只跑 1 輪。

**修法**：`storedHash.Length == 0 || salt.Length == 0` 即 `return false`；`iterations` 設下限（如 10,000）；
`st_user.password` 加入 `ProtectedFields`。

### P1-3　`DataTable` 儲存格的 `decimal` / `int64` 在 JSON codec 上以 JSON number 輸出　✅ 已修（2026-09-04）

`src/Bee.Base/Serialization/DataTableJsonConverter.cs:119`
（`JsonSerializer.Serialize(writer, val, val.GetType(), options)`）

子代理 probe 輸出：`"amount":79228162514264337593543950335, "bigint":9007199254740993`

**同一個 codec 的另一半做了相反的事**：`src/Bee.Api.Core/Json/WireValueJsonConverter.cs:28-32` 的
`IMPORTANT` 明寫「`decimal`、`long`、`ulong` 寫成 JSON 字串而非數字。JSON number 對每個 JavaScript 讀取端
都是 double，撐不住 decimal 精度也撐不住 2^53 以上的整數，不加引號會**在這個 codec 存在的目的所服務的
那些客戶端上毀掉金額與識別碼**」，`:172-173` 照此實作。

但 ADR-044 §6 第三條把「DataTable 的儲存格不走封套」排除在該規則外，而 **ERP 的金額幾乎都走儲存格、
不走 `object` 封套**。`JSON.parse` 在客戶端程式碼看到值之前就已毀掉數字；column metadata 的
`"type":"Decimal"` 救不了。症狀是**錯值不是錯誤**。

放大這一點的是三份互相佐證的產物：`wire-fixtures/README.md` 明列「decimal/int64/uint64 是 JSON 字串」
為讀者必須做對的規則，而 `wire-fixtures/bodies/datatable.json` 的樣本值 `1234.56` / `99.99`
**恰好都能安全通過 `JSON.parse`**，示範不出危險；`wire-contracts/messages.d.ts` 把儲存格宣告為
`Record<string, unknown>` 不帶警示。

**時效性是這條列 P1 的理由**：`wire-fixtures/` 一旦有實際跨語言 client 依循，改動就是破壞性變更；**現在改幾乎免費。**

**修法（2026-09-04 已落地：走 (a) 改編碼）**：儲存格與欄位 `defaultValue` 共用一個 `WriteCellValue`，
對 `decimal` / `long` / `ulong` 以 invariant 格式寫成 JSON 字串（依**值的執行期型別**判斷，
讀取端則依**欄位的 CLR 型別**解回，兩邊各用最可靠的來源）。讀取端**仍接受裸數字**，
所以照 4.27.0 fixtures 實作的 client 不會被打斷。

> **樣本值一併換掉**：`BuildTable` 原本用 `1234.56` / `99.99`，那種值就算被當成 double 也看不出
> 差別 —— 樣本示範不出它要示範的規則。改用 `decimal.MaxValue`、最小刻度、2^53+1 與 `long.MaxValue`。
>
> **影響範圍比回報的大一層**：這個 converter 由 `JsonCodec`（Plain / ADR-014）、
> `JsonPayloadSerializer`（JSON codec / ADR-044）與 `ApiInputConverter` 三處共用，兩條 wire 都是
> 給 JavaScript 讀的，所以同一個問題在 ADR-014 那條也存在、一起關掉。
代價是要改 `wire-fixtures/` 的既有樣本，但目前沒有外部 client 依循，成本最低的時機就是現在。
（未採用的 (b)：維持現狀、在 `wire-fixtures/README.md` 與 `messages.d.ts` 前言明寫此例外，
並把 `datatable.json` 的樣本值換成會爆掉 double 的數字。）

### P1-4　「伺服端以同一個 codec 回應」是新機制的核心不變式，零測試　✅ 已修（2026-09-04）

`src/Bee.Api.Core/JsonRpc/JsonRpcExecutor.cs:145` —— `Codec = request.Params.Codec`

全 `tests/` 對 `Codec` 的斷言只在 `tests/Bee.Api.Client.UnitTests/ApiConnectorExecuteTests.cs`，
而那裡的假 provider **自己**寫 `Codec = req.Params.Codec`（`:164`）—— 它證明的是 stub 的行為，不是 executor 的。
拿掉那一行，整個測試套件仍全綠，而每一個協商過 codec 的 client 都會收到解不開的回應。

**修法**：加一條「request 宣告 json → `response.Result.Codec == "json"` 且該 body 真的由 JSON codec 解得開」，
並反向驗證（暫時清掉那個賦值應變紅）。

### P1-5　fixture 完整性閘門恆真，且現況已違反　✅ 已修（2026-09-04）

`tests/Bee.Api.Core.UnitTests/WireFixtureTests.cs:287-289`

```csharp
// 每個判別碼都要有樣本：漏一個就是 TS 端某個型別會靜默錯值。
Assert.Equal(22, codeCases.Count);
```

實際的 22 個 `value-*` 是**判別碼 1–20、22 共 21 個 + `value-null`**（`null` 不是判別碼）。
**`WireValueCode.DataTable`(21) 在 `object` 封套內的 JSON 形狀沒有任何樣本**。
新增判別碼 23 而不補樣本，`codeCases.Count` 仍是 22，**測試照樣綠** —— 它數的是樣本數而不是「碼 ⊆ 樣本」。

**連帶的第二個缺口**：型別→判別碼的對映有兩份獨立權威 ——
`WireValueFormatter.s_codes`（`src/Bee.Api.Core/MessagePack/WireValueFormatter.cs:52`）與
`WireValueJsonConverter.ResolveCode`（`src/Bee.Api.Core/Json/WireValueJsonConverter.cs:129-154`，手寫 if-chain），
**沒有任何測試比對兩者**。只在 MessagePack 端加一個碼，JSON 端會靜默改走型別名逃生門。

而 `src/Bee.Api.Core/CLAUDE.md:27-28` 與 `Wire/WireValueCode.cs:7-11` 都寫「兩條 wire 共用同一組判別碼
（`WireValueCodePinTests` 同時釘住兩者）」——**`WireValueCodePinTests` 只驅動 MessagePack**。

**修法**：(a) 斷言改為由 `WireValueCode` 常數表驅動的「每個常數都有對應案例」；(b) 補 `value-datatable`；
(c) 加一條逐一比對 `s_codes` 與 `ResolveCode` 的測試。

### P1-6　`[ApiAccessControl].ReplayProtection` 是第三個安全維度，卻無任何守門　✅ 已修（2026-09-04）

`src/Bee.Definition/Attributes/ApiAccessControlAttribute.cs:43`（4.26.0 新增）
使用點 5 處：`BusinessObject.cs:258`（`ExecFunc`）、`FormBusinessObject.Write.cs:47`（`Save`）、`:195`（`Delete`）、
`SystemBusinessObject.Session.cs:93`（`EnterCompany`）、`:126`（`LeaveCompany`）

主代理複驗：`tests/Bee.Api.Core.UnitTests/ApiAccessControlPinTests.cs` 與
`tests/Bee.Business.UnitTests/BoApiSurfaceTests.cs` 對 `ReplayProtection` 的命中數**各為 0**
（兩者的期望表都只有 `(Protection, Requirement)`）。

**把 `Save` 的 `ReplayProtection = UniqueSequence` 刪掉，框架主要寫入路徑的重放防護就靜默關閉，
而兩道 pin 測試都是綠的。** 同時它對客戶端作者是行為契約（不送遞增序號就收 `-32005 ReplayRejected`），
卻不在 `docs/api-method-reference.md`。

**修法**：`ApiSurfaceEntry` 加第四欄；方法參考表加一欄。

### P1-7　測試對 process-wide 快取實例併發改寫 → 實測 flaky　✅ 已修（2026-09-04）

`tests/Bee.Db.UnitTests/Manager/DbConnectionManagerTests.cs:30,39,47,56`、
`tests/Bee.Db.UnitTests/Manager/DbAccessFactoryTests.cs:39,58`

**主代理實測**：連跑兩次 `./test.sh`，第一次全綠，第二次 `Bee.Db.UnitTests` 出現 1 筆失敗：

```
GetConnectionInfo_EmptyConnectionString_ThrowsInvalidOperationException
  System.ArgumentOutOfRangeException : Index was out of range.
    at List`1.RemoveAt
    at KeyCollectionBase`1.RemoveItem  (KeyCollectionBase.cs:154)
    at KeyCollectionBase`1.Remove      (KeyCollectionBase.cs:61)
    at DbConnectionManagerTests.RemoveItem (:41)
```

兩個測試類別都拿 `IDefineAccess.GetDatabaseSettings()` 回傳的**快取實例**做 `Items!.Add/Remove`，
都是 `IClassFixture<SharedDbFixture>`、都沒有 `[Collection]`，`Bee.Db.UnitTests` 也沒有
`DisableTestParallelization` → xUnit 平行執行 → `KeyCollectionBase<T>` 非執行緒安全：
`RemoveItem` 取完 `this[index]` 後 `base.RemoveItem(index)` 索引越界。

同時違反 `.claude/rules/definition.md`「Cache 內的物件 init 後不可異動」。

**假保證**：`DbConnectionManagerTests.cs:13` 註解寫「使用唯一 databaseId 以避免與其他測試共用的全域快取
互相干擾」—— **唯一 key 只避開 key 碰撞，避不開集合層級的競態**。

引入：`c07c447b`（2026-04-19）／`89f8ae73`（2026-05-13），早於基準 → 既有問題首次掃出。
上輪測試面向掃的是「顯式等待」與「fixture 污染」，**沒掃「測試對共用快取的併發改寫」**。

**修法（2026-09-04 已落地，走「不碰快取」那條）**：兩個類別自帶隔離的 `DatabaseSettings`
（`IsolatedDatabaseSettingsProvider`），直接建構 `DbConnectionManagerService`。沒有選 `[Collection]`
序列化 —— 那只是讓違規不再產生症狀。連 `SharedDbFixture` 也一併去掉（這兩個類別測的是連線字串組裝，
本來就不需要資料庫），19 個測試從「要等容器」變成 20ms。驗證方式是 `./test.sh` **連跑兩次**皆全綠，
與當初判定這筆 flaky 的方法相同。

### P1-8　寫入路徑每列一次 DB round trip（`UpdateBatchSize` 從未設定）　✅ 已修（2026-09-04）

`src/Bee.Db/DbAccess.Update.cs:68-78`（`ApplySpec`）

主代理複驗：`UpdateBatchSize` 在全 repo（src/tests/tools/apps/samples）**0 命中**，
即維持 ADO.NET 預設值 `1` —— adapter 對每一列各發一次 `ExecuteNonQuery`。

**子代理實測**（本機 docker `sql2025`，100 列 INSERT，`UpdatedRowSource = None` 已具備）：

```
UpdateBatchSize = 1（現況）:  30.06 ms  (301 µs/row)
UpdateBatchSize = 0（不限）:   2.11 ms  ( 21 µs/row)      ratio = 14.3x
單次 round trip 獨立量測    : 323.9 µs   ← 與 301 µs/row 吻合，成本確實是往返
```

**頻率**：每次 `Save`，每張表的每一列改動。一張 100 列明細的單據是 **30 ms／Save**，
比整條序列化管線（8.6 ms 序列化 + 4.3 ms gzip）還大，而本機 docker 已是最樂觀情境。

對照：上輪判為雜訊的 PERF-2 是 0.50 µs／請求、P-3 是 8 µs／Save。

**修法（2026-09-04 已落地）**：`ApplySpec` 內以**能力偵測**開啟批次，而非硬寫 provider 清單 ——
基底 setter 擲 `NotSupportedException`，所以「問它」就是檢查本身；清單會漂，也管不到宿主為某個
`DatabaseType` 註冊了哪個 factory。結果以 adapter 型別為 key 快取。批次上限用有界的 100 而非 0。

實測支援度：**SQL Server / MySQL / Oracle 接受；Npgsql 與框架自己的 SQLite adapter 擲例外**
（已由 `ProviderBatchingSupportTests` 釘住，provider 改行為就會紅）。

走 `UpdateDataTables` 自身重新實測（本機 SQL Server 容器，三次中位數）：
**100 列 32 ms → 3 ms（10.7×）、500 列 145 ms → 9 ms（16.1×）**。

> **順帶補掉一個覆蓋缺口**：在此之前 adapter 寫入路徑的 DB 測試**只有 SQLite** —— 而 SQLite 恰好是
> 不支援批次的兩個之一，也就是說批次實際生效的三個 provider 對這條路徑是零覆蓋。新增
> `UpdateDataTablesBatchingTests`（五個 provider 各一個 `[DbFact]`）。

引入：`c6dc285f`，遠早於本輪 → 既有缺陷，兩輪體檢都沒往 ADO.NET adapter 這個框架邊界看。

### P1-9　`apps/Bee.Northwind/README` 教一個現在必定失敗的步驟　✅ 已修（2026-09-04）

`apps/Bee.Northwind/README.md:274` / `README.zh-TW.md:267`

> There is no `FormLayout` file to write — the framework generates the layout from the `FormSchema` at delivery time.

ADR-039 / `29d844d1`（4.23.0）已移除執行階段推導，CHANGELOG 4.23.0 明寫「有 `FormSchema` 卻沒有對應
`FormLayout` 檔的部署，開啟表單時會失敗」。這是「新增一張表單」教學段落的**第 2 步**，讀者照做必得一張開不起來的表單。

**程式碼是對的，只有文件錯**：Northwind 自己的 `Define/FormLayout/` 有 9 個檔、與 9 個 FormSchema 一一對應（主代理複驗）。
`docs/architecture-overview`、`docs/definition-files-overview`、`docs/development-cookbook` 都已正確補上
"at design time"，`f7a5e335` 的同步範圍未含 `apps/`。

### P1-10　`src/Bee.Definition/README` 雙語宣稱上游相依含 MessagePack，且宣稱「no I/O」　✅ 已修（2026-09-04）

`src/Bee.Definition/README.md:15` / `README.zh-TW.md:15`：`- **Upstream dependencies**: Bee.Base, MessagePack`

主代理複驗 csproj：實際只有 `Microsoft.Extensions.Localization.Abstractions` + `Bee.Base`。
**這句話直接推翻 ADR-036 / ADR-038 的核心結論**，而 README 是 `rules/public-docs.md` 明列的公開文件、
外部開發者第一手讀物。照它推導的人會認為「定義層可以帶傳輸格式套件」—— 正是兩道閘門與整份 ADR 要防的判斷。

同段 `:13` 另宣稱此套件 **no business logic and no I/O**，而 `Bee.Definition` 現有 **7 個檔 / 1,121 行**
的檔案 IO（`Storage/FileDefineStorage.cs` 323 行等，即遞延中的 A-5）。這不是「還沒更新」，
是**主動否認一個已知且被記錄兩輪的架構問題**。**不要只刪掉 "no I/O" 三個字** —— 留下的敘述仍會讀成保證。

引入：`0b68854b`（2026-08-10）移除相依時未同步 README，該敘述在上輪基準當下已經是假的。

### P1-11　NUL 位元組讓 repo 的整套 grep 閘門對一個檔靜默失明　✅ 已修（2026-09-04）

`src/Bee.Api.Client/Definitions/SnapshotLanguageService.cs:43`

```csharp
public static string BuildKey(string lang, string ns) => $"{lang}\x00{ns}";
```

該 NUL 是**以原始位元組寫在原始碼裡**（offset 2062），不是 `\0` 逸出序列。主代理複驗：

```
file(1)                          → data
grep -c '///' 該檔               → （空輸出）
grep -hv '^\s*///' 該檔 | wc -c  → 0        ← 5,432 bytes 的原始碼貢獻 0 bytes
```

`check-xmldoc-refs.sh` 的比對母體正是以 `grep -hv '^\s*///'` 建的 → **該檔的型別宣告不進母體**
（可造成別處 `<c>` 的假陽性），**該檔自己的 `<c>` 也永遠不被檢查**（它有一個：第 8 行 `<c>(lang, namespace)</c>`）。
`check-public-docs.sh` 掃 `.cs` 的兩道同樣跳過它，**任何未來的 grep 型閘門也會**。

Python 逐檔驗證全 repo（src/tests/tools/apps/samples）含 NUL 的 `.cs` **就這一個**。

`BuildKey` 是 `PublicAPI.Shipped.txt:147` 的已發布 API，NUL 是鍵分隔語意的一部分，**行為本身沒問題**。

**修法**：改成 `$"{lang}\0{ns}"`（位元組完全相同、行為零變化，檔案回復為文字）。
**並在 `check-*.sh` 加一道「`src/**/*.cs` 不得含 NUL」的斷言** —— 否則下一個誰再寫一個就又靜默了。

---

## P2 — 結構、並行、一致性

| # | 項目 | 位置 | 說明 |
|---|------|------|------|
| **ARCH-1** ✅ 已修（2026-09-04） | 四條硬約束零可執行閘門 | `tests/` | 「BO 無 `Bee.Db`」「後端無 `Bee.Api.Client`」「Repository 抽象未被繞過」「Contracts 零實作污染」目前**只靠每輪體檢重掃**。全 `tests/` 只有 `DefinitionDependencyGateTests.cs` 一支做相依斷言。前兩條可用同一份 `deps.json` 讀取碼斷言（約 20 行、零新相依），第四條可用反射斷言 Contracts 組件內零 `MethodBody`。<br>**2026-09-04 已落地**：`ArchitectureBoundaryGateTests`（`tests/Bee.Api.AspNetCore.UnitTests/`）—— 那是唯一同時看得到整條後端與 `Bee.Api.Client` 的專案，而**被禁的節點必須在圖中**，否則斷言會因看不見而恆真（兩個節點的存在性各有一條斷言守著）。四條各自製造真實違規做過負向驗證，其中 `Bee.Hosting → Bee.Api.Client` 的違規讓 `Bee.Api.AspNetCore` 一起紅，證明傳遞性有效。<br>順帶實測到：`Bee.Api.Core → Bee.Api.Client` **結構上無法違反**（循環相依，編譯器先擋），該條目仍保留 —— 它描述的是意圖，編譯器擋不擋得住不該是這條約束成立的理由 |
| **GATE-1** ✅ 已修（2026-09-04） | BEE9001 的啟用條件本身無 canary | `src/Directory.Build.targets:27` | `BeeEnforceDependencyBoundary` 的 Condition 是三個 `MSBuildProjectName` 字串比對。專案改名或編輯時漏掉一項，target 靜默不執行，**沒有任何東西會紅**。`Bee.Base` 有閉包測試當 backstop；**`Bee.Api.Contracts` 沒有**（它在 `Bee.Definition` 的下游，不在任何閉包測試觀察範圍內）。修法：把閉包測試參數化為多個 root。<br>**2026-09-04 已落地**：`DefinitionDependencyGateTests` 改為三個 root（`Bee.Base` / `Bee.Definition` / `Bee.Api.Contracts`），各帶對應 `BeeAllowedDependency` 的白名單。負向驗證同時模擬「BEE9001 對 `Bee.Api.Contracts` 靜默失效」與「該專案長出 MessagePack」，建置照樣過而測試側攔住 —— 這正是本項要證的事 |
| **API-1** ✅ 已修 | 上輪最高槓桿建議只落地一半 | `tests/Bee.Business.UnitTests/BoApiSurfaceTests.cs` | (a) action 常數↔BO 方法 ✅ **已落地且做得更好**（`86887768` 的 `ActionSurfaceTests`，雙向 + 防空轉，首跑即抓到兩個未登記常數）；(b) baseline↔`docs/api-method-reference.md` ❌ **未落地**（該測試全檔零檔案 IO）。而 `docs/api-method-reference.md:10-13` 雙語對外宣稱「the build will fail otherwise」。目前 41 筆同步是靠紀律。**加上那個 Fact 當天就會綠**（已逐筆比對確認），一次關閉 API-1/P1-6/DOC-5 三項的復發路徑 |
| **API-2** ✅ 已修 | 新增的閘門缺防空轉斷言 | `tests/Bee.Business.UnitTests/Contracts/BusinessContractPairingTests.cs` | 只有一個 `[Theory]`，反射列舉回零筆時**恆綠**。同家族四個閘門（`ApiContractPairingTests` / `ActionSurfaceTests` / `PayloadZoneCoverageGuardTests` / `WireContractDriftTests`）都有。**一致性缺口而非知識缺口**，加 4 行。該檔在 `227daa70` 不存在 → 上輪把這條寫成教訓 #4，隔天新增的閘門就違反了 |
| **CON-1** ✅ 已修（2026-09-04） | `AuditLogWriterService.ExecuteAsync` 無例外護欄 | `src/Bee.Hosting/Audit/AuditLogWriterService.cs:60-77` | 只 catch `OperationCanceledException`；其他例外 → `BackgroundService` Faulted → .NET 預設 `StopHost` **整個應用停機**。**本 repo 的 `docs/adr/adr-017:77` 已明文寫下這條規則**，`CacheNotifyPoller.SafePoll` / `ExpiredSessionCleanupService.SafeCleanup` 都照做了，**唯獨稽核寫入器沒有**。逸出點：`SpillToFile` 的 `NotSupportedException`／`ArgumentException`、`TimeoutException`、**任何自訂 `IAuditLogSink`**（公開 DI 接縫，這種部署下應視為 P1）。引入 `abbff6fc`（2026-07-05） |
| **CON-2** ✅ 已修（2026-09-04） | 稽核檔案 fallback 多執行緒無鎖 append | `src/Bee.Hosting/Audit/AuditLogDbSink.cs:66-88` | `File.AppendAllText` 無序列化，三條並行路徑指向同一檔（背景 drain、佇列飽和時的**每條請求執行緒**、`SynchronousAuditLogWriter`）。Windows 下 `FileShare.Read` 開檔並行會擲 `IOException` → 被 `:82` catch → **該批稽核靜默遺失**，推翻 `AuditLogWriterService` 類別註解「records are never silently lost」。**最大並行度恰好出現在唯一會走到這條路徑的時刻**（log DB 失敗 → 佇列塞滿 → 全部改走同步 fallback） |
| **CON-3** ✅ 已修（2026-09-04） | `Bee.UI.Core.ClientInfo` per-user 狀態放 public static，零警語 | `src/Bee.UI.Core/ClientInfo.cs:13-23` | CON-1（上輪）修掉的形狀往上一層：`_accessToken`、`_capabilities`（權限快照）、`_company`、`_defineAccess`（帶 tenant customization）全是「屬於某個登入使用者」的狀態。對照組 `ApiSessionContext` 帶了完整 WARNING，`ClientInfo` 只有一句「Provides client-side connection state」。repo 內無多使用者可達鏈（Blazor 只參考 `Bee.Api.Client`），但 **`Bee.UI.Core` 是發佈的 NuGet 套件**。另三個 `??=` lazy init 無同步。<br>**2026-09-04 已落地**：補上與 `ApiSessionContext` 對等的 WARNING（隨 NuGet 進 IntelliSense），並以 `Lock` 保護三處延遲初始化與重設。**刻意不用 `Lazy<T>`** —— 這些是可重設的，換掉整個 `Lazy` 實例只是把競態搬家。`AccessToken` setter 的四個欄位併進同一個 lock，讓身分變更成為單一可見步驟 |
| **CON-4** ✅ 已修（2026-09-04） | `XmlCodec.Serialize` 無 try/finally，例外讓共用實例**永久**卡在 serialize 狀態 | `src/Bee.Base/Serialization/XmlCodec.cs:15-35` | `NotifyBefore` → 序列化擲例外 → `NotifyAfter` 永不執行。與 N-1 疊加：`SerializeDefine` 序列化的就是 process-wide 快取實例，一次失敗 → 該快取 FormSchema/TableSchema 永遠停在 `Serialize` 狀態 → 所有空集合 getter 從此回 `null` → 四個裸 `!` 解參考點 NRE（`PgCreateTableCommandBuilder.cs:111` 等，**無索引的表完全正常**）。**把 N-1 的「瞬時」轉成「永久」**。<br>**2026-09-04 已落地**：同一形狀有**兩份**（`XmlCodec` 與 `JsonCodec.SerializeCore`），故不是在兩處各加 try/finally，而是抽出 `SerializationLifecycle.BeginSerialize` 回傳 `readonly struct` scope —— 「`NotifyBefore` 必須配對 `NotifyAfter`」改為結構性保證，重複兩次的形狀不會再分岔 |
| **D-1** ✅ 已修（2026-09-04） | 兩個 UI head 的 `FormDataObject` 182 行逐字重複，doc 自承無 enforcement 卻仍無閘門 | `src/Bee.Web.Blazor.Server/DataObjects/FormDataObject.cs` vs `src/Bee.UI.Avalonia/DataObjects/FormDataObject*.cs` | Blazor 版 213 行非瑣碎碼中 182 行（85%）與 Avalonia 版逐字相同（5 組完整方法）。兩邊 doc 都寫「Deliberately parallel」+「Nothing enforces this at compile time」——**寫下來之後就停在那裡**。重複內容含錯誤訊息字面值、`InvariantCulture` 日期格式決策、`FieldDbType`→預設值對映。**論證另有事實漏洞**：兩邊都說「共同的家只能是 `Bee.UI.Core`」，但兩個 head 都已直接參考 `Bee.Api.Client`，那是一個不會把 Blazor 拉進 `Bee.UI.*` 家族的共同祖先。修法擇一：純資料/轉換的 5 個 helper 下沉到 `Bee.Api.Client`；或補 drift 測試把 WARNING 變成會紅的閘門。<br>**2026-09-04 已落地（下沉）**：`Bee.Api.Client.FormValueBinding`（`BuildEmptyDataSet` / `ToBindingString` / `ToColumnValue` / `GetEmptyValue`）+ `FormDataGuard`（`RequireConnector` / `RequireMasterRowId`）。刻意分兩個類而非一個共用袋 —— 前者是值轉換、後者是前置條件，合在一起就是 grab-bag。`GridControl` 原本借用 Avalonia 版 `internal static` 的兩個呼叫點一併改指共用實作。<br>**⚠️ 漂移已經發生，不是預測**：逐一比對 5 個 helper，3 個逐字相同、2 個已漂。`FormatForBinding` 只差註解措辭；**`ConvertToColumnValue` 是實質 bug** —— Avalonia 版已修好「NOT NULL 欄位遇到 `DefaultValue` 仍是 `DBNull` 的原始 ADO.NET column 時不可回 `DBNull`」（否則 `EndEdit` 擲 `NoNullAllowedException`），Blazor 版帶著那個 bug 繼續跑。而伺服器回應常態就是原始 ADO.NET column，**這不是邊角情境**。下沉順帶修掉它。<br>**兩份 doc 的論證同步改寫**：原文說「共同的家只能是 `Bee.UI.Core`」，但兩個 head 都已直接參考 `Bee.Api.Client` —— 那是不帶 `Bee.UI.*` 家族語意的共同祖先。舊的 WARNING（「必須手動同步、沒有機制強制」）已刪除，因為現在沒有兩份可同步。<br>**閘門**：兩個 head 各一道 `FormDataObjectSinkGateTests`，反射檢查 `FormDataObject` 不得重新宣告這 6 個成員名。**已在 doc 註明它擋不到改名的副本** —— 它擋的是實際發生過的那種失誤（在本 head 撞到 bug、不知道有共用實作、就地補一個私有方法）。負向驗證：把私有副本貼回 Blazor head 並真的接回呼叫點後閘門確實變紅（只貼不用會先被 IDE0051 擋掉，那不算驗到閘門）。另補 16 條共用實作的單元測試，含那個 `DBNull` bug 的回歸；把修正改回 Blazor 舊寫法後回歸測試確實變紅 |
| **T-2** ❌ 查證後不成立 | `Bee.Hosting.UnitTests` 完全沒有並行保護，5 個類別驅動同一個 process-wide static | `tests/Bee.Hosting.UnitTests/` | `CacheInfo.NotifyVersions`（`public static { get; set; }`）由 `CacheNotifyPollSession.Poll()` 寫入；`CacheNotifyPollerTests`／`CacheNotifyPollerUnitTests`／`CacheNotifyPollerExecuteAsyncTests`／`CacheNotifyPollSessionUnitTests`／`DbDefineCacheInvalidationTests` 五個類別皆觸及，該組件**既無 `[Collection]` 也無 `DisableTestParallelization`**。這是上輪 TEST-3 對 `Bee.Definition.UnitTests` 的判定一字不改地適用於鄰居 —— **修正沒有推廣到同形的地方**（與 P1-7 同一族）。<br>**2026-09-04 查證後判定不成立**：`CacheInfo.NotifyVersions` 背後是 `ConcurrentDictionary`（`CacheNotifyVersionStore.cs:15`），沒有 `KeyCollectionBase` 那種可被破壞的結構；且該組件**沒有任何測試寫那個屬性**（grep 零命中），5 個類別是經 production 的 `CacheNotifyPollSession.Poll` 間接寫入 store。加 `DisableTestParallelization` 會為不存在的問題付出整組序列化的代價。該組件真正的殘留是 **T-6**（`CrossNode_PostgreSQL` 的全表 poll cursor），與此無關 |
| **SEC-3** ✅ 已修（2026-09-04） | MySQL `EscapeSqlString` 只做引號加倍，反斜線逸出未處理 | `src/Bee.Db/Providers/MySql/MySqlSchemaSyntax.cs:81` | MySQL 預設把 `\` 當逸出字元（SQL Server / PostgreSQL / Oracle 的引號加倍正確，**MySQL 是唯一例外**）。輸入 `a\'` → `a\''` → `\'` 被吃成一個引號、隨後那個 `'` 提前關閉字串。三個 sink 全在 DDL 升級路徑：`MySqlDescriptionSyncCommandBuilder.cs:41`、`MySqlSchemaSyntax.cs:183`、`:108`。可達性：需已是「定義作者」（`SaveDefine` 為 `LocalOnly`），屬**定義作者→升級時任意 DDL** 的權限提升。**`fe0097de`（4.27.0）的四方言描述同步把 sink 從 2 個擴到 3 個** —— 上輪「這個形狀在 repo 裡還有幾份？」再次被驗證 |
| **PERF-2** ✅ 已修（2026-09-04） | `Request.EnableBuffering()` 讓每個 > 30 KB 的 body 落磁碟再讀回 | `src/Bee.Api.AspNetCore/Controllers/ApiServiceController.cs:88-92` | **重繞能力零使用**（主代理複驗：全 repo 只有這一處讀 body，controller 無 `[FromBody]`，進來時 body 本來就在位置 0）。子代理實測門檻與成本：<br>`29 KB → InMemory=True`，`40 KB → InMemory=False`（ASP.NET Core 預設 `bufferThreshold` 30 KB）<br>`64 KB` 0.45→0.14 ms（3.17×）、`256 KB` 0.94→0.36 ms、`1 MB` 2.74→1.11 ms、`4 MB` 10.41→4.56 ms<br>**16 KB 那列無差異，正是因果證明。** 信封裡 `params.value` 是 base64 的 gzip body（1.33× 膨脹），任何帶明細的 Save/GetData 都在門檻之上。修法：拿掉那兩行，改 `JsonSerializer.DeserializeAsync(Request.Body, ...)`。<br>**2026-09-04 已落地**，新增 `JsonCodec.DeserializeAsync<T>(Stream, CancellationToken)`（沿用與字串版**相同的 options 實例** —— 另建一份會讓 STJ 的型別 contract 快取每次 miss，即上輪 PERF-1 那一類）。<br>主代理重新實測（完整 HTTP 往返、20 次平均）：64 KB `0.46 → 0.22 ms`、1 MB `2.81 → 1.28 ms`、4 MB `9.51 → 4.76 ms`，而 **16 KB 零差異** —— 因果確定在 30 KB 門檻的磁碟 spill，不是「多一份字串」。<br>行為差異：只有空白字元的 body 由 `InvalidRequest` 改為 `ParseError`（HTTP 狀態不變，既有測試只釘狀態碼）；另補 `OperationCanceledException` 直接重拋，避免每個被放棄的連線在 log 留下 parse error |

---

## P3 — 文件漂移與低風險清理

### 公開文件把已移除／已更名的型別描述為現行機制

| # | 位置 | 內容 |
|---|------|------|
| **DOC-1** ✅ 已修（2026-09-04） | `src/Bee.Db/README.md:37,160,178,179,180` + `.zh-TW.md:37,158,176,177,178` | `949b4f4b` 移除的 5 個介面（`ISelectBuilder` / `IFromBuilder` / `IWhereBuilder` / `ISortBuilder` / `ILimitBuilder`）仍以現在式列在「Key Components」「Builder Pattern」與目錄結構圖。該 README 在移除**之後**還被編輯過一次（`b72318d8`）<br>**已落地**：型別清單改指具象 builder（介面移除、具象類別仍在） |
| **DOC-2** ✅ 已修（2026-09-04） | `docs/development-cookbook.md:572-575` + `.zh-TW.md:550-553` | 同一批介面出現在 `GetList` 執行流程圖 —— 外部開發者理解 `Bee.Db` 的主要入口<br>**已落地**：同 DOC-1，cookbook 雙語流程圖 |
| **DOC-3** ✅ 已修（2026-09-04） | `docs/development-constraints.md:285` + `.zh-TW.md:272`；`docs/development-cookbook.md:150` + `.zh-TW.md:145` | 四處同時寫錯兩件事且皆為現在式：「`ApiContractRegistry` **is still used** as a MessagePack **Typeless** serialization whitelist」。`ApiContractRegistry` 由 `2510e343` 移除、Typeless 由 ADR-037 移除。**加重情節**：HEAD 的 `93ef5713` 正是一輪文件盤點，修掉了 `src/Bee.Api.Core/README` 的同一型別卻沒掃到 `docs/` 這四處，而 `src/Bee.Api.Contracts/README.md:99` 早就寫著「它已被移除」—— 公開文件內部彼此矛盾<br>**已落地**：四處改為敘述現況並指向 ADR-007 / ADR-037 |
| **DOC-4** ✅ 已修（2026-09-04） | `src/Bee.Api.Core/README.md:111,116` + `.zh-TW.md:107,112` | `93ef5713` 的 commit message 宣稱「兩份 README 的資料夾清單現在與實際目錄逐項一致」，實測不成立：`Messages/` 仍列已移除的 `ApiErrorInfo`；`Json/` 列 `FilterNodeJsonConverter` 但它在 `src/Bee.Definition/Filters/`（跨組件歸錯位置），**而這一行是該 commit 本次新增的**<br>**已落地**：移除 `ApiErrorInfo`、`FilterNodeJsonConverter`（後者在 `Bee.Definition/Filters/`） |
| **DOC-5** ✅ 已修（2026-09-04） | `src/Bee.Base/README.md:62,107` + `.zh-TW.md:60,104` | `12e96696` 把 `Bee.Base.Tracing.TraceListener` 更名為 `TraceDispatcher`，同步改了 `docs/permission-authorization*`，**漏掉組件自己的 README**；更名後又被 `2510e343` 編輯過一次仍未修。（`SysInfo.TraceListener` 屬性與 `ITraceListener` 介面刻意保留，`SysInfo.cs:16` 的 doc 正確）<br>**已落地**：`TraceListener` → `TraceDispatcher`，目錄圖補 `ITraceListener` |
| **DOC-6** ✅ 已修（2026-09-04） | `src/Bee.Business/README.md:27,50` + `.zh-TW.md:27,50` | `2510e343` 從 `ISystemBusinessObject` 移除 `GetDefine`/`SaveDefine`，README 仍逐一列名且寫「7 個成員」（實際 5 個）。**名字錯了讀者看得出來，數字錯了看不出來 —— 本例兩者同時漂**<br>**已落地**：成員清單改為實際 5 個，並**移除**「7 個成員」的數字 |
| **DOC-7** ✅ 已修（2026-09-04） | `docs/caching.md:228-232, 368-377` + `.zh-TW` | 逐字複寫 `ICacheDataSourceProvider` 的介面宣告卻缺 `GetCompanyAuditRules`；「Cache Inventory › Database caches」6 個只列 5 個，缺 `CompanyAuditRulesCache`。最後編輯（`86cb67e3`）與 `CompanyAuditRules` 引入（`684dd139`）**同一天**。對照 `docs/development-constraints.md:63-68` 的同一份清單是對的 —— 兩份公開文件說法不一致<br>**已落地**：介面宣告的逐字複寫改為指路；兩份表格補 `CompanyAuditRulesCache` |
| **DOC-8** ✅ 已修 | `docs/api-method-reference.md:22` + `.zh-TW.md:20` | Protection 欄位說明寫「`Public` / `Encoded` / `Encrypted`」三級，**同一份文件的表格裡有 5 列在用 `LocalOnly`**。與 `.claude/rules/security.md` 2026-08-12 修掉的是同一筆漂移，只修了 agent 那份、沒修外部讀者那份。修法比照：改成指向 `ApiProtectionLevel` 的 XML doc，不複寫成員 |
| **DOC-9** ✅ 已修（2026-09-04） | `docs/api-bo-contract-design.md:58-71, 188-197` + `.zh-TW` | 整節教已被 ADR-036 移除的 MessagePack 標註（`[MessagePackObject(keyAsPropertyName: true)]` 範例 ×2、`[IgnoreMember]`），且**自我矛盾**（`:60` 說不要加 `[Key(int)]`，`:191` 的表格說「`[Key(n)]` Yes (from 100)」，兩半都已失效）。上輪 DOC-1 修掉了同型內容的 skill，**這份公開文件不在該次盤點範圍內**。**真正的成本**：`WireContract`/`WireContracts`/`MessagePackCodec` 全是 `internal`，`PublicAPI.Shipped.txt` 沒有任何註冊 formatter 的公開接縫 → 外部開發者**沒有辦法**依 ADR-037 註冊自己的訊息型別，選項實際只剩「桌面限定」或「改用 JSON codec」，而這一點目前沒有任何文件說明 |
| **DOC-10** ✅ 已修（2026-09-04） | `docs/jsonrpc-frontend-integration.md:19-25, 60` + `.zh-TW` | 仍寫「`params.format` — **always `0`** from JS」並把 payload 加密列為 .NET client 專屬 —— ADR-044 存在的唯一理由就是解除這個限制。同時 `wire-fixtures/README.md` 與 `wire-contracts/README.md`（真正寫給跨語言 client 作者的兩份）**沒有被 `docs/README.md` 或任何 `docs/*.md` 引用過**，新機制的說明書存在但從公開索引走不到<br>**已落地**：`params.format` 改為敘述 ADR-044 的協商；雙語補加密可用於 JS 的說明；`wire-contracts` / `wire-fixtures` 納入 `docs/README` 索引 |

### 清點數字、版號與 ADR 註記

| # | 項目 |
|---|------|
| **DOC-11** ✅ 已修（2026-09-04） | 「16 個 `src/` 專案」實際 17（`docs/dependency-map.md:5` + `.zh-TW`、`docs/README.md:40` + `.zh-TW`）。mermaid 圖只有 16 個節點（排除 `Bee.Analyzers` 合理），但句子宣稱的是「`src/` 專案數」，且全文沒有一句說明為何排除；ADR-038:25/:102 寫的「17 個」才是對的 —— 公開文件內部矛盾。連帶 `Target Framework Summary` 的「所有專案皆 `net10.0` 單一目標」對它不成立（`Bee.Analyzers` 是 `netstandard2.0`）<br>**已落地**：移除「16 個專案」數字，補「為何圖中不含 `Bee.Analyzers`」與其 `netstandard2.0` 例外 |
| **DOC-12** ✅ 已修（2026-09-04） | 「nine define types / 九種定義類型」實際 10（`docs/dependency-map.md:123` + `.zh-TW`、`tools/DefineEditor/README.md:3,11-25,41`）。`e95c0875` 新增 `MenuSettingsDocumentView`，DefineEditor README 的支援型別表**整列缺 MenuSettings**，且「8 個 multi-instance editor」實際 14 個 runner<br>**已落地**：DefineEditor README 補 **MenuSettings** 那列（共 10 種，與 `*DocumentView` 一致）；smoke 的「8 個」改指 `Smoke.Run` |
| **DOC-13** ✅ 已修（2026-09-04） | `src/Directory.Build.targets:6`「`Bee.Definition` has **six** direct dependents」實際 7。這是上輪 DOC-6 的**第二份拷貝**（上輪只修了 `docs/`），`src/Bee.Definition/README.md:16` 是第三份。依 `single-source.md` 應改為不帶數字的敘述，**不是把 six 改成 seven**<br>**已落地**：`Directory.Build.targets` 移除「six direct dependents」（**不是改成 seven**）。⚠️ 原記的「第三份拷貝在 `src/Bee.Definition/README.md:16`」查證後**不存在** |
| **DOC-14** ✅ 已修（2026-09-04） | BEE9001 涵蓋範圍仍寫「兩個組件」（`docs/analyzer-rules.md:60,67-68` + `.zh-TW.md:57,64`、`docs/adr/adr-038:80`），實際 `src/Directory.Build.targets:27` 已擴為三個（含 `Bee.Api.Contracts`）。**上輪 X-7 自己的修正造成的真回歸**；`docs/analyzer-rules.md` 之後又被改過兩次仍未被抓到。（`adr-038:152` 的驗證表是「當時量到什麼」，屬紀錄，**不必改**）<br>**已落地**：兩份 analyzer-rules 與 adr-038 的「兩個組件」改為不帶清單的敘述 + 指向 `Directory.Build.targets` |
| **DOC-15** ✅ 已修（2026-09-04） | 9 份 per-package README 的 Upstream/Downstream 區塊與實際不符（雙語共 18 檔）。**共同根因**：READMEs 建於 `5d139186`（11 個專案時期），`Bee.Hosting` 於 `0c081b99` 抽出成獨立 composition root 後，四份 README 的 Downstream 都少了它，四個月未被發現。**上輪的「28/28 吻合」只驗了 `docs/dependency-map*.md`，`src/*/README.md` 不在掃描單位內**。依 `single-source.md`，權威來源就是 csproj —— 要嘛改成指路，要嘛納入檢查；只把數字改對，下一輪還會漂<br>**已落地**：**26 份** README 的 Upstream/Downstream 散文清單全部改為指向 `dependency-map`，保留不可推導的註記（ADR-013、非 ASP.NET Core 宿主等）。實測漂移：10 個專案有缺漏，`Bee.Hosting` 在 4 份下游缺席 |
| **DOC-16** ✅ 已修（2026-09-04） | `docs/dependency-map` 外部套件表的 `Bee.Web.Blazor.Server` 那列不成立（雙語 `:98`）：寫「`Microsoft.AspNetCore.Components.Web` and related」，實際 csproj **零 `PackageReference`**，用的是 `<FrameworkReference>` —— 與同表 `Bee.Api.AspNetCore` 那列的正確寫法自相矛盾<br>**已落地**：Blazor 那列改為 `FrameworkReference: Microsoft.AspNetCore.App`（實測 csproj 零 `PackageReference`） |
| **DOC-17** ✅ 已修（2026-09-04） | `docs/changelogs/` 缺 **4.24.0 / 4.25.0** 兩版明細檔（雙語共 4 檔）。4.3.0–4.27.0 共 26 個版本，**唯獨這兩版**沒有，根 CHANGELOG 兩節也缺了其餘每版都有的「📄 Full notes and design context」連結。兩版皆含二進位破壞性變更（4.24.0 五個屬性上提到 `AnomalyEntry`；4.25.0 `ICacheContainer` 新成員）。三個面向獨立發現同一筆。修法擇一：補兩份明細檔（內容可直接取自根 CHANGELOG，本來就夠詳細）+ 補 `📄` 連結；或明示裁決「根檔已足夠時不另立」並寫進發版流程 —— **別留在「看起來像漏了」的狀態**<br>**已落地**：補 4.24.0 / 4.25.0 雙語明細檔 + 根 CHANGELOG 的 📄 連結。檔內**明講是事後補建、內容取自 CHANGELOG**，不假裝重建當時未記錄的敘事 |
| **DOC-18** ✅ 已修（2026-09-04） | 三份 ADR 引用一個從未發布的「v5.0」（`adr-003:5`、`adr-010:3`、`adr-011:35`）。框架自 4.x 走到 4.27.0，從未有 v5.0。`adr-011:35` 屬「當時的打算」（紀錄，可留）；`adr-003:5` 與 `adr-010:3` 是**對現況的斷言**（複寫，該修）—— 外部讀者拿 v5.0 去查 CHANGELOG 會一無所獲<br>**已落地**：`adr-003` / `adr-010` 移除 v5.0（對現況的斷言）；`adr-011` 兩處屬「當時的打算」保留 |
| **DOC-19** ✅ 已修（2026-09-04） | ADR-004（MessagePack payload）與 ADR-036 缺少指向 ADR-044 的 superseded-in-part 註記。ADR-004 對 ADR-030 / 036 / 037 都補了註記，**唯獨 ADR-044 這一輪沒補**，打破本 repo 已建立的 ADR 自我註記慣例<br>**已落地**：`adr-004` / `adr-036` 補 ADR-044 的 superseded-in-part 註記 |
| **M-1** ✅ 已修（2026-09-04） | `src/Bee.Api.Core/JsonRpc/JsonRpcErrorContract.cs:58` 的公開 XML doc 寫「The **six BCL rows** collapsing into `UserMessage`」—— 該處實際 6 列，但其中只有 5 列是 BCL 型別，第 6 列 `JsonRpcException` 是框架自有型別。**數字對則標籤錯，標籤對則數字錯**。這正是 `code-style.md`「不寫程式碼構件的清點數字」禁止的樣態，而該條就是上輪盤點之後才寫進規範的。`GenerateDocumentationFile=True`，這段隨 NuGet 進消費端 IntelliSense。引入 `999722d9`（**2026-09-02**）→ 真回歸<br>**已落地**：「The six BCL rows」改為**逐一列名**。原文數字對、標籤錯 —— 6 列中 `JsonRpcException` 是框架自有型別，所以任何帶數字的寫法都不成立
| **M-2** ✅ 已修（2026-09-04） | 中文 in-body 註解 18 → **23 行**（+5），而新增的 5 行**全部來自上輪自己的修正批次**：`src/Bee.Expressions/DynamicExpressoEvaluator.cs:68-70`（上輪 M-3 的 `#pragma` 說明）、`src/Bee.Definition/Forms/FormTable.cs:40-41`（上輪 CON-2 的 CS0027 說明）。說明上輪 M-2 當時只掃了 `#region` 那一個形狀。（中文 `#region` 已 31 → **0** ✅、中文 XML doc **0 行 / 27,554 行** ✅）<br>**已落地**：6 個檔 23 行中文 in-body 註解全部翻成英文（`code-style.md`：公開 repo 一律英文），依 S125 避雷寫法用完整句子與反引號。殘留歸零

### 序列化與註解漂移

| # | 項目 |
|---|------|
| **S-4** ✅ 已修（2026-09-04） | `JsonPayloadSerializer` 的 XML doc 宣稱「The shape matches what a Plain payload already puts on the wire」，實測**兩處差異只承認了一處**：(a) `object` 成員的封套 —— 而 `ApiInputConverter`（`src/Bee.Api.Core/Conversion/ApiInputConverter.cs:22-29`）的 options **沒有 `WireValueJsonConverter`**，照 `wire-fixtures/` 的 body 形狀送 Plain 請求，`FilterCondition.Value` 會變成裝著 `[12,"100"]` 的 `JsonElement` 直接流進 WHERE 建構，**沒有例外、沒有紀錄**；(b) `"parameters":[]` —— `JsonPayloadSerializer` 不派發 `SerializationLifecycle`，`IsSerializeEmpty` 短路不生效，而 `wire-fixtures/bodies/message-ping-request.json` 把它釘住，**等於把兩種方言的差異固化成合約**<br>**已落地**：**查證後兩處都不是程式缺陷，是文件陷阱**：(a) doc 其實**有**承認 `WireValueJsonConverter`（「The one addition is…」），報告說「只承認一處」不成立。實質風險在執行期 —— 實測確認 Plain 收到 wire 形狀的 `[12,"100"]` 會變成 `JsonElement` 一路流進 WHERE，**沒有例外**。<br>**但不能把 converter 加進 Plain 讀取路徑**：`WireValueJsonConverter.Read` 對非 `StartArray` 直接擲例外，加上去會打斷所有送裸值的既有 client。<br>修法因此是把陷阱寫明：`JsonPayloadSerializer` 的 doc 明列兩處差異與「加不進去」的原因，`wire-fixtures/README` 開頭加警告「這些是 Encoded/Encrypted body，不是 Plain request body」並指路前端整合指引
| **S-5** ✅ 已修（2026-09-04） | 漂移閘門把 `[JsonIgnore(Condition = Never)]` 判成「被忽略」—— 語意剛好相反（`tests/Bee.Api.Core.UnitTests/WireContractDriftTests.cs:160-166`）。`FormField.cs:90` / `DbField.cs:75` 已在用這個寫法，目前不在 wire 閉包內故為潛伏。`de682143`（2026-07-30）剔除 BEE4007 時已記錄「規則只看 attribute 存在、未讀 `Condition`，是實作 bug」—— **同一個 bug 現在活在測試的 `WireMemberNames` 裡**<br>**已落地**：`WireMemberNames` 改讀 `JsonIgnoreAttribute.Condition`，`Never` 視為 wire 成員。補 `WireMemberNamesSemanticsTests`，負向驗證通過
| **S-6** ✅ 已修（2026-09-04） | `src/Bee.Definition/Bee.Definition.csproj:31` 的註解與相鄰三行的實際設定**完全相反**：寫「deliberately NOT analyzed by it (no `OutputItemType="Analyzer"`)」，而 `:36` 就寫著 `OutputItemType="Analyzer"`。`de682143` 的 commit message 明載當時是刻意改成套用，註解沒跟<br>**已落地**：csproj 註解改為與實際設定一致（`Bee.Definition` **確實**被自己的規則檢查，那是刻意的）
| **S-7** ✅ 已修（2026-09-04） | `MessagePackPayloadSerializer.SerializationMethod` 用字面值 `"messagepack"` 而非 `PayloadCodecNames.MessagePack`（`:13`）。`ResolvePayloadSerializer` 拿 codec 名去 ordinal 比對它，所以它是 wire 契約的一部分卻有兩份權威（`JsonPayloadSerializer` 用的是常數）<br>**已落地**：`MessagePackPayloadSerializer.SerializationMethod` 改用 `PayloadCodecNames.MessagePack`
| **S-8** ✅ 已修（2026-09-04） | 三參數 `Encode`/`Decode` 的觸發條件與 doc 不符（`IApiPayloadTransformer.cs:31-38`）：doc 說「codec **other than the deployment default**」，實際是 `!string.IsNullOrEmpty(payload.Codec)`。client 顯式設 `PayloadCodec = "messagepack"`（合法且最自然的寫法）會讓自訂 transformer 吃到 `NotSupportedException`，儘管什麼都沒改變<br>**已落地**：改**行為**而非改 doc：新增 `UsesDefaultCodec`，只有「會改變 body 的 codec」才走三參數多載。顯式指名部署預設是合法且最自然的寫法，不該讓自訂 transformer 吃到 `NotSupportedException`。負向驗證通過
| **S-9** ✅ 已修（2026-09-04） | `AcceptedPayloadCodecs`（doc：「always all of them」）與 `CurrentSettingsSummary` 不含自訂 `PayloadSerializer.SerializationMethod`，而 `ResolvePayloadSerializer` 接受它 —— 裝了自訂 serializer 的部署，公開的「可用 codec」清單是錯的<br>**已落地**：`AcceptedPayloadCodecs` 補上自訂 `PayloadSerializer.SerializationMethod`，與 `ResolvePayloadSerializer` 實際接受的一致
| **S-10** ✅ 已修（2026-09-04） | JSON 逃生門少一層縱深：MessagePack 端在名稱白名單後**還**呼叫 `ThrowIfDeserializingTypeIsDisallowed`（gadget blocklist + 形狀複查），JSON 端只做名稱白名單就 `Type.GetType` + `Deserialize`（`WireValueJsonConverter.cs:258-276`）。主閘門兩端一致、STJ 的 gadget 面遠小於 MessagePack，故列 P3；但部署把 `AllowedTypeNamespaces` 開寬時兩條 wire 縱深不同<br>**已落地**：JSON 逃生門在 `Type.GetType` 之後補 `WireTypeWhitelist.IsRuntimeTypeAllowed`（形狀複查），與 MessagePack 端的 `ThrowIfDeserializingTypeIsDisallowed` 對齊

### 安全 hardening 小項

| # | 項目 |
|---|------|
| **SEC-4** ✅ 已修（2026-09-04） | 定義檔反序列化失敗把伺服器絕對路徑回傳給遠端呼叫者（`XmlCodec.cs:106`、`JsonCodec.cs:163`）。`InvalidOperationException` 在 `JsonRpcErrorContract:84` 映射為 `UserMessage`，`MapException` 對契約覆蓋型別原樣回傳 `ex.Message` → 遠端已認證的 `System.GetDefine` 遇到損毀定義檔即收到 `/…/Define/FormSchema/xxx.xml`。違反 `scanning.md`「例外訊息禁止包含內部路徑」。修法：只回檔名，完整路徑寫 log<br>**已落地**：訊息只留檔名，完整路徑改放 `Exception.Data`（新增 `SerializationErrorData.FilePath`）供伺服端記錄。`XmlCodec` / `JsonCodec` 兩處
| **SEC-5** ✅ 已修（2026-09-04） | 稽核記錄的租戶過濾是 fail-open 形狀（`src/Bee.Repository/AuditLog/AuditLogRepository.cs:71,100,113,127,157,185,279`）。`company_id` 這個**租戶邊界**與 `prog_id`/`user_id` 這些**選用篩選**共用同一個「null 就略過」helper；`LogBusinessObject.CurrentCompanyId()` 未進入公司時回 null → 條件消失 → 回傳全部公司的稽核記錄。**目前不可達**（`CompanyAuthorizationService.Can` 在 `CompanyId` 為空時回 `false`），但跨租戶隔離因此依賴另一個**可被宿主替換的**型別的實作細節。修法：`ChangeLogQuery.CompanyId` 為空時直接擲例外<br>**已落地**：七個入口（`GetChangeLog` / `GetLoginLog` / `GetAccessLog` / `GetApiAnomalyLog` / `GetChangeById` / `GetApiAnomalySummary` / `GetTopApiMethods`）全部改為 fail-closed，缺租戶範圍即擲例外。<br>**先查過有無合法的跨租戶讀取**：`LogBusinessObject.CurrentCompanyId()` 就是 session 的 CompanyId，deployment admin 沒有旁路 —— 沒有。<br>**⚠️ 我又漏做 CON-6 的教訓**：改完才發現 `AuditLogQueryDbFactTests` 有 6 筆在守相反行為（「無 company filter → 兩列都回」）。判讀後確認那是**描述缺陷、不是刻意決定**（沒有任何註解宣告允許跨租戶讀取），於是測試改為斷言新規格，並**多驗一條租戶隔離**（c2 看不到 c1 的列）
| **SEC-6** ✅ 已修（2026-09-04） | 未標 `PermissionModelId` 的表單完全沒有授權，且**無任何偵測機制**（`FormBusinessObject.Permission.cs:32,48,100,136` 四處「為空 → 直接 return」）。這是刻意的 opt-in 授權（XML doc 註明 gradual adoption），但 `src/Bee.Analyzers/` 沒有對應規則（BEE3001/3003 只管 `[ApiAccessControl]`），一張漏標的表單會靜默對所有已認證使用者全開，build 綠、測試綠。修法：比照 BEE3001 加一條定義層 analyzer（`Bee.Analyzers/Definitions/` 已有讀 FormSchema 的基礎設施）<br>**結論**：新增 **BEE1008**（`PermissionModelAnalyzer`）：FormSchema 未宣告 `PermissionModelId` 時報告「這張表單對所有已認證呼叫者全開」。<br>**刻意只報告、不強制**：「未標記的表單保持開放」是 `FormBusinessObject.Authorize` 的 XML doc 明載的漸進採用決定，改執行期會讓每個採用到一半的部署當場壞掉。缺的從來不是規則，是這件事沒有任何地方說得出來。<br>**嚴重度經使用者裁決維持 `Info`**。誠實的代價已寫進 `docs/analyzer-rules` 雙語：`Info` 對應 MSBuild message、預設 verbosity 不印，而 definition analyzer 都是 CompilationEnd、IDE 也不即時顯示 —— **機制存在但預設看不到**，導入完成後要在 `.editorconfig` 提升為 warning 才有實際把關。<br>**⚠️ 過程中查出我先前修 S-6 時寫錯一句**：`OutputItemType="Analyzer"` 只讓本專案的 **C#** 受 BEE3xxx/BEE4xxx 檢查，**不代表** `Defaults/` 受 BEE1xxx/BEE2xxx 檢查 —— 那些走 AdditionalFiles，而 glob 是消費者的 `Define/**`，本專案也不 import 自己的 buildTransitive targets。`Defaults/` 由 `DefaultsTests` 驗。已更正 csproj 註解
| **CON-5** ✅ 已修（2026-09-04） | `MemoryReplayWindowStore` 的 sweep 有 TOCTOU（`:28-35, 68-73`）：`GetOrAdd` 建立的 `Entry` 其 `LastTouchedMs` 預設 **0**，在 `Volatile.Write` 之前若另一執行緒進 `SweepIfDue`，會把它當過期移除 → 該 session 下一個請求拿到全新 window（`_highest = -1`）→ **重放防護在該 session 上重置一次**。機率極低但這是安全控制，修法一行（建構時初始化）。引入 `7a4cb0e9`（4.27.0）→ 本輪唯一的新並行缺陷<br>**已落地**：建構子蓋時間戳。**閘門用結構性斷言而非行為測試**：這個 race 沒辦法用行為乾淨隔離 —— 要撞到它就得把淘汰期調到毫秒級，但那樣執行緒被排程延遲超過淘汰期就是**合法**淘汰，修正在位時也會紅（實測三次紅一次）。第一版並行測試就是這樣，把 bug 放回去仍全綠
| **CON-6** ❌ 撤回（2026-09-04） | `ExpiredSessionCleanupService.SafeCleanup` 只 catch `DbException`，姊妹的 `CacheNotifyPoller.SafePoll` 同時 catch `InvalidOperationException` —— 而 `_repositoryFactory.Create<ISessionRepository>()` 正是會擲後者的呼叫。逸出即 StopHost（同 CON-1）<br>**結論**：**❌ 判定撤回，不修**（2026-09-04）。加上 `catch (InvalidOperationException)` 後整個 `Bee.Hosting.UnitTests` 掛住，二分後定位到既有測試 `NonDbException_IsNotSwallowed`（「非 DbException 的例外不應被吞掉——那不是這個 catch 要處理的失敗」）。這是**刻意的決定**，姊妹方法的寫法不同不代表這邊錯。方法論教訓已記入本文件下方

### 其他

| # | 項目 |
|---|------|
| **API-3** ✅ 已修（2026-09-04） | `AnalyzerReleases.Shipped.md` **自建立以來從未填過一行**，19 條診斷全部停在 Unshipped，而 analyzer 隨 `Bee.Definition` 已出過 15 個版本。`Bee.Analyzers.csproj:44-48` 的註解自己寫明這是「analyzer equivalent of the PublicAPI baselines」。後果同 `PublicAPI.Shipped.txt` 永遠是空的：擋得到「新規則未申報」，**擋不到「已出貨規則被移除或改嚴重度」**——BEE4004 的退役正是這個盲點的實例（從 Unshipped 消失，Shipped 沒有 "Removed Rules" 條目，沒有任何東西出聲）。根因是 `/dev-workflow:release` 的步驟 ③ 只寫 `PublicAPI.*`，全文零次 `AnalyzerReleases`<br>**已落地**：`AnalyzerReleases.Shipped.md` 依 **tag 快照回填**（不是憑記憶）：4.16.0 新增 22 條、4.18.0 新增 BEE3003、**4.19.0 移除 BEE4001–BEE4004**。Unshipped 只留尚未出貨的 BEE1008。<br>**負向驗證證明盲點確實關上**：把一條已出貨規則（BEE1007）改掉 id 後，`RS2003`「shipped in 4.16.0 but is no longer a supported diagnostic」現在會擋 —— Shipped 是空的時候這條**永遠不可能觸發**。<br>發版流程的第三份基準寫進 `docs/repo-ops/public-api-baseline.md` 與 analyzer csproj 註解。**`/dev-workflow:release` skill 在另一個 repo（`jeff377-plugins`），未代改**，需另行更新其步驟 ③。<br>查證更正：BEE4007 從未出現在任何 tag 的 Unshipped，是版本之間短暫存在，不需 Removed 條目
| **API-4** | 236 處 `<c>Xxx</c>` 藏著本 solution 內、cref 解析得到的型別／成員（112 處同組件、124 處向下相依組件；101 個相異識別字）。`code-style.md` 明訂這種情況一律用 `<see cref>` 交給編譯器把關（CS1574），而 `check-xmldoc-refs.sh` **只驗「這個名字還存在嗎」，不驗「這裡本來就該用 cref」** —— 腳本檔頭自己寫的優先序沒有任何機制在執行。基準 `227daa70` 量得 245、HEAD 259，**沒在收斂**。建議先改同組件那 112 處（機械性、零風險），並把反向檢查加進腳本 |
| **API-5** | 契約介面 5 個集合成員中 3 個曝露具象 `List`/`Dictionary`（`ISaveResponse.AffectedRows`、`ICheckPackageUpdateRequest.Queries`/`Response.Updates`、`IEnterCompanyResponse.Capabilities`），而兩個新的（`Fields`、`ApiKeys`）已用 `IReadOnlyList` —— 慣例已成形，舊的沒回頭收。**注意真實張力**：wire DTO 與 BO 型別**必須**是可寫具象集合（序列化繫結需要），**只有契約介面那一層可以收斂**（介面宣告 `IReadOnlyList`，實作照樣持有 `List`）。`Dictionary` → `IReadOnlyDictionary` 是二進位＋原始碼雙重破壞，**趁 pre-stable 視窗**，錯過要再開一次 |
| **D-2** ✅ 已修（2026-09-04） | `SplitFullKey` 跨組件逐字重複（226 字元）：`src/Bee.Definition/Language/LanguageService.cs:194` vs `src/Bee.Api.Client/Definitions/SnapshotLanguageService.cs:113`。這是 `ILanguageService` 的 key 拆解慣例，慣例若改 server 與 client 會靜默不同步。`Bee.Api.Client` 已相依 `Bee.Definition`，可下沉<br>**已落地**：`SplitFullKey` 下沉為 `Bee.Definition.Language.LanguageKey.Split`（公開，因為 key 格式**就是** `ILanguageService` 的契約，不是實作細節）。回傳 tuple 取代 out 參數；server 與 client 共 8 個呼叫點
| **D-3** ✅ 已修（2026-09-04） | `ReadStringArray` 同組件同資料夾內重複（259 字元）：`DataSetJsonConverter.cs:164` vs `DataTableJsonConverter.Read.cs:113`。合併成本近乎零<br>**已落地**：`ReadStringArray` 下沉為 `Bee.Base.Serialization.JsonReaderExtensions`（internal，同組件）
| **D-4** ✅ 已修（2026-09-04） | Provider dialect 逐字重複 7 組（S4144）。**這一項要小心裁決**：`rules/database.md` 明示各 provider 應保有獨立演化空間，「刻意不共用」在這裡是合理設計。但其中 `StripStringLiteral`、`BuildIndexFieldList`、`GetIndexesCommandText` 是**方言中立的純解析／組字串**，建議只抽這幾個，**發 SQL 的部分（`GetStatements`/`GetTableSchema`）維持獨立**。<br>**正面對照**：本視窗新增的 `*DescriptionSyncCommandBuilder` 四件套**做對了** —— 共用部分抽成 `DescriptionSyncChanges.Collect(...)`，各 provider 只剩 20–50 行方言輸出。新程式碼的模式是對的，舊的沒回頭補<br>**已落地**：**只抽方言中立的部分，發 SQL 的維持獨立**（`rules/database.md`）。<br>`StripStringLiteral` → `SqlLiteralParser`（Pg / Oracle 兩份逐字相同，屬「讀回值並解除引號」而非產生 SQL）。<br>`GetIndexesCommandText` → `IndexStatementJoiner.Join`，四個 provider 共用**迴圈與 PK 過濾**，各自的 `GetIndexCommandText` 原封不動。負向驗證：拿掉 PK 過濾後全套 12 筆紅。<br>**⚠️ 報告的「逐字重複 7 組」高估了**：`BuildIndexFieldList` 實測是**六種相異實作**散在 8 檔，只有 Sqlite / MySql 各自的 Alter+Rebuild 成對相同（同 provider 內），不是跨 provider 重複 —— 未抽。<br>順帶發現 SQL Server 那份叫 `GetIndexsCommandText`（拼字錯），已一併正名
| **M-3** ✅ 已修（2026-09-04） | 14 個**可變** `private static` 欄位仍用 `_` 前綴，與實例欄位無法區分（`ApiServiceOptions.cs:14-20` 7 個、`ClientInfo.cs:18-23` 6 個、`SysInfo.cs:49` 1 個）。`.editorconfig:70-75` 的 `s_` 規則 `required_modifiers = static, readonly` **只管不可變靜態**，可變靜態落到 `private_fields_underscore` —— 而**可變的 process-wide 狀態恰恰是最需要在讀者眼前標記出來的那一類**（這三個檔全是 `static class`，正是 CON-3 那類問題的溫床）。上輪 M-3 的結案敘述沒提到這個缺口。修法：`required_modifiers` 放寬為 `static`，14 處改名皆為私有、零公開 API 影響<br>**已落地**：`.editorconfig` 的 `required_modifiers` 由 `static, readonly` 放寬為 `static`，**19 處**改名（比原記的 14 多 5 個測試側的 `SharedDatabaseState` / `TestProcessBootstrap`）。<br>⚠️ **踩到一個順帶的雷**：`src/Bee.Analyzers/.editorconfig` 以**規則名稱**覆寫嚴重度，根檔一改名那道豁免就靜默失效，整個組件炸出 38 個 IDE1006。已在該檔加註警告。<br>另 `ClientInfo` 的欄位改名打斷了 `Bee.UI.Core.UnitTests` 的反射重設（4 個檔）—— 同 T-6 的形狀：反射把改名變成執行期失敗而非編譯錯誤
| **T-3** ✅ 已修（2026-09-04） | `MemoryReplayWindowStore`（出貨預設的防重放儲存）**零測試** —— 全 `tests/` 對它與 `IReplayWindowStore` 的參照數 0。它甚至公開 `public int Count`，doc 寫「intended for tests and diagnostics」，**而沒有任何測試用它**。這是上輪 SEC-2（`LoginAttemptTracker` 無淘汰）的同形重演：每個預設部署都在用的 per-session map，其淘汰政策無人驗證（兄弟型別 `ReplayWindow` 有 10 筆測試）<br>**已落地**：補 `MemoryReplayWindowStoreTests` 4 筆（同 token 回同一 window、不同 token 隔離、建構時蓋時間戳的回歸、閒置淘汰）
| **T-4** ✅ 已修（2026-09-04） | DB 相依快取的直接覆蓋普遍缺席：`CompanyAuditRulesCache`（新）、`CompanyInfoCache`、`DepartmentTreeCache`、`ApiKeyGateCache`、`CompanyRolePermissionsCache` 在 `tests/` 的參照數皆為 **0**。依 `rules/definition.md`，這是「沒有 `SaveX`、只靠 cache-notify 失效」的高風險類別（漏 notify 就全 process 拿舊值）。服務層有測試，快取層本身沒有<br>**已落地**：補 `DatabaseBackedCacheTests` 7 筆，涵蓋 5 個快取的 read-through 只打一次、`Remove` 後重載、無 data source 不讀，以及 `ApiKeyGateCache` 刻意與 `ApiKeyInfo` 共用 cache group。為此在 `Bee.ObjectCaching` 補 `InternalsVisibleTo`（其餘六個專案早有，本專案漏了）。負向驗證：讓 `Get` 永遠 miss 後 7 筆中 5 筆變紅
| **T-5** ✅ 已修（2026-09-04） | `tests/CLAUDE.md:152-155` 的並行保護清單是錯的：寫「`Bee.Api.Client` / `Bee.Api.Core` / **`Bee.Definition`** / `Bee.ObjectCaching` / `Bee.UI.Avalonia`」五個組件用 `DisableTestParallelization`，實際只有 **4 個**（`Bee.Definition.UnitTests` 沒有該屬性，走的是只序列化 3 個類別的 `ProcessWideStateCollection`）。危害在下半句：「那比逐類別掛 `[Collection]` 可靠」—— 文件宣稱 `Bee.Definition` 有**較強**的保護，實際只有它自己承認會漏的那道。<br>**先查有沒有實質漏網**：`ProcessWideStateCollection` 只涵蓋 3 個類別，但實測全組件掃過 `BEE_MASTER_KEY` / `GlobalEvents` / `BuildServiceProvider` 的檔案後，另兩個命中（`DefaultsTests`、`MasterKeySourceTests`）都只是把 `"BEE_MASTER_KEY"` 當**字串值**用、沒碰環境變數 —— **沒有漏網**，T-5 是純文件錯誤。<br>順帶查證「每個名稱都有對應的 `CollectionDefinition`，零孤兒」：成立。grep 命中的兩處 `[Collection("Initialize")]` 都在註解裡描述舊狀態，不是真的 attribute。<br>**2026-09-04 已落地，修的是宣稱而不是文字**：文件自己就說 `DisableTestParallelization` 「比逐類別掛 `[Collection]` 可靠」，那就讓它成真 —— 給 `Bee.Definition.UnitTests` 補上該屬性。成本實測 352–634 ms → 723–823 ms（1,086 筆），在動輒數分鐘的全套裡可忽略。`ProcessWideStateCollection` 因此變成冗餘，保留作為紀錄並註明「不要因為它冗餘就換回它」。<br>**並消掉會再漂的那份清單**：五個組件名改成一道 `grep` 指令（`single-source.md`：有權威來源就只指路）。原本那份清單漂掉時**沒有任何機制會發現** —— 這次是靠人工盤點才抓到 |
| **T-6** ⚠️ 部分完成（2026-09-04） | 未結案 flaky `DbDefineCacheInvalidationTests.CrossNode_PostgreSQL` 期間**零 commit 觸及**。結構性曝險三項：poll cursor 是**全表**的（`CacheNotifyReader.cs:53` `SELECT MAX(sys_update_time) FROM st_cache_notify`）而該表被每個平行測試行程共寫；PG 的時間來源是 `LOCALTIMESTAMP`（**交易起始時間**）而欄位 DEFAULT 是 tz-aware，這條不對稱只在 PG/Oracle 存在；加上 T-2 的無序列化。建議修法：把 poll baseline/delta 限縮到測試自己的 key 前綴（測試已用 `progId = "E2E_" + Guid`，只差 reader 沒有 key filter 多載）。<br>另一筆 `ApiAspNetCoreTests.ExecFunc_Hello_ReturnsNotNull` 的**根因已由 `99636efd` 處理**（`CrossProcessLock` + seed 單一交易 + setup 失敗不再靜默），建議冷啟連跑數次後正式結案。<br>**⛔ 原本的 flake 沒有重現，不宣稱修好**：`CrossNode_PostgreSQL` 單跑 20/20 綠，今日四種 DB 完整模式 CI 亦綠，全套連跑 12 輪 0 失敗。plan 點名的「poll cursor 是全表的」結構性曝險仍在，但**修它要為了測試在 `ICacheNotifyReader` 上新增 production API**（key filter 多載），在重現不出來的前提下不划算。**維持觀察，再犯再說。**<br>**✅ 但在同一個元件裡查到一個真的 bug（原 plan 未指出）**：`CacheNotifyReader` 的空表 baseline 自帶一份方言對照表，回的是**本地時間**（`getdate()` / `LOCALTIMESTAMP` / `CURRENT_TIMESTAMP(6)`），而每一列的 `sys_update_time` 都由寫入端以 **UTC** 戳（欄位 DEFAULT 與 `CacheNotifyService.BuildUpsertSpec` 都讀 `IDialectFactory.GetDefaultValueExpression`）。**五個方言中四個不對稱**，只有 SQLite 剛好一致。<br>危害：全新部署（`st_cache_notify` 空表）的第一個 poll 游標落在未來，之後每次 poll 的視窗都撈不到列 —— **快取失效機制靜默停擺**，直到牆鐘追上。UTC+8 就是八小時。違反 ADR-032 D1「資料庫一律存 UTC、時區轉換在用戶端」。<br>**活證據**（負向驗證時實測，非推論）：Oracle 回 `15:18:34` 而 UTC 是 `07:18:34`，正好差 8 小時。且 Oracle 的 `LOCALTIMESTAMP` 取的是**用戶端 session 時區**而非資料庫時區 ——基準取決於哪台機器在跑 poller，比伺服器時區更糟。<br>**為什麼一直沒被發現**：本機四個容器與 GitHub runner 全跑 UTC，兩式在那裡剛好相等。Oracle 是唯一會現形的，因為它看的是用戶端時區。<br>修法：讀取端改從寫入端同一個來源取值，刪掉那份重複的方言表（`single-source`）。<br>閘門兩層：表達式必須與寫入端完全相同（純邏輯，UTC 環境下唯一驗得到的方式）＋四方言實跑驗回傳值貼近 UTC。負向驗證：改回舊的本地時間表達式後兩層都紅 |
| **T-7** ✅ 已修（2026-09-04） | `tests/Bee.Api.Core.UnitTests/WireFrameReplayTests.cs` 用 `[Fact]` 卻需要 DB（無容器時 2 筆紅：`Connection string for database 'common' is null`）。違反 `rules/testing.md` 第 2 條，應改 `[DbFact]`。<br>**2026-09-04 已落地，但沒照處方做**：那 2 筆驗的是重放序號判斷（`ApiServiceOptions.ReplayWindowStore`，純記憶體），DB 是**意外相依** —— 它們拿 `Guid.NewGuid()` 當 token，於是每次呼叫都落到 session rebuild 路徑讀 `st_session`。改用 `TestSessionFactory.CreateAccessToken(_fx)` 植入 session 快取後兩筆完全不碰 DB，整個類別因此也從 `SharedDbFixture` 降為 `BeeTestFixture`，class doc 一併改寫（原文詳細論證了「必須用 SharedDbFixture」，那個前提已經不存在）。`rules/testing.md` 第 2 條本來就說「純邏輯測試不適用 —— 那種有 bug 該直接修，不該跳過」，**`[DbFact]` 會把這條紅燈藏起來而不是修掉它**。<br>兩種環境各驗一次：有 `.runsettings` 與沒有，都是 10/10 綠 |
| **ARCH-2** ✅ 已修（2026-09-04） | `Bee.Api.AspNetCore` 直接取用 `Bee.ObjectCaching` 的 `ICacheContainer`（`BeeFrameworkApplicationBuilderExtensions.cs:2,52`）。`ICacheContainer` 宣告在**快取實作組件**，不是 `Bee.Definition`，因此 API 層對快取實作產生型別相依（且 csproj 未宣告，經 `Bee.Hosting` 傳遞）。`docs/development-constraints.md:134` 的禁止表只列「API 層直接參考 Repository 層」，這條落在表外但形狀相同。修法：把「API key gate 是否生效」抽成 `Bee.Definition` 的唯讀查詢介面。<br>**2026-09-04 已落地**：`IApiKeyGateStateProvider`（定義層）+ `ApiKeyGateStateProvider`（ObjectCaching，一行轉發）+ 組裝層註冊；API 層對 `Bee.ObjectCaching` 的參考歸零。<br>**並加進 ARCH-1 的閘門，但第一版無效**：用 deps.json 比對直接邊時，製造真實違規後仍是綠的 —— SDK 會把傳遞專案參考併進 `@(ProjectReference)`，程式碼可以 `using` 一個只是傳遞進來的組件而 deps.json 上沒有那條邊（正是 DEP-1 記載的性質）。改用 `Assembly.GetReferencedAssemblies()`（編譯後 IL 真正參考誰）後負向驗證才正確變紅。**deps.json 抓「宣告了沒用」，IL 參考抓「用了沒宣告」**。<br>**⚠️ 2026-09-04 後續**：ARCH-1 的另一道閘門 `ApiContracts_ContainNoImplementation` 在完整模式 CI 上紅了 —— 覆蓋率插樁會**往組件裡注入型別**（`Coverlet.Core.Instrumentation.Tracker.*`，帶 `RecordHit` 等方法），被判成「合約軸混進了實作」。**這是閘門自己的缺陷，不是真違規**，而它在本機與精簡模式 CI 上綠了好幾週 —— 覆蓋率只在完整模式收。修法是以命名空間限縮到原始碼宣告的型別，並把防空轉斷言移到過濾之後。方法論已寫進 `tests/CLAUDE.md` §「本機綠、CI 紅」第 4 條 |
| **ARCH-3** ✅ 已修（2026-09-04） | `HttpUtilities`（`src/Bee.Base/HttpUtilities.cs`，159 行、**5** 筆公開 API——報告寫的 10 筆是錯的，實際是型別 1 + 方法 4）的 3 個呼叫點**全在 `Bee.Api.Client`**。`Bee.Base` 是每個消費者都繼承的組件，把「對外發 HTTP 請求」放在最底層等於讓每個消費者的公開表面都帶一個用不到的網路原語。**不違反 ADR-038**（`HttpClient` 是 BCL），但 `rules/dependency-boundary.md` 說「`Bee.Base` 也不是抽屜：只搬『零外部相依、多層共用』的抽象進去」—— 這個型別**不是多層共用**。搬家是破壞性變更（5 筆 Shipped），成本明顯低於 A-5。<br>**2026-09-04 已落地**：型別與其測試搬到 `Bee.Api.Client`（命名空間隨之改），`Bee.Base` 的 5 筆 Shipped 轉為 `*REMOVED*`、`Bee.Api.Client` 補上對應 5 筆，雙語 README 的元件清單同步。**破壞性判定**：型別搬家對外部消費者是 source + binary 破壞，但唯一的呼叫方式是 `HttpUtilities.PostAsync(...)` 等靜態呼叫，改一行 `using` 即可；pre-stable 政策下以 minor 發布。<br>**並補上閘門**（`tests/Bee.Base.UnitTests/BaseLayerCapabilityGateTests.cs`）：`Bee.Base` 的 IL 參考不得含 `System.Net.Http`。刻意**只**擋這一個組件而非整個 `System.Net` —— `IPValidator` 的 `IPAddress` 是 CIDR 值比對、不會連到任何地方，把兩者混為一談的閘門會逼人為了通過而搬錯東西。負向驗證：在 `Bee.Base` 放一行 `new HttpClient()` 後閘門確實變紅 |

| **T-8**（T-7 的實際母體） | **T-7 記的是 2 筆，實測是 73 筆** —— 修掉 WireFrameReplay 那 2 筆後仍有 **71 筆**用 `[Fact]` 卻需要資料庫，跨 5 個組件：`Bee.Business.UnitTests` 53、`Bee.Api.Client.UnitTests` 8、`Bee.Api.Core.UnitTests` 6、`Bee.Api.AspNetCore.UnitTests` 2、`Bee.ObjectCaching.UnitTests` 2。全部同一個根因，錯誤訊息一字不差：`Connection string for database 'common' is null or empty`，堆疊都經 `DbConnectionManagerService.CreateConnectionInfo`。<br>**掃描手法**：`dotnet test Bee.Library.slnx -c Release` **不帶** `--settings .runsettings`（env var 全空 → `[DbFact]` 自動跳過 → 剩下會紅的就是違規者）。這是窮盡執行，不是 grep 推理。<br>**⚠️ 但 T-7 的處方本身有問題，別照抄**：`[DbFact]` 只檢查 `BEE_TEST_CONNSTR_*` **有沒有設值**（見 `DbFactAttribute` 建構子），**不檢查容器連不連得上**。而 `.runsettings` 已入版控、裡面是寫死的連線字串，所以「跑 `./test.sh` 但容器沒開」這個情境下 env var 是**有值**的 —— `[DbFact]` 在那裡提供**零**保護，一樣會紅。兩個情境必須分開講：<br>　· 情境 A（沒帶 `.runsettings`）：`[DbFact]` 有效 —— 這就是上面 71 筆的來源<br>　· 情境 B（帶了 `.runsettings` 但容器沒開）：`[DbFact]` 無效，`[DbFact]` 與 `[Fact]` 一樣紅<br>**母體是混合的，不能機械換 attribute**：抽樣判讀顯示兩類都有。`WireFrameReplayTests` 是意外相依（已修）；`CacheContainerTests.SessionInfo_SetGetRemove_BehavesCorrectly` 看似純快取測試，實際是 `SessionInfo.Get` 在 cache miss 時走 read-through 讀 `st_session`，**真的**需要 DB。`SystemBusinessObjectLoginTests` 等則是貨真價實的整合測試。逐筆要問的是「這個測試的**主題**需要 DB 嗎」——不需要就照 T-7 的做法拆掉相依，需要才標 `[DbFact]`。<br>**優先序判斷**：CI 一律有 SQL Server + SQLite，情境 A 在 CI 不會發生；本機 `./test.sh` 會自動起容器。受影響的只有「在 IDE 直接跑 `dotnet test` 而沒帶 runsettings」的開發者。**2026-09-04 已完成分類**：A 類意外相依 34 方法／38 case、B 類真需要 DB 33 方法／33 case，逐類別的主題、DB 觸發點與判準見 [plan-db-dependent-tests.md](plan-db-dependent-tests.md)。建議只做 A 類。**2026-09-04：A 類 38 個 case 已全數拆除**，無 DB 環境紅燈 71 → 33，剩下的正好是 B 類。B 類維持現狀（未標 `[DbFact]`），理由見該文件 |

---

## P4 — 觀察／待裁決

| # | 項目 |
|---|------|
| **Z-1** | `UpgradeOptions.Default` 是可變型別的共用 static 單例（`src/Bee.Db/Schema/UpgradeOptions.cs:13,17`）。`AllowColumnNarrowing { get; set; }` + `static Default { get; } = new()`，任何呼叫端寫入就把「允許欄位縮短（可能截斷資料）」開給全 process。repo 內**零寫入點**，故為 API 形狀風險。對照 `MaterializeOptions` 同形狀但用 `init` → 結構上不可變。修法：改 `init` |
| **Z-2** | `CacheDefineAccess.GetDatabaseSettings` 每次呼叫都對快取實例 `DecryptInPlace`（`CacheDefineAccess.Settings.cs:38-43`）。字面上違反「cache 內物件不可異動」，實務上收斂（`Decrypt` 對無 `enc:` 前綴原樣回傳、字串賦值原子、每個呼叫端從頭跑完整解密所以自癒），**目前不是活的 bug**。但它是 repo 內唯一一處「明知在 mutate 快取」卻沒把理由寫在旁邊的地方。建議補 WHY 註解，否則下一個讀者會照抄這個形狀到不收斂的地方 |
| **Z-3** | N-1（`SerializeDefine` 對快取實例序列化）的 `<remarks>` 對損害的描述仍不完整。現行寫「effect is transient」，但實際上：(a) A、B 並行序列化同一實例時 A 的 `NotifyAfter` 會提早結束 B 的狀態 → B 產生的 XML 把空集合輸出成 `<Items />` 而非省略，**這是已送出的錯誤輸出、不是 transient**；(b) 併發讀者不只看到 null，還可能 NRE（四個裸 `!` 點）。不必改行為，但把 remarks 補到與實情相符 —— 這正是「明示接受」該有的樣子 |
| **Z-4** | `ApiCallContext` 從不上 wire，卻有完整 wire 註冊（`WireContracts.Envelope.cs:39-42`）並被發佈進 `wire-contracts/messages.d.ts:80-84`。`RegisteredContracts_AreReachableFromTheClosure`（`ApiErrorInfo` 那道反方向閘門）**結構上抓不到它** —— 閉包是命名空間字串比對，「在 Messages 命名空間」不等於「會上 wire」。連帶把 `format: PayloadFormat`（字串聯集）送進 TS 合約，而信封自己的 `format` 是**數字**，同名不同型。且它是**安全判定型別**（帶 `IsLocalCall`）成為客戶端可具現的反序列化目標（目前不可利用） |
| **Z-5** | `ApiPayloadOptionsFactory.CreateSerializer` **零 production 呼叫者**（4.27.0 起 `Initialize` 不再呼叫它）。留著會讓人以為「codec 仍是設定值」，而拿它的產物去呼叫 `Initialize(serializer,…)` 會改掉**未宣告 codec 的預設**，靜默打斷所有既有 client。建議移除（公開 API 變更）或在 doc 明寫「這不是設定 codec 的方法」 |
| **Z-6** | `BusinessObject` 建構子 `isLocalCall = true` 預設（`src/Bee.Business/BusinessObject.cs:37` 與三個子類），**上輪明示遞延，本輪後果已具體化**：四處 defence-in-depth（`SystemBusinessObject.Plugin.cs:46-49,82-85`、`DeploymentAdmin.cs:43-46`、`Define.cs:361-366`）的註解都寫「a caller constructing the BO directly never passes through `ApiAccessValidator`」，而**直接建構走預設路徑時 `IsLocalCall == true`，四道守衛全數放行** —— 只有刻意寫 `false` 的呼叫端會被擋，那是最不需要擋的一種。`IsLocalCall == true` 解鎖的能力包含：任意鑄造 API key、`SetDeploymentAdmin`、`SaveDefine`、以及 `ApiAccessValidator.cs:39` 的 `if (context.IsLocalCall) return;`（**整個存取控制跳過**）。P0-1 證明這種漏傳在框架內部就會發生。<br>選項：移除四個預設值（破壞性，約 80 呼叫點含 `apps/`/`samples/`）；或**改預設為 `false`**（安全的那一邊，破壞性小得多）；或至少把四處守衛的註解改成描述實情。連帶 `BusinessObject.cs:70` 的 `public bool IsLocalCall { get; } = false;` 初始式與建構子預設相反，**讀起來像預設 false** |
| **Z-7** | `nuget-publish.yml` 的 build 清單與 pack 清單不對稱（`:44-61` vs `:66-83`）：`src/Bee.Expressions` **在 pack 清單、不在 build 清單**，而 pack 帶 `--no-build`。目前靠 `Bee.UI.Avalonia`/`Bee.Hosting` 的 `ProjectReference` 傳遞建到它才沒炸；那兩條參考一旦消失，**發版當下才會失敗** |
| **Z-8** | 公開文件連進 `.claude/`（`docs/api-method-reference.md:22,140,141` + `.zh-TW`、`docs/development-constraints{,.zh-TW}.md:6`、`adr-006:65`）。`rules/public-docs.md` 明列 `.claude/` 為非產品文件；其中 `adr-006:65` 指的是 `~/.claude/rules/code-style.md`（**使用者家目錄**，外部讀者永遠開不了）。`check-public-docs.sh` 只掃 `docs/plans/`，不涵蓋這個方向 |
| **Z-9** | CHANGELOG 破壞性節標題大小寫漂移：舊 10 版 `### Breaking Changes`，4.24.0/4.25.0/4.27.0 三版改用 `### Breaking changes`。中文版 13 版全為「破壞性變更」，一致。三版皆在基準後 → 回歸 |
| **Z-10** | `docs/plans/plan-property-grid-control.md:10-16` 把 `[DefaultValue]`（144 處）併入「純編輯器用途、零消費端」清單，**但它不是零消費端** —— `XmlSerializer` 依它省略等於預設值的成員。實證：`FormField.Type`/`ControlType`/`MaxLength` 三個屬性在 `tests/Define/FormSchema/AuditRule.FormSchema.xml` 的 `<FormField>` 中**完全不輸出**。動它會改變所有定義檔的序列化形狀。是 plan 不是公開文件，但這句話會直接誤導實作者 |
| **Z-11** | `ApiSessionContext.Ambient` 逃生門：多使用者 host 若忘記註冊 scoped context 而落到 `Ambient`，共用的 `NextSequence()` 會讓單一 session 的序號出現大間隔；同一 session 自己的亂序請求若差距 ≥ `WindowSize`(64) 會被誤判為重放。`Bee.Web.Blazor.Server` 已正確 `AddScoped`，外部 host 自行接線時是陷阱 |
| **Z-12** | `DbDefineStorage.cs:89-91` 的 `_connectionManager ??= Resolve<T>()` 無同步，**目前安全只因為解析出來的也是 singleton**。若日後改成 transient，會靜默產生 per-race 實例且無任何徵兆 |
| **Z-13** | `LoginAttemptTracker._nextSweepUtc` 非 volatile 的 `DateTime` 在鎖外讀（`:54,170`）。64-bit 上對齊讀寫原子 → 只在 32-bit runtime 才可能撕裂，最壞結果是多掃一次（無害）。**不建議改**，記錄以免下次重複判讀 |
| **Z-14** | `ApiAccessValidator.cs:46` 的死分支：`if (attr.ProtectionLevel == LocalOnly && !context.IsLocalCall)` —— 上方 `:39` 已 `if (context.IsLocalCall) return;`，故後半恆真。無害，但讀者會以為它在做事 |
| **Z-15** | `DateInterval`（`src/Bee.Base/DateInterval.cs`）是 0 消費者的 shipped public enum（`src/` 內沒有任何 API 接受它，只有 `tests/` 引用）。依「0-caller 框架公開 API 保留」應留，但建議與 `IDefineField` 一併納入「未接線設計清冊」追蹤。同類另有 `IPValidator`（`src/Bee.Base/IPValidator.cs`，149 行，僅測試消費） |
| **Z-16** | `BeeServiceProviderExtensions`（`src/Bee.Definition/`）是潛在 CS0121 地雷：以 `internal` 提供 `GetService<T>`/`GetRequiredService<T>`，經 `InternalsVisibleTo` 被 `Bee.Business` 約 50 個呼叫點消費。目前只有 `FormPluginRunner.cs` 一個檔 `using Microsoft.Extensions.DependencyInjection` 且該檔沒有這類呼叫，所以編得過。**任何人在該檔（或未來新檔）同時引入這兩者並呼叫，就會是 CS0121**。XML doc 已寫撞名理由，但沒寫這個觸發條件 |

---

## 上輪遞延項複驗（狀態確認，非新發現）

| # | 上輪狀態 | 本輪實測 |
|---|---------|---------|
| **A-5** | Domain Core 夾帶 1,119 行檔案 IO，記錄阻擋（資料遷移） | **仍在，範圍未變**：7 檔 **1,121 行**（+2，純註解／格式）。`BackendDefaultTypes.DefineStorage` 逐字未變，無新的 IO 洩漏進定義層。**唯一新增的是 P1-10：README 主動否認它** |
| **A-3** | `Bee.UI.Core/Permissions/` 位置 + Blazor 無權限降級，使用者暫緩 | **仍在，逐字未變**；`src/Bee.Web.Blazor.Server` 對 `Sensitive|Capabilit|Permission` 零命中 |
| **DEP-1** | 22 處「使用未宣告的組件」，明示不修 | **仍為精確的 22 處**。**額外驗證：`OUTSIDE_CLOSURE` 全空** —— 沒有任何專案使用了不在其傳遞閉包內的命名空間。**下輪不需再提** |
| **SEC-3（上輪）** | 啟動時 API key gate 失效改為硬失敗，遞延 major | 4.20.0–4.27.0 未動。上輪承諾的「runtime 降級可見化」**已落地**（`SystemBusinessObject.ApiKey.cs:198` 停用最後一把金鑰時記 `LogError`）。<br>**本輪新查**：`ApiKeyRepository.CountEnabled()`（`:284`）只看 `enabled` 不看 `expired_at`，而 `SetApiKeyExpiry`（`:221`）**不呼叫** `ReportIfApiKeyGateFellOutOfForce` → 兩條輪替路徑行為不對稱（方向上 fail-closed，屬可用性陷阱而非安全破口） |
| **SEC-4~10（上輪）** | 帳號鎖定僅以 userId 為 key | 未加 IP 維度。SEC-2 的有界化（自帶到期 + 排程清掃 + `MaxTrackedAccounts = 10_000`）**確認仍在**。`ApiServiceController` 的 client IP 仍只用於異常稽核，未流到 BO 層 |
| **建構子 `isLocalCall`** | 上輪只改工廠面 | 見 Z-6。**後果本輪已具體化** |
| **P-2(b)** | 欄名逐列重複，明示接受 | 未重列為缺陷。另量了「JSON codec 是否放大它」：**gzip 後 0.99×，沒有放大** |
| **P-3 / PERF-2（上輪）** | 實測後不修 | 現況未變，無新證據推翻。期間**未新增更貴的反射點** |

---

## 掃描為乾淨的項目（供下輪回歸偵測；每項註明**用什麼方法掃的**）

> 上輪教訓 7：基準清單要記「用什麼方法掃的」，否則下一輪會用同一個盲區重新確認同一個結論。

### 架構與相依
- **28 條 consumer-facing 相依邊、零循環** —— Python 解析 17 份 csproj 的 `<ProjectReference>`（排除 `ReferenceOutputAssembly="false"`），DFS 三色標記找 back edge，`back edges = []`
- **mermaid 相依圖 28/28 逐條吻合，雙語各一份** —— 程式化擷取 mermaid 區塊的 `A --> B`，經 alias 表映回專案名後與 csproj 邊集合雙向差集，兩份皆空
- **四條硬約束全綠** —— **非只看 csproj**：逐專案 `grep "^using Bee.<X>"` 原始碼級掃描 + 傳遞閉包計算
- **ADR-038 落實度全綠** —— `Bee.Base.csproj` 零 Package/ProjectReference（全文檢視）；`Bee.Definition` 閉包經 `deps.json` BFS 無 DynamicExpresso/MessagePack；`Bee.Expressions` 檔案數 = 1
- **BEE9001 目前確實掛在三個目標專案上** —— `dotnet msbuild -getProperty:BeeEnforceDependencyBoundary`，三個 `true` / 兩個對照組空值（⚠️ 這是**當下**啟用，非**保證**啟用 → GATE-1）
- **`Bee.Base` 與 `Bee.Definition` 零向上參照** —— `grep "^using Bee\."` 全量去重
- **無新專案、無誤置資料夾** —— `git diff --name-status 227daa70..HEAD -- src/` 全量 A/D/R 逐條判讀，64 個新檔全部落在既有且正確的層
- **`Bee.Api.Contracts` 零實作污染** —— 74 檔逐一掃方法體語句，**0 命中**；4 個非 interface 型別逐檔檢視為純 auto-property DTO 與 enum
- **`Bee.Hosting` composition-root 紀律成立**、**`Bee.Api.Core` 無上帝專案徵狀** —— 逐檔／逐資料夾語意檢視

### 安全
- **SQL 注入 0（值）** —— `grep 'new DbCommandSpec(' -A3` 全 62 處 + 濾 `$"`/`+`/`${`
- **SQL 注入 0（識別符）** —— 逐一讀 `src/Bee.Db/Dml/`、`Storage/`、`CacheNotify/`、`src/Bee.Repository/System/`，全部經 `QuoteIdentifier`
- **`EXEC('…')` 巢狀語境 SQL Server 側全部正確** —— 上輪漏的 `SqlTableAlterCommandBuilder.cs:134-147` 已補 literal escaping（含 WARNING）；`SqlTableRebuildCommandBuilder`、`SqlExtendedPropertyCommandBuilder`、`SqlCreateTableCommandBuilder` 逐一覆核（MySQL 側見 SEC-3）
- **加密原語全綠** —— 逐行讀四個 cryptor：AES-256-CBC + 隨機 IV + **encrypt-then-MAC** + `FixedTimeEquals`；RSA-2048 OAEP-SHA256；PBKDF2-SHA256/100k/16-byte 鹽
- **全 repo 零 `==` 比較 HMAC/雜湊**；SHA1 僅存在於 `PBKDF2SHA1Legacy` 的唯讀驗證路徑
- **payload 管線順序正確且存取驗證在解密之前** —— 讀 `JsonRpcExecutor.ExecuteAsyncCore:110-135`：`ParseMethod` → `CreateBusinessObject` → `GetMethod` → `ValidateAccess` → **才**取金鑰 → `RestoreFrom`
- **型別白名單 parser 修法徹底且已擴散** —— 完整逐行讀 `WireTypeWhitelist.cs`（238 行）+ 追四個呼叫點（含 4.27.0 新增的 JSON codec）：括號配對、array rank vs 泛型引數、pointer/byref 拒絕、深度 8／長度 1024 上限、解析失敗即拒絕
- **`new Random(` 0**、**XXE 0**（5 個解析入口全 `DtdProcessing.Prohibit` + `XmlResolver = null`）、**`throw ex;` 0**、**TLS 憑證驗證繞過 0**、**硬編碼機密 0**、**MD5 0**
- **裸手動 `Dispose` 0** —— 9 處全在 `finally`／`catch` 清理／`Dispose(bool)` 內
- **空 catch 0** —— 10 處裸 `catch` 全部是「清理後 `throw;`」或 rollback best-effort，皆附說明
- **機密不落 log 0** —— `grep -iE 'log\w+\(.*(token|password|secret|apikey|privatekey|masterkey)'`
- **未標註 = 拒絕（fail-closed）** —— `ApiAccessValidator.ValidateAccess` + BEE3001 + BEE3003 雙重把關
- **路徑穿越 0** —— `PathOptions.ValidatePathSegment` 拒絕 `..`/`/`/`\`/`IsPathRooted`；租戶 customize 根另有 `GetFullPath` + `StartsWith(root + sep)` 二次驗證
- **API key 錯誤訊息不可列舉** —— 五種拒絕原因回同一訊息與同一 status；DB 故障不會被誤判為 `NotConfigured`
- **`NoEncryptionEncryptor` 非 debug 擲例外，無旁路**

### 維護性與散落
- **文化相依字串比對 0** —— `CurrentCultureIgnoreCase`／`StringComparison.CurrentCulture`／無 Invariant 的 `ToLower/ToUpper` 全 `src/` 皆 0
- **無 `StringComparison` 的 `StartsWith`/`Contains`/`IndexOf`：7 處，0 違規**（上輪 19 處）—— grep 後**逐一開檔驗證**（多行三元續行帶 `Ordinal`、`Array.IndexOf(char[])`、`const char` 分隔符走 ordinal 多載、`EndsWith(char)`）
- **`string.Compare`/`CompareTo` 0 筆**；`KeyCollectionBase` 建構子為 `StringComparer.OrdinalIgnoreCase`
- **`*Func`／`*Helper`／`*Util`／`*Mgr` 型別 0** —— `ExecFunc*` 家族 10 個型別逐一確認為 JSON-RPC domain 型別，未誤報
- **擴充 `object` 0**；**與 BCL instance method 同名的擴充方法 0** —— 列出全部 **69 個**擴充方法逐一比對目標型別的 BCL instance 成員
- **一型別一檔 0 違規** —— 全 `src/` 逐檔計算頂層型別宣告數（兩種縮排皆掃）。24 檔含 2 型別：21 檔為 `<Collection>` + `<Collection>Extensions`（明列例外），3 檔為主型別 + internal 輔助。**上輪 N-2 點名的 3 檔全部已拆**
- **檔名 ≠ 型別名 0** —— 對 1,063 個宣告做 stem 比對，零命中（上輪的 `FilterNodeSubtypeFormatters.cs` 已改名）
- **`src/` > 500 行檔案 0**（主代理獨立複驗）—— 最大 495（`FormField.cs`，明列豁免的純屬性袋）。追蹤項：`ValueUtilities` **413**（已拆 `.Temporal` 239）、`CacheDefineAccess` **188**（已拆 `.Settings` 209 / `.Schemas` 150）。38 個 partial 分檔皆循 `<TypeName>.<Concern>.cs`，無 `Part1`/`Part2`
- **資料夾↔命名空間例外外 0** —— 腳本逐檔比對，53 筆落差**全部**在 `src/Bee.Definition/Settings/`（唯一明文例外）
- **中文 `#region` 0**（上輪 31）；**中文 XML doc 0 行 / 27,554 行**（硬基準）
- **S125 真陽性 0** —— 13 筆疑似逐筆開檔，全為折行英文散文。**未刪任何註解**
- **`#pragma warning disable` 1 處且附完整說明**；**`SuppressMessage` 4/4 全附 Justification**
- **`TODO`/`FIXME`/`HACK` 標記 0**
- **grab-bag 0、空 class 0、`[Obsolete]` 0、0-caller 非公開型別 0、0-caller 方法 0 實質、純 facade 0** —— 建立全 repo identifier→檔案索引（2,448 檔、364,567 筆），對 1,063 個型別與 2,324 個方法宣告算外部引用；24 + 79 個 0 命中者**逐一複驗全為假陽性**（擴充方法類名不出現在呼叫端、Roslyn method group、事件處理常式、BCL/Blazor override、XmlSerializer 慣例）
- **四類假陽性存活形式各自查過**：DI 註冊（37 個介面逐一查引用，全部 ≥3 檔且含真實消費點）、佔位測試（兩路交叉，33 筆單一 `Assert.NotNull` 的受測型別皆有生產呼叫者）、反射/慣例式消費（`nameof`+反射、`TypeDescriptor`、embedded resource 列舉三類明確辨識）、XAML/Razor（索引含 `.axaml`/`.xaml`/`.razor`）
- **本視窗 64 個新增檔全數已接線** —— 逐一查引用（replay 全鏈、`JsonRpcErrorContract`、JSON codec 三件套、AuditRule 全棧 9 型別、`DescriptionSync*` 五件套、`FilterNodeJsonConverter`、`CacheSingleFlight`、`ApiSessionContext`、`WireValueCode` 等）
- **上輪清除項目零回歸** —— `MessagePackContract`、`AuditLogOptions.ExecEnabled`、`Bee.Db.Dml` 五介面、`ApiCallContext.ShouldValidateEncoding`、`ClientInfoTestScope`、`ApiErrorInfo` 原始碼皆 **0**
- **D-9（`IsEmpty` 語意分歧）已收斂** —— `StringUtilities.IsEmptyText` 已改名，兩邊 doc 互指，且有直接釘死「對 `Guid.Empty`/`DateTime.MinValue` 結論相反」的測試

### 序列化
- **回應 codec 對稱性（實作面）正確** —— 逐行讀 executor/converter/client 全路徑（測試缺口見 P1-4）
- **未宣告 codec 的回退確為 MessagePack** —— 讀碼 + `ApiPayloadJsonConverter.Write:116-117` 空 codec 不寫欄位，信封逐位元與協商前相同
- **`PayloadFormat` ⊥ codec** —— `TransformTo`/`RestoreFrom` 對 `Plain` 一律 early-return
- **殘留 `<Serializer>` 元素的靜默失效風險實質為零** —— 全 repo 無該元素；4.27.0 之前 `CreateSerializer` 只認 `"messagepack"`，其餘擲例外，**所以任何既有部署的合法值恰好等於新的相容性常數**
- **JSON / MessagePack 在 `object` 通道型別覆蓋等價**（桌面）—— scratchpad probe 逐案 round-trip 對照 22 個判別碼；**拒絕集合也一致**（`List<object>`/`DataSet`/`char`/`TimeOnly` 兩端都擲例外，非沉默）
- **`Guid`/`decimal`/`byte[]`/`DateTimeOffset`/`int64` 在封套內保真**；**`DataTable` 儲存格的 `DateTimeKind` 不是 codec 不對稱**（賦值時就正規化為 `Unspecified`，三方對照相同）
- **`WireValueCode` 判別碼以字面數字釘死**，且連 MessagePack 實際位元組一起釘
- **`WireContractDriftTests` 非空轉**（GATE-1/GATE-2 上輪修正確實有效）—— 讀測試 + 執行；**且在 `-p:DynamicCodeSupport=false` 下同樣全綠**（385 通過 / 1 略過）
- **漏註冊的 wire 型別／封閉泛型具現 0** —— 15 enum / 5 `Nullable<T>` / 9 `List<T>` / 3 `Dictionary<K,V>` / `string[]` 全對應
- **`[Union]`／整數 `[Key]`／`[MessagePackObject]` 殘留 0**；**Newtonsoft 0**；**定義層零傳輸格式套件**
- **三棲 ignore 標籤：4 處不對稱全部是已裁決的正確設計** —— 自寫 AST-lite 掃描器逐屬性比對 + 回溯 `de682143` 剔除 BEE4007 的裁決（「跨格式不對稱是常見且正確的設計」）
- **靠私有 setter 隱性避免上 wire 0**；**`[XmlElement]` 標註的 get-only 集合屬性 0**（硬基準）
- **Z-3（`ScopeResolver` 的 `List<object>`）已修** —— 三處全改走 `FilterCondition.In()`；**Z-4/Z-5/Z-6/Z-7（上輪註解漂移）四處全部已修且敘述正確**

### 公開 API
- **契約↔wire 雙向零孤兒**、**契約軸命名空間↔資料夾 100%** —— 自建 Python 雙向差集
- **四層對齊 70/70 零孤兒零半成品** —— 解析 base list 建「介面→實作」反向表逐一查；Client 層以 `PublicAPI.Shipped.txt` 比對，`Form` 6/6、`Log` 9/9、`System` 缺的 7 個**逐一判讀全部有明文理由**
- **BO 表面 ↔ `docs/api-method-reference` 41/41 全對，雙語一致**（目前靠紀律，無機制 → API-1）
- **public 可變欄位 0** —— 23 筆命中全為 `public event` 或 private nested 內欄位
- **對既有 public 成員新增 optional 參數 0** —— `PublicAPI.Shipped.txt` 的 `227daa70..HEAD` diff 以成員名前綴配對 +/− 行，比較兩側 `=` 次數
- **`PublicAPI.Unshipped.txt` 16/16 皆已清空**；**快照涵蓋所有可發布專案 16/16**
- **11 筆破壞性變更逐筆審查全部正確申報**（CHANGELOG 破壞性節 + `PublicAPI` diff 雙向核對）；`86887768`（AuditLog 收編）與 `c7b035b3`（稽核/異常拆介面）兩處**重點複驗通過**
- **`Shipped.txt` 正確性抽查：未發現 `ExecFuncLocal` 等級的壞 API** —— `ExecFuncLocal` 全 repo 零殘留；無條件擲例外的公開方法只有已裁決的兩個擴充點；`[Obsolete]`/`[EditorBrowsable]` 0；`Bee.Api.Core.MessagePack` 在 Shipped 的條目數 0
- **`<param>` 名稱不符 0** —— 兩層：CS1572/CS1573 編譯期硬把關 + 自建掃描器 34 筆疑似逐筆判讀全為解析器誤判

### 測試
- **S2699 0 違規 / 4,344 個測試方法**（上輪 195/4,133）—— Python 解析屬性+大括號配對取 body，200 筆無 `Assert.` 者**逐一 1-hop 解析到 asserting helper 並記錄解析範圍**：194 同檔、6 同專案、0 跨專案、**0 未解析**
- **空洞 round-trip 0** —— (a) 上輪 T-2 的 7 處逐一複驗全部已補值比對，**六處就地留下了「為何空實例不構成證據」的註解**；(b) 全量重掃同形狀 23 筆逐筆開檔判讀全為 null/empty 邊界案例
- **`SharedDbFixture` 誤用 0** —— **先枚舉 14 條會觸 DB 的路徑**（含 4.25.0 新增的 `CompanyAuditRules`）再對 743 個測試檔交叉比對，65 個候選逐一開檔判讀；18 個 `IClassFixture<BeeTestFixture>` 類別逐檔查 token 來源，**無一以 `Guid.NewGuid()` 當有效 token**。⚠️ **此判定為靜態推理，方法論上弱於上輪的實跑（drop `st_session` 後跑排除子集）**
- **顯式等待造成的 flaky 0** —— grep 五組 API 後**逐一開檔判讀等待的作用**：`Thread.Sleep` 3 處皆良性（塞在 factory 內拉長並行重疊窗、lock 輪詢間隔），`Task.Delay` 4 處中 3 處為 deadline-bounded 輪詢，`[Fact(Timeout=)]` 0
- **上輪 TEST-1 的七處時區斷言全數改為 `DateTime.UtcNow.Date` 並就地註明**（修正正確且完整）
- **fixture 污染 0 / 113 個 `SaveXxx` 呼叫點**（上輪 58）—— grep 全量按檔分組，12 個無 temp-path 標記的檔逐一開檔看實際呼叫對象
- **production static 被測試改動：五組 API 在 `tests/` 全部 0**；唯一的 `Environment.SetEnvironmentVariable`（8 處）已由 `[Collection]` 序列化
- **`[Collection]` 零孤兒** —— 30 個實際屬性 / 5 個名稱 / 6 個宣告（`SysInfoStatic` 跨組件各宣告一次，必須如此），2 筆疑似命中經開檔確認為 doc 註解。新增的 `ProcessWideStateCollection` 用 `const Name` 而非字面值
- **`[DisplayName]` 100%（4,344/4,344）；零案例 Theory 0** —— 8 + 3 筆疑似**逐一開檔核對**皆為多行屬性區塊擷取失真
- **上輪 T-3 的五項覆蓋缺口全數關閉** —— 逐項驗證（`ExpiredSessionCleanupService` 5 筆、`LogApiConnector` 9 action 路由**且同時釘住 request 型別**、三個集合成員帶值比對、`WireValueCode` 以字面數字釘死 + `Count_IsOnePastTheHighestCode` + `DateTimeOffset` round-trip、`CreateLogBO` 隨死碼清理移除）
- **本週期新功能覆蓋充分** —— JSON codec 10 筆（含「宣告錯 codec 必須解碼失敗而非靜默預設值」）、anti-replay 10 + 8 筆、per-form audit rule **5 種 DB 全覆蓋**、row-level tenancy、TypeScript connector 6 筆
- **新增的 8 道閘門全部具備防空轉斷言**（缺口僅 API-2 一筆）
- **CI AOT 閘門已擴至 3 個專案**（`Bee.Api.Core` + `Bee.Definition` + `Bee.Base`），且註解就地記下已知盲區
- **跨行程建表競賽已修**（`99636efd`：`CrossProcessLock` + seed 單一交易 + setup 失敗不再靜默）

### 文件
- **相對連結與 anchor：公開文件 0 壞 / 2,457 條 / 379 份 `.md`** —— 自寫 Python 掃描，**先以刻意壞掉的 probe 驗證 checker 非空轉**（2/2 抓到）
- **`docs/changelogs/` 子目錄相對路徑 0**（上輪教訓已固化）
- **`check-public-docs.sh` 通過**：(1) 僅剩 `docs/README{,.zh-TW}.md:104` 的性質說明；(2)(4)(5) 無輸出；(3) 4 筆**全為已知誤報**（`Orchestrator.Execute`/`plan.Warnings` 是 API 識別符、adr-023「另立 plan」指不到現存檔）
- **`check-xmldoc-refs.sh` 通過**（主代理獨立複跑）
- **ADR 索引 44/44 雙向吻合、狀態格式 44/44 一致**
- **Analyzer 規則涵蓋 22/22/22** 三方完全相同（含 BEE9001–9003）
- **`dependency-map` 外部套件表逐列吻合**（含 `MessagePack 3.1.7` 只在 `Bee.Api.Core` 這條 ADR-036 軸心）
- **`api-method-reference` 41/41 雙向零落差**；**`development-constraints` 快取型別清單 6/6 完整正確**；**`DefineType` 13 值雙語正確**
- **雙語配對 74 對逐對比 heading 數，0 對偏差 > 1**；**CHANGELOG 雙語條目數 10 版全等**
- **版號複寫 0**（`Version.props` 單一來源守住）；**`<Serializer>` 殘留 0**
- **`getting-started` 開機碼逐行對簽章正確**

### 效能與並行
- **codec 協商零效能債** —— 兩個 codec 的 options 皆 `static readonly`（`JsonPayloadSerializer.cs:47` 附 `WARNING: shared and must stay shared`，**直接對應上輪 PERF-1 的教訓**）；逐請求選 codec 實測 **0.050 µs/次**（對照被判為雜訊的 PERF-2 是 0.50 µs，這是它的十分之一）
- **JSON codec 實測比 MessagePack 略快，壓縮後位元組數幾乎相同** —— 1000×30 DataSet：序列化 6.93 vs 8.63 ms（0.80×）、反序列化 12.29 vs 13.20 ms、gzip 後 192,309 vs 194,155 bytes（**0.99×**）
- **上輪四項效能修正 100% 保持** —— PERF-1 以重跑同規格 harness 驗證（12.64 vs 上輪 12.2 ms）；P-4 已擴為三個索引；PERF-3 仍在；P-2(a) 兩條路徑都在且留了 WARNING
- **`KeyCollectionBase<T>` 仍是真 O(1)** —— 直接讀 `:22` `base(StringComparer.OrdinalIgnoreCase)`，該多載的 `dictionaryCreationThreshold` 預設 0，**無 threshold 陷阱**
- **零處 per-row 線性欄位查找**（硬基準）—— grep 四種形狀交叉 `field|column` 後逐筆判讀
- **`new XmlSerializer`/`HttpClient`/`Regex`/加密器全數已快取或池化**
- **N+1（讀取路徑）0** —— 讀 `DataFormRepository` 全部 `Execute` 呼叫點：master 一次 + 每個 detail table 一次
- **同步阻塞非同步僅 1 處且伺服端不走它**（`JsonRpcExecutor.cs:89`，保留給同步宿主的 0-caller 公開 API）
- **`async void` 0**、**`lock(this)`/`lock(typeof)` 0**、**巢狀鎖 0**、**鎖內做 IO/DB 0**
- **唯一的 DCL（`DepartmentTree.EnsureIndex`）帶正確 `volatile` 且先建後發佈**
- **DI captive dependency 0** —— 45 `AddSingleton` + 1 `TryAddSingleton` + **2 `AddScoped`** + 1 `AddTransient`；期間新增的兩個 Scoped 都在 Blazor，其唯一 Singleton 相依是 POCO，**方向正確**
- **singleton 服務的可變 instance 欄位 0**（17 個型別逐一 grep）
- **裸集合作為共享狀態 0** —— src 的 static 集合全部 `readonly` 且僅在 type initializer 填充；需要並行處全用 `ConcurrentDictionary`
- **event 訂閱洩漏 0** —— 唯一 `public static event` 的唯一訂閱者已實作 `IDisposable` 退訂
- **`[ThreadStatic]` 唯一一處作用窗論證仍成立**（單一同步 `lambda.Invoke`，中間無 await，支援巢狀）
- **ADO.NET 物件存在 singleton 上 0**
- **快取實例被 mutate（定義檔 + 資料庫相依兩類）0** —— 兩路交叉：全量追 51 個 `IDefineAccess.GetX(...)` 呼叫點 + 對集合變異方法 grep；唯二寫入點都在 `Clone()` 之後。**`XmlCodec.Serialize(cached)` 23 筆逐一判定來源**，只有 `SerializeDefine` 的 6 個作用於快取（= 明示接受的 N-1）
- **ADR-037 註冊機制零並行債** —— 全部 type-initializer + `readonly`/`ImmutableArray`，零 lazy 快取、零鎖、零 runtime 註冊路徑；4.27.0 新增的 `WireValueFormatter`/`FilterGroupFormatter`/`JsonRpcErrorContract` 同樣成立
- **上輪 9 項並行修正全部複驗有效，且無一引入新的並行攻擊面**（逐項對帳；SEC-2 特別確認**未引入背景執行緒**、`AddOrUpdate` 的 CAS 語意用對了）

---

## 建議執行順序

1. **P0-1**（`AuditRule` 建構子）—— 一行修法 + 一個閘門測試。已出貨功能遠端不可用，且該閘門一次關閉整類問題
2. **P1-2**（空雜湊段 + `st_user.password` 進 `ProtectedFields`）—— 三行 + 一筆清單，關掉一條完整的權限提升鏈
3. **P1-1**（記錄範圍繞過）—— 止血一行（缺主檔即拒絕），完整解另排
4. **P1-3**（DataTable 金額在 JSON codec 失真）—— **時效性最高**：`wire-fixtures/` 一旦有跨語言 client 依循就變成破壞性變更，現在改幾乎免費
5. **P1-11 + API-1 + P1-6 + P1-4 + P1-5 + API-2**（六道閘門）—— NUL 一個字元、`BoApiSurfaceTests` 加一個 Fact（加上去當天就綠）、`ApiSurfaceEntry` 加一欄、codec 對稱一條測試、fixture 完整性改由常數表驅動、`BusinessContractPairingTests` 加四行。**合計半天不到，關掉「宣稱有把關但實際沒有」六處**
6. **P1-7 + T-2**（測試併發改寫共用快取）—— 兩個組件同一形狀，一起修
7. **P1-8**（`UpdateBatchSize`）—— provider-gated，**需跑 `[all-db]`**。目前框架最大的單筆每請求成本
8. **P1-9 + P1-10 + DOC-1~DOC-10**（公開文件把已移除機制寫成現行）—— 低成本高價值；DOC-1（Northwind）是唯一「照做會壞」等級
9. **P2 的 ARCH-1 + GATE-1**（四條硬約束與 BEE9001 的可執行閘門）+ **CON-1 + CON-4**（各一處，`SafeDrain` 與 try/finally）
10. **P2/P3 其餘** + **P4 裁決**（尤其 Z-6 的 `isLocalCall` 預設，建議至少改為 `false` 這個安全的一邊）

> **執行實況（2026-09-04）**：1–9 全部落地。第 10 步中，P2 已全數關閉、P3 的 DOC 系列（19 條）
> 與 ARCH-2 / ARCH-3 / T-5 / T-7 亦已關閉。**實際順序與上表有出入**，因為執行中發現的東西改變了
> 優先序：T-7 掃出 71 筆同類（衍生 T-8）、T-6 的 flake 沒重現卻在同元件查到一個真的 UTC 基準 bug、
> ARCH-1 的閘門自己在完整模式 CI 上紅了。**這些都不是原計畫看得出來的**，這也是為什麼上表的
> 「項目數」不再寫死。

---

## 方法論教訓（下輪沿用）

1. **主代理親自跑第二次測試抓到了 10 個代理都沒抓到的東西。** 測試面向代理依指示不執行測試（靜態審查），並誠實自述「我的『0 違規』方法論上弱於上輪的實跑」。P1-7 那筆 flaky 是主代理連跑第二次 `./test.sh` 才浮出來的 —— 第一次全綠。**下輪固定把「同一套測試連跑兩次」列為主代理的必做步驟**，成本一次全量測試，收益是靜態掃描原理上看不見的東西。

2. **「掃描單位決定盲區」這次同時得到正例與反例。** 測試代理在 `Bee.Hosting.UnitTests` 找到「多個類別驅動同一個 process-wide static、無並行保護」的形狀（T-2），卻沒在 `Bee.Db.UnitTests` 找到同一形狀的**實際失敗**（P1-7）—— 因為它掃的是「static 欄位」，而 P1-7 的共享狀態是「快取實例的集合」。兩者合起來才看得出這是系統性的。**下輪對每個確認的形狀追問「它的另一種載體是什麼？」**

3. **基準清單本身會漂，而且沒有任何機制會發現。** 本輪發現 skill 的「已知基準」表停在 2026-08-07 / v4.17.0（實際上輪是 2026-08-11 / v4.19.0）；「尚未複驗」清單把 `GetFormSchemaRequest`/`Response` 列為待查死碼（它們是**活的**，真正被刪的是 `GetChangeLogResponse`）；「應維持為 0」清單把 `[DbTheory]`/`[LocalOnly*]` 列為應清除（上輪明文裁決保留）。**基準清單要與 plan 一起更新，且下輪第一步是驗證基準清單本身。**

4. **「這個形狀在 repo 裡還有幾份？」連續兩輪都命中。** 上輪 SEC-1 的 `IndexOf(',')` 從 `ApiPayloadConverter` 複製到 `WireValueFormatter`；本輪 DOC-13 的「six direct dependents」在 repo 裡有**三份**（上輪只修了一份）、SEC-3 的 MySQL 逸出缺口在 4.27.0 從 2 個 sink 擴到 3 個、`docs/changelogs` 缺漏被**三個面向獨立發現**。**這條追問應該升格為每個確認缺陷的固定流程。**

5. **上輪的修正本身是本輪的主要缺陷來源之一。** DOC-14（BEE9001 涵蓋範圍）源自上輪 X-7 自己的擴大閘門；M-2 新增的 5 行中文註解全部來自上輪 M-3/CON-2 的說明文字；API-2 的新閘門是在上輪把「防空轉」寫成教訓 #4 的**隔天**新增的。**上輪教訓 #9「上輪修法本身要當成新攻擊面掃一次」被再次驗證，且應擴大為：連修正順手寫下的註解與文件也要掃。**

6. **平台預設值是一個沒被檢查過的面向。** PERF-1（`UpdateBatchSize = 1`）與 PERF-2（`bufferThreshold = 30 KB`）的共同形狀是**框架把一個平台預設值原樣吃下來，而那個預設值對本框架的使用模式是錯的**。兩輪體檢都沒往 ADO.NET adapter 與 ASP.NET 管線這兩個框架邊界看。**下輪加一條檢查項：我們沿用了哪些平台預設值？對我們是否正確？**

7. **實測會改變結論，而且兩個方向都會。** 本輪實測**升格**了三項（P0-1 的 `MissingMethodException`、P1-2 的 `VerifyPassword = True`、P1-8 的 14.3×），也**降格**了三項（`ApiInputConverter` 的 `GetRawText` 實測 0.98–1.11× 而非瓶頸、`BuildVariables` 上限只有 17%、信封字串具現只有 1.03–1.66× 而非主因）。**降格與升格同樣有價值** —— 它把注意力從看起來像問題的地方移開。

8. **代理的建議會與既有的刻意決策衝突，而且只有實跑抓得到。** CON-6 建議把
   `ExpiredSessionCleanupService` 的 catch 清單對齊姊妹服務，讀起來完全合理 —— 直到改下去
   讓整個測試組件卡死，二分定位到 `NonDbException_IsNotSwallowed` 這條把相反行為釘住的既有測試。
   **本輪第二次**（第一次是 T-2）。兩次的共同形狀是「代理看到不一致，但沒看到那個不一致是被
   決策過的」。下輪對每一條「對齊姊妹 / 補上缺的那半」型建議，固定先 grep 有沒有測試在守
   相反的行為。

9. **「plan 完成」不等於「文件完成」，而且殘留會活很久。** `plan-definition-messagepack-decoupling.md`
   於 2026-08-09 標記完成，程式碼確實改乾淨了（`src/` 對 MessagePack 標註命中 0），但文件裡有
   **九處**教人加那些標註的敘述活到 2026-09-04 才被清 —— 分佈在 `Bee.Definition/README`（4 處）、
   `api-bo-contract-design`（3 處）、`Bee.Api.Contracts/README`（1 處）、以及一條失效的 ADR-030 引用。
   其中最危險的是設計慣例與範例程式碼：外部開發者會照抄，而照做即違反 BEE9001 擋的相依邊界。
   **下輪對每一份標記完成的 plan，固定問一次：它改了哪些型別／標註，公開文件跟了嗎？**

10. **「文件宣稱有保證」本輪出現至少八次**（上輪五次）：`BoApiSurfaceTests` 的 DisplayName（**上輪點名未修**）、`docs/api-method-reference` 的「the build will fail otherwise」、`WireFixtureTests` 的「每個判別碼都要有樣本」、`src/Bee.Api.Core/CLAUDE.md` 的「兩條 wire 共用判別碼（同時釘住兩者）」、`DbConnectionManagerTests` 的「使用唯一 databaseId 以避免干擾」、`AuditLogWriterService` 的「records are never silently lost」、`FormBusinessObject.Write` 的「once the master passes」、`Bee.Definition/README` 的「no I/O」。**這個數字在上升，說明對「宣稱」的產出速度高於對它的驗證速度。** 建議把「帶絕對語氣的註解必須指出對應執行路徑」做成 code review 的固定檢查項，而不只是體檢時的追問。
