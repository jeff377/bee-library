using System.ComponentModel;
using Bee.Base.Collections;
using Bee.Base.Serialization;
using Bee.Definition.Collections;
using MessagePack;

namespace Bee.Definition.UnitTests.Collections
{
    /// <summary>
    /// MessagePackKeyCollectionBase / MessagePackKeyCollectionItem 行為測試。
    /// 使用既有 <see cref="Parameter"/> / <see cref="ParameterCollection"/> 作為受測樣本。
    /// </summary>
    public class MessagePackKeyCollectionTests
    {
        /// <summary>
        /// 用於測試 protected 成員（owner 建構子）的子類別。
        /// </summary>
        private sealed class OwnerAwareKeyCollection : MessagePackKeyCollectionBase<Parameter>
        {
            public OwnerAwareKeyCollection() : base() { }
            public OwnerAwareKeyCollection(object owner) : base(owner) { }
        }

        [Fact]
        [DisplayName("Add 項目後可依索引位置與 Key 取得項目")]
        public void Add_Item_CanBeRetrievedByKeyAndIndex()
        {
            // Arrange
            var collection = new ParameterCollection
            {
                new Parameter("P1", 100),
                new Parameter("P2", "ABC")
            };

            // Act
            var byKey = collection["P2"];
            var byIndex = collection[0];

            // Assert
            Assert.Equal("P2", byKey.Name);
            Assert.Equal("ABC", byKey.Value);
            Assert.Equal("P1", byIndex.Name);
        }

        [Fact]
        [DisplayName("Contains 應回傳 Key 是否存在")]
        public void Contains_ReturnsWhetherKeyExists()
        {
            // Arrange
            var collection = new ParameterCollection
            {
                new Parameter("P1", 1)
            };

            // Act & Assert
            Assert.True(collection.Contains("P1"));
            Assert.False(collection.Contains("missing"));
        }

        [Fact]
        [DisplayName("Key 比對應忽略大小寫")]
        public void Contains_IsCaseInsensitive()
        {
            // Arrange
            var collection = new ParameterCollection
            {
                new Parameter("MyParam", 1)
            };

            // Act & Assert
            Assert.True(collection.Contains("myparam"));
            Assert.True(collection.Contains("MYPARAM"));
        }

        [Fact]
        [DisplayName("Remove 應移除指定 Key 的項目")]
        public void Remove_RemovesItemByKey()
        {
            // Arrange
            var collection = new ParameterCollection
            {
                new Parameter("P1", 1),
                new Parameter("P2", 2)
            };

            // Act
            collection.Remove("P1");

            // Assert
            Assert.Single(collection);
            Assert.False(collection.Contains("P1"));
            Assert.True(collection.Contains("P2"));
        }

        [Fact]
        [DisplayName("Insert 應將項目插入指定位置")]
        public void Insert_AddsItemAtSpecifiedIndex()
        {
            // Arrange
            var collection = new ParameterCollection
            {
                new Parameter("P1", 1),
                new Parameter("P3", 3)
            };

            // Act
            collection.Insert(1, new Parameter("P2", 2));

            // Assert
            Assert.Equal(3, collection.Count);
            Assert.Equal("P2", collection[1].Name);
        }

        [Fact]
        [DisplayName("Clear 應移除所有項目")]
        public void Clear_RemovesAllItems()
        {
            // Arrange
            var collection = new ParameterCollection
            {
                new Parameter("P1", 1),
                new Parameter("P2", 2)
            };

            // Act
            collection.Clear();

            // Assert
            Assert.Empty(collection);
        }

        [Fact]
        [DisplayName("ItemsForSerialization 取值應回傳目前項目的拷貝")]
        public void ItemsForSerialization_Get_ReturnsCurrentItems()
        {
            // Arrange
            var collection = new ParameterCollection
            {
                new Parameter("P1", 1),
                new Parameter("P2", 2)
            };

            // Act
            var items = collection.ItemsForSerialization;

            // Assert
            Assert.NotNull(items);
            Assert.Equal(2, items!.Count);
        }

        [Fact]
        [DisplayName("以 owner 為參數的建構子應設定 Owner")]
        public void Constructor_WithOwner_SetsOwner()
        {
            var owner = new object();
            var col = new OwnerAwareKeyCollection(owner);
            Assert.Same(owner, col.Owner);
        }

        [Fact]
        [DisplayName("Add(IKeyCollectionItem) 應加入項目並可由索引取得")]
        public void Add_ViaInterface_AddsItem()
        {
            var col = new ParameterCollection();
            IKeyCollectionItem item = new Parameter("P1", 1);

            col.Add(item);

            Assert.Single(col);
            Assert.Equal("P1", col[0].Name);
        }

        [Fact]
        [DisplayName("Insert(IKeyCollectionItem) 應插入於指定 index")]
        public void Insert_ViaInterface_InsertsAtIndex()
        {
            var col = new ParameterCollection
            {
                new Parameter("A", 1),
                new Parameter("C", 3)
            };
            IKeyCollectionItem mid = new Parameter("B", 2);

            col.Insert(1, mid);

            Assert.Equal(3, col.Count);
            Assert.Equal("B", col[1].Name);
        }

        [Fact]
        [DisplayName("Remove(IKeyCollectionItem) 應依 Key 移除項目")]
        public void Remove_ViaInterface_RemovesByKey()
        {
            var col = new ParameterCollection
            {
                new Parameter("P1", 1),
                new Parameter("P2", 2)
            };
            IKeyCollectionItem target = col[0];

            col.Remove(target);

            Assert.Single(col);
            Assert.False(col.Contains("P1"));
        }

        [Fact]
        [DisplayName("ChangeItemKey 應更新項目的 Key 索引")]
        public void ChangeItemKey_UpdatesKeyIndex()
        {
            var col = new ParameterCollection
            {
                new Parameter("Old", 1)
            };
            var item = col["Old"];
            item.Name = "New";

            col.ChangeItemKey("New", item);

            Assert.True(col.Contains("New"));
            Assert.False(col.Contains("Old"));
            Assert.Equal(1, col["New"].Value);
        }

        [Fact]
        [DisplayName("SetSerializeState 應更新集合與所有項目的 SerializeState")]
        public void SetSerializeState_PropagatesToItems()
        {
            var col = new ParameterCollection
            {
                new Parameter("P1", 1),
                new Parameter("P2", 2)
            };

            col.SetSerializeState(SerializeState.Serialize);

            Assert.Equal(SerializeState.Serialize, col.SerializeState);
            foreach (var item in col)
            {
                Assert.Equal(SerializeState.Serialize, ((IObjectSerialize)item).SerializeState);
            }
        }

        [Fact]
        [DisplayName("Tag 預設為 null,可設為任意物件")]
        public void Tag_DefaultAndAssignment()
        {
            var col = new ParameterCollection();
            Assert.Null(col.Tag);

            col.Tag = "meta";
            Assert.Equal("meta", col.Tag);
        }

        [Fact]
        [DisplayName("MessagePack 序列化應保留項目")]
        public void MessagePack_RoundTrip_PreservesItems()
        {
            // ItemsForSerialization setter + OnBeforeSerialize/OnAfterDeserialize 會在序列化流程被觸發
            var original = new ParameterCollection
            {
                new Parameter("A", 10),
                new Parameter("B", "x")
            };
            var options = MessagePackSerializerOptions.Standard
                .WithResolver(MessagePack.Resolvers.ContractlessStandardResolver.Instance);

            byte[] bytes = MessagePackSerializer.Serialize(original, options);
            var restored = MessagePackSerializer.Deserialize<ParameterCollection>(bytes, options);

            Assert.Equal(2, restored.Count);
            Assert.Equal("A", restored[0].Name);
            Assert.Equal("B", restored[1].Name);
        }
    }
}
