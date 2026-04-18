using System.ComponentModel;
using Bee.Base.Serialization;
using Bee.Definition;
using Bee.Definition.Collections;

namespace Bee.Definition.UnitTests.Collections
{
    /// <summary>
    /// MessagePackCollectionItem / MessagePackKeyCollectionItem 基底行為測試。
    /// 使用 <see cref="SortField"/>/<see cref="SortFieldCollection"/>（非 keyed）
    /// 與 <see cref="Parameter"/>/<see cref="ParameterCollection"/>（keyed）作為受測樣本。
    /// </summary>
    public class MessagePackCollectionItemTests
    {
        [Fact]
        [DisplayName("預設建構之 MessagePackCollectionItem，SerializeState 為 None、Tag 為 null、Collection 為 null")]
        public void DefaultState_IsExpected()
        {
            var item = new SortField("Id", SortDirection.Asc);

            Assert.Equal(SerializeState.None, item.SerializeState);
            Assert.Null(item.Tag);
            Assert.Null(item.Collection);
        }

        [Fact]
        [DisplayName("SetSerializeState 應更新自身狀態")]
        public void SetSerializeState_UpdatesState()
        {
            var item = new SortField();

            item.SetSerializeState(SerializeState.Serialize);

            Assert.Equal(SerializeState.Serialize, item.SerializeState);
        }

        [Fact]
        [DisplayName("Tag 屬性應可被設定並讀回")]
        public void Tag_Settable()
        {
            var item = new SortField();
            var marker = new object();

            item.Tag = marker;

            Assert.Same(marker, item.Tag);
        }

        [Fact]
        [DisplayName("加入集合後，Collection 應回傳所屬集合")]
        public void Collection_AfterAdd_ReturnsOwner()
        {
            var collection = new SortFieldCollection();
            var item = new SortField("Id", SortDirection.Asc);

            collection.Add(item);

            Assert.Same(collection, item.Collection);
        }

        [Fact]
        [DisplayName("Remove 應從所屬集合中移除自身")]
        public void Remove_RemovesSelfFromCollection()
        {
            var collection = new SortFieldCollection();
            var item = new SortField("Id", SortDirection.Asc);
            collection.Add(item);
            Assert.Single(collection);

            item.Remove();

            Assert.Empty(collection);
        }

        [Fact]
        [DisplayName("未加入集合時呼叫 Remove 應不拋出例外")]
        public void Remove_WithoutCollection_DoesNotThrow()
        {
            var item = new SortField();

            item.Remove();

            Assert.Null(item.Collection);
        }
    }

    /// <summary>
    /// MessagePackKeyCollectionItem 專屬行為測試（Key 設定、Remove、SerializeState）。
    /// </summary>
    public class MessagePackKeyCollectionItemTests
    {
        [Fact]
        [DisplayName("預設建構，Key 為空字串、SerializeState 為 None、Collection 為 null")]
        public void DefaultState_IsExpected()
        {
            var item = new Parameter();

            Assert.Equal(string.Empty, item.Key);
            Assert.Equal(SerializeState.None, item.SerializeState);
            Assert.Null(item.Collection);
            Assert.Null(item.Tag);
        }

        [Fact]
        [DisplayName("未加入集合時，Key 可自由設定")]
        public void Key_Settable_WhenNotInCollection()
        {
            var item = new Parameter { Key = "Alpha" };

            Assert.Equal("Alpha", item.Key);
            Assert.Equal("Alpha", item.Name);
        }

        [Fact]
        [DisplayName("加入集合後變更 Key，集合索引應跟著更新")]
        public void Key_Change_WhileInCollection_UpdatesCollectionIndex()
        {
            var collection = new ParameterCollection
            {
                new Parameter("Alpha", 1)
            };
            var item = collection["Alpha"];

            item.Key = "Beta";

            Assert.False(collection.Contains("Alpha"));
            Assert.True(collection.Contains("Beta"));
            Assert.Same(item, collection["Beta"]);
        }

        [Fact]
        [DisplayName("Key 設為相同值應為無動作")]
        public void Key_SetSameValue_NoOp()
        {
            var collection = new ParameterCollection
            {
                new Parameter("Alpha", 1)
            };
            var item = collection["Alpha"];

            item.Key = "Alpha";

            Assert.True(collection.Contains("Alpha"));
            Assert.Single(collection);
        }

        [Fact]
        [DisplayName("SetSerializeState 應更新自身狀態")]
        public void SetSerializeState_UpdatesState()
        {
            var item = new Parameter("P", 1);

            item.SetSerializeState(SerializeState.Serialize);

            Assert.Equal(SerializeState.Serialize, item.SerializeState);
        }

        [Fact]
        [DisplayName("Remove 應從所屬集合中移除自身")]
        public void Remove_RemovesSelfFromCollection()
        {
            var collection = new ParameterCollection
            {
                new Parameter("A", 1),
                new Parameter("B", 2)
            };
            var item = collection["A"];

            item.Remove();

            Assert.False(collection.Contains("A"));
            Assert.True(collection.Contains("B"));
        }

        [Fact]
        [DisplayName("未加入集合時呼叫 Remove 應不拋出例外")]
        public void Remove_WithoutCollection_DoesNotThrow()
        {
            var item = new Parameter("X", 1);

            item.Remove();

            Assert.Null(item.Collection);
        }
    }
}
