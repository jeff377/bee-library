// ApiClientInfoTests / ApiConnectValidatorTests 必須暫時改寫 process-wide static
// ApiClientInfo.SupportedConnectTypes / ConnectType / Endpoint / ApiKey / ApiEncryptionKey
// 才能驗證對應邏輯；而 ApiConnector 在每次請求路徑上都會讀 ApiEncryptionKey
// （ApiConnector.cs:202,209,231）、RemoteApiProvider 讀 ApiKey、ApiConnectValidator 讀
// SupportedConnectTypes，平行執行下讀取端會落在寫入端的 try/finally 還原視窗裡。
//
// ApiClientInfoState collection 已收了三個類別（含刻意加入的讀取端
// TenantCustomizationEndToEndTests），但 SystemApiConnectorTests 與 ClientDefineAccessTests
// 這兩個同樣會驅動 connector 的讀取端漏掉了。這與 Bee.Api.Core.UnitTests 在 CI build
// #31169045420 踩到的是同一種讀寫不對稱——那次是實際紅了，這裡目前只是潛伏。
//
// 讀取端會隨新測試增加，逐類補 [Collection] 必然遺漏；整體關閉平行最簡且不易遺漏
// （同 Bee.ObjectCaching.UnitTests / Bee.Api.Core.UnitTests 的做法）。
// 根治方式是把 ApiClientInfo 的 per-session 狀態 DI 化（體檢項目 N-2）。
[assembly: Xunit.CollectionBehavior(DisableTestParallelization = true)]
