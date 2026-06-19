# 計畫：Bee.UI.Avalonia 控件 Demo Center（DevExpress Demo Center 模式）

**狀態：🚧 進行中（2026-06-19）**

| 階段 | 範圍 | 狀態 |
|------|------|------|
| 1 | Shell 骨架：導覽樹 + 模組宿主 + 工具列（主題切換、FormMode 切換），現有 Gallery 比對遷為一種場景 | ✅ 已完成（2026-06-19） |
| 2 | `DemoModule` 抽象 + 模組註冊表 + View Source（嵌入式原始碼） | ✅ 已完成（2026-06-19） |
| 3 | Data Editors 場景組（7 個編輯器 × 多場景） | 📝 待做 |
| 4 | Grid / Views 場景組（GridControl、FormView、ListView） | 📝 待做 |
| 5 | 收尾：主題矩陣、README/smoke、定位為試點對齊基準 | 📝 待做 |

## 背景與目標

目前 `Bee.UI.Avalonia` 控件的展示面是 `samples/Avalonia.Editors.Gallery`——一個**平鋪滾動視窗**，每個 `ControlType` 一個區塊，原生控件 vs 繼承控件並排、正常 vs 唯讀。它驗證「樣式一致性」很好用，但缺乏 DevExpress **Demo Center** 那種結構：左側控件樹、每控件多個情境模組、每模組可看原始碼、全域主題/模式切換。

**目標**：把 Gallery 升級成 DevExpress Demo Center 模式的控件展示中心，一物三用：

1. **展示廳** — 外部框架使用者瀏覽控件能力的門面（呼應「開發者 skill 包」的消費端對象，見 [[bee-developer-skill-pack-goal]]）
2. **活文件 / 對齊基準** — 每個控件的標準行為與外觀契約；日後把繼承式控件移植到 Maui/Blazor 時，這份 Demo Center 就是「對齊範本」（Avalonia 是 UI 架構試點，見 [[avalonia-pilot-ui-architecture]]）
3. **開發驗證面** — 修改控件外觀時的目視回饋場（例如 `plan-readonly-underline-appearance.md` 的「唯讀去框」會落為其中一個場景模組）

> **與 readonly 去框的關係**：readonly 去框照舊在現有 Gallery 推進、即時驗證，不等本 plan。Demo Center 成形後，把「唯讀 vs 編輯」遷為本中心的一個場景模組。兩 plan 並行、不互卡。

## 與現有 sample 的定位區隔

| Sample | 定位 | 本 plan 是否動它 |
|--------|------|----------------|
| `Avalonia.Demo` | 端到端**應用**示範（登入 → 連線 → JSON-RPC → 渲染表單），證明框架可接成 app | ❌ 不動，定位不同 |
| `Avalonia.Editors.Gallery` | **控件組**展示/回歸面 | ✅ 升級為 Demo Center（本 plan 主體） |

**待拍板**：是「就地把 Gallery 升級（沿用專案名）」還是「改名為 `Avalonia.DemoCenter` 另立」。就地升級不碎裂 sample 數量，但改名需同步 `Bee.Samples.slnx`、`.smoke.yaml`、README。傾向**就地升級**，必要時最後一階段再評估改名。

## 架構設計

### Shell（MainWindow）

```
┌─────────────────────────────────────────────────────────┐
│ 工具列：[主題 ▾ Fluent/Semi] [FormMode ▾ View/Add/Edit]   │
├──────────────┬──────────────────────────────────────────┤
│ 導覽樹        │  選定場景模組宿主（活的互動控件）          │
│ ├ Data Editors│                                          │
│ │  ├ TextEdit │  ┌────────────────────────────────────┐  │
│ │  ├ DateEdit │  │ 場景說明                            │  │
│ │  └ …        │  │ [互動控件區]                        │  │
│ ├ Grid        │  └────────────────────────────────────┘  │
│ │  └ GridCtrl │  ┌─[ Demo │ Source ]─────────────────┐    │
│ └ Views       │  │  該模組原始碼（View Source）        │    │
│    ├ FormView │  └────────────────────────────────────┘    │
│    └ ListView │                                          │
└──────────────┴──────────────────────────────────────────┘
```

- **導覽樹**：類別 → 控件 → 場景（DevExpress 風格：選控件後右側列出該控件的多個場景）。
- **全域工具列**：
  - **主題切換**：Fluent / Semi（兩個現有 sample 各用其一，Demo Center 要能當場翻，驗證繼承控件在不同 ControlTheme 下都對）。
  - **FormMode 切換**：驅動 `FormScope.SetFormMode`，全域翻 View/Add/Edit——直接服務「唯讀去框是否清爽」的驗證。
- **View Source 面板**：顯示當前場景模組的原始碼。

### DemoModule 抽象

每個場景是一個自足單元：

```csharp
public interface IDemoModule
{
    string Category { get; }      // "Data Editors" / "Grid" / "Views"
    string ControlName { get; }   // "TextEdit"
    string ScenarioTitle { get; } // "唯讀 vs 編輯"
    string Description { get; }
    Control BuildView();          // 活的互動控件
    string GetSourceText();       // View Source 內容
}
```

- **模組註冊表**：集中註冊所有模組，導覽樹由此生成（依 Category / ControlName 分組）。
- **View Source 機制（待拍板）**：傾向把每個模組的 `.cs` 以 **EmbeddedResource** 嵌入，runtime 讀出顯示（貼近 DevExpress「直接看真實原始碼」、不會與實作脫鉤）；替代方案是手維護片段（易脫鉤，不建議）。

### 內容規劃（控件 × 場景）

| 類別 | 控件 | 場景模組 |
|------|------|---------|
| Data Editors | TextEdit / MemoEdit / ButtonEdit / DateEdit / YearMonthEdit / DropDownEdit / CheckEdit | ① 基本綁定（即時值讀出）② 唯讀 vs 編輯（FormMode 切換，readonly 去框落點）③ Metadata（MaxLength / ListItems）④ 原生 vs 繼承（沿用現 Gallery 的回歸比對） |
| Data Editors | ButtonEdit | ⑤ lookup picker 流程 |
| Grid | GridControl | ambient 綁定 / in-cell 編輯 / EditForm 模式（見 [ADR-021](../adr/adr-021-avalonia-datagrid-editing-strategy.md)） |
| Views | FormView | master-detail、FormMode 三態 |
| Views | ListView | 清單呈現 |

> 現有 Gallery 的「原生 vs 繼承並排」回歸價值不丟——遷為每控件的場景 ④。

## 階段細節

### 階段 1：Shell 骨架
- 建導覽樹 + 模組宿主 + 工具列（主題、FormMode 切換）。
- 把現有 Gallery 的比對內容**先整塊塞進一個場景**，確保升級過程零回歸損失。
- 此階段先求「殼能跑、能切換、能掛一個場景」。

### 階段 2：DemoModule 抽象 + View Source
- 定 `IDemoModule` + 註冊表 + 導覽樹生成。
- 實作 View Source（EmbeddedResource 讀取）。
- 把階段 1 那塊內容重構成符合 `IDemoModule` 的第一批模組。

### 階段 3：Data Editors 場景組
- 7 個編輯器逐一補齊場景 ①–④（+ ButtonEdit 的 ⑤）。
- 「唯讀 vs 編輯」場景與 readonly 去框成果對接。

### 階段 4：Grid / Views 場景組
- GridControl、FormView、ListView 的場景模組。

### 階段 5：收尾
- 主題矩陣掃過（Fluent × Semi × Light/Dark）逐場景目視。
- 更新 README（雙語）、`.smoke.yaml`；評估是否改名 `Avalonia.DemoCenter` 並同步 `Bee.Samples.slnx`。
- 文件化「本 Demo Center 為 Maui/Blazor 移植的對齊基準」。

## 已拍板決議（2026-06-19）

1. **改名 `Avalonia.DemoCenter` 另立** —— 不就地沿用 Gallery 專案名；階段 1 即建新專案、移植內容、移除舊 `Avalonia.Editors.Gallery`、同步 `Bee.Samples.slnx` / README / `.smoke.yaml`。
2. **僅 Semi（+ Light/Dark）** —— 工具列主題切換只做 Light/Dark，沿用現有 `Semi.Avalonia`；不納 Fluent runtime 切換。
3. **View Source 走 EmbeddedResource 讀真實 `.cs`** —— 模組檔案組織以此為前提（階段 2 實作）。
4. **只聚焦控件層** —— app 級端到端維持由 `Avalonia.Demo` 負責，Demo Center 不涉入 Login / CRUD 流程。

## 風險

- **跨主題 template 差異**：繼承控件以 `StyleKeyOverride` 沿用原生 ControlTheme，Fluent/Semi 的 part 結構不同；主題切換場景可能暴露既有不一致，需逐一檢視。
- **View Source 與實作脫鉤**：若不走 EmbeddedResource 而手維護片段，會隨重構失準。
- **範圍膨脹**：場景數量大，嚴格依階段交付，每階段可獨立 run 起來看，不一次吞完。
