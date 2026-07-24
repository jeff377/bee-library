using System.ComponentModel;
using Bee.Api.Core.MessagePack;
using Bee.Api.Core.Messages.System;
using Bee.Definition.Forms;
using Bee.Definition.Language;
using Bee.Definition.Layouts;

namespace Bee.Api.Core.UnitTests.MessagePack
{
    /// <summary>
    /// 物件型 response 的 MessagePack byte round-trip 測試。
    /// </summary>
    /// <remarks>
    /// 這三個 response 直接把 Define 物件（含大量 KeyCollectionBase / CollectionBase 集合、
    /// 且未帶 MessagePack 標記）送上 wire，靠 ContractlessStandardResolver 反射處理 —— 那正是
    /// 行動端 AOT 最脆弱的一面。既有的 JSON-RPC round-trip 測試走 in-process dispatch（直接回傳
    /// 物件參考），並未真的過 byte round-trip，故此處補上實際序列化位元組的驗證。
    /// </remarks>
    public class ObjectResponseMessagePackTests
    {
        [Fact]
        [DisplayName("GetFormSchemaResponse MessagePack byte round-trip 應保留 FormSchema")]
        public void GetFormSchemaResponse_ByteRoundTrip_PreservesSchema()
        {
            var original = new GetFormSchemaResponse
            {
                Schema = new FormSchema("Employee", "員工資料")
            };

            byte[] bytes = MessagePackCodec.Serialize(original);
            var restored = MessagePackCodec.Deserialize<GetFormSchemaResponse>(bytes);

            Assert.NotNull(restored);
            Assert.NotNull(restored.Schema);
            Assert.Equal("Employee", restored.Schema.ProgId);
            Assert.Equal("員工資料", restored.Schema.DisplayName);
        }

        [Fact]
        [DisplayName("GetFormLayoutResponse MessagePack byte round-trip 應保留 FormLayout")]
        public void GetFormLayoutResponse_ByteRoundTrip_PreservesLayout()
        {
            var original = new GetFormLayoutResponse
            {
                Layout = new FormLayout
                {
                    LayoutId = "Default",
                    ProgId = "Employee",
                    Caption = "員工資料",
                    ColumnCount = 3
                }
            };

            byte[] bytes = MessagePackCodec.Serialize(original);
            var restored = MessagePackCodec.Deserialize<GetFormLayoutResponse>(bytes);

            Assert.NotNull(restored);
            Assert.NotNull(restored.Layout);
            Assert.Equal("Default", restored.Layout.LayoutId);
            Assert.Equal("Employee", restored.Layout.ProgId);
            Assert.Equal("員工資料", restored.Layout.Caption);
            Assert.Equal(3, restored.Layout.ColumnCount);
        }

        [Fact]
        [DisplayName("GetLanguageResponse MessagePack byte round-trip 應保留 LanguageResource")]
        public void GetLanguageResponse_ByteRoundTrip_PreservesResource()
        {
            var original = new GetLanguageResponse
            {
                Resource = new LanguageResource
                {
                    Namespace = "Bee.Definition.Forms",
                    Lang = "zh-TW"
                }
            };

            byte[] bytes = MessagePackCodec.Serialize(original);
            var restored = MessagePackCodec.Deserialize<GetLanguageResponse>(bytes);

            Assert.NotNull(restored);
            Assert.NotNull(restored.Resource);
            Assert.Equal("Bee.Definition.Forms", restored.Resource.Namespace);
            Assert.Equal("zh-TW", restored.Resource.Lang);
        }
    }
}
