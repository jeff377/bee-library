using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Xml.Serialization;
using Bee.Base;
using MessagePack;
using Newtonsoft.Json;

namespace Bee.Define
{
    /// <summary>
    /// 具鍵值的強型別集合。
    /// </summary>
    /// <typeparam name="T">集合成員型別。</typeparam>
    [Serializable]
    public class TKeyCollectionBase<T> : KeyedCollection<string, T>, IKeyCollectionBase, IObjectSerialize, ITagProperty, IMessagePackSerializationCallbackReceiver
        where T : class, IKeyCollectionItem  // 定義成員型別必須實作 IKeyCollectionItem 介面
    {
        private ESerializeState _SerializeState = ESerializeState.None;

        #region 建構函式

        /// <summary>
        /// 建構函式。
        /// </summary>
        public TKeyCollectionBase() : base(StringComparer.CurrentCultureIgnoreCase)
        {
        }

        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="owner">擁有者。</param>
        public TKeyCollectionBase(object owner) : this()
        {
            Owner = owner;
        }

        #endregion

        #region IKeyCollectionBase 介面

        /// <summary>
        ///  擁有者。
        /// </summary>
        [XmlIgnore, JsonIgnore, IgnoreMember]
        [Browsable(false)]
        public object Owner { get; } = null;

        /// <summary>
        /// 變更成員鍵值。 
        /// </summary>
        /// <param name="key">鍵值。</param>
        /// <param name="value">成員。</param>
        public void ChangeItemKey(string key, IKeyCollectionItem value)
        {
            base.ChangeItemKey((T)value, key);
        }

        /// <summary>
        /// 移除成員。
        /// </summary>
        /// <param name="value">成員。</param>
        public void Remove(IKeyCollectionItem value)
        {
            base.Remove(value.Key);
        }

        /// <summary>
        /// 加入成員。
        /// </summary>
        /// <param name="value">成員。</param>
        public void Add(IKeyCollectionItem value)
        {
            base.Add((T)value);
        }

        /// <summary>
        /// 插入成員。
        /// </summary>
        /// <param name="index">索引位置。</param>
        /// <param name="value">成員。</param>
        public void Insert(int index, IKeyCollectionItem value)
        {
            base.Insert(index, (T)value);
        }

        #endregion

        #region IObjectSerialize 介面

        /// <summary>
        /// 序列化狀態。
        /// </summary>
        [XmlIgnore, JsonIgnore, IgnoreMember]
        [Browsable(false)]
        public ESerializeState SerializeState
        {
            get { return _SerializeState; }
        }

        /// <summary>
        /// 設定序列化狀態。
        /// </summary>
        /// <param name="serializeState">序列化狀態。</param>
        public virtual void SetSerializeState(ESerializeState serializeState)
        {
            _SerializeState = serializeState;
            foreach (object item in this)
            {
                if (item is IObjectSerialize)
                    ((IObjectSerialize)item).SetSerializeState(serializeState);
            }
        }

        #endregion

        #region ITagProperty 介面

        /// <summary>
        /// 儲存額外資訊。
        /// </summary>
        [XmlIgnore, JsonIgnore, IgnoreMember]
        [Browsable(false)]
        public object Tag { get; set; } = null;

        #endregion

        #region IMessagePackSerializationCallbackReceiver 介面

        private List<T> _itemsBuffer;

        /// <summary>
        /// MessagePack 透過這個欄位代理序列化 Items 的內容。
        /// </summary>
        /// <remarks>父類別 KeyedCollection 不支援 MessagePack 序列化，需透過 ItemsForSerialization 屬性序列化資料。</remarks>
        [Key(0)]
        public List<T> ItemsForSerialization
        {
            get => Items.ToList();      // 將內部項目轉為 List 傳給 MessagePack
            set => _itemsBuffer = value;
        }

        /// <summary>
        /// MessagePack 透過這個欄位反序列化資料。
        /// </summary>
        void IMessagePackSerializationCallbackReceiver.OnBeforeSerialize()
        {
            _itemsBuffer = null; // 確保序列化前不會有資料
        }

        /// <summary>
        /// MessagePack 透過這個欄位反序列化資料。
        /// </summary>
        void IMessagePackSerializationCallbackReceiver.OnAfterDeserialize()
        {
            if (_itemsBuffer != null)
            {
                foreach (var item in _itemsBuffer)
                {
                    Add(item);
                }
                _itemsBuffer = null;
            }
        }

        #endregion

        /// <summary>
        /// 取得成員鍵值。
        /// </summary>
        /// <param name="item">成員。</param>
        protected override string GetKeyForItem(T item)
        {
            return (item as IKeyCollectionItem).Key;
        }

        /// <summary>
        /// 覆寫 InsertItem 方法。
        /// </summary>
        /// <param name="index">索引。</param>
        /// <param name="item">成員。</param>
        protected override void InsertItem(int index, T item)
        {
            base.InsertItem(index, item);
            (item as IKeyCollectionItem).SetCollection(this);
        }

        /// <summary>
        /// 覆寫 RemoveItem 方法。
        /// </summary>
        /// <param name="index">索引。</param>
        protected override void RemoveItem(int index)
        {
            (this[index] as IKeyCollectionItem).SetCollection(null);
            base.RemoveItem(index);
        }

    }
}
