using System.ComponentModel;
using System.Text;
using System.Text.Json;
using Bee.Definition.Filters;

namespace Bee.Definition.UnitTests.Filters
{
    /// <summary>
    /// FilterNodeCollectionJsonConverter 的 Read/Write 測試。
    /// </summary>
    public class FilterNodeCollectionJsonConverterTests
    {
        // 實務上 ApiPayload 外層以 camelCase 命名策略序列化，讓 converter Read 路徑能對上 "kind" 小寫屬性
        private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new FilterNodeCollectionJsonConverter() }
        };

        [Fact]
        [DisplayName("Write null collection 應輸出 null")]
        public void Write_NullCollection_WritesNull()
        {
            FilterNodeCollection? collection = null;
            var json = JsonSerializer.Serialize(collection, Options);
            Assert.Equal("null", json);
        }

        [Fact]
        [DisplayName("Write 空集合應輸出 []")]
        public void Write_EmptyCollection_WritesEmptyArray()
        {
            var collection = new FilterNodeCollection();
            var json = JsonSerializer.Serialize(collection, Options);
            Assert.Equal("[]", json);
        }

        [Fact]
        [DisplayName("Write/Read round-trip 應還原 FilterCondition")]
        public void ReadWrite_RoundTrip_FilterCondition()
        {
            var collection = new FilterNodeCollection
            {
                new FilterCondition("Name", ComparisonOperator.Equal, "Alice")
            };

            var json = JsonSerializer.Serialize(collection, Options);
            var restored = JsonSerializer.Deserialize<FilterNodeCollection>(json, Options);

            Assert.NotNull(restored);
            Assert.Single(restored!);
            var cond = Assert.IsType<FilterCondition>(restored[0]);
            Assert.Equal("Name", cond.FieldName);
            Assert.Equal(ComparisonOperator.Equal, cond.Operator);
        }

        [Fact]
        [DisplayName("Write/Read round-trip 應還原 FilterGroup 含子節點")]
        public void ReadWrite_RoundTrip_FilterGroup()
        {
            var group = new FilterGroup(LogicalOperator.Or);
            group.Nodes.Add(new FilterCondition("Age", ComparisonOperator.GreaterThan, 18));

            var collection = new FilterNodeCollection
            {
                group
            };

            var json = JsonSerializer.Serialize(collection, Options);
            var restored = JsonSerializer.Deserialize<FilterNodeCollection>(json, Options);

            Assert.NotNull(restored);
            Assert.Single(restored!);
            var g = Assert.IsType<FilterGroup>(restored[0]);
            Assert.Equal(LogicalOperator.Or, g.Operator);
            Assert.Single(g.Nodes);
        }

        [Fact]
        [DisplayName("Read null token 應回傳 null")]
        public void Read_NullToken_ReturnsNull()
        {
            var result = JsonSerializer.Deserialize<FilterNodeCollection>("null", Options);
            Assert.Null(result);
        }

        [Fact]
        [DisplayName("Read 非 StartArray token 應拋出 JsonException")]
        public void Read_NonStartArray_ThrowsJsonException()
        {
            Assert.Throws<JsonException>(() =>
                JsonSerializer.Deserialize<FilterNodeCollection>("123", Options));
        }

        [Fact]
        [DisplayName("Read kind 為字串 'Condition' 應解為 FilterCondition")]
        public void Read_StringKindCondition_ParsesAsFilterCondition()
        {
            var json = """[{"kind":"Condition","fieldName":"X","operator":0,"value":"a"}]""";
            var restored = JsonSerializer.Deserialize<FilterNodeCollection>(json, Options);

            Assert.NotNull(restored);
            Assert.Single(restored!);
            Assert.IsType<FilterCondition>(restored[0]);
        }

        [Fact]
        [DisplayName("Read kind 為字串 'Group' 應解為 FilterGroup")]
        public void Read_StringKindGroup_ParsesAsFilterGroup()
        {
            var json = """[{"kind":"Group","operator":0,"nodes":[]}]""";
            var restored = JsonSerializer.Deserialize<FilterNodeCollection>(json, Options);

            Assert.NotNull(restored);
            Assert.Single(restored!);
            Assert.IsType<FilterGroup>(restored[0]);
        }

        [Fact]
        [DisplayName("Read kind 為整數 0 應解為 FilterCondition")]
        public void Read_IntKindCondition_ParsesAsFilterCondition()
        {
            var json = """[{"kind":0,"fieldName":"X","operator":0,"value":"a"}]""";
            var restored = JsonSerializer.Deserialize<FilterNodeCollection>(json, Options);

            Assert.NotNull(restored);
            Assert.IsType<FilterCondition>(restored![0]);
        }

        [Fact]
        [DisplayName("Read kind 為整數 1 應解為 FilterGroup")]
        public void Read_IntKindGroup_ParsesAsFilterGroup()
        {
            var json = """[{"kind":1,"operator":0,"nodes":[]}]""";
            var restored = JsonSerializer.Deserialize<FilterNodeCollection>(json, Options);

            Assert.NotNull(restored);
            Assert.IsType<FilterGroup>(restored![0]);
        }

        [Fact]
        [DisplayName("Read 元素無 kind 屬性應預設為 FilterCondition")]
        public void Read_ElementWithoutKind_DefaultsToFilterCondition()
        {
            var json = """[{"fieldName":"X","operator":0,"value":"a"}]""";
            var restored = JsonSerializer.Deserialize<FilterNodeCollection>(json, Options);

            Assert.NotNull(restored);
            Assert.Single(restored!);
            Assert.IsType<FilterCondition>(restored[0]);
        }

        [Fact]
        [DisplayName("Read 未知 kind 整數應拋出 JsonException")]
        public void Read_UnknownIntKind_ThrowsJsonException()
        {
            var json = """[{"kind":99}]""";
            Assert.Throws<JsonException>(() =>
                JsonSerializer.Deserialize<FilterNodeCollection>(json, Options));
        }

        [Fact]
        [DisplayName("Write 直接呼叫 converter 於 null collection 應寫入 JSON null")]
        public void Write_DirectConverter_NullCollection_WritesNull()
        {
            // JsonSerializer.Serialize<FilterNodeCollection?>(null, ...) 會由框架短路
            // 寫入 null，不會進入 converter.Write；直接呼叫才能覆蓋 value == null 分支。
            var converter = new FilterNodeCollectionJsonConverter();
            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream))
            {
                converter.Write(writer, null!, new JsonSerializerOptions());
            }

            var json = Encoding.UTF8.GetString(stream.ToArray());
            Assert.Equal("null", json);
        }

        [Fact]
        [DisplayName("Read 直接呼叫 converter 於 Null token 應回傳 null")]
        public void Read_DirectConverter_NullToken_ReturnsNull()
        {
            // JsonSerializer.Deserialize<FilterNodeCollection?>("null", ...) 會由框架短路
            // 回傳 null，不會進入 converter.Read；直接呼叫才能覆蓋 TokenType.Null 分支。
            var converter = new FilterNodeCollectionJsonConverter();
            var bytes = Encoding.UTF8.GetBytes("null");
            var reader = new Utf8JsonReader(bytes);
            Assert.True(reader.Read());

            var result = converter.Read(ref reader, typeof(FilterNodeCollection), new JsonSerializerOptions());
            Assert.Null(result);
        }
    }
}
