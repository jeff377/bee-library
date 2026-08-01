using System.ComponentModel;
using Bee.Api.Core.MessagePack;
using Bee.Api.Core.Messages.System;
using Bee.Base.Serialization;
using Bee.Definition.Forms;
using Bee.Definition.Language;
using Bee.Definition.Layouts;

namespace Bee.Api.Core.UnitTests.MessagePack
{
    /// <summary>
    /// 定義型 response 的 MessagePack byte round-trip 測試。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 這三個 response 以前直接把 Define 物件送上 wire，靠 ContractlessStandardResolver 反射處理。
    /// 那條路對<b>回程</b>是壞的：定義型別的巢狀集合是 get-only，JSON / MessagePack 依可寫性繫結，
    /// 送得出去卻收不回來——.NET 呼叫端會拿到只有純量欄位、沒有 Tables／Sections 的空殼，且不報錯。
    /// </para>
    /// <para>
    /// 現在三者一律改帶 XML 字串。本測試因此改為驗證兩件事：wire 上的字串 byte round-trip 無損，
    /// 以及還原後的 XML 反序列化回定義物件時，<b>巢狀集合確實還在</b>——後者正是舊做法失守、
    /// 而這次改版要保證的那一點。
    /// </para>
    /// </remarks>
    public class ObjectResponseMessagePackTests
    {
        [Fact]
        [DisplayName("GetFormSchemaResponse byte round-trip 後，XML 仍能還原出帶 Tables 的 FormSchema")]
        public void GetFormSchemaResponse_ByteRoundTrip_PreservesNestedCollections()
        {
            var schema = new FormSchema("Employee", "員工資料");
            schema.Tables!.Add("Employee", "員工主檔");
            var original = new GetFormSchemaResponse { Xml = XmlCodec.Serialize(schema) };

            byte[] bytes = MessagePackCodec.Serialize(original);
            var restored = MessagePackCodec.Deserialize<GetFormSchemaResponse>(bytes);

            Assert.NotNull(restored);
            Assert.Equal(original.Xml, restored.Xml);
            var roundTripped = XmlCodec.Deserialize<FormSchema>(restored.Xml!);
            Assert.NotNull(roundTripped);
            Assert.Equal("Employee", roundTripped!.ProgId);
            Assert.Equal("員工資料", roundTripped.DisplayName);
            // 舊做法在這一行失守：集合會是空的
            Assert.Single(roundTripped.Tables!);
        }

        [Fact]
        [DisplayName("GetFormLayoutResponse byte round-trip 後，XML 仍能還原出帶 Sections 的 FormLayout")]
        public void GetFormLayoutResponse_ByteRoundTrip_PreservesNestedCollections()
        {
            var layout = new FormLayout { LayoutId = "Employee", ProgId = "Employee", Caption = "員工資料", ColumnCount = 3 };
            layout.Sections!.Add(new LayoutSection { Name = "Main", Caption = "主要資料" });
            var original = new GetFormLayoutResponse { Xml = XmlCodec.Serialize(layout) };

            byte[] bytes = MessagePackCodec.Serialize(original);
            var restored = MessagePackCodec.Deserialize<GetFormLayoutResponse>(bytes);

            Assert.NotNull(restored);
            var roundTripped = XmlCodec.Deserialize<FormLayout>(restored.Xml!);
            Assert.NotNull(roundTripped);
            Assert.Equal("Employee", roundTripped!.LayoutId);
            Assert.Equal(3, roundTripped.ColumnCount);
            Assert.Single(roundTripped.Sections!);
        }

        [Fact]
        [DisplayName("GetLanguageResponse byte round-trip 後，XML 仍能還原出帶 Items 的 LanguageResource")]
        public void GetLanguageResponse_ByteRoundTrip_PreservesNestedCollections()
        {
            var resource = new LanguageResource { Namespace = "Common", Lang = "zh-TW" };
            resource.Items.Add("Greeting", "你好");
            var original = new GetLanguageResponse { Xml = XmlCodec.Serialize(resource) };

            byte[] bytes = MessagePackCodec.Serialize(original);
            var restored = MessagePackCodec.Deserialize<GetLanguageResponse>(bytes);

            Assert.NotNull(restored);
            var roundTripped = XmlCodec.Deserialize<LanguageResource>(restored.Xml!);
            Assert.NotNull(roundTripped);
            Assert.Equal("Common", roundTripped!.Namespace);
            Assert.Equal("你好", roundTripped.GetText("Greeting"));
        }

        [Fact]
        [DisplayName("定義不存在時回空 Xml，byte round-trip 後仍為空")]
        public void EmptyXml_ByteRoundTrip_StaysEmpty()
        {
            var original = new GetFormLayoutResponse { Xml = string.Empty };

            var restored = MessagePackCodec.Deserialize<GetFormLayoutResponse>(MessagePackCodec.Serialize(original));

            Assert.NotNull(restored);
            Assert.True(string.IsNullOrEmpty(restored.Xml));
        }
    }
}
