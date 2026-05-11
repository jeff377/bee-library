# 計畫：存取策略修補

**狀態：✅ 已完成（2026-05-10）**

## 背景

存取策略審查發現四項待修補問題，依嚴重程度排序：

| 編號 | 問題 | 嚴重性 |
|------|------|--------|
| P0 | 解密先於授權 | 🔴 高 |
| P1 | `BackendInfo` 缺啟動期驗證 | 🟡 中 |
| P3 | `ApiAccessControlAttribute` 不支援 Class 層級 | 🟡 中 |
| P4 | 缺細粒度授權（RBAC）框架 | 🟢 低（需另立計畫） |

> P2（IsLocalCall 信任問題）已確認為設計正確行為，不納入修補範圍。

---

## P0：解密先於授權

### 問題描述

`JsonRpcExecutor.ExecuteAsyncCore()` 目前執行順序：

```
GetApiEncryptionKey(format)        // 取 Session Key（AccessToken → SessionInfoService）
RestoreFrom(...)                   // Decrypt → Decompress → Deserialize  ← 先解密
ParseMethod(request.Method)
ExecuteMethodAsync()
  └─ CreateBusinessObject()
  └─ GetMethod(action)
  └─ ApiAccessValidator.ValidateAccess()  // Token 驗證才在這裡
  └─ method.Invoke()
```

**攻擊面**：伺服器在確認 AccessToken 有效之前已完成解密。若配置使用共用靜態金鑰（`BackendInfo.ApiEncryptionKey`），未認證的請求可觸發完整的反序列化路徑，造成：
- CPU / 記憶體資源濫用
- 在 MessagePack 反序列化路徑中暴露潛在的反序列化漏洞面

### 核心挑戰

Token 預驗的困難在於：在解密之前，我們無法取得 `ApiAccessControlAttribute`（需要知道 progId 與 action 才能 Reflect 取得 method），而不知道 AccessRequirement 就無法決定是否需要驗 Token（Login 等 Anonymous API 不需要 Token）。

然而，`request.Method` 是明文欄位（不在加密 Payload 中），可以在解密前就取得 progId 與 action。

### 解決方案

**在解密前先 Parse Method，再做 Token 預驗**：

```
ParseMethod(request.Method)                  // 移至最前
FindAccessAttribute(progId, action)          // 取得 ApiAccessControlAttribute
if Authenticated → PreValidateToken()        // Token 預驗，無效直接 throw
GetApiEncryptionKey(format)                  // 有效才取 Key
RestoreFrom(...)                             // 才解密
ExecuteMethodAsync()
  └─ CreateBusinessObject()
  └─ GetMethod(action)
  └─ ApiAccessValidator.ValidateAccess()     // 完整驗證（含 Format 檢查）
  └─ method.Invoke()
```

### 實作細節

1. 新增 `FindAccessAttributeForMethod(progId, action)` 靜態輔助方法，在不建立 BO 實例的情況下透過 Reflection 找到對應 MethodInfo 並取得 attribute。

2. 「預驗 Token」只做最低限度檢查（Guid.Empty 與 Session 存在性 + 過期），不重複完整的 Format 驗證（完整驗證仍由 `ApiAccessValidator.ValidateAccess` 執行，避免邏輯重複）。

3. 此重構不影響 `ApiAccessValidator` 的邏輯，完整驗證路徑不變。

### 影響範圍

- `Bee.Api.Core/JsonRpc/JsonRpcExecutor.cs`（主要改動）
- `Bee.Api.Core/Validator/ApiAccessValidator.cs`（可能新增 static helper）
- 對應單元測試需新增「Token 無效時應在解密前 throw」的測試案例

### 風險

- `FindAccessAttributeForMethod` 需要能在不建立 BO 實例的情況下 Reflect，需確認 BO 型別解析路徑與 `CreateBusinessObject` 一致，避免兩段邏輯不同步。

---

## P1：`BackendInfo` 缺啟動期驗證

### 問題描述

三個必要元件以 `null!` 初始化：

```csharp
// Bee.Definition/BackendInfo.cs
public static IApiEncryptionKeyProvider ApiEncryptionKeyProvider { get; set; } = null!;
public static IAccessTokenValidator AccessTokenValidator { get; set; } = null!;
public static IBusinessObjectFactory BusinessObjectFactory { get; set; } = null!;
```

若 `BackendInfo.Initialize()` 未被呼叫，或 `InitializeComponents` 因某些原因未完整執行，系統在第一次收到請求時才會拋出 `NullReferenceException`，且錯誤訊息不明確。

### 解決方案

在 `BackendInfo.Initialize()` 末尾新增 `ValidateComponents()` 呼叫，啟動時快速失敗：

```csharp
private static void ValidateComponents()
{
    if (SysInfo.IsSingleFile)
        return;  // Single-file 模式不使用這些元件

    if (ApiEncryptionKeyProvider == null)
        throw new InvalidOperationException(
            $"{nameof(ApiEncryptionKeyProvider)} is not configured. Call BackendInfo.Initialize() with a valid configuration.");
    if (AccessTokenValidator == null)
        throw new InvalidOperationException(...);
    if (BusinessObjectFactory == null)
        throw new InvalidOperationException(...);
}
```

### 影響範圍

- `Bee.Definition/BackendInfo.cs`（新增 `ValidateComponents` + 於 `Initialize` 末尾呼叫）
- 單元測試：驗證未配置時的啟動期例外

### 注意事項

`SysInfo.IsSingleFile` 分支不使用這些元件，驗證需跳過（如現行 `InitializeComponents` 的做法）。

---

## P3：`ApiAccessControlAttribute` 不支援 Class 層級

### 問題描述

```csharp
[AttributeUsage(AttributeTargets.Method, Inherited = true)]
public class ApiAccessControlAttribute : Attribute { ... }
```

目前每個 BO 方法都必須逐一標注。新增方法時若遺漏，會在執行期拋出例外（Fail-Closed，行為正確），但開發體驗不佳，且每個 BO class 都有大量重複的屬性宣告。

### 解決方案

**擴充 `AttributeTargets.Method | AttributeTargets.Class`，並更新查找順序**：

```csharp
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, Inherited = true)]
public class ApiAccessControlAttribute : Attribute { ... }
```

查找順序（由特殊到一般）：
1. Method 上的直接 `[ApiAccessControl]`（最高優先）
2. Method 的 base definition 上的 `[ApiAccessControl]`（override 繼承）
3. Method 的 `DeclaringType`（Class）上的 `[ApiAccessControl]`（class 預設）
4. `null` → throw `UnauthorizedAccessException`（Fail-Closed 維持）

### 典型使用場景

```csharp
// Class 層級設定預設：全部 Encrypted + Authenticated
[ApiAccessControl(ApiProtectionLevel.Encrypted)]
public class OrderBusinessObject : BusinessObjectBase
{
    // 繼承 class 預設，無需重複標注
    public OrderResult GetOrder(GetOrderArgs args) { ... }

    // 特例：覆蓋為公開 API
    [ApiAccessControl(ApiProtectionLevel.Public, ApiAccessRequirement.Anonymous)]
    public PingResult Ping(PingArgs args) { ... }
}
```

### 影響範圍

- `Bee.Definition/Attributes/ApiAccessControlAttribute.cs`（`AttributeUsage` 修改）
- `Bee.Api.Core/Validator/ApiAccessValidator.cs`（`FindAccessAttribute` 擴充查找邏輯）
- 現有所有 BO 可**選擇性**遷移至 class 層級宣告（不影響現有方法層級標注）
- 需新增測試案例：class 層級屬性繼承、方法層級覆蓋 class 層級

### 向下相容性

現有標注方式完全相容，不需強制遷移。

---

## P4：細粒度授權（RBAC）

### 問題描述

目前框架僅有二元授權：`Anonymous` vs `Authenticated`。所有已登入使用者對同一 API 具有相同存取權，業務層無法宣告式地表達「只有管理員可呼叫」這類需求。

### 評估

此項改動對架構影響較大（涉及 Session 中的使用者角色/權限欄位、Attribute 宣告方式、執行期驗證邏輯），且未知現有 BO 層是否已有自己的授權實作。

**建議**：另立獨立計畫，進行充分的需求討論後再設計實作方案。本修補計畫暫不納入。

---

## 執行順序

```
P1（BackendInfo 啟動驗證）   ← 改動最小，安全網最直接
P0（解密先於授權）           ← 核心安全問題，需仔細測試
P3（Class 層級屬性）         ← 功能增強，向下相容
P4（RBAC）                   ← 另立計畫
```

P1 先做是因為它是安全網——若 P0 改動中有初始化相關的錯誤，P1 的驗證機制能在啟動期提前暴露問題。

---

## 受影響檔案清單（預估）

### Bee.Definition
- `BackendInfo.cs`

### Bee.Definition（Attributes）
- `Attributes/ApiAccessControlAttribute.cs`

### Bee.Api.Core
- `JsonRpc/JsonRpcExecutor.cs`
- `Validator/ApiAccessValidator.cs`

### Tests
- `tests/Bee.Api.Core.UnitTests/` — P0、P3 驗證
- `tests/Bee.Definition.UnitTests/` — P1 驗證
