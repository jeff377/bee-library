# Bee.Business：BO 介面的開設判準

本檔在 agent 觸及 `src/Bee.Business/` 下任何檔案時自動載入（巢狀 `CLAUDE.md` 為 lazy loading）。

> 這一節原本記在 `.claude/rules/definition.md`，但 `IBusinessObject` /
> `ISystemBusinessObject` / `IFormBusinessObject` 都在**本專案**，2026-08-12 歸位。

## BO 介面是 BO-to-BO 解耦層，與 API 開放面各自獨立

axis 介面的定位是 **BO-to-BO 解耦層**：caller 透過 `IBusinessObjectFactory` 以 progId 解析、
cast 到介面後呼叫，不繫結具體 BO 類別。這樣 host 端 BO 客製化（多租戶換 SystemBO 子類、
業務替換 FormBO 子類）才不破壞 caller。

**兩個表面各自獨立，彼此不蘊含**：`[ApiAccessControl]` 是給**外部**（client 經
`JsonRpcExecutor` 呼叫）的表面，axis 介面是給**內部**呼叫的表面。**沒有硬性規定**
——開放給 API 的方法不必然要上介面，介面上的方法也不必然要開放給 API。

判準只有一條。新增 BO method 時問：「會不會有另一個 BO、背景作業或排程透過
`_ctx.BoFactory.CreateXxxBO(...)` 拿到後呼叫它？」是 → 放介面；否 → 不放。
介面爆成「所有 public 方法集合」就失去意義，也增加 host 端客製化負擔。

> **`CreateFormBO` / `CreateSystemBO` 在 `src/` 內零 caller 是預期的、不是死碼。**
> 框架內部沒有 BO-to-BO 場景（`JsonRpcExecutor` 依 progId 派送、不知道是哪條軸），
> 呼叫端是 **host 的業務 BO**。2026-08-12 的未使用型別盤點一度列為清理候選，查證後保留。
>
> **不是每條軸都要有介面 —— 只有「會被別的 BO 呼叫」的軸才要。** 因此 `ILogBusinessObject`
> 與 `CreateLogBO` 已於 2026-08-12 移除（其 XML doc 自承是「reserved for future」——那是預留、
> 不是需求）；`LogBusinessObject` 的方法照樣經 `JsonRpcExecutor` 對外開放，不受影響。
>
> **判斷時要把 server 端的背景呼叫端算進去，不能只看 client。** `Login` 曾被本規則誤列為
> 「只給 client、不放介面」（2026-08-12 更正）—— 它有真實內部呼叫端：**背景作業會以某身份
> 登入建立連線**，再模擬該使用者操作。判準沒錯，錯在漏算背景作業這類呼叫端。
