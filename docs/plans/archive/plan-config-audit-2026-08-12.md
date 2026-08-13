# 設定檔健檢（2026-08-12）

**狀態：✅ 已完成（2026-08-12）**

| 階段 | 範圍 | 狀態 |
|------|------|------|
| 1 | P0：正在誤導 agent 的失效引用（plugin 名、skill 舊名、AOT 結論矛盾） | ✅ 已完成（2026-08-12） |
| 2 | 清除常駐設定內的清點數字，改定性描述（含已漂與尚未漂者） | ✅ 已完成（2026-08-12） |
| 3 | `apple-mobile-trim.md` 推導內容搬按需區（新建 `gotchas/mobile-trim-aot.md`） | ✅ 已完成（2026-08-12） |
| 4 | `testing.md` 程式碼樣板搬按需區（新建 `docs/repo-ops/testing-patterns.md`） | ✅ 已完成（2026-08-12） |
| 5 | P3：跨層錯置與示意路徑（scope、skill 歸屬、ADR 路徑形狀） | ✅ 已完成（2026-08-12） |
| 6 | 週邊：soarcloud-libraries plugin scope 補更新、`.gitignore` 補 env 樣式 | ✅ 已完成（2026-08-12） |
| 7 | 砍不改變行為的散文（追加階段：階段 3/4 只搬位置，文字量沒真的降） | ✅ 已完成（2026-08-12） |
| 8 | 路徑限定規則下沉 —— 機制驗證 + `testing.md` 下沉 | ✅ 已完成（2026-08-12） |
| 9 | 其餘路徑限定 rules 下沉（逐支判定「audience 是否真的只有一棵樹」） | ✅ 已完成（2026-08-12） |
| 10 | 使用者層設定進版控 + 最小化（`code-style.md`／`releasing.md`／`CLAUDE.md`） | ✅ 已完成（2026-08-12） |
| 11 | 去除「可執行檔行為說明」的複寫（`test.sh`／`pre-commit-verify.sh`） | ✅ 已完成（2026-08-12） |

## 背景

設定檔語料（`CLAUDE.md`、`rules/`、`skills/`、`commands/`、memory）是指揮 agent 的憲法，
但**沒有任何機制會告訴你它漂了** —— 編譯器不看它、測試不跑它、CI 不驗它。
本次為**首次基線**，以 `dev-workflow:config-audit` 的方法論執行。

判準是「這段在下一個 session 會不會改變 agent 的行為」，不是「長不長」。
常駐量是指標不是目標，因此下列各項**沒有一項是為了讓數字好看**。

### 基線（2026-08-12 當日量測）

| 層 | 量 |
|----|----|
| 常駐 rules + `CLAUDE.md`（17 檔） | 124,978 字元（約 35k tokens） |
| Skill description（專案 + 使用者 + plugin） | 8,936 字元（約 2.6k tokens） |
| **常駐合計** | **約 38k tokens** |

前五大常駐檔：`testing.md`、`~/.claude/rules/code-style.md`、`apple-mobile-trim.md`、
`serialization.md`、`~/.claude/rules/releasing.md`。

> 上表是**當時的量測紀錄**，不是要維護的現況宣告 —— 下次健檢重新量、與此比對趨勢即可，
> 不需回頭更新這裡的數字。

---

## 階段 1 — P0：正在誤導 agent

**已完成（2026-08-12）。** 執行中另發現同類的第三處（1.3），一併修掉。

### 1.1 `.claude/CLAUDE.md:79-80` 的 plugin 名已失效

文件寫的是 `/plan-workflow:plan-write` skill、「由 `jeff377-plugins` marketplace 的
`plan-workflow` plugin 提供」。實際 `.claude/settings.json` 啟用的是
`dev-workflow@jeff377-plugins`，skill 路徑為 `dev-workflow:plan-write`。

佐證：`~/.claude/plugins/cache/jeff377-plugins/` 下 `plan-workflow` 目錄仍在，
但**已不在安裝註冊表** —— 是改名前的殘留，不是另一個仍在用的 plugin。

**動作**：兩處名稱改為 `dev-workflow`。

**為何是 P0**：agent 依此指路會叫不到 skill，而失敗形式是「找不到」而非報錯，
容易被當成 skill 不存在而改用預設做法。

### 1.2 `.claude/rules/apple-mobile-trim.md:225` 與 `serialization.md` 直接矛盾

該行寫：

> `rules/serialization.md` —— MessagePack / DynamicExpresso 的 AOT 結論（皆有 reflection fallback）

但 [`serialization.md`](../../../.claude/rules/serialization.md) 的節標題就是
「AOT：MessagePack 的 reflection fallback **只涵蓋有標註的型別**（2026-08-10 修正）」，
內文明載 contractless **沒有** reflection fallback，並記錄該誤判曾讓 iOS 端 wire 整條壞掉。

諷刺的是同一檔 line 80 還引用了那次誤判當教材 —— 但 § 相關 的摘要仍是被推翻前的舊版。

**動作**：括號內改為「MessagePack 需顯式註冊 formatter（無 contractless fallback）／
DynamicExpresso 自動退回直譯器」。

**為何是 P0**：agent 若只讀到這行，會重新推導出已被實測推翻的結論 ——
正是該檔自己警告的錯法（「實測推翻推測時，先問我的樣本涵蓋了推測所指的那條路徑嗎」）。

### 1.3 `.claude/skills/README.md` 的 skill 清單落後 plugin（執行 1.1 時發現）

修 1.1 時順手搜全 repo，發現該檔的 plugin skill 表列 `/dev-workflow:plan-handoff` ——
那是 v2.2.2 以前的舊名，v2.3.0 已改為 `session-handoff`；v2.4.0 新增的 `config-audit`
也不在表內。該檔是本 repo 的 skill 權威索引，agent 照它呼叫會叫不到。

**動作**：改為新名、補上 `config-audit`、加一行改名註記。表下「三者涵蓋…」的計數
依階段 2 判準改為不帶數字的敘述。

**這一處說明了「找到一個就搜全 repo」的必要**：同一次 plugin 改名造成兩處失效，
而健檢的路徑檢查只掃常駐檔（`skills/README.md` 不在常駐區），抓不到它。
**下次健檢應把 `.claude/` 下所有 README 納入 skill 名稱的比對範圍。**

---

## 階段 2 — 清除常駐設定內的清點數字

### 2.0 判準

設定檔**不標非重點的數字**。特別是「repo 裡有幾個 X」這類清點數：專案數、處數、行數、條數。
理由有兩層：

1. **必然漂移**，且沒有任何機制會發現。
2. **對 agent 完全沒幫助** —— agent 需要的是「該做什麼」與「怎麼判斷」，不是庫存清單。

判別法：**這個數字是「可執行的閾值／已列舉的程序步數」還是「repo 內容的清點」？**

| 留 | 刪或改定性 |
|----|-----------|
| 閾值：「> 500 行就拆」「連跑 2–3 次判 flaky」「`TimeSpan.FromSeconds(1)`」 | 清點：「6 個下游」「28 處」「18 條」「495 行」 |
| 規格：「256-bit」「SHA-256」「縮排 4 個空格」 | 統計：「零使用」「只有 1 處使用」 |
| 已列舉的程序步數：「三處都要改，缺一不可」「四個步驟」 | 節標題的項數：「兩個反覆根因」 |

**歷史量測不受此限**：「2026-08-10 實測 37 → 185 筆失敗」、「無 descriptor 砍 57%」是
**紀錄當時量到什麼**，不是宣告現況，**保留原值**。
判別法：**這句在講「現在是什麼」還是「當時發生了什麼」？** 前者才清。

> 附帶效果：清掉清點數後，config 健檢的「重跑量測比對數字」那一步無事可做 ——
> **移除漂移源優於定期偵測漂移**。

### 2.1 已漂移者（結論不變，數字錯）

| 位置 | 現況 | 實測 | 動作 |
|------|------|------|------|
| `.claude/CLAUDE.md:54` + `dependency-boundary.md:7` | 「`Bee.Definition` 有 **6 個**直接下游（Contracts、Db、RepoAbs、Caching、Business、Api.Core）」 | `src/` 內為 7 個，列舉漏了 `Bee.UI.Avalonia`；含 `tools/` 則 9 | 去掉總數，改「有多個直接下游，且是每個 UI head 傳遞閉包的成員」。**一併消除同一數字複寫兩處** —— 那違反 `releasing.md` 自己立的單一來源原則 |
| `testing.md:267` | 「另有**五個組件**改以 `DisableTestParallelization`（列出四個）」 | 實為 Api.Client／Api.Core／**Definition**／ObjectCaching／UI.Avalonia | 去掉總數，補齊列舉 |
| `testing.md:385` | 節標題「『本機綠、CI 紅』的**兩個**反覆根因」 | 下有 3 個子節（第 3 條後補、標題沒跟） | 改「反覆根因」不帶數字 |
| `~/.claude/rules/code-style.md:131` | 「`FormField`（495 行、**36 個** `[XmlAttribute]` 屬性）」 | 495 行精準；`[XmlAttribute]` 為 24 個 | 兩個數字都拿掉（改「近 500 行、通篇 `[XmlAttribute]` 屬性」），只留「純屬性袋、沒有自然分群」；拆分閾值（> 500 行）本身留 |
| ~~`~/.claude/rules/code-style.md:140-145`~~ | ~~`ValueUtilities` `.Temporal` 224 行~~ | 239 行 | **執行時更正為「不動」** —— 詳下 |
| `.claude/rules/avalonia.md:63` | gotchas 有「**18 條**實證雷」 | 17 條 | 改「實證雷」不帶條數 —— 條數會隨新雷增加，註定再漂 |

### 2.2 尚未漂移但同樣該清（健檢時數字仍精準）

這批在 2026-08-12 量測時完全正確，**但正確只是暫時的**，留著就是留一個沒人會發現的漂移源：

| 位置 | 清點數 | 改法 |
|------|--------|------|
| `testing.md:257` | 「全 repo 有 **28 處** `[Collection]` 序列化」＋分佈表的「處數」欄 | 拿掉總數與處數欄，保留「Collection 名稱 → 保護對象」兩欄（那才是 agent 要的） |
| `testing.md:267` | 「**五個**名稱都有對應的 `CollectionDefinition`，零孤兒」 | 改「每個名稱都有對應的 `CollectionDefinition`」—— 不變的是這個不變量，不是名稱個數 |
| `testing.md:159` | 「兩者目前皆**零使用**」「`[DbTheory]` 同樣只有 **1 處**使用」 | 改「目前無使用者，但情境仍成立」；「別去 grep 它」的告誡本身留 |
| `serialization.md` | MessagePack `PackageReference`「只在 `Bee.Api.Core` **一處**」 | 改「只在 `Bee.Api.Core`」—— 位置是約束，處數不是 |

**不清**（同批出現但屬前表左欄）：`serialization.md`「全 repo 已無整數 `[Key]`」是狀態不變量、
`definition.md`「`CreateFormBO` / `CreateSystemBO` 在 `src/` 零 caller 是預期的、不是死碼」
存在的目的就是防止誤刪、`releasing.md`「版號只有這一個來源」是不變量。

### 2.3 執行時的一項自我更正：`ValueUtilities` 行數不該清

`code-style.md:140-145` 的 `ValueUtilities` 行數（501 → 637 → 拆出 `.Temporal` → 主檔 413）
原被 2.1 列為待清，實際動手時判定**不動**。兩個理由：

1. 它是**帶日期署名的歷史敘事**（「（bee-library，2026-08-12）」），講的是當天拆分前後量到
   什麼，不是宣告現況 —— 正好落在本階段判準的「歷史量測不受此限」。
2. 那些行數**是論證本身**：整段要說明的是「501 行時判不拆是錯的、長到 637 才動手」，
   而拆分閾值恰好是 500 行。拿掉數字，這段就不成立。

`.Temporal` 現在是 239 行而非文中的 224，屬「當時量測與事後略有出入」，不需回頭改。

**這一則值得留在 plan 裡**：它示範了機械套用「清掉所有數字」會誤傷什麼 ——
判別法始終是「這句在講現在是什麼，還是當時發生了什麼」，不是「有沒有數字」。

### 2.4 收尾掃描

清完後重跑同一道 grep，確認剩餘數字**全部**落在「該留」那欄：

- 歷史量測：`serialization.md` 的 37 → 185 筆、`public-docs.md` 三處帶日期的「14 處長期漏網」、
  `avalonia.md` 的 2026-07-09 升版敘事、`testing.md` 的 2026-08-04 窮盡掃描結果（4 個 vs 第 1 個
  的對比就是該段論證）
- 規格：`code-style.md` 的縮排 4 個空格 / XML 2 個空格
- 不變量：`testing.md` 的「零孤兒」
- 判別表的示例本身：`releasing.md` 用「37 → 185」示範「歷史量測該保留」，屬元層級示範

---

## 階段 3 — `apple-mobile-trim.md` 推導內容搬按需區

**已完成（2026-08-12）。** 裁決結果：新增 `docs/repo-ops/gotchas/mobile-trim-aot.md`
——`gotchas/` 目錄已存在且有 README 索引，加一份檔是沿用既有結構、不是新建結構。
`apple-mobile-trim.md` 13,288 → 6,797 字元（−49%），常駐總量 124,978 → 118,536。

`apple-mobile-trim.md` 是第三大常駐檔，其中約七成是實測數據、操作配方與歷史實驗表：

- § reflection-only 重現法的保真度與例外判讀 —— 實測數據 + 命令配方
- § Apple Release-mode trim 決策樹 —— 歷史實驗表 + 已否決路徑

動手前的障礙是 **`docs/repo-ops/gotchas/` 沒有行動端／AOT 的檔**：`database`／`serialization`／
`avalonia`／`testing` 都有對應 gotchas 出口，只有這條沒有。已新增
`mobile-trim-aot.md` 並補進 [`gotchas/README.md`](../../repo-ops/gotchas/README.md) 索引。

搬入內容：

1. § 已知不可行的「修法」歷史實驗表
2. § 可行修法 1–3 的已否決路徑與實測百分比（歷史量測，原值照搬）
3. § 3 / § 4 的完整 build 命令配方（Mac Catalyst、iOS 模擬器、NativeAOT）與 crash stack 特徵

常駐**留**（皆為判準或硬性要件，不可搬）：

- § Sandbox 與 IO
- § 序列化型別的行動端相容要件（硬性型別形狀要件）
- § 1 的「Android 保有 JIT，因此驗不到動態碼那半」判準
- § 2 的「例外種類不可當診斷依據，pass / fail 邊界才可以」判別法
- § 診斷雜訊（`2,2` 不是 XML 壞掉）
- 一行回歸閘門命令（`-p:DynamicCodeSupport=false`）
- § 4 採用解法（ILLink descriptor）的結論

**搬家是移動不是重寫**：原文照搬到目標檔，常駐區改成結論 + 指路。

---

## 階段 4 — `testing.md` 程式碼樣板搬按需區

`testing.md` 為最大常駐檔。兩節幾乎全是可直接貼用的程式碼樣板，
只在「要寫某類測試時」才需要：

- § 測試撰寫模式
- § 共享 fixture 檔案隔離

**已完成（2026-08-12），但目的地與原計畫不同。**

原計畫寫「已有現成的家：`gotchas/test-ci-release.md`」，動手時發現**不成立**：
`gotchas/` 的 README 明文自述「這不是規範文件」，且要求每則能回答
「症狀長什麼樣 / 根因 / 正解」三件事 —— 程式碼樣板不是踩雷紀錄，放進去會牴觸該目錄
自己的寫入原則。也查過其他候選：`tests/` 無 README、`docs/development-cookbook.md`
是**公開文件**（讀者是框架使用者，而這些樣板講的是本 repo 自家測試套件）。

改放 **`docs/repo-ops/testing-patterns.md`** —— `docs/repo-ops/` 依 `public-docs.md`
的定義就是「本 repo 的維運文件、與框架使用者無關」，扁平無索引檔，加一份即可。

**實際減量小於估計**：`testing.md` 22,151 → 19,125（−3,026，估計約 −5,000）。
差距是刻意的：只搬了程式碼區塊，決策表、env var 命名規則、fixture 選擇判準全部留在常駐區
——那些才是 agent 每次都要的。

**動作**：常駐留決策表（哪種測試用哪個 attribute／哪個 fixture、env var 命名規則），
完整程式碼樣板搬按需。

**不搬**：§ 本機跑測試前的環境檢查的「例外類型 → 容器」判別表與
`command -v docker` 適用性捷徑 —— 那些是判準，每個 session 都該知道。

---

## 階段 5 — P3：跨層錯置與示意路徑

**已完成（2026-08-12）。** 四項全部落地。前三項在使用者層（`~/.claude/`），
**不入本 repo 版控 —— 換機器就沒有**；第四項已搬進本 repo。

### `bee-jsonrpc-backend` 已搬入 `.claude/skills/`（使用者裁決）

判準是「拿掉 Bee.NET 這個框架，這份 skill 還剩下什麼？」——它會整份失效
（通篇 `AddBeeFramework` / `ApiServiceOptions` / `Define/` XML / BO 繼承），
其餘使用者層 skill 則照樣可用（`avalonia-*` 的 Bee 提及全是「拿 `tools/DefineEditor`
當參考實作」這類舉例，`macos-pdf-generation` / `maui-app-scaffold` 零提及）。

**真正的好處是取得版控**：`~/.claude` 本身不是 git repo，該 skill 先前完全沒有備份。
修正常駐錯置只是附帶效果——它的常駐成本只有 description 那段，本文與 `references/`
都是呼叫時才載。

**發佈前做過的事**（bee-library 是 public repo，搬入即公開）：

1. 讀完全部 5 個檔，確認無真實金鑰、無 email、無外部 host
   （`"xxx-dev"` 是佔位符；`Password = "demo"` 是該 skill 自己文件化的 demo 登入樣板；
   `http://www.w3.org` 是 XML 命名空間宣告）。
2. **中性化 4 處他專案代號**（`SKILL.md` 的 description 一處、三個 `references/` 檔的
   「驗證來源」各一處），改為「實際專案」。該 skill 本身的佔位符慣例就是 `Xxx`，改法一致。

### 順帶發現：與 `bee-app-scaffold` 觸發面重疊

兩者都吃「建一個 Bee 後端」。已在 `.claude/skills/README.md` 標明分界：要**從零搭出
能跑的 server + client 往返**選 `bee-jsonrpc-backend`；已有 host、要處理 **DB scope /
company context / seeder / 定義樹接線**選 `bee-app-scaffold`。

> 這是健檢步驟 5「觸發重疊」該抓到卻沒抓到的一筆 —— 原因是當時兩支 skill 分處
> 使用者層與專案層，我只在各自層內比對。**下次健檢應把兩層的 description 合併後再比對重疊。**

### 另一筆清點數字（階段 2 的漏網）

`.claude/skills/README.md` 寫 `bee-framework-review` 是「**九面向**唯讀審查」，
但該 skill 的 description 自述**十一面向**。已改為「多面向」不帶數字。
漏掉的原因同上：階段 2 只掃常駐區，`skills/README.md` 不在常駐區。

| 項 | 問題 | 處置 |
|----|------|------|
| `~/.claude/rules/code-style.md:211,246` | 註解範例引用 `docs/adr/0007-payload-pipeline.md`，本 repo ADR 慣例是 `adr-NNN-*.md`（無 `0007-` 形式） | 示意用途非真引用，但示範了錯誤的路徑形狀 → 改成慣例形式 |
| `~/.claude/rules/scanning.md` 末段 | 「已在 `security.md` 中定義的禁止事項」—— scanning.md 是**使用者層**（跨 repo 常駐），security.md 是**專案層**（僅 bee-library）；在其他 repo 下解不到 | 改為條件式敘述，或把該條內容收進 scanning.md 自身 |
| `bee-jsonrpc-backend` skill | 位於 `~/.claude/skills/`（使用者層 = 每個 repo 都載描述），內容完全是 Bee.NET 專屬，與專案層 `bee-*` skill 同族卻不同層 | 搬到 `.claude/skills/` |
| `maui-app-scaffold` skill | 與 `avalonia-mobile` 在「起一個行動 app」觸發面重疊；本 repo 已於 2026-07-28 移除 `Bee.UI.Maui` | 依「先修描述、不急著刪」，在描述末尾劃界 |

---

## 階段 6 — 週邊

**已完成（2026-08-12）。** 兩項皆已落地：plugin 三個 scope 現皆為 2.4.2；
`.gitignore` 已補 env 樣式並以 `git check-ignore` 驗證（`.env` / `.env.local` /
`*.local.env` / 巢狀路徑皆命中，`.env.example` 以 `!` 例外保留，且無既有追蹤檔被誤傷）。

### 6.1 soarcloud-libraries 的 plugin project scope 落後

註冊表現況（健檢執行時）：user scope 與 bee-library project scope 皆為 2.4.2；
**soarcloud-libraries project scope 停在 2.2.2** —— 缺 `config-audit`，
交接 skill 仍是舊名 `plan-handoff`。

`claude plugin update` 預設只更新 user scope，project scope 必須顯式指定，
且**得到該 repo 目錄下執行**：

```bash
claude plugin update dev-workflow@jeff377-plugins --scope project
```

開發 clone（`~/Desktop/repos/claude-plugins`）與 marketplace clone 同 commit、工作區乾淨
→ **前兩層無漂移**。cache 殘留的 `plan-workflow` 目錄與階段 1.1 同源。

### 6.2 `.gitignore` 未涵蓋 env 樣式

bee-library 是 **public repo**，但 `.gitignore` 無 `.env` / `*.local.env` 樣式。
若哪天要在本 repo 放同類機密檔，`git add -A` 會直接吃進去。

這條目前只存在於使用者的私有 memory —— 私有 memory 換機器就沒了、且平常看不到，
因此值得獨立補上 ignore 規則。

> memory 本身**不搬進 repo**：它跨兩個 repo、且含私有 repo 的本機 env 檔路徑，
> 屬使用者私有設定而非團隊知識。內容經重驗仍成立
> （`.claude/commands/sonar-fix.md` 用 `SONAR_TOKEN`；`.gitignore` 仍無相關樣式）。

---

## 動手時的約束

1. **逐項確認後才改**，不整批套用 —— 誤刪的規則不會報錯，只會讓未來每個 session 悄悄做錯。
2. **一類一次 commit**（階段 1 一批、階段 2 一批…），讓每筆改動可獨立回退。
3. **階段 3 / 4 的搬家是移動不是重寫** —— 順手改寫等於在沒有測試的情況下重構。
4. 動到有 `.md` / `.zh-TW.md` 雙版的文件時兩份都要改。
5. **不砍踩雷紀錄與自我更正**（「本節先前寫的是 X，已於 YYYY-MM-DD 推翻」這類）——
   它們是唯一阻止重蹈覆轍的東西。真要精簡時先問能不能搬，再問能不能刪。
6. **階段 2 只動數字、不動論證** —— 拿掉「36 個」不等於拿掉「純屬性袋沒有接縫所以不拆」。

---

## 階段 7 — 砍不改變行為的散文（追加）

**已完成（2026-08-12）。** 追加原因：階段 3／4 是**搬家**，動的是位置不是文字量
（累計只降 7.4%）。本階段改為在原地砍散文。

判準延續階段 2：**這段在下一個 session 會不會改變 agent 的行為？** 三類東西被砍：

| 類型 | 例 |
|------|----|
| **章節框架**（標題＋表格外殼，規則本身只有一行） | `sonarcloud.md` 的 13 個編號小節 |
| **操作配方**（只在做某件事時才需要，且該是可執行的） | `public-docs.md` 的 5 道 grep、`testing.md` 的窮盡掃描命令 |
| **重複示範同一原則的範例** | `code-style.md` 註解規範的三組 ✅/❌ |
| **已修 bug 的過程敘事**（結論留、過程砍） | `commit-verification.md` 的 cwd 判定史 |

### 各檔結果

| 檔 | 前 | 後 | 做法 |
|----|----|----|------|
| `public-docs.md` | 8,043 | 4,750（−41%） | 5 道 grep → 新增 `check-public-docs.sh`（可執行、輸出已驗證與原檢查一致）；「哪些檔何時漏網」的考古清單壓成「別自行縮減範圍，理由在腳本檔頭」 |
| `sonarcloud.md` | 6,885 | 4,018（−42%） | 13 個編號小節攤平成單一表格，**35 個規則 ID 全數保留**；只留 Regex／GH Actions／S125 三處需要字面形式或判斷流程的內容 |
| `testing.md` | 19,125 | 17,087 | 環境檢查的 rationale 散文壓縮（適用範圍 4 段 → 1 句 `command -v docker`）；窮盡掃描命令搬進 `gotchas/test-ci-release.md` |
| `definition.md` | 6,469 | 5,901 | BO 介面的三段歷史註記壓縮，**三則自我更正全部保留** |
| `commit-verification.md` | 3,693 | 3,268 | hook-vs-條文 的論證與 cwd 判定的 bug 史壓成結論 |
| `~/.claude/rules/code-style.md` | 18,076 | 17,210 | 註解規範的重複範例組合併 |

**小計 −10,057 字元。常駐 124,978 → 105,621（−15.5%，約 35k → 30k tokens）。**

### 沒有砍的

- **判準與判別法**：一律留（它們處理指令沒列舉到的情境）。
- **自我更正**（「本節先前寫的是 X，已於 YYYY-MM-DD 推翻」）：一律留。
  `serialization.md` 的 MessagePack fallback 那段明寫「留著這段是因為它示範了一個反覆出現的
  錯法」，作者已預期有人想刪它。
- **誤報清單**：`public-docs.md` 的 (3) 誤報判別表原樣保留 —— 砍掉會讓後續每次執行重踩。
- **歷史量測**（37 → 185 筆、砍 57%）：紀錄當時量到什麼，不是現況宣告。

> **搬去腳本比搬去文件好**：`check-public-docs.sh` 是可執行的，跑一次就知道有沒有壞，
> 而抄在規則裡的 grep 只會靜默過期。**本次已驗證腳本輸出與原本 5 道 grep 完全一致。**

---

## 階段 8 — 路徑限定規則下沉（追加）

**狀態：✅ 已完成（2026-08-12）。** 巢狀 `CLAUDE.md` 的 lazy loading 已由**頂層 session**
實測確認，`testing.md` 的下沉（commit `b412f591`）保留。

### 已驗證：巢狀 `CLAUDE.md` 是 lazy loading（頂層 session 覆核，2026-08-12）

先由 subagent 實測、再由頂層 session 覆核，兩者結論一致。頂層覆核的原始觀察：

| 步驟 | 觀察 |
|---|---|
| **1. 尚未呼叫任何工具時** | claudeMd 區塊列出 18 個來源（使用者層 `CLAUDE.md` + 4 支使用者層 rules、專案 `CLAUDE.md` + 12 支 `.claude/rules/`、`MEMORY.md`），**`tests/CLAUDE.md` 不在其中**；字串「測試規範（完整）」不在 context 內（常駐區只有下沉後的「測試規範（骨幹）」） |
| **2. 僅 Read `tests/Bee.Tests.Shared/BeeTestFixture.cs` 之後** | **出現新的 system-reminder**，內容為 `Contents of /Users/jeff/Desktop/repos/bee-library/tests/CLAUDE.md`，全文注入；「測試規範（完整）」自此在 context 內 |

對應判讀表第二列（步驟 1 沒有 / 步驟 2 有）→ **lazy loading 成立**，下沉有實際效益且規則不會
靜默消失。**這條結論現在是頂層 session 的直接觀察，不再是 subagent 代理。**

> subagent 的 claudeMd 清單與頂層一致，事後看是可信代理 —— 但當時無從得知，
> 覆核的成本（一次 Read）遠低於賭錯的代價（17k 規則靜默消失）。

### 未驗證：Write 新檔是否也觸發（本 session 無法乾淨重測）

原假設是「lazy 載入在 **Read** 該目錄下的檔案時觸發」，那麼「只 Write 一個新檔、
沒 Read 任何既有檔」會不會觸發？**此步在本 session 無法乾淨驗證** —— 步驟 2 已經觸發過載入，
`tests/CLAUDE.md` 已在 context 內，之後 Write `tests/_probe_write.cs`（已刪除）
未再出現新的 system-reminder，但**這不構成證據**：已載入的檔本來就不會重複注入，
「不觸發」與「觸發但不重複注入」在此無法區分。

**因此常駐區那句保險必須留**：`.claude/rules/testing.md` 開頭的
「要動 `tests/` 下任何檔案前，先 Read `tests/CLAUDE.md`」不可移除。
要乾淨驗證得在**全新 session 的第一個動作**就 Write，且全程不 Read `tests/` 下任何既有檔。

### `testing.md` 的切分結果

| | 字元 | 內容 |
|---|---|---|
| `.claude/rules/testing.md`（常駐） | 17,087 → **2,564** | 五條「動筆前必須知道」的硬約束 + 命名規則 + 指路 |
| `tests/CLAUDE.md`（lazy） | **13,903** | 環境檢查、attribute／fixture 完整判準、平行安全細節、fixture 檔案隔離、analyzer 退件、「本機綠 CI 紅」三條 |

常駐總量 105,621 → **91,098**（自基線 124,978 累計 **−27.1%**，約 35k → 26k tokens）。

### 問題

專案層 rules 共 67,366 字元，其中 **52,340（77%）是路徑限定**的：

| 路徑限定 | 適用範圍 |
|---|---|
| `testing.md` 17,087 | `tests/` |
| `serialization.md` 8,960 | `Bee.Api.Core`、`Bee.Definition` |
| `apple-mobile-trim.md` 6,797 | 行動 head |
| `definition.md` 5,901 | `Bee.Definition` |
| `avalonia.md` 4,825 | 4 個 Avalonia 頭（`src/`、`tools/`、`samples/`、`apps/`） |
| `database.md` 4,505 | `Bee.Db`、`Repository*` |
| `dependency-boundary.md` 4,265 | 三個受管 csproj |

真正跨層的只有 `sonarcloud.md`、`security.md`、`commit-verification.md`、`public-docs.md`。
一個純改文件的 session 白讀約 15k tokens。

### 三種機制與取捨

| 機制 | 精準度 | 風險 |
|------|-------|------|
| 巢狀 `CLAUDE.md` | 高（按路徑） | **載入時機未經驗證**（見下）；一個檔只綁一棵樹，`avalonia.md` 適用四處分散位置 |
| Skill | 中（語意觸發） | **漏觸發等於沒有規則** —— skill 適合「要做某件事」的流程，不適合「不准這樣做」的約束 |
| `PreToolUse` hook 比對 glob 注入 | 最高（動筆那一刻） | 要寫腳本，但**機制在本 repo 已證實可用**（`pre-commit-verify.sh`） |

### 關鍵設計：不是整檔搬走，而是「檔內再分一層」

這些檔混了兩種東西，只有後者可以下沉：

- **動筆前必須知道的設計約束**（「集合屬性一律繼承 `KeyCollectionBase`」、「wire 型別一律顯式
  註冊」、「`CategoryId` 只認三值」、「文字數值欄一律 NOT NULL」）——
  **晚載入就沒用了**，因為讀到檔案時設計已經選錯。但它們也是最短的部分，每支約 3–8 行。
- **動到才需要的踩雷細節與操作程序** —— 佔絕大多數，可下沉。

粗估常駐可從 30k tokens 降到 12k–15k。**`testing.md` 一支佔一半效益且時機風險最低**
（寫測試必定會讀寫 `tests/` 下的檔），建議先只做它。

### 為何要先驗再搬（賭錯的代價不對稱）

驗證前的三種可能後果差距極大，這是「先驗再搬、且不接受 subagent 代理」的理由：

| 若機制是 | 後果 |
|---|---|
| 啟動即載全文 | 省 0，無害 |
| **啟動發現、觸及才載** | **省 17,087 字元 ✅（實測即此）** |
| 只認 root `CLAUDE.md` | 17k 測試規則靜默消失，不報錯，只讓未來每個寫測試的 session 少掉全部規則 |

第三種是危險情況：**規則會靜默失效、不報錯**。實測排除了它。若當時結果是第一或第三種，
處置都是 `git revert b412f591`；第三種另需改走 `PreToolUse` hook（機制在本 repo 已證實可用，
精準度反而更高 —— 動筆那一刻才注入，而非讀檔時）。

### 其餘六支路徑限定 rules：另案，不在本階段

`serialization.md`、`apple-mobile-trim.md`、`definition.md`、`avalonia.md`、`database.md`、
`dependency-boundary.md`（共約 35k 字元）**本階段不動**，待使用者確認 `testing.md` 這支
下沉後的實際使用感受再逐支處理。

已知的未解問題（處理前必須先有結論，不可自行決定）：**`avalonia.md` 適用四個分散位置**
（`src/Bee.UI.Avalonia`、`tools/DefineEditor`、`samples/Avalonia.*`、`apps/Bee.Northwind`），
一個巢狀 `CLAUDE.md` 綁不了四棵樹 —— 複製四份會產生同步負擔，只放一處會漏。

---

## 階段 9 — 其餘路徑限定 rules 下沉

**狀態：🚧 四支已下沉，兩支判定不該下沉。**

階段 8 確認機制可用後逐支處理。**關鍵發現：`tests/` 是特例** —— 一個目錄、一種讀者、
內容自足。其餘 rules 沒有一支這麼乾淨，必須逐節判 audience，且**有兩支判定為不該下沉**。

### 已下沉

| 來源（常駐） | 前 → 後 | 下沉到 | 搬了什麼 |
|---|---|---|---|
| `serialization.md` | 8,960 → **4,216** | `src/Bee.Api.Core/CLAUDE.md`（6,144） | wire formatter 註冊程序、三個誤判點、`object` 判別式封套、AOT 實測與歷史自我更正、回歸閘門、ctor 順序歷史 |
| `definition.md` | 5,901 → **1,710** | `src/Bee.Definition/CLAUDE.md`（4,308）＋`src/Bee.Business/CLAUDE.md`（2,322） | 集合屬性繼承基底、欄位參照命名、`Defaults/` 定位、集合型別的行動端形狀要件 ／ BO 介面判準 |
| `avalonia.md` | 4,825 → **2,224** | `src/Bee.UI.Avalonia/CLAUDE.md`（3,147） | Semi 主題、UI 架構定位、控件驗收基準、控件踩雷指路 |
| `apple-mobile-trim.md` | 6,797 → **6,244** | （併入 `src/Bee.Definition/CLAUDE.md`） | 集合型別形狀三要件的完整條文 |

**順帶修正一處歸屬錯誤**：BO 介面判準（`IBusinessObject` / `ISystemBusinessObject` /
`IFormBusinessObject`）原本記在 `definition.md`，但那些介面全在 **`src/Bee.Business`**。
已歸位到 `src/Bee.Business/CLAUDE.md`。

### 判定不該下沉（留常駐，附理由）

| 檔 | 為何不下沉 |
|---|---|
| `database.md`（4,505） | 內容是**跨層的**：`CategoryId` 只認三值 → 定義檔作者；NOT NULL 加欄 checklist → TableSchema + 所有 INSERT + seed + 測試；round-then-sum → Repository 寫入層。而「明細加總＝總合」是**財務不變量**，選錯代價是帳目對不上。實測 `decimal.Round` 目前只出現在 `Bee.Definition`，而規則是**規定 Repository 未來要做** —— audience 更散。 |
| `dependency-boundary.md`（4,265） | 適用面是三個受管 csproj（`Bee.Base` / `Bee.Definition` / `Bee.Api.Contracts`）**分處三個目錄**，一個巢狀檔綁不了。而且 `BEE9001` 建置期鎖已經硬性擋下違反 —— 晚載入無所謂（你會先撞到編譯錯誤再來讀），下沉的效益比風險小。 |
| `apple-mobile-trim.md` 其餘（6,244） | 剩下的是判準（「Android 驗不到動態碼那半」「例外種類不可當診斷依據」）與診斷雜訊，而 audience 橫跨 `src/Bee.UI.Avalonia`（iOS TFM）與 `apps/Bee.Northwind/*.iOS|.Android` **兩棵樹**。這些雷的症狀是**裝置上靜默失敗**，漏載代價高。 |

### 判準（供下次沿用）

**不是「這個檔講哪個專案」，而是「違反這條規則的人會在哪個目錄工作」。**
兩者常常不同 —— `definition.md` 的 cache immutability 講的是定義層物件，
但違反者是 BO / Repository / UI 的作者，所以它留常駐。

---

## 階段 10 — 使用者層設定進版控 + 最小化

**已完成（2026-08-12）。**

### 先修一個安全網缺口：`~/.claude` 原本不是 git repo

使用者說「若造成語意偏移，再由 git 回推歷程找回」時，**那個安全網對使用者層並不存在** ——
專案層 `.claude/rules/`（11 支）在 bee-library 的 git 內，但使用者層的 `CLAUDE.md` 與
`rules/`（4 支，含最大的 `code-style.md`）在 `~/.claude`，而它**不是任何 repo**。
`bee-library/.claude/rules/` 裡也沒有 `code-style.md` —— 它是經
`@~/.claude/rules/code-style.md` 從家目錄 import 的。

已 `git init ~/.claude`，採**白名單式 `.gitignore`**：預設排除所有內容，只放行
`CLAUDE.md`、`rules/`、`skills/`、`scripts/`。**刻意排除** `projects/`（session 逐字稿）、
`plugins/`（marketplace 與 cache）、`settings*.json` 與任何憑證；staged 前逐一確認過。
無 remote，純本機歷程。

**第一個 commit 刻意是「壓縮前」狀態**，所以本階段的每一次精簡都能 diff 與回推
（已驗證 `git show HEAD~1:rules/code-style.md | wc -c` = 17,210 = 原始大小）。
順帶把 5 支使用者層 skill（約 90k 字元，同樣先前零備份）一併納入。

### 精簡結果

| 檔 | 前 → 後 | 做法 |
|---|---|---|
| `~/.claude/rules/code-style.md` | 17,210 → **12,059**（−30%） | 13 個小節的重複鋪陳改緊湊敘述；**`CA1724`（BCL 撞名）與 UI 框架撞名合併為一節** —— 兩者是同一類問題，差別只在 analyzer 會不會告訴你；XML doc 的兩段範例壓成一句敘述 + 一個 `<remarks>` 實例 |
| `~/.claude/rules/releasing.md` | 8,153 → **6,483** | `PublicAPI` 合併的 30 行 bash 抽成 `~/.claude/scripts/merge-public-api-shipped.sh`；§2 單一來源的敘事壓縮 |
| `.claude/CLAUDE.md` | 5,869 → **5,067** | `./test.sh` 的容器偵測細節併入 `tests/CLAUDE.md`（原本兩處重複）；plan 內連結慣例改指 `plan-write` skill（已確認該 skill 確實承接，非空指路） |

**保留的判準**：「比對身分還是呈現給人」、「貼到 UI 專案會不會需要 `using` alias」、
「這個型別的成員本質上是同一份清單」、「這個版號在講現在是哪一版還是當時量到什麼」、
Turkish-I 的具體症狀、`ValueUtilities` 帶日期署名的歷史敘事（含行數 —— 那些是論證本身）、
以及兩個實際踩過的版號漂移教訓（`CLAUDE.md` 落後六個 minor、`tools/Bee.Cli` 停在 4.8.0
十二個 minor 且 per-project 閘門擋不到）。

> **抽成腳本比抄在規則裡好** —— 抄在文件裡的 bash 只會靜默過期；抽成可執行檔至少跑得起來、
> 改一處。`merge-public-api-shipped.sh` 的檔頭寫進了兩個容易寫錯的細節
> （`LC_ALL=C` 排序讓 `~override` 排對位置、`grep -Fxv -f` 對空移除清單會濾掉全部所以要兜底）
> 與 `RS0024` 的成因。

### 全案最終數字

| | 字元 | tokens |
|---|---|---|
| 健檢基線 | 124,978 | ~35k |
| 最終 | **71,386** | **~20k** |
| | **−42.9%** | |

零知識損失 —— 每一項都是「移到只在需要時才載入的位置」、「抽成可執行腳本」，
或「砍掉不改變行為的字」。

---

## 階段 11 — 去除「可執行檔行為說明」的複寫（使用者指出）

**已完成（2026-08-12）。** 由使用者指出：`CLAUDE.md` 裡那段 `./test.sh` 的註解「應該寫在
`test.sh` 中即可」。查證後發現**它本來就寫在 `test.sh` 檔頭**（容器偵測、自動 skip、
env var override 連預設值都有），CLAUDE.md 那段是逐字重複 —— 而階段 10 把它搬到
`tests/CLAUDE.md` 是**製造第二份重複**，也一併清掉。

### 新原則：可執行檔的行為說明寫在該檔案裡，規則只放判準

| 檔 | 前 → 後 | 複寫的對象 |
|---|---|---|
| `.claude/CLAUDE.md` | 5,869 → **5,067** | `test.sh` 檔頭 |
| `tests/CLAUDE.md` | −362 | 同上（階段 10 誤植於此） |
| `.claude/rules/commit-verification.md` | 3,268 → **1,851** | `pre-commit-verify.sh` 檔頭 |

`pre-commit-verify.sh` 的檔頭已寫明「兩項檢查為何不對稱」「為何 `--no-incremental`」
「必須 fail open」「其他 repo 的 cwd 如何判定」。規則檔只留**腳本裡沒有、agent 才需要的**四條：
build 失敗會擋、`PublicAPI` 異動須說明相容性判定、**不得為了通過而改測試或 src**、
`--no-verify` 對它無效。前三條是行為要求，第四條是容易誤解的事實。

**單一來源的落實**：容器名（`sql2025` / `pgvector-db` / `mysql8` / `oracle23ai`）
現在只存在 `test.sh`，`.claude/` 與 `tests/CLAUDE.md` 皆不複寫 —— 已 grep 驗證。
`tests/CLAUDE.md` 的「例外類型 → 容器」表改為對映 **DB 類型**而非容器名，
表本身的價值（哪種例外該懷疑哪個 DB）不受影響。

> 與階段 7 抽 `check-public-docs.sh`、階段 10 抽 `merge-public-api-shipped.sh` 是同一條原則的
> 三種形態：**能執行的東西不要抄進規則**。抄進去的只會靜默過期，而且會出現「規則說一套、
> 腳本做另一套」而沒有任何機制發現。

常駐總量 71,386 → **69,969**（自基線 124,978 累計 **−44.0%**）。

---

## 下次健檢的三項改進（本次執行暴露的方法缺口）

本次有三筆問題**是健檢該抓到卻沒抓到、動手做別的事時才順手發現的**。缺口都在
「掃描範圍」而非判準，記下來以免下次重蹈：

1. **把 `.claude/` 下所有 README 納入掃描範圍。** 健檢的失效引用檢查與清點數字檢查
   都只掃**常駐區**（`CLAUDE.md` + `rules/`），而 `.claude/skills/README.md` 不在常駐區
   —— 於是它的 `plan-handoff` 舊名（階段 1.3）與「九面向」清點數（階段 5）雙雙漏網。
   **它不常駐但它是 agent 會讀的權威索引**，同樣會誤導。
2. **把使用者層與專案層的 skill description 合併後再比對觸發重疊。** 本次是分層各自比對，
   因此沒看出 `bee-jsonrpc-backend`（當時在使用者層）與 `bee-app-scaffold`（專案層）
   都吃「建一個 Bee 後端」。**agent 看到的是合併後的單一清單，比對就該在合併後做。**
3. **「重跑量測比對數字」那一步可以大幅縮減，改驗「有沒有新增清點數字」。**
   階段 2 已把清點數清掉，該步驟原本的工作量（逐條重跑 grep 對數字）幾乎歸零。
   新的檢查是反向的：**grep 有沒有人又寫回清點數字**。移除漂移源優於定期偵測漂移。

> 前兩項是本 repo 執行時的範圍設定問題，可自行補；第三項屬 `config-audit` skill 本身的
> 步驟 2 該調整，要動得回 plugin repo。

---

## 已檢查且乾淨（下次健檢不必重看）

以下為 2026-08-12 的檢查結果，**屬當時的量測紀錄**，非需維護的現況宣告。

**引用完整性**

- `@import`：專案 `CLAUDE.md` 的 `@rules/*` 全部解得到；`.claude/rules/` **零孤兒**
  （無未被 import 的規則檔）
- 失效路徑：全部常駐檔僅 3 筆命中，**全為 markdown 示意區塊**（誤報）
- `.claude/CLAUDE.md` 引用的所有路徑皆存在

**規則自述的落地檢查**

- `public-docs.md § 落地檢查` 5 段全部重跑 → **100% 符合自述預期**
  （(1) 只剩 `docs/README*.md` 的性質說明；(2)(4)(5) 無輸出）
- `gotchas/README.md` 索引覆蓋全部 gotchas 檔

**不變量成立**

- `testing.md` 的 `[Collection]` 名稱**每個都有對應 `CollectionDefinition`**（零孤兒）；
  `ClientInfoState` 的使用點全在 `Bee.UI.Core.UnitTests` 同組件內，無跨組件隱式分組
- Phase 5 宣稱已移除者實際碼中皆已不存在（`[Collection("Initialize")]`／`GlobalFixture`／
  `BaseTests`／`BeeTestServices`／`TempDefinePath`／`DefinePathInfo`／`CacheContainer`），
  僅存於 XML 註解的歷史提及
- `serialization.md`：整數 `[Key]` 全 repo 已無；`BEE4004` 原始碼已無（僅殘留於未清理的
  `bin`/`obj` 產物）；MessagePack `PackageReference` 只在 `Bee.Api.Core`；
  `CreateSerializer` switch 只有 `messagepack` 一 case
- `dependency-boundary.md`：`BEE9001` 在 `src/Directory.Build.targets`；
  `DefinitionDependencyGateTests.cs` 存在；受管專案 Base／Definition／Api.Contracts 皆在
- `apple-mobile-trim.md`：`LanguageEnum.Entries` 已具「清空後逐一 `Add`」的 public setter
- `definition.md`：`ILogBusinessObject`／`CreateLogBO` 已移除；
  `CreateFormBO`／`CreateSystemBO` 在 `src/` 確實零 caller（僅定義 + XML doc 提及）
- `commit-verification.md`：hook 腳本存在、`.claude/settings.json` 有 `hooks` 區塊
- `releasing.md` 單一版號來源：全 repo 只有 `Version.props` 有 `<Version>`，
  `src/` 與 `tools/` 兩個 `Directory.Build.props` 皆 import；`.claude/CLAUDE.md` 只指路不複寫

**Skill**

- 全部 skill 的 `name` 與目錄名一致、皆有 `description`（零 frontmatter 問題）

**跨檔分工（邊界宣告仍成立）**

- `scanning` ↔ `sonarcloud`（例外處理）
- `code-style` ↔ `sonarcloud`（S125）
- `testing` ↔ `pull-request`（CI 失敗處理）
- `testing` 檔內（並行 flaky 的兩種判定，已自述不衝突）
- `apple-mobile-trim` ↔ `serialization`（trim 半 A／wire 半 B，line 65 的邊界宣告成立
  —— 唯 line 225 的摘要過期，見階段 1.2）
