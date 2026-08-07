// ApiServiceOptionsTests 與 ApiPayloadTransformerTests 必須暫時改寫 process-wide static
// ApiServiceOptions.PayloadSerializer / PayloadCompressor / PayloadEncryptor 才能驗證
// Initialize 的組裝路徑；與此同時**整個 payload 管線的讀取端**（約 19 個 JSON-RPC
// round-trip 測試類）都會讀同一組靜態值，平行執行下會 race。
//
// 原本只把兩個「寫入端」類別加進 ApiServiceOptionsState collection，漏掉讀取端——
// 但讀取端一樣會踩。CI build #31169045420（2026-08-07）即因此紅在
// JsonRpcSerializationTests.JsonRpcRequest_Serialize_ReturnsValidJson：
// Encode 用 GzipPayloadCompressor 壓完，另一類別在視窗內把 Compressor 換成
// NoCompressionCompressor，Decode 於是把 gzip bytes 原樣餵給 MessagePack →
// `Unexpected msgpack code 31`（0x1F 正是 gzip magic 的第一個 byte）。
// 錯誤訊息完全指向序列化，看不出根因是測試互相污染。
//
// 讀取端會隨新的 round-trip 測試持續增加，逐類補 [Collection] 必然遺漏；整體關閉平行
// 最簡且不易遺漏（同 Bee.ObjectCaching.UnitTests 的既有做法）。實測代價約 0.25 秒
// （平行 ~0.40s → 串行 ~0.66s）。根治方式仍是把這三個元件 DI 化。
[assembly: Xunit.CollectionBehavior(DisableTestParallelization = true)]
