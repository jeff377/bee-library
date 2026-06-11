# Avalonia.Editors.Gallery

驗證 `Bee.UI.Avalonia` field editor 控件組（`TextEdit` / `MemoEdit` / `ButtonEdit` / `DateEdit` / `YearMonthEdit` / `DropDownEdit` / `CheckEdit`）在 **Semi.Avalonia** 主題下與原生控件的樣式一致性，同時示範 `FormScope` ambient 綁定的最小用法。

繼承控件以 `StyleKeyOverride` 沿用原生 ControlTheme，理論上視覺應與原生完全一致；本 gallery 把兩者並排，讓回歸一眼可見。

## 前置條件

- .NET 10 SDK
- 無需後端、無需資料庫——資料來源是 in-memory `FormSchema` + `FormDataObject`

## 跑起來

```bash
dotnet run --project samples/Avalonia.Editors.Gallery/Avalonia.Editors.Gallery.csproj
```

## 比對 checklist

每個 `ControlType` 一個區塊，左欄原生控件、右欄繼承控件，各含「一般」與「唯讀 / 停用」兩種狀態：

1. **靜態樣式**：左右兩欄的背景、邊框、圓角、字體、間距應完全一致
2. **互動偽類**：hover / focus（點進輸入框）/ pressed 的視覺反應應一致
3. **Light/Dark**：右上角開關切換主題，逐區重看 1–2
4. **綁定行為**：在右欄輸入，底部「FormDataObject 即時欄位值」應即時更新；
   `TextEdit` 受 `MaxLength=20` 限制、`DropDownEdit` 選項來自 `FormField.ListItems`
5. **唯讀列**：右欄唯讀（經 `LayoutField.ReadOnly` 綁定）與左欄手動停用的視覺應對等

## 對應 library 元件

| Demo 行為 | library 元件 |
|-----------|--------------|
| 繼承控件沿用 Semi 樣式 | [src/Bee.UI.Avalonia/Controls/Editors/](../../src/Bee.UI.Avalonia/Controls/Editors/)（各控件 `StyleKeyOverride`） |
| ambient 綁定（容器設一次） | [FormScope.cs](../../src/Bee.UI.Avalonia/Controls/Editors/FormScope.cs) |
| 共用綁定狀態機 | [FieldEditorBinder.cs](../../src/Bee.UI.Avalonia/Controls/Editors/FieldEditorBinder.cs) |
| 欄位值即時刷新 | [FormDataObject.cs](../../src/Bee.UI.Avalonia/DataObjects/FormDataObject.cs) 的 `FieldValueChanged` 事件 |

## 與其他 sample 的關係

- `Avalonia.Demo`（Fluent 主題）示範 `FormView` 走完整 Connection → Login → CRUD 流程；本 gallery 不連後端，專職樣式比對
- 主題對齊 `tools/DefineEditor`（Semi.Avalonia 12.0.3），即 field editor 的實際使用情境

## 不做的事

- 不連後端、不示範 Login / CRUD（看 `Avalonia.Demo`）
- 不比對 Fluent 主題（`StyleKeyOverride` 與主題無關，Semi 驗過即足以代表機制正確）
- `ButtonEdit` 只展示內嵌按鈕外觀；lookup 開窗流程不在 field editor 第一版範圍
