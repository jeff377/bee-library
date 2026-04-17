using System.ComponentModel;
using System.Data;
using Bee.Base.Collections;
using Bee.Base.Serialization;

namespace Bee.Base.UnitTests
{
    public class CollectionBaseTests
    {
        private sealed class Item : CollectionItem
        {
            public string Name { get; set; } = string.Empty;
        }

        private sealed class Items : CollectionBase<Item>
        {
            public Items() { }
            public Items(object owner) : base(owner) { }
        }

        [Fact]
        [DisplayName("Add 會設定 Item.Collection，Remove 會清空")]
        public void AddAndRemove_UpdatesOwningCollectionReference()
        {
            var items = new Items();
            var item = new Item { Name = "a" };

            items.Add(item);
            Assert.Same(items, item.Collection);

            items.Remove(item);
            Assert.Null(item.Collection);
        }

        [Fact]
        [DisplayName("Item.Remove 應將自身從集合中移除")]
        public void Item_Remove_RemovesFromOwningCollection()
        {
            var items = new Items();
            var item = new Item { Name = "a" };
            items.Add(item);

            item.Remove();

            Assert.Empty(items);
            Assert.Null(item.Collection);
        }

        [Fact]
        [DisplayName("Insert 應將項目插入指定索引並設定 Collection")]
        public void Insert_AtIndex_InsertsAndSetsCollection()
        {
            var items = new Items();
            items.Add(new Item { Name = "a" });
            items.Add(new Item { Name = "c" });

            var middle = new Item { Name = "b" };
            items.Insert(1, middle);

            Assert.Equal(new[] { "a", "b", "c" }, items.Select(i => i.Name));
            Assert.Same(items, middle.Collection);
        }

        [Fact]
        [DisplayName("以 ICollectionBase 介面操作應與強型別方法一致")]
        public void InterfaceMethods_BehaveLikeTypedMethods()
        {
            var items = new Items();
            ICollectionBase untyped = items;
            var item = new Item { Name = "x" };

            untyped.Add(item);
            Assert.Single(items);
            Assert.Same(items, item.Collection);

            untyped.Insert(0, new Item { Name = "y" });
            Assert.Equal(2, items.Count);

            untyped.Remove(item);
            Assert.Null(item.Collection);
        }

        [Fact]
        [DisplayName("Owner 於建構子指定應可讀取，Tag 可讀寫")]
        public void Owner_AndTag_AreSettable()
        {
            var owner = new object();
            var items = new Items(owner) { Tag = "tag" };

            Assert.Same(owner, items.Owner);
            Assert.Equal("tag", items.Tag);
        }

        [Fact]
        [DisplayName("SetSerializeState 應同步到所有子項目")]
        public void SetSerializeState_PropagatesToItems()
        {
            var items = new Items();
            var a = new Item { Name = "a" };
            var b = new Item { Name = "b" };
            items.Add(a);
            items.Add(b);

            items.SetSerializeState(SerializeState.Serialize);

            Assert.Equal(SerializeState.Serialize, items.SerializeState);
            Assert.Equal(SerializeState.Serialize, a.SerializeState);
            Assert.Equal(SerializeState.Serialize, b.SerializeState);
        }
    }

    public class KeyCollectionBaseTests
    {
        private sealed class KeyedItem : KeyCollectionItem
        {
            public int Value { get; set; }
        }

        private sealed class KeyedItems : KeyCollectionBase<KeyedItem>
        {
            public KeyedItems() { }
            public KeyedItems(object owner) : base(owner) { }
        }

        [Fact]
        [DisplayName("Add 後應可依 Key（忽略大小寫）查找項目")]
        public void Add_AllowsCaseInsensitiveLookup()
        {
            var items = new KeyedItems();
            items.Add(new KeyedItem { Key = "Alpha", Value = 1 });

            Assert.True(items.Contains("alpha"));
            Assert.True(items.Contains("ALPHA"));
            Assert.Equal(1, items["alpha"].Value);
        }

        [Fact]
        [DisplayName("GetOrDefault 於不存在應回傳 null")]
        public void GetOrDefault_MissingKey_ReturnsNull()
        {
            var items = new KeyedItems();
            items.Add(new KeyedItem { Key = "Alpha" });

            Assert.NotNull(items.GetOrDefault("alpha"));
            Assert.Null(items.GetOrDefault("beta"));
        }

        [Fact]
        [DisplayName("變更 Item.Key 應同步更新集合索引")]
        public void ChangingItemKey_UpdatesCollectionIndex()
        {
            var items = new KeyedItems();
            var item = new KeyedItem { Key = "Old" };
            items.Add(item);

            item.Key = "New";

            Assert.False(items.Contains("Old"));
            Assert.True(items.Contains("New"));
        }

        [Fact]
        [DisplayName("ChangeItemKey 介面方法應重新註冊 Key")]
        public void ChangeItemKey_UpdatesIndex()
        {
            var items = new KeyedItems();
            var item = new KeyedItem { Key = "Old" };
            items.Add(item);

            ((IKeyCollectionBase)items).ChangeItemKey("Renamed", item);

            Assert.True(items.Contains("Renamed"));
        }

        [Fact]
        [DisplayName("KeyCollectionItem.Remove 應從集合中移除自身")]
        public void KeyedItem_Remove_RemovesFromCollection()
        {
            var items = new KeyedItems();
            var item = new KeyedItem { Key = "x" };
            items.Add(item);

            item.Remove();

            Assert.Empty(items);
            Assert.Null(item.Collection);
        }

        [Fact]
        [DisplayName("以 IKeyCollectionBase 介面的 Add/Insert/Remove 應與強型別一致")]
        public void InterfaceMethods_BehaveLikeTypedMethods()
        {
            var items = new KeyedItems();
            IKeyCollectionBase untyped = items;

            var a = new KeyedItem { Key = "a" };
            var b = new KeyedItem { Key = "b" };
            untyped.Add(a);
            untyped.Insert(0, b);

            Assert.Equal(2, items.Count);
            Assert.Equal("b", items[0].Key);

            untyped.Remove(a);
            Assert.Single(items);
        }

        [Fact]
        [DisplayName("SetSerializeState 應同步到所有子項目")]
        public void SetSerializeState_PropagatesToItems()
        {
            var items = new KeyedItems();
            var a = new KeyedItem { Key = "a" };
            items.Add(a);

            items.SetSerializeState(SerializeState.Serialize);

            Assert.Equal(SerializeState.Serialize, items.SerializeState);
            Assert.Equal(SerializeState.Serialize, a.SerializeState);
        }

        [Fact]
        [DisplayName("Owner 於建構子指定應可讀取")]
        public void Owner_IsSet()
        {
            var owner = new object();
            var items = new KeyedItems(owner);
            Assert.Same(owner, items.Owner);
        }
    }

    public class StringHashSetTests
    {
        [Fact]
        [DisplayName("字串大小寫視為同值，不會重複加入")]
        public void Add_IsCaseInsensitive()
        {
            var set = new StringHashSet { "Apple" };
            Assert.False(set.Add("apple"));
            Assert.Single(set);
        }

        [Fact]
        [DisplayName("Add(string, delimiter) 應分割字串並加入所有 token")]
        public void AddWithDelimiter_SplitsAndAddsTokens()
        {
            var set = new StringHashSet();
            set.Add("a,b,c", ",");

            Assert.Equal(3, set.Count);
            Assert.Contains("a", set);
            Assert.Contains("c", set);
        }

        [Fact]
        [DisplayName("Add(string, delimiter) 空字串應直接忽略")]
        public void AddWithDelimiter_EmptyInput_NoOp()
        {
            var set = new StringHashSet();
            set.Add(string.Empty, ",");

            Assert.Empty(set);
        }
    }

    public class DictionaryTests
    {
        [Fact]
        [DisplayName("Dictionary<T> 應使用不區分大小寫的 Key")]
        public void Lookup_IsCaseInsensitive()
        {
            var dict = new Dictionary<int> { ["Alpha"] = 1 };

            Assert.Equal(1, dict["alpha"]);
            Assert.True(dict.ContainsKey("ALPHA"));
        }
    }

    public class CollectionExtensionsTests
    {
        [Fact]
        [DisplayName("GetValue 命中時應回傳對應值")]
        public void GetValue_Hit_ReturnsValue()
        {
            var table = new DataTable();
            table.ExtendedProperties["Key"] = 42;

            int result = table.ExtendedProperties.GetValue<int>("Key", 0);
            Assert.Equal(42, result);
        }

        [Fact]
        [DisplayName("GetValue 未命中時應回傳預設值")]
        public void GetValue_Miss_ReturnsDefault()
        {
            var table = new DataTable();

            int result = table.ExtendedProperties.GetValue<int>("Missing", -1);
            Assert.Equal(-1, result);
        }
    }
}
