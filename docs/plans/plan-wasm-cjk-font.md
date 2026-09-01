# 計畫：WASM head 的中文字型缺失

**狀態：✅ 已完成（2026-09-01）**

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

## 決策：固定 WASM head 的 UI 語系，不內嵌字型

原本規劃內嵌 subset 過的 Noto Sans TC（見下方「被推翻的路線」）。實作到一半量出兩個數字後
改變方向：

- **要顯示的中文只有 24 個 key**（`Order.Language.xml`，34 個唯一漢字）
- **最小可用的 CJK 字型是 5.4 MB**（`NotoSansTC-Regular.otf`，SubsetOTF/TC 版）

為 24 個 key 讓 demo 背 5.4 MB 不成比例。改為在 Browser head 固定 UI 語系：

```csharp
CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("en-US");
```

定義的在地化走 `CultureInfo.CurrentUICulture`，釘住它就不會載入 `Define/Language/zh-TW`，
標籤回到 `FormSchema` 本身的英文 `Caption`，中文不再出現，也就不需要 CJK 字形。

**只改 Browser head。** 其餘三個 head 有系統字型可借（macOS / iOS 的 PingFang、Android 的
Noto CJK），中文顯示完全正常，仍跟隨系統語系並看得到 zh-TW 資源——那是
`71f995b3` 加語系資源時要示範的東西，不該為了 WASM 的顯示問題在所有平台關掉。

**副作用是好的**：`.smoke.yaml` 的 `expect_text`（`Order Details`、`Total Amount`）本來在中文
語系下必然對不上，釘住語系後冒煙流程的文字比對才真正有效。

### 被推翻的路線：內嵌 CJK 字型

留著是因為量到的數字對日後重啟這個議題有用，而不是要照著做。

| 方案 | 淨增量（相對 61 MB 基數） |
|------|------------------------|
| 完整 Noto Sans TC 取代 Inter | +3.6 MB（Inter 本身佔 1.81 MB） |
| subset 至常用字集取代 Inter | −0.3 MB，但需 fonttools 與常用字表，且罕用字**無聲**變方塊 |

**若日後 WASM 真的要支援中文**（例如接上 `SessionInfo.Culture` 讓使用者自選語系），
就得回到這條路線；屆時完整字型優於 subset——在 61 MB 的基數上，4 MB 差距換不到
「罕用字失敗且不報錯」這個風險。

### D3：其餘 `WithInterFont()` 呼叫點不動（維持）

全 repo 共 6 處。其餘 5 處都是桌面或有系統字型 fallback 的平台，目前無症狀。
`tools/DefineEditor` 在 Linux 上可能缺中文字型，但那是未經實測的推測，不憑推測擴大範圍。

## 實作結果

- [Program.cs](../../apps/Bee.Northwind/Bee.Northwind.Browser/Program.cs) 一行 culture 釘選 +
  說明為何如此（含「其他 head 不受影響」）
- 未新增任何資產、未改 csproj、bundle 大小不變

## 驗證（2026-09-01 實測）

於中文語系瀏覽器完成連線 → 登入 → Orders 清單 → 訂單 10248 明細：

- 欄位標籤全部正常顯示（`Order No`、`Customer Name`、`Total Amount`、`Order Details` 等），無方塊
- 資料與金額正確：P011 Queso Cabrales 252 + P003 Aniseed Syrup 100 = Total Amount 352
- 其餘三個 head 未改動

## 明確不納入

- **其餘五個 `WithInterFont()` 呼叫點**（D3）
- **內嵌任何 CJK 字型**——見「被推翻的路線」
- **讓 WASM 支援中文**：本計畫是讓 demo 不顯示中文，不是讓它正確顯示中文。
  真要支援得回到字型路線。
- **接上 `SessionInfo.Culture`**：框架有 `st_user.culture` → `SessionInfo.Culture` 的機制，
  但這個 demo 的 UI 讀的是 `CultureInfo.CurrentUICulture`，兩者無關；改接是另一個議題。
