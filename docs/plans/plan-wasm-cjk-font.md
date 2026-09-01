# 計畫：WASM head 的中文字型缺失

**狀態：📝 擬定中（2026-09-01）**

## 背景

`apps/Bee.Northwind` 的 Browser（WASM）head 在中文語系下，所有欄位標籤顯示為豆腐方塊
（`▯▯▯▯`）。2026-09-01 四平台冒煙測試中發現。

**這不是亂碼。** 字串與編碼都正確，是字型裡沒有那些字的字形（glyph）——編碼錯誤會顯示成
錯誤的字元（`æ¸¬è©¦`）而非整齊的方塊，且不會只影響中文。

功能本身完全正常：連線、RSA 握手、登入、FormSchema 載入、master-detail 查詢與金額加總
（352 = 252 + 100）都通過，純粹是顯示問題。

## 根因

所有 Avalonia head 都呼叫 `.WithInterFont()`，而 **Inter 字型不含 CJK 字形**，repo 內也沒有
任何內嵌的 `.ttf` / `.otf`。

**為何只有 WASM 出問題**：其餘平台在字形缺失時會 fallback 到系統字型——macOS 與 iOS 有
PingFang、Android 有 Noto CJK。瀏覽器沙箱裡沒有系統字型可借，Avalonia 只能用 app 內嵌的
那份 Inter，缺字就只能畫方塊。

實測佐證（同一份 UI、同一個字型，差別只在語系）：

| Head | UI 語系 | 結果 |
|------|---------|------|
| Browser (WASM) | zh-TW | 標籤全為方塊 |
| Android | en-US | 正常 |
| Desktop / iOS | — | 正常（系統字型 fallback） |

語系來自 `CultureInfo.CurrentUICulture.Name`
（[NorthwindDefinitions.ResolveLang](../../apps/Bee.Northwind/Bee.Northwind.UI/Controls/NorthwindDefinitions.cs)），
瀏覽器語系為中文時即載入 `Define/Language/zh-TW`。

## 需要多少字

目前 `Define/Language/zh-TW/` 只有 `Order.Language.xml` 一個檔，**唯一漢字 34 字**。

但 34 這個數字不能拿來當 subset 依據——加一張表單、加一個欄位就破，而破掉的樣子正是
使用者看到方塊、開發者看不出哪裡錯。**資料值**同樣要算進來：Northwind 的種子資料
（`Alfreds Futterkiste`、`Queso Cabrales`）全是西文，目前無中文資料，但這是 demo 的巧合
而非保證。

## 決策點

### D1：字型涵蓋範圍 —— subset 至常用字集（已定案 2026-09-01）

| 選項 | 大小 | 風險 |
|------|------|------|
| A. 完整 Noto Sans TC | ~5–6 MB | 無邊界問題；WASM 首載變重 |
| B. subset 至常用字集（Big5 常用 5401 字） | ~1–1.5 MB | 罕用字（人名、地名）仍為方塊 |
| C. subset 至現有語言檔用字（34 字） | ~10 KB | **不建議**——加一個欄位就破，且破得無聲 |

**採 B。** 5401 常用字覆蓋現代中文絕大多數用字，1.5 MB 對 WASM 可接受；A 的 5 MB 對
首次載入是實質負擔，而它多買到的是罕用字，對這個 demo 的價值不高。選 B 須在文件標明
「資料含罕用字時仍會缺字」這個邊界。

subset 以 `fonttools` 產生後**產物入版控**，不納入建置流程——一次性工作換掉持續維護成本，
字型升版時再重跑一次即可。

### D2：放在 app 層（已定案 2026-09-01）

**放 app 層（`Bee.Northwind.Browser`）。**

字型選擇是應用的品牌與在地化決策，不該由框架替所有消費者決定，更不該讓
`Bee.UI.Avalonia` 套件因此增加數 MB——那會傳染給每一個下游，包含根本不需要中文的專案。

框架該做的是**把這個雷寫進文件**：任何 Bee 的 WASM head 都會遇到同一件事，而它在桌面上
完全看不出來。

### D3：其餘 `WithInterFont()` 呼叫點不動（已定案 2026-09-01）

全 repo 共 6 處：Northwind 四個 head、`samples/Avalonia.DemoCenter`、`tools/DefineEditor`。

**只改 `Bee.Northwind.Browser`。** 其餘都是桌面或有系統字型 fallback 的平台，目前
無症狀。`tools/DefineEditor` 在 Linux 上可能缺中文字型，但那是未經確認的推測，不在本計畫
範圍——真要處理應另案並先實測。

## 實作步驟

1. 取得 Noto Sans TC（SIL OFL 授權，可再散布），以 `fonttools` subset 至常用字集（D1），產物入版控
2. 字型檔放入 `apps/Bee.Northwind/Bee.Northwind.Browser/Assets/`，設為 `AvaloniaResource`
3. `Program.cs` 移除 `.WithInterFont()`，改以 `FontManagerOptions` 指定內嵌字型為預設，
   並將 Inter 保留為拉丁字型的 fallback（見
   [Program.cs](../../apps/Bee.Northwind/Bee.Northwind.Browser/Program.cs)）
4. 確認授權檔（OFL.txt）一併納入
5. 量測 WASM 產出大小變化，記錄於本計畫

## 驗證

- 瀏覽器語系設為 zh-TW，登入後 Orders 清單與訂單明細的欄位標籤正確顯示中文
- 瀏覽器語系設為 en-US，畫面與現況一致（無回歸）
- 記錄 `dotnet publish` 後 `wwwroot` 的總大小變化
- 其餘三個 head 不受影響（本計畫不動它們）

## 明確不納入

- **其餘五個 `WithInterFont()` 呼叫點**（D3）
- **框架層提供 CJK 字型**（D2）——會讓所有下游套件變重
- **動態字型載入 / web font**：Avalonia 自行管理字型堆疊，不走瀏覽器的 `@font-face`，
  這條路需要先驗證可行性，不在本計畫範圍
