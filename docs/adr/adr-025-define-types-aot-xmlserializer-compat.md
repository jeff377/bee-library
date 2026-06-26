# ADR-025：定義型別相容 AOT reflection XmlSerializer（單一 Add + 無參數建構子）

## 狀態

已採納（2026-06-26）

## 背景

Bee 的定義型別（`FormSchema`、`TableSchema`、`ProgramSettings`、`FormLayout`、`LanguageResource`、`DbCategorySettings`、`PermissionModels` 等）以 **XML 持久化**，且 client 端透過 `ClientDefineAccess.GetDefineAsync<T>` → `XmlCodec.Deserialize`（`System.Xml.Serialization.XmlSerializer`）反序列化。桌面（`net10.0`）與瀏覽器（Avalonia WASM，非 AOT）執行環境**有 `Reflection.Emit`**，XmlSerializer 走 **code-generated** 路徑，多年來運作正常。

新增 `Bee.Northwind.iOS`（Avalonia iOS head，見 `docs/plans/plan-northwind-ios.md`）時，連線 + 登入成功，但**載入任何 XML 定義即崩**。根因：**iOS 禁止動態產碼（無 `Reflection.Emit`）**，XmlSerializer 退回 **reflection-only** 路徑（`ReflectionXmlSerializationReader`），暴露 Bee 定義型別兩個與該路徑不相容之處：

1. **集合多載 `Add` → `AmbiguousMatchException`**：reflection reader 以 `Type.GetMethod("Add")`（**不帶參數型別**）尋找集合的 add 方法。Bee 集合（`KeyCollectionBase<T>` / `CollectionBase<T>` 子類）通常有多個 public `Add`：繼承的 `Add(T)`、基底介面的 `Add(I…CollectionItem)`、各集合的便利 `Add(string, …)`。多於一個即丟 `AmbiguousMatchException`。
2. **集合無無參數建構子 → `MissingMethodException`**：reflection reader 以 `Activator.CreateInstance(type)` 建立集合。許多定義集合是 **owner-coupled**（建構子只收 owner，如 `ProgramCategoryCollection(ProgramSettings)`），無 public 無參數建構子。code-gen 路徑用 getter 的既有實例（lazy-init with owner）故無此需求，reflection 路徑則需要。

兩者皆**僅在 AOT/reflection 路徑觸發**；桌面 / WASM 的 code-gen 路徑本就正常。此為框架對外 API surface（定義型別）與其序列化機制之間的結構性約束，故立此 ADR。

## 考慮過的選項

1. **集合基底實作 `IXmlSerializable`**：自訂 Read/Write，繞過 reflection 對 `Add` / 建構子的探測。**否決**——member 一旦是 `IXmlSerializable`，XmlSerializer 會**忽略 member 層的 `[XmlArray]` / `[XmlArrayItem]` 與多型處理**（如 `LanguageResource.Items` 的 `[XmlArray("Items")]`、`FilterNodeCollection` 的 `[XmlInclude]` 元素名多型）。等於要在 ReadXml/WriteXml 裡重做 XmlSerializer 的 array + 多型邏輯，又大又脆，且**會改變既有 XML 格式**。
2. **定義 wire 改 JSON / MessagePack**：client 改用 `System.Text.Json` / MessagePack 反序列化定義（`ICollection<T>.Add(T)` 不撞多載、不需 reflection-only XmlSerializer）。技術可行且合「XML=持久化、JSON/MsgPack=wire」分軸，但**否決**——使用者明確選擇保留 XML 反序列化路徑；且需動 server + client 的定義傳輸，blast radius 大。
3. **iOS head 預生成 XmlSerializer（`Microsoft.XmlSerializer.Generator` / sgen）**：build 期把定義型別的 serializer 展成靜態碼，繞過 runtime reflection。**否決**——每個需序列化型別要納入 generator、build 設定繁瑣，且只是 iOS 端 band-aid，未消除框架型別本身的不相容。
4. **讓定義型別本身相容 reflection XmlSerializer（採用）**：見下節。最小、格式不變、一次涵蓋所有 AOT 目標、不需 per-type 維護。

## 決策

採**選項 4**，兩處修正，**XML 格式逐字不變**（XmlSerializer 仍照常處理集合，`[XmlArray]` / 多型保留）：

- **D1 — 每個定義集合只剩一個 public instance `Add(T)`**：
  - 基底 `KeyCollectionBase<T>` / `CollectionBase<T>` / `MessagePackKeyCollectionBase<T>` / `MessagePackCollectionBase<T>` 的介面 `Add(IKeyCollectionItem)` / `Add(ICollectionItem)` 改為**顯式介面實作**（`void IKeyCollectionBase.Add(…)`）——從 `Type.GetMethod("Add")` 的 public 探測中消失，介面呼叫照常。
  - 各定義集合的便利 `Add(...)`（如 `FormFieldCollection.Add(string, string, FieldDbType)`）改為**擴充方法**（`public static … Add(this XxxCollection? collection, …)` + `ArgumentNullException.ThrowIfNull`，置於同檔同 namespace）。呼叫端 `.Add(...)` 語法不變（C# 找不到 instance 多載即解析到擴充方法），`Type.GetMethod("Add")` 不會探測到擴充方法。
- **D2 — owner-coupled 集合補 public 無參數建構子**：為缺無參數建構子的 12 個定義集合補 `public Xxx() : base()`。reflection reader 對唯讀集合屬性會建一個 temp（需此建構子）再把項目落入 getter 的 owner-coupled 集合，故 **owner 不受影響**（temp 為 null owner、items 進真正的 owner 集合）。item 型別本就有無參數建構子。

## 影響

- iOS（及任何 AOT 目標：未來 MAUI iOS、NativeAOT 等）可正確反序列化所有 XML 定義。Northwind iOS 端到端通過（連線→登入→選單→清單真實資料→開記錄）。
- **格式不變**：Definition 723 + Base 464 round-trip 序列化測試全綠驗證；既有持久化定義檔不需遷移。
- **桌面 / 瀏覽器零行為影響**：code-gen 路徑本就用 getter + 強型別 Add，此修法對其透明。
- **API surface 微調（breaking，見 CHANGELOG）**：便利 `Add(...)` 從 instance method 變擴充方法——**原始碼相容**（`.Add(...)` 語法不變、擴充類別同 namespace 不需新 using），但對**已編譯**的外部使用者是 binary breaking。少數內部呼叫端 fallout 一併修（補 `using Bee.Definition.Collections`、`!` null 斷言、介面 Add 測試 cast 到介面）。
- **後續規範（定義型別撰寫約束）**：新增定義集合時——
  1. **只暴露一個 public instance `Add`**（繼承的強型別 `Add(T)`）；便利多載一律以**擴充方法**提供。
  2. **必備 public 無參數建構子**（owner-coupled 集合除了 owner 建構子外，補一個 `: base()`）。
  3. 序列化的 **item 型別亦須有 public 無參數建構子**。
  以 reflection XmlSerializer 相容為硬性要求，避免未來新型別再於 AOT 目標踩雷。

## 參考

- `docs/plans/plan-northwind-ios.md`（階段 3 結果）
- 記憶：`ios-xmlserializer-ambiguous-add`
