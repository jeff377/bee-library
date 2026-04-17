# Pull Request 與 CI 驗證流程

## 工作流選擇（依開發環境而定）

**使用者所在環境能執行 `dotnet build`／`dotnet test`（MacOS、Windows 桌面）**：
- **預設直接提交到 `main`**，不建立分支與 PR
- 本機先完成 build + test 驗證，通過後再 `git push origin main`
- 這類環境下，PR 只是額外開銷；CI 驗證在本機已經做過
- `build-ci.yml` 仍會在 push 後於遠端跑一次，若失敗再依下節處理

**使用者所在環境無法本機驗證（手機、平板、網頁版 Claude）**：
- **必須走分支 + PR**，讓 `build-ci.yml` 在 PR 上驗證
- 本機不可驗證，PR 成為唯一的正確性把關管道

**使用者明確指定的情況**：
- 使用者如說「發 PR」、「開分支」即使在桌面環境也照辦
- 使用者如說「直接改 main」即使規則預設要 PR 也照辦
- 使用者指令優先於預設規則

## 分支 + PR 流程（適用於無本機驗證環境）

1. 從 `origin/main` 建立新分支（命名：`claude/<主題>`）
2. 完成改動後 `git push -u origin <branch>`
3. 用 `gh pr create` 建立 PR
4. 建立完成立即呼叫 `subscribe_pr_activity` 訂閱 PR 活動
5. 告知使用者已訂閱、會在有新事件時處理

## 直接改 main 流程（適用於本機可驗證環境）

1. 在 `main` 分支上工作（或工作完成後 rebase 到 main）
2. 本機執行 `dotnet build --configuration Release` + `dotnet test --configuration Release --settings .runsettings`（或只對受影響的專案）
3. 通過後 `git commit` 並 `git push origin main`
4. push 後遠端 `build-ci.yml` 仍會跑一次，若失敗需立即修復（見下節）

## CI 失敗處理

**PR 上的 CI 失敗（訂閱事件）**：

1. 取 `get_check_runs` 與對應 log，定位失敗原因
2. 分類處理：
   - **明確可修**（編譯錯誤、測試 assertion 失敗、lint、格式）→ 直接修復、commit、push
   - **架構性 / 語意不明** → 先向使用者說明狀況再決定
   - **無法作用**（例如外部服務暫時性失敗）→ 說明原因並建議重跑
3. 修復後等待 CI 重新驗證
4. 不擅自關閉或忽略失敗

**push 到 main 後 CI 失敗**：

1. 同樣以 `gh run list` / `gh run view` 取得 log 定位原因
2. 立即以後續 commit 修復（不回退已推送的 commit，除非使用者明確要求）
3. 告知使用者失敗與修復狀況

## 合併後

PR 合併後系統會自動取消訂閱，流程結束；不需額外處理。
