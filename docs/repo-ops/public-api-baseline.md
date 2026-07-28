# 公開 API 基準（PublicAPI.*.txt）維運

`src/` 下每個套件都有一對基準檔，記錄該組件已宣告的公開表面：

| 檔案 | 內容 |
|------|------|
| `PublicAPI.Shipped.txt` | **已發布**版本的公開表面。發版時才整批更新 |
| `PublicAPI.Unshipped.txt` | 上次發版**之後**新增的公開 API |

由 `Microsoft.CodeAnalysis.PublicApiAnalyzers` 在每次 build 比對，兩個方向都擋：

| 情況 | 診斷 | 你要做的事 |
|------|------|-----------|
| 新增了 public 型別／成員 | `RS0016` | 把診斷訊息裡的那一行加進 `PublicAPI.Unshipped.txt` |
| 刪除或改了簽名 | `RS0017` | 從基準檔刪掉舊的那一行（**這正是 review 要看見的 diff**） |
| Razor 產生碼的 nullable-oblivious 簽名 | `RS0041` | 已於 `Bee.Web.Blazor.Server.csproj` 以 `NoWarn` 關閉，見該處註解 |

> **為什麼要有這個機制**：在此之前，「公開表面有刪改」的唯一把關是 commit subject 要帶 `!`，
> 而這道人工關卡已經連續兩次漏掉真實的 breaking（`IExcelHelper`、`IEvictableCache`）。
> 基準檔把它變成 build 失敗，以及 review 時看得見的一行 diff。

## 日常：改了公開 API 怎麼辦

build 失敗的訊息本身就含正確格式的那一行，例如：

```text
error RS0016: Symbol 'Bee.Base.Foo.Bar() -> void' is not part of the declared public API
```

把單引號內的字串整行貼進該專案的 `PublicAPI.Unshipped.txt` 即可（維持排序不是硬性要求，但建議）。
IDE 內也可用分析器提供的 code fix（*Add to public API*）自動加入。

## 發版時

把各專案 `PublicAPI.Unshipped.txt` 的內容併入同專案的 `PublicAPI.Shipped.txt`，
再把 `Unshipped` 清空（保留 `#nullable enable` 標頭）。併入前的 `Unshipped` 內容就是
該版新增公開 API 的完整清單，可直接拿來對帳 CHANGELOG。

## 整批重建基準（少用）

只有在基準檔大規模失準時才需要——例如剛導入分析器，或一次搬動大量命名空間。

```bash
SARIF=$(mktemp -d)
for i in $(seq 1 10); do
  rm -f "$SARIF"/*.sarif
  dotnet build Bee.Library.slnx --configuration Release -p:BeeSarifDir="$SARIF" >/dev/null 2>&1
  for proj in src/*/*.csproj; do
    name=$(basename "$proj" .csproj)
    [ -f "$SARIF/$name.sarif" ] && python3 tools/scripts/gen-public-api.py "$SARIF/$name.sarif" "$(dirname "$proj")/PublicAPI.Shipped.txt"
  done
done
```

需要迴圈是因為相依專案要先編譯成功，下一層才會被分析——每跑一輪解開一層，
本 repo 的相依深度約需 6 輪收斂。

`-p:BeeSarifDir` 這個開關定義在 `src/Directory.Build.props`，未傳值時完全不生效。
