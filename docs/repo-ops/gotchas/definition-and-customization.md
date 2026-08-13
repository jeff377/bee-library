# 踩雷誌：定義層與客製覆蓋層

對應硬規則見 `.claude/rules/definition.md`；定義型別的設計規範見 `src/Bee.Definition/CLAUDE.md`。

## 「客製範圍有幾種」有兩種數法，而文件各用各的 → 同一個漏連踩三次

**症狀**：想知道客製覆蓋層到底服務哪些定義，翻文件會拿到三個互相對不上的答案，而且每一個
看起來都很肯定。實際踩到的三處（全部在 2026-08-12 至 08-13 之間陸續查出）：

| 文件 | 當時寫的 | 漏了什麼 |
|------|---------|---------|
| `docs/adr/adr-016-*.md` 的 2026-08-06 修訂表 | 「現行**五類**」 | `MenuSettings` 整個不在表上 |
| `docs/customization.md` / `.zh-TW.md` 的「該用哪一種」表 | 五列 | 同上 |
| `docs/definition-files-overview.md` / `.zh-TW.md` §7 | 「**四種型別**，三種粒度」 | `PluginSettings`，也就是唯一「相加」的那種粒度 |

三處漏的**不是同一項**，所以交叉比對也對不出來：前兩處漏 `MenuSettings`，第三處漏
`PluginSettings`。

**根因不是誰粗心，是「客製範圍」本來就有兩種數法，而三處各用各的、又都只寫一個數字**：

- 以**定義檔**計 → **五份**：`Language` / `FormLayout` / `ProgramSettings` / `PluginSettings` /
  `MenuSettings`。**這個數字是穩定的**，因為它就是 `ICustomizeDefineReader` 的方法數。
- 以**「想改什麼」**計 → 數字取決於切多細：`ProgramItem` 的兩個綁定（`BusinessObject` /
  `Repository`）各自獨立算兩件，`Language` 的文字與選項集又是兩種粒度算兩件。
  同一套機制因此可以被說成五件、六件或七件，**每一種都不算錯**。

於是「五類」在 ADR 裡指的是意圖、在別處可能指的是檔案，兩邊都自稱五而內容不同，
**看起來一致反而讓漏掉的那一項更難被發現**。

**正解**：

1. **要查客製範圍，看原始碼不看文件。** 權威來源是
   `src/Bee.Definition/Customization/CustomizeOverlay.cs` 的 XML doc（逐條列出粒度與理由）
   與 `src/Bee.Definition/CustomizeOnlyPathOptions.cs`（自陳「the override layer serves only
   those five types」）。這兩處比所有文件都精確。
2. **文件寫數字時必須指明數的是檔案還是意圖。** 只寫「五類」不寫數法，就是下一次漏掉一項
   而沒有人發現的原因。
3. **往覆蓋層加第六種定義時，要同步的是三個地方**（ADR-016 的現況表、`customization` 雙語的
   「該用哪一種」表、`definition-files-overview` 雙語的 §7 表），而不是只改離你最近的那一份。

**已修**：`7160afd2`（補 `PluginSettings`）、`c4014ee5`（補 `MenuSettings`）。
**殘留的注意事項**：三處表格仍然各用各的數法，只是現在都標明了是哪一種；
**沒有任何機制保證它們與 `ICustomizeDefineReader` 同步**，加第六種時仍然只能靠人記得。

## 客製覆蓋層的粒度不是「越細越好」，`PluginSettings` 是唯一相加的那種

容易誤以為粒度是實作方便度的結果，實際上分界線是**這份東西的性質**
（理由完整寫在 `CustomizeOverlay` 的 XML doc）：

| 性質 | 粒度 | 哪些 |
|------|------|------|
| 一袋彼此獨立的值 | key 級疊加 | `Language` 的文字 |
| 一個組合起來才成立的整體 | 整檔／整組取代 | `FormLayout`、`MenuSettings`、`Language` 的選項集 |
| 一組彼此獨立的綁定 | progId 級再分屬性級 | `ProgramSettings` |
| **一條依序執行的鏈** | **progId 級相加** | **`PluginSettings`** |

`PluginSettings` 之所以自成一類，是因為它兩者都不是：plugin 本來就是「加一段」而不是
「取代一段」，所以套裝鏈先跑、客製鏈接著跑，兩層不互斥。**這也是它最容易在文件裡被漏掉的
原因**——它不符合「挑一個」那個心智模型，寫表格的人數到「粒度有幾種」時會漏掉它。

⚠️ **連帶的一條**：`PluginSettings` 同時是**唯一可寫的客製定義**（`LocalOnly` 維護 API），
客製層其餘一律唯讀。所以「客製層唯讀」這句話在引用時要加上例外，否則會與維護 API 的存在矛盾。

## `FormSchema` 中樞圖畫三個層，但「資料庫」那一格不只有 `TableSchema`

**症狀**：`definition-files-overview` §2 的圖把 `FormSchema` 的下游畫成
`FormLayout` / `TableSchema` / 規則，而**正下方的「對資料庫」那一條講的是「執行期依
`FormSchema` 產生 SQL」**。圖畫的是結構定義，內文講的是執行期存取，兩者不是同一件事，
讀者對不起來。

**根因**：`FormSchema` 往資料庫那個方向有**兩種**衍生，時機完全不同：

- **scaffold 期**：由 `FormSchema` 產出一份 `TableSchema`（之後它就是獨立的定義檔，不再跟著變）
- **執行期**：每次請求依 `FormSchema` 現組 SQL（沒有 ORM、沒有 entity 類別）

把兩者塞進同一格而只寫前者，會讓框架最有特色的那一半（執行期組 SQL）從圖上消失。

**已修**：`7160afd2`，該格改為 `TableSchema ＋ 執行期 SQL`，說明列改為「存在哪裡 · 怎麼進出」。

⚠️ **已複驗過而不必動的一處**：`docs/architecture-overview.*` §11 的整體架構圖也是
`FormSchema → FormLayout / TableSchema` 兩節點，乍看是同一個問題，**實際上不是**——
那是一張**分層**架構圖，SQL 產生掛在下方 Repository 那一層
（「FormSchema-driven（CRUD SQL 自動產生）」），歸屬正確。
記在這裡是為了避免下次有人看到那兩個節點又重開一次同樣的檢查。
