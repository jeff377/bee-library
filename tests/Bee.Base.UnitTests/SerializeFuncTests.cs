using System.ComponentModel;
using System.Text.Json.Serialization;
using System.Xml.Serialization;
using Bee.Base.Serialization;

namespace Bee.Base.UnitTests
{
    public class SerializeFuncTests : IDisposable
    {
        private static readonly string[] s_xmlSerializeEvents = { "Before:Xml", "After:Xml" };
        private static readonly string[] s_xmlDeserializeEvents = { "AfterDeser:Xml" };
        private static readonly string[] s_jsonSerializeEvents = { "Before:Json", "After:Json" };
        private static readonly string[] s_jsonDeserializeEvents = { "AfterDeser:Json" };

        private readonly string _tempDir;

        public SerializeFuncTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "bee-base-serialize-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(_tempDir))
                    Directory.Delete(_tempDir, true);
            }
            catch (IOException)
            {
                // Temp files may still be held by test runner; ignore on teardown.
            }
            catch (UnauthorizedAccessException)
            {
                // Temp files may still be held by test runner; ignore on teardown.
            }
            GC.SuppressFinalize(this);
        }

        public class TestPayload : IObjectSerializeBase, IObjectSerialize, IObjectSerializeFile, IObjectSerializeProcess
        {
            public string Name { get; set; } = string.Empty;
            public int Age { get; set; }

            private SerializeState _state = SerializeState.None;
            [XmlIgnore, JsonIgnore]
            public SerializeState SerializeState => _state;
            public void SetSerializeState(SerializeState state) => _state = state;

            private string _objectFilePath = string.Empty;
            [XmlIgnore, JsonIgnore]
            public string ObjectFilePath => _objectFilePath;
            public void SetObjectFilePath(string filePath) => _objectFilePath = filePath;

            [XmlIgnore, JsonIgnore]
            public List<string> Events { get; } = new();

            public void BeforeSerialize(SerializeFormat format) => Events.Add($"Before:{format}");
            public void AfterSerialize(SerializeFormat format) => Events.Add($"After:{format}");
            public void AfterDeserialize(SerializeFormat format) => Events.Add($"AfterDeser:{format}");
        }

        private string TempPath(string fileName) => Path.Combine(_tempDir, fileName);

        [Fact]
        [DisplayName("ObjectToXml 與 XmlToObject 應可完整 round-trip")]
        public void Xml_Roundtrip_PreservesValues()
        {
            var source = new TestPayload { Name = "Alice", Age = 30 };

            string xml = SerializeFunc.ObjectToXml(source);
            var restored = SerializeFunc.XmlToObject<TestPayload>(xml);

            Assert.NotNull(restored);
            Assert.Equal("Alice", restored!.Name);
            Assert.Equal(30, restored.Age);
        }

        [Fact]
        [DisplayName("ObjectToXml 於 null 輸入時應回傳空字串")]
        public void ObjectToXml_Null_ReturnsEmpty()
        {
            Assert.Equal(string.Empty, SerializeFunc.ObjectToXml(null!));
        }

        [Fact]
        [DisplayName("XmlToObject 於空字串輸入時應回傳型別預設值")]
        public void XmlToObject_EmptyString_ReturnsDefault()
        {
            var result = SerializeFunc.XmlToObject<TestPayload>(string.Empty);
            Assert.Null(result);
        }

        [Fact]
        [DisplayName("XML 序列化應依序觸發 Before/After Serialize 與 AfterDeserialize 回呼")]
        public void Xml_InvokesProcessCallbacksInOrder()
        {
            var source = new TestPayload { Name = "Bob", Age = 20 };

            string xml = SerializeFunc.ObjectToXml(source);
            Assert.Equal(s_xmlSerializeEvents, source.Events);
            Assert.Equal(SerializeState.None, source.SerializeState);

            var restored = SerializeFunc.XmlToObject<TestPayload>(xml)!;
            Assert.Equal(s_xmlDeserializeEvents, restored.Events);
        }

        [Fact]
        [DisplayName("JSON 序列化應依序觸發 Before/After Serialize 與 AfterDeserialize 回呼")]
        public void Json_InvokesProcessCallbacksInOrder()
        {
            var source = new TestPayload { Name = "Carol", Age = 40 };

            string json = SerializeFunc.ObjectToJson(source);
            Assert.Equal(s_jsonSerializeEvents, source.Events);

            var restored = SerializeFunc.JsonToObject<TestPayload>(json)!;
            Assert.Equal("Carol", restored.Name);
            Assert.Equal(40, restored.Age);
            Assert.Equal(s_jsonDeserializeEvents, restored.Events);
        }

        [Fact]
        [DisplayName("ObjectToXmlFile / XmlFileToObject 應可 round-trip 並設定 ObjectFilePath")]
        public void XmlFile_Roundtrip_SetsObjectFilePath()
        {
            var source = new TestPayload { Name = "Dan", Age = 50 };
            string path = TempPath("payload.xml");

            SerializeFunc.ObjectToXmlFile(source, path);

            Assert.True(File.Exists(path));
            Assert.Equal(path, source.ObjectFilePath);

            var restored = SerializeFunc.XmlFileToObject<TestPayload>(path)!;
            Assert.Equal("Dan", restored.Name);
            Assert.Equal(path, restored.ObjectFilePath);
        }

        [Fact]
        [DisplayName("ObjectToJsonFile / JsonFileToObject 應可 round-trip 並設定 ObjectFilePath")]
        public void JsonFile_Roundtrip_SetsObjectFilePath()
        {
            var source = new TestPayload { Name = "Eve", Age = 33 };
            string path = TempPath("payload.json");

            SerializeFunc.ObjectToJsonFile(source, path);

            Assert.True(File.Exists(path));
            Assert.Equal(path, source.ObjectFilePath);

            var restored = SerializeFunc.JsonFileToObject<TestPayload>(path)!;
            Assert.Equal("Eve", restored.Name);
            Assert.Equal(path, restored.ObjectFilePath);
        }

        [Fact]
        [DisplayName("XmlFileToObject 於檔案不存在時應回傳 null")]
        public void XmlFileToObject_MissingFile_ReturnsNull()
        {
            // FileReadText returns empty for missing files, and XmlToObject(string.Empty) returns default.
            var result = SerializeFunc.XmlFileToObject<TestPayload>(TempPath("missing.xml"));
            Assert.Null(result);
        }

        [Fact]
        [DisplayName("XmlFileToObject 於內容損毀時應包成 InvalidOperationException")]
        public void XmlFileToObject_MalformedXml_Throws()
        {
            string path = TempPath("broken.xml");
            File.WriteAllText(path, "<not-valid-xml");

            var ex = Assert.Throws<InvalidOperationException>(
                () => SerializeFunc.XmlFileToObject<TestPayload>(path));
            Assert.Contains("XmlFileToObject", ex.Message);
        }

        [Fact]
        [DisplayName("JsonFileToObject 於檔案不存在時應拋出 InvalidOperationException")]
        public void JsonFileToObject_MissingFile_Throws()
        {
            var ex = Assert.Throws<InvalidOperationException>(
                () => SerializeFunc.JsonFileToObject<TestPayload>(TempPath("missing.json")));
            Assert.Contains("JsonFileToObject", ex.Message);
        }

        [Fact]
        [DisplayName("XmlSerializerCache.Get 應對相同型別回傳相同實例")]
        public void XmlSerializerCache_Get_ReturnsCachedInstance()
        {
            var a = XmlSerializerCache.Get(typeof(TestPayload));
            var b = XmlSerializerCache.Get(typeof(TestPayload));

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

        [Fact]
        [DisplayName("SerializationExtensions.ToXml / ToJson 應對應 SerializeFunc 等同結果")]
        public void SerializationExtensions_ToXmlAndToJson_WorkAsFacade()
        {
            var source = new TestPayload { Name = "Frank", Age = 18 };

            string xml = source.ToXml();
            string json = source.ToJson();

            Assert.Equal(SerializeFunc.ObjectToXml(new TestPayload { Name = "Frank", Age = 18 }), xml);
            Assert.Contains("\"name\"", json);
        }

        [Fact]
        [DisplayName("SerializationExtensions.ToXmlFile / ToJsonFile 應寫入檔案")]
        public void SerializationExtensions_FileMethods_WriteFile()
        {
            var source = new TestPayload { Name = "Grace", Age = 22 };
            string xmlPath = TempPath("ext.xml");
            string jsonPath = TempPath("ext.json");

            source.ToXmlFile(xmlPath);
            Assert.True(File.Exists(xmlPath));

            source.ToJsonFile(jsonPath);
            Assert.True(File.Exists(jsonPath));
        }

        [Fact]
        [DisplayName("SerializationExtensions.Save 應依副檔名寫入對應格式")]
        public void SerializationExtensions_Save_DispatchesByExtension()
        {
            var xmlSource = new TestPayload { Name = "Henry", Age = 1 };
            string xmlPath = TempPath("save.xml");
            xmlSource.SetObjectFilePath(xmlPath);
            xmlSource.Save();
            Assert.True(File.Exists(xmlPath));

            var jsonSource = new TestPayload { Name = "Ivy", Age = 2 };
            string jsonPath = TempPath("save.json");
            jsonSource.SetObjectFilePath(jsonPath);
            jsonSource.Save();
            Assert.True(File.Exists(jsonPath));
        }

        [Fact]
        [DisplayName("SerializationExtensions.Save 於空 ObjectFilePath 應拋出 ArgumentException")]
        public void SerializationExtensions_Save_EmptyPath_Throws()
        {
            var source = new TestPayload { Name = "John", Age = 5 };
            Assert.Throws<ArgumentException>(() => source.Save());
        }

        [Fact]
        [DisplayName("SerializationExtensions.Save 於不支援副檔名應拋出 NotSupportedException")]
        public void SerializationExtensions_Save_UnknownExtension_Throws()
        {
            var source = new TestPayload { Name = "Kate", Age = 6 };
            source.SetObjectFilePath(TempPath("save.dat"));
            Assert.Throws<NotSupportedException>(() => source.Save());
        }
    }
}
