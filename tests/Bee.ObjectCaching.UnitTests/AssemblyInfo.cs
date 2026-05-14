// CacheInfoTests.Initialize_DifferentProviderType_ReplacesProvider 必須暫時改寫
// process-wide static CacheInfo.Provider 才能驗證 Initialize 的型別比對路徑；
// 與此同時其他 cache 相關測試（KeyObjectCache / ObjectCache / FormLayoutCache /
// SessionInfoService / *SettingsCache 等）都會讀寫同一個 Provider，平行執行下會
// race，CI 偶發 KeyObjectCacheTests.Set_WithIKeyObject_UsesGetKey 因 FakeCacheProvider
// 介入而拿不到剛 Set 的值（Issue: 2026-05-14 build #25838584823）。
// 此 assembly 測試數量少且皆為輕量，整體關閉平行最簡且不易遺漏。
[assembly: Xunit.CollectionBehavior(DisableTestParallelization = true)]
