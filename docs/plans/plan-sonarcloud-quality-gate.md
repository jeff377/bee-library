# 計畫：解除 SonarCloud Quality Gate ERROR

**狀態：📝 擬定中**

## 背景

SonarCloud `jeff377_bee-library` 自 v4.0.1（2026-04-17）以後，main 分支的 Quality Gate 持續為 **ERROR**。
2026-04-28 巡檢時兩個失敗條件如下：

| metric | 實際 | 門檻 | 狀態 |
|---|---|---|---|
| `new_duplicated_lines_density` | 3.04% | < 3.0% | ❌ ERROR |
| `new_security_hotspots_reviewed` | 66.7% | = 100% | ❌ ERROR |

其餘 reliability/security/maintainability rating 與 new_coverage（87.9%）皆 OK，整體 coverage 92.5%。

兩項都不是 `/sonar-fix` 自動修正範圍內的問題（hotspot 改狀態需 token、duplications 涉及 public API），須以本計畫追蹤。

---

## 項目一：S5766 Security Hotspot 標記為 Safe

**狀態：✅ 已完成（2026-04-28）** — 使用者已於 SonarCloud UI 手動標記為 Safe。

### 現況
- 位置：`src/Bee.Api.Core/Messages/ApiErrorInfo.cs:29`
- Hotspot key：`AZ3Pym7ZqgvhdYcrszSU`
- 規則：`csharpsquid:S5766`（object-injection / LOW probability）
- 訊息：Make sure not performing data validation after deserialization is safe here.

### 安全性評估

依 `.claude/rules/sonarcloud.md` §14（S5766）：

> `[Serializable]` marker 類別之建構子若**未**實作 `(SerializationInfo, StreamingContext)`、屬性皆為原始型別、不經 `BinaryFormatter` 反序列化，則非實際反序列化入口，於 Sonar UI 標記 **Safe** 並說明理由即可，不必改程式。

`ApiErrorInfo` 完全符合：

```csharp
[Serializable]
public class ApiErrorInfo : IObjectSerializeBase
{
    public ApiErrorInfo() { }
    public ApiErrorInfo(Exception exception, bool includeStackTrace = false) { ... }

    public string Message { get; set; } = string.Empty;
    public string StackTrace { get; set; } = string.Empty;
    public bool IsHandled { get; set; } = false;
    // ...
}
```

- ✅ 沒有 `(SerializationInfo, StreamingContext)` constructor
- ✅ 屬性皆為 `string` / `bool`（原始型別）
- ✅ 全專案使用 System.Text.Json，未使用 BinaryFormatter

### 處置

到 SonarCloud UI 將該 hotspot 標 **Safe**，理由模板：

```
ApiErrorInfo is a plain JSON-RPC DTO. The class does not implement the
(SerializationInfo, StreamingContext) constructor and all properties are
primitive types (string/bool). This codebase exclusively uses
System.Text.Json (Newtonsoft.Json and BinaryFormatter are forbidden by
project rules), so this constructor is never reached through legacy
serialization paths. No additional validation is required.
```

操作路徑：SonarCloud → Project → Security Hotspots → 該條目 → "Safe" → 填理由 → Submit

完成後 `new_security_hotspots_reviewed` 應回到 100%。

---

## 項目二：對稱型別重複行重構評估

### 現況
- `new_duplicated_lines_density = 3.04%`（剛超過 3.0% 門檻）
- 整體 `duplicated_lines = 1446`、`duplicated_blocks = 37`、`duplicated_lines_density = 4.3%`
- 主要來源（依檔案 dup_lines 排序）：
  | 檔案 | dup_lines |
  |---|---|
  | `src/Bee.Definition/Layouts/LayoutItem.cs` | 68 |
  | `src/Bee.Definition/Layouts/LayoutColumn.cs` | 68 |
  | `src/Bee.Base/Collections/KeyCollectionItem.cs` | 52 |
  | `src/Bee.Base/Collections/CollectionItem.cs` | 52 |
  | （其餘散落於 Bee.Db、Bee.ObjectCaching、Bee.Api.Client 各 ~46 行） |

### 觀察
- 這些檔案在 v4.0.1 之前就已存在重複，但 SonarCloud 在新版本分析時把對稱型別重新計入 leak。
- 屬於「**對稱型別**」（item / column、collection / keyed-collection）刻意分離的設計，而非單純複製貼上。
- 涉及 `public` 類別 API，無法以 `/sonar-fix` 自動修正規則重構。

### 待評估方向

| 方向 | 風險 | 影響 |
|---|---|---|
| A. 抽出共用基底（abstract base / generic base） | API 可能變更（新增父型別） | 需確認下游 NuGet 使用者；可能要 major bump |
| B. 以 source generator 產生重複部分 | 學習成本高、debug 困難 | API 不變，但增加建置複雜度 |
| C. 不重構，調整 SonarCloud 的 leak 計算規則或門檻 | 治標不治本，未來新增類似類別會繼續累積 | 0 程式碼風險 |
| D. 不重構，於 SonarCloud 把這些檔案排除在 duplication 計算外 | 同 C | 0 程式碼風險 |

### 暫定立場
短期：**選 D**（在 SonarCloud 設定 duplication exclusions 排除已知對稱型別）以解除 ERROR 狀態。
長期：v5.x 規劃時再評估 A，作為 API 重構的一部分。

### 待辦
1. 列出所有「對稱型別對」（含 Bee.Db / Bee.ObjectCaching / Bee.Api.Client 散落的部分）
2. 在 SonarCloud → Project Settings → General → Analysis Scope → Duplications 加入 exclusion pattern
3. 觸發重新分析，確認 `new_duplicated_lines_density` 回落到門檻以下
4. 將 exclusion 規則同步寫入 `sonar-project.properties`（若有），讓設定可版控

---

## 完成定義

- SonarCloud Quality Gate 為 **PASSED**
- `new_security_hotspots_reviewed = 100%`（hotspot 已標 Safe）
- `new_duplicated_lines_density < 3.0%`（exclusion 生效或重構完成）
