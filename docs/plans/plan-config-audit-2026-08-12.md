# 設定檔健檢（2026-08-12）

**狀態：🚧 進行中（2026-08-12）**

| 階段 | 範圍 | 狀態 |
|------|------|------|
| 1 | P0：正在誤導 agent 的失效引用（plugin 名、skill 舊名、AOT 結論矛盾） | ✅ 已完成（2026-08-12） |
| 2 | 清除常駐設定內的清點數字，改定性描述（含已漂與尚未漂者） | ✅ 已完成（2026-08-12） |
| 3 | `apple-mobile-trim.md` 推導內容搬按需區（新建 `gotchas/mobile-trim-aot.md`） | ✅ 已完成（2026-08-12） |
| 4 | `testing.md` 程式碼樣板搬按需區（新建 `docs/repo-ops/testing-patterns.md`） | ✅ 已完成（2026-08-12） |
| 5 | P3：跨層錯置與示意路徑（scope、skill 歸屬、ADR 路徑形狀） | 📝 待做 |
| 6 | 週邊：soarcloud-libraries plugin scope 補更新、`.gitignore` 補 env 樣式 | 📝 待做 |

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

但 [`serialization.md`](../../.claude/rules/serialization.md) 的節標題就是
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

問題是 **`docs/repo-ops/gotchas/` 目前沒有行動端／AOT 的檔**。`database`／`serialization`／
`avalonia`／`testing` 都有對應 gotchas 出口，只有這條沒有 —— 它是唯一沒有按需家可搬的規則。

**待裁決**：是否新增 `docs/repo-ops/gotchas/mobile-trim-aot.md`（並補進
[`gotchas/README.md`](../repo-ops/gotchas/README.md) 索引）。

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

| 項 | 問題 | 處置 |
|----|------|------|
| `~/.claude/rules/code-style.md:211,246` | 註解範例引用 `docs/adr/0007-payload-pipeline.md`，本 repo ADR 慣例是 `adr-NNN-*.md`（無 `0007-` 形式） | 示意用途非真引用，但示範了錯誤的路徑形狀 → 改成慣例形式 |
| `~/.claude/rules/scanning.md` 末段 | 「已在 `security.md` 中定義的禁止事項」—— scanning.md 是**使用者層**（跨 repo 常駐），security.md 是**專案層**（僅 bee-library）；在其他 repo 下解不到 | 改為條件式敘述，或把該條內容收進 scanning.md 自身 |
| `bee-jsonrpc-backend` skill | 位於 `~/.claude/skills/`（使用者層 = 每個 repo 都載描述），內容完全是 Bee.NET 專屬，與專案層 `bee-*` skill 同族卻不同層 | 搬到 `.claude/skills/` |
| `maui-app-scaffold` skill | 與 `avalonia-mobile` 在「起一個行動 app」觸發面重疊；本 repo 已於 2026-07-28 移除 `Bee.UI.Maui` | 依「先修描述、不急著刪」，在描述末尾劃界 |

---

## 階段 6 — 週邊

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
