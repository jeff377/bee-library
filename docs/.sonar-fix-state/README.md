# `/sonar-fix` 狀態檔

本目錄由 `/sonar-fix`（見 `.claude/commands/sonar-fix.md`）讀寫，git 追蹤以便跨機器共享 skip 記錄與每日快照。

## 檔案

- **`skip.json`**：已放棄嘗試的 issue 與檔案清單（達 3 輪重試上限）
  - `issues`：以 issue key 索引，記錄 rule、component、attempts、reason、lastAttempt
  - `files`：以檔案路徑索引，記錄 attempts、reason、lastAttempt
  - 若未來確認某項可處理，手動從 JSON 移除對應條目即可重新納入
- **`snapshot.json`**：daily 模式的前次快照
  - `capturedAt`、`qualityGate`、`coverage`、`leakIssueKeys`、`allOpenIssueKeys`
  - 僅用於比對「今日 vs 前日」變化，手動觸發（`/sonar-fix`）不會使用此檔

## 手動清空

```bash
# 重置 skip list（欲重新嘗試所有項目時）
echo '{"issues":{},"files":{}}' > docs/.sonar-fix-state/skip.json

# 重置 snapshot（下次 daily 將視所有現況為「新增」）
rm docs/.sonar-fix-state/snapshot.json
```
