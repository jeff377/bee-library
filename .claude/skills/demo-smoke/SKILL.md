---
name: demo-smoke
description: 對 `samples/<Sample.Name>/` 的 demo 跑一輪端到端冒煙測試（透過 computer-use 點擊 UI 元素、驗證關鍵畫面），以宣告 demo 在本機跑得通。讀取 `samples/<Sample.Name>/.smoke.yaml` 設定檔。當使用者要「跑 demo」、「驗證 sample」、「demo 跑得通嗎」、「smoke test sample」之類需求時使用。
---

# Sample Demo 端到端冒煙

對 `samples/<Sample.Name>/` 跑一輪可重現的 UI 流程驗證。整個流程**從外部觀察**：先起依賴（QuickStart.Server 等）、開 app、模擬點擊、截圖核對、清乾淨。各 sample 的「成功畫面長什麼樣」寫成 `.smoke.yaml`，本 skill 是 runner。

## 適用場景

- 改完 Bee.UI.Maui / Bee.Web.Blazor.* 後想確認 sample 還能跑
- 加新 sample 後做最後 sanity check
- Release 前對所有 sample 一次冒煙

## 不適用

- 單元測試（用 `dotnet test`）
- 完整 UI E2E 測試（用真正的 Playwright / Selenium / Appium 工具鏈，本 skill 是「快速看一眼」級別）
- 沒有 GUI 的 sample（Console 用 stdout grep 即可，不需 computer-use）

## 前置

- macOS 主開發機（Mac Catalyst sample / Blazor in Safari 都靠它）
- `claude` desktop 有 computer-use MCP 權限
- 對應 sample 已 build 過（本 skill 不負責 build）

## 設定檔：`samples/<Sample.Name>/.smoke.yaml`

每個有 GUI 的 sample 自帶一份 `.smoke.yaml`。範例：

```yaml
# samples/Maui.Demo/.smoke.yaml
name: Maui.Demo
display_name: Bee MAUI Demo

# 起 demo 之前要跑的依賴 process
prerequisites:
  - id: server
    cwd: samples/QuickStart.Server
    cmd: dotnet run --configuration Release --no-build
    ready_when: "Now listening on"   # 等 stdout 出現這串才算 ready
    timeout_seconds: 30

# demo app 自身啟動方式
launch:
  app_name: Bee MAUI Demo
  bundle_path: samples/Maui.Demo/bin/Debug/net10.0-maccatalyst/maccatalyst-arm64/Bee MAUI Demo.app
  # 或：
  # launch_cmd: open <bundle_path>
  # 預設用 macOS `open` 命令啟動 .app

# 點擊與驗證流程
flow:
  - step: take initial screenshot
    action: screenshot
    expect_text:                          # 至少要看到的字串（用 OCR / 截圖比對）
      - "Bee MAUI demo"
      - "Endpoint"
      - "Connect"

  - step: click Connect
    action: click
    # target 描述按鈕；runner 可用截圖座標、accessibility tree、或 label fallback
    target: { label: "Connect" }
    wait_after_seconds: 4                 # 等 reachability check + ping
    expect_text:
      - "Login"
      - "Sign in"

  - step: click Sign in
    action: click
    target: { label: "Sign in" }
    wait_after_seconds: 6                 # 等 RSA 握手 + Login
    expect_text:
      - "Employee"
      - "Alice Chen"                      # 種子第一筆
      - "Bob Liu"                         # 第二筆
      - "Carol Wang"                      # 第三筆

  - step: final screenshot
    action: screenshot
    save_as: maui-demo-employee-page.png  # 給人工確認用

# 清乾淨
teardown:
  - kill_app: true                        # 結束時 kill app process
  - kill_prerequisites: true              # 結束時 kill server
```

## 執行流程

### Step 1：讀 `.smoke.yaml`

```bash
test -f samples/<Sample.Name>/.smoke.yaml || {
  echo "Sample 沒有 .smoke.yaml，請先為它寫一份"
  exit 1
}
```

不存在 → 停下，問使用者要不要從樣板生成一份（不自動生）。

### Step 2：起 prerequisites

對每個 prerequisite：
- 在 background 跑 `cmd`（用 Bash tool 的 `run_in_background`）
- poll log 直到 `ready_when` 字串出現，或超過 `timeout_seconds` 失敗
- 紀錄 pid 給 teardown 用

若任一 prerequisite 失敗 → stop，輸出 log tail，**不**啟動 app。

### Step 3：呼叫 `request_access`，啟動 app

```
mcp__computer-use__request_access(
  apps=[launch.app_name],
  reason="Run smoke test for samples/<Sample.Name>"
)
```

接著 `open <bundle_path>`（or 跑 `launch.launch_cmd`），等 5-8 秒讓 MAUI app 起 UI。

### Step 4：跑 flow 每一步

依序執行：

```python
for step in flow:
  if step.action == "screenshot":
    screenshot()  # 截圖；若有 expect_text，做 OCR / 圖比對
    verify_expected_text(step.expect_text)
    if step.save_as: persist_screenshot_to(step.save_as)

  elif step.action == "click":
    target = locate(step.target)  # 用 accessibility / label / coordinate
    left_click(target.x, target.y)
    if step.wait_after_seconds: wait(step.wait_after_seconds)
    if step.expect_text: screenshot_and_verify(step.expect_text)

  elif step.action == "type":
    type_text(step.text)
    if step.wait_after_seconds: wait(...)

  elif step.action == "key":
    press_key(step.text)
```

任一 step 失敗（看不到 expect_text、target 找不到、wait 超時）→ 截最後一張圖、停止 flow、進入 teardown。

### Step 5：Teardown

```bash
# kill app
pgrep -fl "<Sample.Name>" | awk '{print $1}' | xargs -r kill

# kill prerequisites
for pid in "${prerequisite_pids[@]}"; do
  kill "$pid" 2>/dev/null
done

# 確認 port 釋放
lsof -i :5050 -sTCP:LISTEN 2>/dev/null  # 應為空
```

### Step 6：輸出結論

```
✅ samples/<Sample.Name> smoke passed.
   - prerequisites: 1/1 ready
   - flow steps:    4/4 passed
   - last screenshot: maui-demo-employee-page.png

------ or ------

❌ samples/<Sample.Name> smoke failed at step "click Sign in".
   - prerequisites: 1/1 ready
   - flow steps:    1/3 passed (step #2 timed out waiting for "Employee")
   - failure screenshot: smoke-failure-2026-05-23.png
```

## 與其他 skill 的分工

| Skill | 處理什麼 |
|-------|---------|
| `verify`（global） | 通用「跑一下確認」— 沒 sample 概念，不讀 `.smoke.yaml` |
| `run`（global） | 啟動專案的 app — 沒驗證 step、不 teardown |
| **`demo-smoke`**（本 skill） | 讀 `.smoke.yaml`、起依賴 + app + 點擊 + 驗證 + 清乾淨 |

`demo-smoke` 是 `run` + `verify` 的 sample 級組合版。

## 知道的雷

- **MAUI app pgrep 名稱**：app 進程名來自 `.app/Contents/MacOS/<AssemblyName>`，不是 csproj 顯示名。teardown 用 AssemblyName。
- **Sandboxed app 第一次跑**：macOS 要使用者批准「網路存取」、「鍵盤監聽」（computer-use 點擊）。若 dialog 出現會卡住 flow；建議在 `.smoke.yaml` 加 `first_run_setup: |` 步驟提示。
- **`net10.0-maccatalyst` 路徑**：Release 構建路徑是 `bin/Release/net10.0-maccatalyst/Bee MAUI Demo.app`（無 RID 子目錄），Debug 構建是 `bin/Debug/net10.0-maccatalyst/maccatalyst-arm64/Bee MAUI Demo.app`（有 RID 子目錄）。`.smoke.yaml` 要分別寫對。
- **expect_text 是 OCR 還是 accessibility？**：MVP 用截圖比對 + OCR；高精度需求改用 macOS Accessibility API（不在本 skill 範圍）。
- **prerequisites 多依賴**：若 sample 需要 SQLite container 之類，把指令寫進 prerequisites；本 skill 不識別 container 工具。

## 不在本 skill 範圍

- 寫 `.smoke.yaml` 本身（沒有自動生成器；模板複製貼上）
- 自動生成失敗截圖的 diff（純截圖比對人工 review）
- 跑全部 sample 的 batch mode（要的話用 `/loop` 或 `/schedule` 包一層）
- 把結果 push 到 dashboard / SonarCloud / 任何外部 reporting
