using System.ComponentModel;
using Bee.Api.Core.JsonRpc;

namespace Bee.Api.Core.UnitTests
{
    /// <summary>
    /// ApiPayloadFrame 的編解碼測試（純邏輯，不觸及 process-wide 狀態）。
    /// </summary>
    public class ApiPayloadFrameTests
    {
        [Fact]
        [DisplayName("Prepend 後 Extract 應還原所有欄位與 body")]
        public void Extract_AfterPrepend_RoundTripsAllFields()
        {
            var body = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };
            var frame = new ApiPayloadFrame(timestampMs: 1_700_000_000_123, sequence: 42);

            var restored = ApiPayloadFrame.Extract(frame.Prepend(body), out var restoredBody);

            Assert.Equal(ApiPayloadFrame.CurrentVersion, restored.Version);
            Assert.Equal(1_700_000_000_123, restored.TimestampMs);
            Assert.Equal(42, restored.Sequence);
            Assert.Equal(body, restoredBody);
        }

        [Fact]
        [DisplayName("Prepend 應以 big-endian 寫入，且 body 緊接在 17 bytes 之後")]
        public void Prepend_WritesBigEndianAtFixedOffsets()
        {
            // 位元組順序固定為 big-endian、與平台無關：兩端可能跑在不同架構上。
            var frame = new ApiPayloadFrame(timestampMs: 0x0102030405060708, sequence: 0x1112131415161718);

            var framed = frame.Prepend(new byte[] { 0xAA });

            Assert.Equal(ApiPayloadFrame.Version1Size + 1, framed.Length);
            Assert.Equal(ApiPayloadFrame.CurrentVersion, framed[0]);
            Assert.Equal(0x01, framed[1]);
            Assert.Equal(0x08, framed[8]);
            Assert.Equal(0x11, framed[9]);
            Assert.Equal(0x18, framed[16]);
            Assert.Equal(0xAA, framed[17]);
        }

        [Fact]
        [DisplayName("Extract 於恰好 17 bytes 應回傳空 body 而非拋錯")]
        public void Extract_ExactlyFrameSized_ReturnsEmptyBody()
        {
            var framed = new ApiPayloadFrame(1, 2).Prepend(Array.Empty<byte>());

            ApiPayloadFrame.Extract(framed, out var body);

            Assert.Empty(body);
        }

        [Fact]
        [DisplayName("Extract 於長度不足應拋出 ReplayRejectedException 而非索引越界")]
        public void Extract_BufferShorterThanFrame_ThrowsReplayRejected()
        {
            // 舊版用戶端沒有 frame，其 body 開頭會被當成 frame 讀；長度不足時必須是
            // 明確的拒絕，不能讓它變成 IndexOutOfRangeException。
            var tooShort = new byte[ApiPayloadFrame.Version1Size - 1];

            Assert.Throws<ReplayRejectedException>(() => ApiPayloadFrame.Extract(tooShort, out _));
        }

        [Fact]
        [DisplayName("Extract 於版本不符應拋出 ReplayRejectedException")]
        public void Extract_UnknownVersion_ThrowsReplayRejected()
        {
            // frame 不自帶長度，讀取端必須先由 version 得知要吃掉幾個 byte，
            // 因此無法辨識的版本只能拒絕。
            var framed = new ApiPayloadFrame(1, 2).Prepend(new byte[] { 0x01 });
            framed[0] = 0xFE;

            var ex = Assert.Throws<ReplayRejectedException>(() => ApiPayloadFrame.Extract(framed, out _));

            Assert.Contains("254", ex.Message, StringComparison.Ordinal);
        }
    }
}
