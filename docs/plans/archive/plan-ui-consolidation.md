# 計畫：UI 專案收斂為 Avalonia + Blazor.Server 雙軌

**狀態：✅ 已完成（2026-07-28）**

| 階段 | 範圍 | 狀態 |
|------|------|------|
| 1 | 移除 `Bee.UI.Maui` 與 `Bee.Web.Blazor.Wasm`（含 tests / samples / CI / slnx） | ✅ 已完成（2026-07-28） |
| 2 | MAUI trim/AOT 知識保留：`rules/maui.md` 重新定位為 Apple 平台通用 | ✅ 已完成（採方案 A，更名 `apple-mobile-trim.md`） |
| 3 | 文件同步（dependency-map、README 專案數、samples 導覽、CHANGELOG breaking） | ✅ 已完成（2026-07-28） |
| 4 | 收斂 `FormDataObject` 重複 | ✅ 已完成（改採雙向註解，理由見下） |

## 背景

`Bee.UI.Avalonia` 以單一 `net10.0` UI 專案 + 各平台 head 的結構，已實際覆蓋
desktop / iOS / Android / WASM 四端（`apps/Bee.Northwind` 有全部四個 head）。

**唯一不被涵蓋的是 `Bee.Web.Blazor.Server`** —— 它是純 Web 專案，提供 Avalonia 做不到的兩件事：

| | Avalonia Browser | Blazor.Server |
|---|---|---|
| 渲染 | canvas（Skia）—— 一張畫布 | 真 HTML DOM |
| SEO / 嵌入既有網頁 / 螢幕閱讀器 | ✗ | ✓ |
| 首次載入 | 需下載 .NET runtime | 免下載（伺服器渲染） |

投入落差也支持這個收斂（2026-05 起的 commit 數）：

| 專案 | 行數 | 控件覆蓋 | commit 數 |
|------|-----:|---------|----------:|
| `Bee.UI.Avalonia` | 6,621 | 20 個編輯器 + View 層 | **82** |
| `Bee.UI.Maui` | 1,417 | 3 檔 | 16 |
| `Bee.Web.Blazor.Server` | 1,245 | 5 元件 | 10 |
| `Bee.Web.Blazor.Wasm` | 1,196 | 5 元件（與 Server 版 950 行逐位元相同） | 9 |

`Bee.Web.Blazor.Wasm` 處在被兩邊夾殺的位置：與 Avalonia Browser 同樣是 WASM、同樣要下載
runtime，唯一差異是 DOM vs canvas；而它又與 `Bee.Web.Blazor.Server` 有 950 行完全重複。

## 階段 1：移除兩個專案

| 類別 | 路徑 | 行數 |
|------|------|-----:|
| src | `src/Bee.UI.Maui`、`src/Bee.Web.Blazor.Wasm` | 2,613 |
| tests | `tests/Bee.UI.Maui.UnitTests`、`tests/Bee.Web.Blazor.Wasm.UnitTests` | 4,408 |
| samples | `samples/Maui.Demo`、`samples/Blazor.Wasm.Demo`、`samples/Blazor.Wasm.Demo.Host` | — |

連帶必改（漏一項就會出現「publish 綠燈但套件沒發佈」或「restore 失敗」）：

- `.github/workflows/build-ci.yml` —— pack 清單兩行
- `.github/workflows/nuget-publish.yml` —— build 清單、pack 清單、套件名清單共 6 處
- `Bee.Library.slnx` —— 4 個 `<Project>`
- `samples/Bee.Samples.slnx` —— 3 個 `<Project>`

> 這兩個 workflow 的清單**不是 glob 而是逐項列舉**，這正是新增套件時容易漏的地方；
> 移除是同一個清單的反向操作，同樣要逐項核。

## 階段 2：MAUI trim/AOT 知識不能跟著刪

`.claude/rules/maui.md` 記錄的「Apple Release-mode trim 決策樹」（Mono linker 砍掉
`XmlSerializer` 反射 fallback、`ILLink.Descriptors.xml` 解法、AOT 與 Interpreter 的組合雷）
**是 Apple 平台通用知識，不是 MAUI 專屬**。現有引用者：

- `.claude/rules/avalonia.md:5` —— 明文說 Avalonia 行動 head 與 MAUI 共用該機制
- `apps/Bee.Northwind/Bee.Northwind.iOS/*.csproj`、`.Android/*.csproj` 的註解

處理方式（二選一，待裁決）：

| 方案 | 做法 |
|------|------|
| A | `rules/maui.md` 更名為 `rules/apple-mobile-trim.md`，刪去 MAUI 專案特定內容（csproj 設定、Platforms stub、`MauiPreferenceEndpointStorage`），保留 trim/AOT 決策樹；更新三處引用 |
| B | 把 trim/AOT 段落併入 `rules/avalonia.md`，刪除 `rules/maui.md` |

建議 A —— 該決策樹有獨立的篇幅與交叉引用，併進 `avalonia.md` 會讓後者失焦。

## 階段 3：文件同步

**要改**（描述現行架構）：

- `docs/dependency-map*.md` —— 相依圖、外部套件表、專案數（18 → 16）
- `docs/README*.md` —— 專案數
- `README*.md`（repo 根）—— 套件清單
- `samples/README*.md` —— 啟動指引、port 表、架構圖、`quickstart.db` 說明（三 host → 兩 host）
- `docs/development-cookbook*.md`、`docs/terminology*.md` —— 移除兩者的段落
- `CHANGELOG*.md` Unreleased —— 列為 breaking：兩個已發布 NuGet 套件移除

**不改**（歷史記錄，改了等於篡改）：

- `docs/changelogs/4.5.0*.md` 等既有版本說明 —— 記錄的是當時事實
- `docs/adr/adr-013`（前端連線策略）、`adr-020` —— ADR 是決策當下的紀錄。
  若此次收斂推翻了 adr-013 的前提，正確做法是**新增一份 ADR 記錄本次決策並標記 adr-013 為部分取代**，
  而不是改寫 adr-013 本身

## 階段 4：`FormDataObject` 收斂 —— 改採雙向註解

體檢 P2-1 原建議「抽 `Bee.Web.Blazor.Core` 承接兩個 Blazor 的 950 行重複」——
階段 1 完成後這件事已不必做（只剩一個 Blazor，沒有共用對象）。

`FormDataObject` 從 4 份降為 2 份（Avalonia 791 行 / Blazor.Server 360 行）。兩份的
`using` 完全一致且零 UI 框架相依，技術上可上移至 `Bee.UI.Core`——**但不應該這麼做**。

`docs/dependency-map.md` 明文記載 **`Bee.UI.*` family 判別準則**：

> 是否消費 `Bee.UI.Core` 抽象（`ClientInfo` / `IEndpointStorage` / `IUIViewService`）。
> 消費 → 歸 `Bee.UI.*`；不消費、有自己的狀態管理 → 獨立 family 前綴
> （如 `Bee.Web.Blazor.*`：Blazor circuit / WASM 環境沒有檔案 IO 與 dialog service 的概念）。

也就是說「Blazor 不消費 `Bee.UI.Core`」是**刻意的架構決策且有明確理由**，不是疏漏。
上移會讓 `Bee.Web.Blazor.Server` 依賴 `Bee.UI.Core`，直接牴觸這條準則——而準則本身
正是以「是否消費 UI.Core」為判別基礎。

評估過的替代方案：

| 方案 | 取捨 | 判定 |
|------|------|------|
| 新增中性套件（如 `Bee.Forms`） | 消除重複且不違反準則 | 為 360 行重複多一個發布套件（CI pack 清單 / dependency-map / README 全要加），不划算 |
| Blazor.Server 引用 `Bee.UI.Core` | 最省事 | 破壞 family 準則的判別基礎 |
| `Bee.UI.Core` 拆「連線狀態」與「表單資料」兩層 | 長期最乾淨 | 工程量遠超過它要解決的問題；若要做應另開 plan |

**採用：維持兩份 + 雙向註解**，說明刻意平行維護、為何不合併、以及改一邊必須同步另一邊。
這與 repo 內既有的平行家族（`Bee.Base.Collections` vs `Bee.Definition.Collections.MessagePack*`）
處理方式一致——體檢曾指出那組「分岔很可能刻意，但兩個檔案都沒有一句話說明」，此次一併把
慣例建立起來。

> **順序很重要**：先移除專案再決定共用層。反過來會為即將刪除的專案建共用基礎設施。

## 風險與確認事項

1. **移除已發布的 NuGet 套件屬 breaking。** 框架為 pre-stable 且無已知外部消費者，
   直接移除即可，但須在 CHANGELOG Unreleased 明列。
2. **MAUI 若未來要復活**，`git log` 保有完整歷史；本計畫不做 `[Obsolete]` 過渡期
   （過渡期對「整個套件不再發布」沒有意義——消費端 restore 時就會失敗，不是編譯警告）。
3. **`bee-sample-add` skill** 的前端類型選單含 Maui / Blazor Wasm，需同步移除選項。
4. **`demo-smoke` skill** 若有 `samples/Maui.Demo/.smoke.yaml` 對應設定，一併移除。
