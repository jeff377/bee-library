using System.ComponentModel;
using Bee.Api.Core.Transformers;

namespace Bee.Api.Core.UnitTests
{
    /// <summary>
    /// MessagePackPayloadSerializer 測試。
    /// </summary>
    public class MessagePackPayloadSerializerTests
    {
        [Fact]
        [DisplayName("SerializationMethod 應為 \"messagepack\"")]
        public void SerializationMethod_IsMessagePack()
        {
            var serializer = new MessagePackPayloadSerializer();

            Assert.Equal("messagepack", serializer.SerializationMethod);
        }

        [Fact]
        [DisplayName("Serialize/Deserialize 應正確還原字串內容")]
        public void SerializeDeserialize_String_RoundTrip()
        {
            var serializer = new MessagePackPayloadSerializer();
            const string original = "Hello, MessagePack!";

            var bytes = serializer.Serialize(original, typeof(string));
            var result = serializer.Deserialize(bytes, typeof(string));

            Assert.Equal(original, result);
        }

        [Fact]
        [DisplayName("Serialize/Deserialize 應正確還原整數內容")]
        public void SerializeDeserialize_Int_RoundTrip()
        {
            var serializer = new MessagePackPayloadSerializer();
            const int original = 123456;

            var bytes = serializer.Serialize(original, typeof(int));
            var result = serializer.Deserialize(bytes, typeof(int));

            Assert.Equal(original, result);
        }
    }
}
