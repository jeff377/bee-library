using System.ComponentModel;
using Bee.Base.Serialization;

namespace Bee.Base.UnitTests
{
    public class XmlCodecTests : SerializationTestBase
    {
        private static readonly string[] s_xmlSerializeEvents = { "Before:Xml", "After:Xml" };
        private static readonly string[] s_xmlDeserializeEvents = { "AfterDeser:Xml" };

        [Fact]
        [DisplayName("Serialize 與 Deserialize 應可完整 round-trip")]
        public void Xml_Roundtrip_PreservesValues()
        {
            var source = new SerializationTestPayload { Name = "Alice", Age = 30 };

            string xml = XmlCodec.Serialize(source);
            var restored = XmlCodec.Deserialize<SerializationTestPayload>(xml);

            Assert.NotNull(restored);
            Assert.Equal("Alice", restored!.Name);
            Assert.Equal(30, restored.Age);
        }

        [Fact]
        [DisplayName("Serialize 於 null 輸入時應回傳空字串")]
        public void Serialize_Null_ReturnsEmpty()
        {
            Assert.Equal(string.Empty, XmlCodec.Serialize(null!));
        }

        [Fact]
        [DisplayName("Deserialize 於空字串輸入時應回傳型別預設值")]
        public void Deserialize_EmptyString_ReturnsDefault()
        {
            var result = XmlCodec.Deserialize<SerializationTestPayload>(string.Empty);
            Assert.Null(result);
        }

        [Fact]
        [DisplayName("XML 序列化應依序觸發 Before/After Serialize 與 AfterDeserialize 回呼")]
        public void Xml_InvokesProcessCallbacksInOrder()
        {
            var source = new SerializationTestPayload { Name = "Bob", Age = 20 };

            string xml = XmlCodec.Serialize(source);
            Assert.Equal(s_xmlSerializeEvents, source.Events);
            Assert.Equal(SerializeState.None, source.SerializeState);

            var restored = XmlCodec.Deserialize<SerializationTestPayload>(xml)!;
            Assert.Equal(s_xmlDeserializeEvents, restored.Events);
        }

        [Fact]
        [DisplayName("SerializeToFile / DeserializeFromFile 應可 round-trip 並設定 ObjectFilePath")]
        public void XmlFile_Roundtrip_SetsObjectFilePath()
        {
            var source = new SerializationTestPayload { Name = "Dan", Age = 50 };
            string path = TempPath("payload.xml");

            XmlCodec.SerializeToFile(source, path);

            Assert.True(File.Exists(path));
            Assert.Equal(path, source.ObjectFilePath);

            var restored = XmlCodec.DeserializeFromFile<SerializationTestPayload>(path)!;
            Assert.Equal("Dan", restored.Name);
            Assert.Equal(path, restored.ObjectFilePath);
        }

        [Fact]
        [DisplayName("DeserializeFromFile 於檔案不存在時應回傳 null")]
        public void DeserializeFromFile_MissingFile_ReturnsNull()
        {
            // FileReadText returns empty for missing files, and Deserialize(string.Empty) returns default.
            var result = XmlCodec.DeserializeFromFile<SerializationTestPayload>(TempPath("missing.xml"));
            Assert.Null(result);
        }

        [Fact]
        [DisplayName("DeserializeFromFile 於內容損毀時應包成 InvalidOperationException")]
        public void DeserializeFromFile_MalformedXml_Throws()
        {
            string path = TempPath("broken.xml");
            File.WriteAllText(path, "<not-valid-xml");

            var ex = Assert.Throws<InvalidOperationException>(
                () => XmlCodec.DeserializeFromFile<SerializationTestPayload>(path));
            Assert.Contains("DeserializeFromFile", ex.Message);
        }

        [Fact]
        [DisplayName("XmlSerializerCache.Get 應對相同型別回傳相同實例")]
        public void XmlSerializerCache_Get_ReturnsCachedInstance()
        {
            var a = XmlSerializerCache.Get(typeof(SerializationTestPayload));
            var b = XmlSerializerCache.Get(typeof(SerializationTestPayload));

            Assert.Same(a, b);
        }

        [Fact]
        [DisplayName("Utf8StringWriter.Encoding 應為不帶 BOM 的 UTF-8")]
        public void Utf8StringWriter_Encoding_IsUtf8NoBom()
        {
            using var writer = new Utf8StringWriter();
            var encoding = (System.Text.UTF8Encoding)writer.Encoding;

            Assert.Equal("utf-8", encoding.WebName);
            Assert.Empty(encoding.GetPreamble());
        }
    }
}
