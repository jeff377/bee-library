# 計畫：更新 ADR-002（Newtonsoft.Json → System.Text.Json 遷移評估）

## 背景

ADR-002 撰寫於 netstandard2.0 + net10.0 雙目標時期，記錄了選擇 Newtonsoft.Json 的理由及未來 STJ 遷移的可行性分析。

然而，**ADR-006 已於 2026-04-14 完成 net10.0 單一目標遷移**，導致 ADR-002 的多項內容與現實不符：

| 項目 | ADR-002 現有描述 | 實際狀態 |
|------|-----------------|---------|
| netstandard2.0 相容性 | 列為選擇 Newtonsoft.Json 的理由之一 | 已不適用，全部專案已遷移至 net10.0 |
| STJ 遷移前置條件 | 「先完成 net10.0+ 遷移」 | **已滿足** |
| JsonSerializationBinder | 「放棄 netstandard2.0 後可移除」 | 仍存在於 `Bee.Base/Serialization/` |
| 遷移可行性結論 | 以「未來方向」呈現 | 前置條件已達成，應升級為明確的遷移路線圖 |

## 修改目標

將 ADR-002 從「記錄現狀 + 未來展望」更新為「記錄現狀 + 已確認可行的遷移路線圖」，反映 net10.0 遷移後的真實狀態。

## 具體修改項目

### 1. 更新「狀態」區段

**現有**：`已採納`

**修改為**：
```
已採納 — 維持使用中，STJ 遷移前置條件已滿足（2026-04-14 完成 net10.0 單一目標遷移）
```

### 2. 更新「理由」區段

移除或標註已失效的理由：

- **netstandard2.0 相容性**（第 30 行）：標註為 `~~已不適用~~`，因所有專案已遷移至 net10.0
- 其餘理由保留（DataSet 序列化、外部介接定位、複雜序列化支援、XML 互通仍然有效）

### 3. 重構「未來方向：System.Text.Json 遷移評估」區段

#### 3a. 更新前置條件段落

- 將「前置條件」從「需先完成 net10.0+ 遷移」改為「**已滿足**（見 ADR-006，2026-04-14 完成）」
- 移除關於「維護雙軌引擎不建議」的討論（已無此問題）

#### 3b. 更新遷移障礙（反映程式碼現況的精確數據）

根據程式碼探索結果更新精確數據：

| 項目 | ADR-002 現有數據 | 程式碼實際狀態 |
|------|-----------------|---------------|
| Attribute 替換 | 25 檔，29 處 | 需重新確認（探索結果：12 `[JsonIgnore]` + 17 `[JsonProperty]` = 29 處，與文件吻合） |
| NullValueHandling | 6 處 | 6 處（JsonRpcRequest 3 處 + JsonRpcResponse 3 處），吻合 |
| DefaultValueHandling | 2 處 | 2 處（FormField + DbField），吻合 |
| JsonSerializationBinder | 文件說「放棄 netstandard2.0 後不再需要」 | **仍存在**，含 mscorlib ↔ System.Private.CoreLib 映射邏輯 |

#### 3c. 將「遷移可行性結論」改為「遷移路線圖」

將目前的結論段落擴充為分階段實施步驟：

```
### 遷移路線圖

前置條件：✅ 已完成 net10.0 單一目標遷移（ADR-006）

#### Phase 1：實作 STJ 自訂 Converter
- 實作 DataSet/DataTable 的 `JsonConverter<T>`（參考 MessagePack DataSetFormatter）
- 實作 ApiPayload 的 `JsonConverter<T>`（利用 TypeName 屬性替代 $type）
- 改寫 FilterNodeCollectionJsonConverter（JArray/JObject → JsonDocument/JsonElement）

#### Phase 2：替換 Attribute 與核心模組
- [JsonIgnore] → STJ [JsonIgnore]（12 處，命名空間變更）
- [JsonProperty("x")] → [JsonPropertyName("x")]（17 處）
- NullValueHandling.Include → JsonIgnoreCondition 配置（6 處）
- DefaultValueHandling.Include → [JsonIgnore(Condition = JsonIgnoreCondition.Never)]（2 處）
- 改寫 SerializeFunc.cs：JsonConvert → JsonSerializer

#### Phase 3：清理
- 移除 JsonSerializationBinder.cs（跨 runtime 名稱對應已不需要）
- 移除 Newtonsoft.Json NuGet 相依（Bee.Base.csproj）
- 更新 code-style.md 規範
```

### 4. 更新「影響」區段

新增說明遷移後的預期影響：
- SerializeFunc.cs 改用 `System.Text.Json.JsonSerializer`
- 所有 `[JsonProperty]` / `[JsonIgnore]` 改用 STJ 命名空間
- 移除 `Newtonsoft.Json` NuGet 套件相依

### 5. 更新「取捨」區段

新增一條：
- **netstandard2.0 已放棄**：不再是選擇 Newtonsoft.Json 的理由，STJ 在 net10.0 上功能完整

## 不修改的部分

- **決策本身**（「採用 Newtonsoft.Json」）不改變 — 這是歷史事實
- **DataSet 序列化分析**的技術細節（FieldDbType 對應表等）保持不變
- **三種序列化格式的定位表**保持不變

## 預估影響範圍

僅修改 1 個檔案：`docs/adr/adr-002-newtonsoft-json.md`
