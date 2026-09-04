using System.ComponentModel;
using System.Xml.Serialization;
using Bee.Base.Serialization;

namespace Bee.Base.UnitTests.Serialization
{
    /// <summary>
    /// 序列化失敗後，物件的 <see cref="SerializeState"/> 必須被清回 <see cref="SerializeState.None"/>。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 兩個 codec 先前都是「<c>NotifyBefore</c> → 序列化 → <c>NotifyAfter</c>」的直線寫法，
    /// 序列化擲例外時最後一步永遠不會執行，物件就**永久**停在 <c>Serialize</c> 狀態。
    /// </para>
    /// <para>
    /// 為什麼「永久」是重點：被序列化的往往是 process-wide 的快取定義實例。一次失敗之後，
    /// 該實例的所有空集合 getter 從此回 <c>null</c>（<c>IsSerializeEmpty</c> 只在
    /// <c>Serialize</c> 狀態下成立），而數個呼叫端用 <c>!</c> 解參考它們 ——
    /// 空集合本來就是完全正常的狀態。瞬時的失敗被轉成了永久的失敗。
    /// </para>
    /// </remarks>
    public class SerializeStateResetTests
    {
        /// <summary>
        /// 屬性 getter 會擲例外，藉此讓序列化在中途失敗。
        /// </summary>
        public class ExplodingValue : IObjectSerialize
        {
            private SerializeState _state;

            /// <summary>序列化時必定擲例外的屬性。</summary>
            /// <remarks>
            /// 必須是可讀寫的：<c>XmlSerializer</c> 會跳過唯讀屬性（集合除外），
            /// 唯讀版本的 getter 根本不會被呼叫，測試就變成什麼都沒驗到。
            /// </remarks>
            public string Boom
            {
                get => throw new InvalidOperationException($"serialization blew up in state {_state}");
                set => _lastSet = value;
            }

            private string _lastSet = string.Empty;

            /// <inheritdoc/>
            [XmlIgnore]
            public SerializeState SerializeState => _state;

            /// <inheritdoc/>
            public void SetSerializeState(SerializeState serializeState) => _state = serializeState;

            /// <inheritdoc/>
            public bool IsSerializeEmpty() => _lastSet.Length < 0;
        }

        [Fact]
        [DisplayName("XmlCodec 序列化失敗後，物件的 SerializeState 應被清回 None")]
        public void XmlCodec_SerializeThrows_StateIsReset()
        {
            var value = new ExplodingValue();

            Assert.ThrowsAny<Exception>(() => XmlCodec.Serialize(value));

            Assert.Equal(SerializeState.None, value.SerializeState);
        }

        [Fact]
        [DisplayName("JsonCodec 序列化失敗後，物件的 SerializeState 應被清回 None")]
        public void JsonCodec_SerializeThrows_StateIsReset()
        {
            var value = new ExplodingValue();

            Assert.ThrowsAny<Exception>(() => JsonCodec.Serialize(value));

            Assert.Equal(SerializeState.None, value.SerializeState);
        }

        [Fact]
        [DisplayName("對照組：序列化成功時 SerializeState 同樣是 None（不是靠一律不設達成）")]
        public void Serialize_Succeeds_StateIsAlsoNone()
        {
            // 沒有這一條，「一律不呼叫 NotifyBefore」也能滿足上面兩條 ——
            // 那會讓 IsSerializeEmpty 永遠不成立，靜默改變輸出。
            var value = new WellBehavedValue();

            var xml = XmlCodec.Serialize(value);

            Assert.Contains("well-behaved", xml, StringComparison.Ordinal);
            Assert.Equal(SerializeState.None, value.SerializeState);
            Assert.True(value.SawSerializeState, "序列化過程中不曾進入 Serialize 狀態，配對根本沒有發生。");
        }

        /// <summary>
        /// 序列化得起來，並記錄過程中是否真的被標記為 <see cref="SerializeState.Serialize"/>。
        /// </summary>
        public class WellBehavedValue : IObjectSerialize
        {
            private SerializeState _state;

            /// <summary>可正常序列化的內容。</summary>
            public string Name { get; set; } = "well-behaved";

            /// <summary>序列化過程中是否曾進入 <see cref="SerializeState.Serialize"/>。</summary>
            [XmlIgnore]
            public bool SawSerializeState { get; private set; }

            /// <inheritdoc/>
            [XmlIgnore]
            public SerializeState SerializeState => _state;

            /// <inheritdoc/>
            public void SetSerializeState(SerializeState serializeState)
            {
                if (serializeState == SerializeState.Serialize) { SawSerializeState = true; }
                _state = serializeState;
            }

            /// <inheritdoc/>
            public bool IsSerializeEmpty() => _state == SerializeState.None && false;
        }
    }
}
