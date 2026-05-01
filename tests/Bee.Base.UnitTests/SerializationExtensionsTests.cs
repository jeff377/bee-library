using System.ComponentModel;
using Bee.Base.Serialization;

namespace Bee.Base.UnitTests
{
    public class SerializationExtensionsTests : SerializationTestBase
    {
        [Fact]
        [DisplayName("SerializationExtensions.ToXml / ToJson 應對應 XmlCodec / JsonCodec 等同結果")]
        public void SerializationExtensions_ToXmlAndToJson_WorkAsFacade()
        {
            var source = new SerializationTestPayload { Name = "Frank", Age = 18 };

            string xml = source.ToXml();
            string json = source.ToJson();

            Assert.Equal(XmlCodec.Serialize(new SerializationTestPayload { Name = "Frank", Age = 18 }), xml);
            Assert.Contains("\"name\"", json);
        }

        [Fact]
        [DisplayName("SerializationExtensions.ToXmlFile / ToJsonFile 應寫入檔案")]
        public void SerializationExtensions_FileMethods_WriteFile()
        {
            var source = new SerializationTestPayload { Name = "Grace", Age = 22 };
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
            var xmlSource = new SerializationTestPayload { Name = "Henry", Age = 1 };
            string xmlPath = TempPath("save.xml");
            xmlSource.SetObjectFilePath(xmlPath);
            xmlSource.Save();
            Assert.True(File.Exists(xmlPath));

            var jsonSource = new SerializationTestPayload { Name = "Ivy", Age = 2 };
            string jsonPath = TempPath("save.json");
            jsonSource.SetObjectFilePath(jsonPath);
            jsonSource.Save();
            Assert.True(File.Exists(jsonPath));
        }

        [Fact]
        [DisplayName("SerializationExtensions.Save 於空 ObjectFilePath 應拋出 ArgumentException")]
        public void SerializationExtensions_Save_EmptyPath_Throws()
        {
            var source = new SerializationTestPayload { Name = "John", Age = 5 };
            Assert.Throws<ArgumentException>(() => source.Save());
        }

        [Fact]
        [DisplayName("SerializationExtensions.Save 於不支援副檔名應拋出 NotSupportedException")]
        public void SerializationExtensions_Save_UnknownExtension_Throws()
        {
            var source = new SerializationTestPayload { Name = "Kate", Age = 6 };
            source.SetObjectFilePath(TempPath("save.dat"));
            Assert.Throws<NotSupportedException>(() => source.Save());
        }
    }
}
