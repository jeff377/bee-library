using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Xml.Serialization;

namespace Bee.Base
{
    /// <summary>
    /// 具鍵值的強型別集合。
    /// </summary>
    /// <typeparam name="T">集合成員型別。</typeparam>
    [Serializable]
    public class KeyCollectionBase<T> : KeyedCollection<string, T>, IKeyCollectionBase, IObjectSerialize, ITagProperty
        where T : class, IKeyCollectionItem  // 定義成員型別必須實作 IKeyCollectionItem 介面
    {
        #region 建構函式

        /// <summary>
        /// 建構函式。
        /// </summary>
        public KeyCollectionBase() : base(StringComparer.CurrentCultureIgnoreCase)
        {
        }

        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="owner">擁有者。</param>
        public KeyCollectionBase(object owner) : this()
        {
            Owner = owner;
        }

        #endregion

        #region IKeyCollectionBase 介面

        /// <summary>
        ///  擁有者。
        /// </summary>
        [XmlIgnore, JsonIgnore]
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
        [XmlIgnore, JsonIgnore]
        [Browsable(false)]
        public SerializeState SerializeState { get; private set; } = SerializeState.None;

        /// <summary>
        /// 設定序列化狀態。
        /// </summary>
        /// <param name="serializeState">序列化狀態。</param>
        public virtual void SetSerializeState(SerializeState serializeState)
        {
            SerializeState = serializeState;
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
        [XmlIgnore, JsonIgnore]
        [Browsable(false)]
        public object Tag { get; set; } = null;

        #endregion

        /// <summary>
        /// 取得成員鍵值。
        /// </summary>
        /// <param name="item">成員。</param>
        protected override string GetKeyForItem(T item)
        {
            return item.Key;
        }

        /// <summary>
        /// 覆寫 InsertItem 方法。
        /// </summary>
        /// <param name="index">索引。</param>
        /// <param name="item">成員。</param>
        protected override void InsertItem(int index, T item)
        {
            base.InsertItem(index, item);
            item.SetCollection(this);
        }

        /// <summary>
        /// 覆寫 RemoveItem 方法。
        /// </summary>
        /// <param name="index">索引。</param>
        protected override void RemoveItem(int index)
        {
            this[index].SetCollection(null);
            base.RemoveItem(index);
        }

    }
}
