# Avalonia 12 內建控件清單與繼承改寫指南

本文件整理 Avalonia 12.0.x 內建控件的分類清單，並說明「繼承內建控件改寫」的三種模式與範例。
適用於本 repo 的 Avalonia 專案：

| 專案 | Avalonia 版本 | 主題 |
|------|--------------|------|
| [src/Bee.UI.Avalonia](../src/Bee.UI.Avalonia) | 12.0.0（+ DataGrid 12.0.0） | 由 host 決定 |
| [tools/DefineEditor](../tools/DefineEditor) | 12.0.4 | Semi.Avalonia 12.0.3 |
| [samples/Avalonia.Demo](../samples/Avalonia.Demo) | 12.0.4 | Fluent |

> 控件清單以本機 NuGet 套件 `avalonia/12.0.4` 的組件中繼資料核對，分類依用途整理。
> `Avalonia.Controls.Primitives` 命名空間的型別標注「(Primitives)」，屬框架基底或控件零件，仍可直接使用與繼承。

---

## 1. 先回答核心問題：可以繼承下來改寫嗎？

**可以，而且這是 Avalonia 的設計預期。** 絕大多數內建控件：

- **非 `sealed`**，可直接繼承
- 公開大量 `protected virtual` 掛載點：`OnTextInput` / `OnKeyDown` / `OnPointerPressed` / `OnGotFocus` / `OnApplyTemplate` / `MeasureOverride` / `ArrangeOverride` 等
- 屬性系統（`StyledProperty` / `DirectProperty`）支援子類別註冊新屬性、用 class handler 監聽變更

但有一個**必踩雷**：Avalonia 11 起，控件以 `StyleKey` 查找 `ControlTheme`，子類別預設的 StyleKey 是**自身型別**。Semi.Avalonia / Fluent 主題只為內建型別提供 ControlTheme，因此**不處理 StyleKey 的子類別會完全沒有樣式（畫面上隱形）**。解法見[模式 A](#4-模式-a直接繼承內建控件)。

---

## 2. 控件繼承體系速覽

```
AvaloniaObject
└─ StyledElement
   └─ Visual
      └─ Layoutable
         └─ Interactive
            └─ InputElement
               └─ Control                  ← 最低可獨立使用的控件基底
                  ├─ Panel                 ← 版面容器（StackPanel / Grid / …）
                  ├─ Shape                 ← 圖形（Ellipse / Path / …）
                  ├─ TextBlock             ← 直接繪製，無 template
                  └─ TemplatedControl      ← 有 ControlTheme / Template 的控件
                     ├─ ContentControl     ← 單一內容（Button / Window / …）
                     │  └─ UserControl     ← 組合式控件基底
                     ├─ ItemsControl       ← 集合內容（ListBox / TreeView / …）
                     └─ RangeBase          ← 數值範圍（Slider / ProgressBar / …）
```

自訂控件的三個切入點即對應後文三種模式：

- **繼承具體控件**（`TextBox`、`Button`…）→ 模式 A
- **繼承 `TemplatedControl`** → 模式 B
- **繼承 `UserControl`** → 模式 C

---

## 3. 內建控件分類清單

「繼承」欄：✅ 適合繼承改寫；⚠️ 可繼承但有注意事項；➖ 不建議或無意義。

### 3.1 按鈕類

| 控件 | 用途 | 繼承基底 | 繼承 |
|------|------|---------|------|
| `Button` | 標準按鈕 | `ContentControl` | ✅ 最常見的繼承對象 |
| `RepeatButton` | 按住重複觸發 | `Button` | ✅ |
| `ToggleButton` (Primitives) | 兩態/三態切換 | `Button` | ✅ `CheckBox` / `RadioButton` 的基底 |
| `CheckBox` | 核取方塊 | `ToggleButton` | ✅ |
| `RadioButton` | 單選按鈕（`GroupName` 分組） | `ToggleButton` | ✅ |
| `ToggleSwitch` | 開關（On/Off 含滑動把手） | `ToggleButton` | ✅ |
| `SplitButton` | 主按鈕 + 下拉箭頭 | `ContentControl` | ✅ |
| `ToggleSplitButton` | SplitButton 的切換版 | `SplitButton` | ✅ |
| `DropDownButton` | 點擊展開 Flyout | `Button` | ✅ |
| `HyperlinkButton` | 超連結樣式按鈕（`NavigateUri`） | `Button` | ✅ |
| `ButtonSpinner` | 內容旁附上下微調鈕 | `Spinner` (Primitives) | ✅ `NumericUpDown` 的零件 |

### 3.2 文字與輸入類

| 控件 | 用途 | 繼承基底 | 繼承 |
|------|------|---------|------|
| `TextBlock` | 唯讀文字 | `Control` | ⚠️ 可繼承，但無 template，自訂外觀要 override `Render`；多數需求用樣式或附加屬性即可 |
| `SelectableTextBlock` | 可選取/複製的唯讀文字 | `TextBlock` | ✅ |
| `Label` | 帶快捷鍵目標（`Target`）的標籤 | `ContentControl` | ✅ |
| `AccessText` (Primitives) | 解析 `_` 快捷鍵底線的文字 | `TextBlock` | ⚠️ 通常作為 template 零件使用 |
| `TextBox` | 單行/多行文字輸入 | `TemplatedControl` | ✅ 攔截輸入、加驗證的首選繼承點（見模式 A 範例） |
| `MaskedTextBox` | 遮罩格式輸入（電話、日期…） | `TextBox` | ✅ |
| `AutoCompleteBox` | 自動完成下拉 | `TemplatedControl` | ✅ |
| `NumericUpDown` | 數值輸入 + 微調鈕 | `TemplatedControl` | ✅ 需要數值欄位時優先用它，而不是自己從 `TextBox` 改 |

### 3.3 清單與集合類

| 控件 | 用途 | 繼承基底 | 繼承 |
|------|------|---------|------|
| `ItemsControl` | 集合呈現基底（無選取） | `TemplatedControl` | ✅ 自訂集合控件的起點 |
| `ListBox` / `ListBoxItem` | 可選取清單 | `SelectingItemsControl` | ✅ |
| `ComboBox` / `ComboBoxItem` | 下拉選取 | `SelectingItemsControl` | ✅ |
| `TreeView` / `TreeViewItem` | 階層樹 | `ItemsControl` | ✅ DefineEditor 方案樹即直接使用 |
| `TabControl` / `TabItem` | 分頁 | `SelectingItemsControl` | ✅ |
| `TabStrip` (含 `TabStripItem`) | 只有頁籤列、無內容區 | `SelectingItemsControl` | ✅ |
| `Carousel` | 輪播（一次顯示一項） | `SelectingItemsControl` | ✅ |
| `PipsPager` | 圓點分頁指示器 | `TemplatedControl` | ✅ 12 新增 |
| `RefreshContainer` | 下拉更新容器（行動慣例） | `ContentControl` | ⚠️ 行動情境用 |
| `DataGrid` | 資料表格（**獨立套件 `Avalonia.Controls.DataGrid`**） | `TemplatedControl` | ⚠️ 可繼承；theme 需另引（Semi 為 `Semi.Avalonia.DataGrid`），綁定策略見 [ADR-020](adr/adr-020-avalonia-datagrid-binding-strategy.md) |

### 3.4 選單類

| 控件 | 用途 | 繼承基底 | 繼承 |
|------|------|---------|------|
| `Menu` | 視窗內選單列 | `MenuBase` | ✅ |
| `MenuItem` | 選單項 | `HeaderedSelectingItemsControl` | ✅ |
| `ContextMenu` | 右鍵選單 | `MenuBase` | ✅ |
| `Flyout` / `MenuFlyout` | 輕量彈出層（附掛於控件） | `FlyoutBase` | ✅ 自訂彈出行為繼承 `FlyoutBase` / `PopupFlyoutBase` |
| `Separator` | 分隔線 | `TemplatedControl` | ✅ |
| `NativeMenu` / `NativeMenuItem` | 原生選單（macOS 選單列等） | `AvaloniaObject` | ➖ 非視覺樹控件，無樣式/模板概念，用組合不用繼承 |
| `TrayIcon` | 系統匣圖示 | `AvaloniaObject` | ➖ 同上 |

### 3.5 版面類（Panel 系）

| 控件 | 用途 | 繼承基底 | 繼承 |
|------|------|---------|------|
| `Panel` | 版面基底（子項重疊） | `Control` | ✅ 自訂版面演算法繼承它，override `MeasureOverride` / `ArrangeOverride` |
| `StackPanel` | 線性堆疊 | `Panel` | ✅ |
| `ReversibleStackPanel` | 可反轉順序的 StackPanel | `StackPanel` | ✅ 12 新增 |
| `DockPanel` | 邊緣停靠 | `Panel` | ✅ |
| `Grid` | 行列格線 | `Panel` | ⚠️ 可繼承，但內部佈局演算法複雜，通常不必 |
| `UniformGrid` (Primitives) | 等分格線 | `Panel` | ✅ |
| `WrapPanel` | 換行排列 | `Panel` | ✅ |
| `Canvas` | 絕對座標 | `Panel` | ✅ |
| `RelativePanel` | 相對關係佈局 | `Panel` | ⚠️ 同 `Grid` |
| `VirtualizingPanel` / `VirtualizingStackPanel` | 虛擬化版面 | `Panel` | ⚠️ 自訂虛擬化版面難度高，先確認現成的不夠用 |

### 3.6 容器與裝飾類

| 控件 | 用途 | 繼承基底 | 繼承 |
|------|------|---------|------|
| `Border` | 邊框/背景/圓角 | `Decorator` | ✅ |
| `Decorator` | 單子項裝飾基底 | `Control` | ✅ 自訂裝飾器起點 |
| `ContentControl` | 單一內容宿主 | `TemplatedControl` | ✅ |
| `HeaderedContentControl` (Primitives) | 標頭 + 內容 | `ContentControl` | ✅ `Expander` / `TabItem` 的基底 |
| `UserControl` | 組合式控件基底 | `ContentControl` | ✅ 模式 C 的基底 |
| `ScrollViewer` | 捲動容器 | `ContentControl` | ✅ |
| `Expander` | 可摺疊面板 | `HeaderedContentControl` | ✅ |
| `GridSplitter` | 格線分隔拖曳條 | `Thumb` (Primitives) | ✅ |
| `SplitView` | 側欄 + 內容（漢堡選單版型） | `ContentControl` | ✅ |
| `Viewbox` | 等比縮放內容 | `Control` | ✅ |
| `LayoutTransformControl` | 對內容套用佈局變形 | `Decorator` | ✅ |
| `TransitioningContentControl` | 內容切換動畫 | `ContentControl` | ✅ |
| `ThemeVariantScope` | 區域性深淺色主題切換 | `Decorator` | ➖ 純功能容器 |
| `ExperimentalAcrylicBorder` | 壓克力（毛玻璃）效果 | `Decorator` | ⚠️ Experimental，API 可能變動 |

### 3.7 日期時間類

| 控件 | 用途 | 繼承基底 | 繼承 |
|------|------|---------|------|
| `Calendar` | 月曆 | `TemplatedControl` | ✅ |
| `CalendarDatePicker` | 文字框 + 下拉月曆 | `TemplatedControl` | ✅ |
| `DatePicker` | 滾輪式日期選取 | `TemplatedControl` | ✅ |
| `TimePicker` | 滾輪式時間選取 | `TemplatedControl` | ✅ |

### 3.8 視窗與覆疊類

| 控件 | 用途 | 繼承基底 | 繼承 |
|------|------|---------|------|
| `Window` | 桌面視窗 | `WindowBase` ← `TopLevel` | ✅ 每個 app 的 `MainWindow` 本來就是它的子類 |
| `Popup` (Primitives) | 低階彈出宿主 | `Control` | ➖ 平台整合複雜，改用 `Flyout` |
| `ToolTip` | 工具提示 | `ContentControl` | ⚠️ 以附加屬性使用為主，少繼承 |
| `AdornerLayer` / `OverlayLayer` (Primitives) | 裝飾/覆疊圖層 | `Canvas` | ➖ 框架圖層 |
| `WindowNotificationManager` / `NotificationCard` (Notifications) | 視窗內浮動通知 | `TemplatedControl` | ✅ |

### 3.9 進度與狀態類

| 控件 | 用途 | 繼承基底 | 繼承 |
|------|------|---------|------|
| `RangeBase` (Primitives) | Min/Max/Value 基底 | `TemplatedControl` | ✅ 自訂數值範圍控件的起點 |
| `ProgressBar` | 進度條（含 indeterminate） | `RangeBase` | ✅ |
| `Slider` | 滑桿 | `RangeBase` | ✅ |
| `ScrollBar` (Primitives) | 捲軸 | `RangeBase` | ⚠️ 通常作為 `ScrollViewer` template 零件 |
| `TickBar` | 滑桿刻度 | `Control` | ⚠️ template 零件 |

### 3.10 圖形與媒體類

| 控件 | 用途 | 繼承基底 | 繼承 |
|------|------|---------|------|
| `Image` | 點陣/向量圖 | `Control` | ✅ |
| `PathIcon` | 以 `Geometry` 畫 icon | `IconElement` | ✅ DefineEditor 已大量使用 |
| `Shape` (Shapes) | 圖形基底 | `Control` | ✅ 自訂圖形 override `CreateDefiningGeometry` |
| `Ellipse` / `Rectangle` / `Line` / `Path` / `Polygon` / `Polyline` | 基本圖形 | `Shape` | ⚠️ 多半繼承 `Shape` 自訂新圖形，而非繼承具體圖形 |
| `Arc` / `Sector` | 弧線 / 扇形 | `Shape` | ✅ 12 新增 |

### 3.11 其他（12 新增與獨立套件）

| 控件 | 用途 | 繼承基底 | 繼承 |
|------|------|---------|------|
| `CommandBar` 系列（`CommandBarButton` / `CommandBarToggleButton` / `CommandBarSeparator`） | 工具列 + 溢出選單 | `ContentControl` 系 | ✅ 12 新增 |
| `Page` / `ContentPage` / `NavigationPage` / `TabbedPage` / `DrawerPage` | 行動式頁面導航體系 | `ContentControl` 系 | ⚠️ 12 新增，行動 app 情境；桌面工具（DefineEditor 類）用不到 |
| `ColorPicker` / `ColorView` | 色彩選取（**獨立套件 `Avalonia.Controls.ColorPicker`**） | `TemplatedControl` | ✅ 需另裝套件與對應 theme |

---

## 4. 模式 A：直接繼承內建控件

適合「**沿用既有控件的外觀，只改行為或加屬性**」。先理解 StyleKey 規則：

### 4.1 StyleKey 規則（必讀）

控件以 `StyledElement.StyleKeyOverride`（預設 = 自身型別）決定兩件事：

1. **ControlTheme 查找**：用 StyleKey 到 Resources 找 `x:Key` 為該型別的 `ControlTheme`
2. **型別 selector 匹配**：`<Style Selector="Button">` 匹配的是 StyleKey，不是 CLR 實際型別

因此繼承後有兩條路：

| 路線 | 寫法 | 效果 |
|------|------|------|
| **A1：沿用基底樣式** | `protected override Type StyleKeyOverride => typeof(基底);` | Semi / Fluent 的基底 theme 與所有 `Selector="基底"` 樣式照常生效；但 `Selector="local\|子類"` **不再匹配**，要對子類加樣式改用 style class（`Classes="xxx"` + `Selector="Button.xxx"`） |
| **A2：自備 ControlTheme** | 不覆寫 StyleKey，提供 `BasedOn` 基底 theme 的 `ControlTheme` | 可在 template 中使用子類新屬性；忘記提供 theme 控件會**隱形** |

### 4.2 範例 1（A1 路線）：`NumericTextBox` — 只允許數字輸入

```csharp
using Avalonia.Input;
using Avalonia.Controls;

namespace Bee.UI.Avalonia.Controls
{
    /// <summary>
    /// A <see cref="TextBox"/> that accepts digit characters only.
    /// </summary>
    public class NumericTextBox : TextBox
    {
        // WARNING: Without this override the subclass looks up a ControlTheme
        // keyed by `NumericTextBox`, which Semi.Avalonia does not provide,
        // and the control renders with no visual at all.
        protected override Type StyleKeyOverride => typeof(TextBox);

        protected override void OnTextInput(TextInputEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Text) && !e.Text.All(char.IsDigit))
            {
                e.Handled = true;
                return;
            }
            base.OnTextInput(e);
        }
    }
}
```

```xml
<!-- 使用：與 TextBox 完全相同，外觀沿用主題 -->
<controls:NumericTextBox Width="120" Watermark="只能輸入數字" />
```

注意：`OnTextInput` 攔不到**貼上**與 IME 組字完成前的內容。要完整防護，再 override `OnKeyDown` 攔 `Ctrl/Cmd+V`，或訂閱 `TextChanging` 事件做最終過濾。

### 4.3 範例 2（A2 路線）：`IconButton` — 新增 `Icon` 屬性並調整外觀

```csharp
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Bee.UI.Avalonia.Controls
{
    /// <summary>
    /// A <see cref="Button"/> that renders a vector icon beside its content.
    /// </summary>
    public class IconButton : Button
    {
        // NOTE: No `StyleKeyOverride` here. The control keeps its own style key
        // so the ControlTheme below (which binds the new `Icon` property) applies.
        public static readonly StyledProperty<Geometry?> IconProperty =
            AvaloniaProperty.Register<IconButton, Geometry?>(nameof(Icon));

        public Geometry? Icon
        {
            get => GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }
    }
}
```

```xml
<!-- Themes/IconButton.axaml：BasedOn 沿用 Button theme 的所有 setter 與互動樣式，只換 Template -->
<ResourceDictionary xmlns="https://github.com/avaloniaui"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                    xmlns:c="using:Bee.UI.Avalonia.Controls">
    <ControlTheme x:Key="{x:Type c:IconButton}" TargetType="c:IconButton"
                  BasedOn="{StaticResource {x:Type Button}}">
        <Setter Property="Template">
            <ControlTemplate>
                <Border Background="{TemplateBinding Background}"
                        BorderBrush="{TemplateBinding BorderBrush}"
                        BorderThickness="{TemplateBinding BorderThickness}"
                        CornerRadius="{TemplateBinding CornerRadius}"
                        Padding="{TemplateBinding Padding}">
                    <StackPanel Orientation="Horizontal" Spacing="6">
                        <PathIcon Data="{TemplateBinding Icon}" Width="14" Height="14" />
                        <ContentPresenter Name="PART_ContentPresenter"
                                          Content="{TemplateBinding Content}"
                                          ContentTemplate="{TemplateBinding ContentTemplate}"
                                          VerticalAlignment="Center" />
                    </StackPanel>
                </Border>
            </ControlTemplate>
        </Setter>
    </ControlTheme>
</ResourceDictionary>
```

```xml
<!-- App.axaml：把 theme 併入 Resources，否則控件隱形 -->
<Application.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <ResourceInclude Source="/Themes/IconButton.axaml" />
        </ResourceDictionary.MergedDictionaries>
    </ResourceDictionary>
</Application.Resources>
```

> `BasedOn="{StaticResource {x:Type Button}}"` 會繼承主題（Semi / Fluent）為 `Button` 定義的背景、hover/pressed 偽類樣式等；自訂 template 時保留主題慣用的零件名稱（如 `PART_ContentPresenter`），偽類樣式才能對得上。

### 4.4 新屬性要用 `StyledProperty` 還是 `DirectProperty`？

| | `StyledProperty` | `DirectProperty` |
|--|------------------|------------------|
| 可被 Style/Theme `Setter` 設定 | ✅ | ❌ |
| 可繼承值、有優先序系統 | ✅ | ❌ |
| 儲存位置 | 屬性系統字典 | 一般 CLR 欄位（輕量） |
| 適用 | 外觀/可設定屬性（多數情況） | 高頻更新、唯讀狀態、集合屬性 |

repo 既有控件（[DynamicForm.cs](../src/Bee.UI.Avalonia/Controls/DynamicForm.cs)）採 `StyledProperty` + 靜態建構式 class handler 監聽變更，新控件沿用同一模式即可。
另外注意：`AvaloniaProperty.Register<TOwner, TValue>` 的 `TOwner` 必須是**子類自身**，不要照抄基底型別。

---

## 5. 模式 B：`TemplatedControl` 自訂控件

適合「**全新外觀的可重用控件**」：控件類別只定義屬性、狀態（偽類）與行為，外觀完全交給 `ControlTheme`，使用端可整顆換掉 template（lookless）。

範例：狀態徽章 `StatusBadge`。

```csharp
using Avalonia;
using Avalonia.Controls.Primitives;

namespace Bee.UI.Avalonia.Controls
{
    public enum BadgeStatus
    {
        Normal,
        Success,
        Error
    }

    /// <summary>
    /// A lookless badge that exposes its status through pseudo-classes,
    /// so the visual treatment lives entirely in the ControlTheme.
    /// </summary>
    public class StatusBadge : TemplatedControl
    {
        public static readonly StyledProperty<string?> TextProperty =
            AvaloniaProperty.Register<StatusBadge, string?>(nameof(Text));

        public static readonly StyledProperty<BadgeStatus> StatusProperty =
            AvaloniaProperty.Register<StatusBadge, BadgeStatus>(nameof(Status));

        public string? Text
        {
            get => GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public BadgeStatus Status
        {
            get => GetValue(StatusProperty);
            set => SetValue(StatusProperty, value);
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);
            if (change.Property == StatusProperty)
            {
                PseudoClasses.Set(":success", Status == BadgeStatus.Success);
                PseudoClasses.Set(":error", Status == BadgeStatus.Error);
            }
        }
    }
}
```

```xml
<!-- Themes/StatusBadge.axaml -->
<ResourceDictionary xmlns="https://github.com/avaloniaui"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                    xmlns:c="using:Bee.UI.Avalonia.Controls">
    <ControlTheme x:Key="{x:Type c:StatusBadge}" TargetType="c:StatusBadge">
        <Setter Property="Background" Value="#E0E0E0" />
        <Setter Property="Foreground" Value="#333333" />
        <Setter Property="Padding" Value="8,2" />
        <Setter Property="Template">
            <ControlTemplate>
                <Border Background="{TemplateBinding Background}"
                        Padding="{TemplateBinding Padding}"
                        CornerRadius="10">
                    <TextBlock Text="{TemplateBinding Text}"
                               Foreground="{TemplateBinding Foreground}"
                               FontSize="12" />
                </Border>
            </ControlTemplate>
        </Setter>

        <!-- Pseudo-class driven visual states -->
        <Style Selector="^:success">
            <Setter Property="Background" Value="#D8F5D0" />
            <Setter Property="Foreground" Value="#2D7A1F" />
        </Style>
        <Style Selector="^:error">
            <Setter Property="Background" Value="#FAD9D5" />
            <Setter Property="Foreground" Value="#B3261E" />
        </Style>
    </ControlTheme>
</ResourceDictionary>
```

使用方式與註冊（`ResourceInclude` 併入 `App.axaml`）同模式 A2。重點：

- **`x:Key` 用 `{x:Type}`**：ControlTheme 以型別為 key，控件才會自動套上
- **偽類（PseudoClasses）表達狀態**：theme 內用 `Selector="^:success"` 切換外觀，類別本身不碰顏色
- **需要存取 template 內零件時**，override `OnApplyTemplate`：

```csharp
protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
{
    base.OnApplyTemplate(e);
    // Template parts are conventionally named with the `PART_` prefix.
    var presenter = e.NameScope.Find<Border>("PART_LayoutRoot");
}
```

---

## 6. 模式 C：`UserControl` 組合式

適合「**把多個現成控件組合成一個元件**」，不需要可替換的 template。repo 內三個自訂控件都是這個模式：

| 控件 | 檔案 | 組合內容 |
|------|------|---------|
| `DynamicForm` | [DynamicForm.cs](../src/Bee.UI.Avalonia/Controls/DynamicForm.cs) | 依 `FormLayout` 動態產生 `TextBox` / `ComboBox` / `CheckBox` 等欄位 |
| `DynamicGrid` | [DynamicGrid.cs](../src/Bee.UI.Avalonia/Controls/DynamicGrid.cs) | 包 `DataGrid`，以 `FuncDataTemplate` 解 `DataRowView` 綁定限制（[ADR-020](adr/adr-020-avalonia-datagrid-binding-strategy.md)） |
| `FormView` | [FormView.cs](../src/Bee.UI.Avalonia/Controls/FormView.cs) | 整合 `DynamicGrid` + `DynamicForm` + 工具列 |

模式骨架（取自 `DynamicForm` 的精簡版）：

```csharp
public class DynamicForm : UserControl
{
    public static readonly StyledProperty<FormLayout?> FormLayoutProperty =
        AvaloniaProperty.Register<DynamicForm, FormLayout?>(nameof(FormLayout));

    static DynamicForm()
    {
        // Rebuild the composed content whenever the driving property changes.
        FormLayoutProperty.Changed.AddClassHandler<DynamicForm>((d, _) => d.Rebuild());
    }

    public DynamicForm()
    {
        Content = new StackPanel { Orientation = Orientation.Vertical };
    }
}
```

> `UserControl` 也是 `ContentControl` 子類，StyleKey 雷在這裡不存在 —— 主題本來就為 `UserControl` 提供（透明的）外觀，組合出來的子控件各自套各自的 theme。

---

## 7. 三種模式怎麼選

| 需求 | 選擇 |
|------|------|
| 只改行為（攔輸入、快捷鍵、焦點行為），外觀沿用主題 | **A1**：繼承具體控件 + `StyleKeyOverride` |
| 加少量屬性、微調外觀，本質仍是「同一種控件」 | **A2**：繼承具體控件 + 自備 `BasedOn` ControlTheme |
| 全新外觀、狀態驅動、要進控件庫讓使用端可換 template | **B**：`TemplatedControl` + ControlTheme |
| 組合多個現成控件成一個 app/框架元件 | **C**：`UserControl`（repo 既有慣例） |
| 只是改顏色、字級、間距 | **不要繼承** — 用 `Styles` / 覆寫主題資源 |
| 只是給既有控件加一段可重用行為 | **不要繼承** — 用附加屬性（attached property）或 behavior |

---

## 8. 注意事項與常見雷

1. **忘記處理 StyleKey → 控件隱形**：繼承具體控件後畫面上什麼都沒有，第一時間檢查是否少了 `StyleKeyOverride`（A1）或 ControlTheme 沒併入 Resources（A2 / B）。
2. **`StyleKeyOverride` 同時影響 selector**：覆寫成基底型別後，`Selector="local|子類"` 不再匹配；要對子類加樣式改用 style class。
3. **override 虛擬方法記得呼叫 `base`**：除非刻意攔截（如 `NumericTextBox` 對非法輸入 return），否則漏掉 `base.OnXxx(e)` 會吃掉基底行為（焦點、偽類、事件）。
4. **`AvaloniaProperty.Register` 的 owner 型別寫子類自身**：照抄範例時最容易錯的位置。
5. **Semi.Avalonia 的 DataGrid theme 是獨立資源**：繼承或使用 `DataGrid` 時，確認 host 專案已引入對應 theme 套件，否則同樣隱形。
6. **`Primitives` 命名空間不是「禁區」**：`TemplatedControl`、`ToggleButton`、`HeaderedContentControl`、`RangeBase` 等都在其中，是官方預期的繼承基底。
7. **能不繼承就不繼承**：樣式問題用 `ControlTheme` / `Styles` 解，行為複用用 attached property 解；繼承留給「真的要一個新控件型別」的場景。
