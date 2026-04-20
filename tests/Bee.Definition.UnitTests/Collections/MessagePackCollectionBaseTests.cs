using System.ComponentModel;
using Bee.Base.Collections;
using Bee.Base.Serialization;
using Bee.Definition.Collections;

namespace Bee.Definition.UnitTests.Collections
{
    /// <summary>
    /// MessagePackCollectionBase 基底行為測試。
    /// 以 SortFieldCollection/SortField 為具體子類型驗證。
    /// </summary>
    public class MessagePackCollectionBaseTests
    {
        /// <summary>
        /// 用於測試 protected 成員（owner 建構子、SetOwner）的子類別。
        /// </summary>
        private sealed class OwnerAwareCollection : MessagePackCollectionBase<SortField>
        {
            public OwnerAwareCollection() : base() { }
            public OwnerAwareCollection(object owner) : base(owner) { }
            public void CallSetOwner(object owner) => SetOwner(owner);
        }

        private static SortField MakeField(string name = "F") =>
            new SortField(name, SortDirection.Asc);

        [Fact]
        [DisplayName("預設建構 Owner 應為 null")]
        public void DefaultConstructor_OwnerIsNull()
        {
            var col = new SortFieldCollection();
            Assert.Null(col.Owner);
        }

        [Fact]
        [DisplayName("Add 應呼叫 InsertItem 並設定 item.Collection")]
        public void Add_SetsItemCollection()
        {
            var col = new SortFieldCollection();
            var item = MakeField();

            col.Add(item);

            Assert.Same(col, item.Collection);
            Assert.Single(col);
        }

        [Fact]
        [DisplayName("ICollectionItem 版 Add 應可加入相容型別")]
        public void Add_ViaInterface_Works()
        {
            var col = new SortFieldCollection();
            ICollectionItem item = MakeField();

            col.Add(item);

            Assert.Single(col);
            Assert.Same(col, ((SortField)item).Collection);
        }

        [Fact]
        [DisplayName("ICollectionItem 版 Insert 應插入於指定 index")]
        public void Insert_ViaInterface_InsertsAtIndex()
        {
            var col = new SortFieldCollection
            {
                MakeField("A"),
                MakeField("B")
            };

            ICollectionItem newItem = MakeField("C");
            col.Insert(1, newItem);

            Assert.Equal(3, col.Count);
            Assert.Equal("C", col[1].FieldName);
        }

        [Fact]
        [DisplayName("ICollectionItem 版 Remove 應從集合移除並清除 item.Collection")]
        public void Remove_ViaInterface_ClearsItemCollection()
        {
            var col = new SortFieldCollection();
            var item = MakeField();
            col.Add(item);

            col.Remove((ICollectionItem)item);

            Assert.Empty(col);
            Assert.Null(item.Collection);
        }

        [Fact]
        [DisplayName("Clear 應移除所有項目")]
        public void Clear_RemovesAllItems()
        {
            var col = new SortFieldCollection
            {
                MakeField("A"),
                MakeField("B")
            };

            col.Clear();

            Assert.Empty(col);
        }

        [Fact]
        [DisplayName("SerializeState 預設為 None")]
        public void SerializeState_DefaultIsNone()
        {
            var col = new SortFieldCollection();
            Assert.Equal(SerializeState.None, col.SerializeState);
        }

        [Fact]
        [DisplayName("SetSerializeState 應更新集合與所有項目的 SerializeState")]
        public void SetSerializeState_PropagatesToItems()
        {
            var col = new SortFieldCollection();
            var item = MakeField();
            col.Add(item);

            col.SetSerializeState(SerializeState.Serialize);

            Assert.Equal(SerializeState.Serialize, col.SerializeState);
            Assert.Equal(SerializeState.Serialize, item.SerializeState);
        }

        [Fact]
        [DisplayName("Tag 預設為 null，可設為任意物件")]
        public void Tag_DefaultAndAssignment()
        {
            var col = new SortFieldCollection();
            Assert.Null(col.Tag);

            col.Tag = "meta";
            Assert.Equal("meta", col.Tag);
        }

        [Fact]
        [DisplayName("以 owner 為參數的建構子應設定 Owner")]
        public void Constructor_WithOwner_SetsOwner()
        {
            var owner = new object();
            var col = new OwnerAwareCollection(owner);

            Assert.Same(owner, col.Owner);
        }

        [Fact]
        [DisplayName("SetOwner 應更新 Owner 屬性")]
        public void SetOwner_UpdatesOwner()
        {
            var col = new OwnerAwareCollection();
            var owner = new object();

            col.CallSetOwner(owner);

            Assert.Same(owner, col.Owner);
        }
    }
}
