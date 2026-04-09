# 發佈規範

## Git Tag 與版本號

- **tag 名稱必須與 NuGet 套件版本完全對應**
  - tag `v4.1.0` → 發佈套件版本 `4.1.0`
  - tag 格式：`v<Major>.<Minor>.<Patch>`
- `nuget-publish.yml` 會從 tag 名稱提取版本（去除 `v` 前綴），透過 `/p:Version=` 傳入 build 與 pack，**不使用 `Directory.Build.props` 的版本號**
- `Directory.Build.props` 的 `<Version>` 僅作為本地開發的備用預設值，每次發佈後應同步更新至與最新 tag 一致

## 發佈流程

1. 確認 `build-ci.yml` 在 main 上通過（build + pack 驗證）
2. 更新 `src/Directory.Build.props` 的 `<Version>`、`<AssemblyVersion>`、`<FileVersion>` 至目標版本
3. commit & push to main
4. 推送對應 tag（例如 `git tag v4.1.0 && git push origin v4.1.0`）
5. 確認 GitHub Actions `nuget-publish.yml` 執行成功

## 注意事項

- **NuGet 套件一經發佈無法刪除**，只能 unlist（在 NuGet.org 上隱藏但仍可安裝）
- 發佈前務必確認 build-ci.yml 通過，避免發出損壞的套件
- 若 tag 推錯需重打：先刪除遠端與本地 tag（`git tag -d <tag> && git push origin :refs/tags/<tag>`），再重新推送
- `nuget-publish.yml` 加有 `--skip-duplicate`，同版本重複推送不會報錯，但不會覆蓋已發佈的內容
- 發佈前確認套件名稱未被 NuGet.org 上的他人佔用（403 錯誤即為名稱衝突，非 API Key 問題）
