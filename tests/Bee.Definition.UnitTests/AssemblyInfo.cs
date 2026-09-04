// 整組件序列化，而非逐類別掛 [Collection]。
//
// 這個組件有數個測試類別會碰同一份 process-wide 狀態（BEE_MASTER_KEY 環境變數、
// GlobalEvents、以及測試 body 內建立的 DI 容器），詳見 ProcessWideStateCollection 的註解。
//
// 逐類別掛 [Collection] 靠的是「新增測試時記得補」，而讀取端會隨新測試持續增加 ——
// 那種要求必然遺漏，且漏掉時看起來有序列化、實際沒有，不會有任何編譯或測試訊號。
// 組件層一次序列化把它變成結構性的。ProcessWideStateCollection 因此變成冗餘，
// 但保留作為「哪些類別會碰 process-wide 狀態」的紀錄（與 Bee.Api.Core 的
// ApiServiceOptionsStateCollection 同一個做法）。
//
// 成本實測（2026-09-04，1,086 筆測試）：序列化前 352–634 ms，序列化後 723–823 ms。
[assembly: Xunit.CollectionBehavior(DisableTestParallelization = true)]
