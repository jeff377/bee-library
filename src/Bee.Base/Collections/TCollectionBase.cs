using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Xml.Serialization;
using Newtonsoft.Json;

namespace Bee.Base
{
    /// <summary>
    /// 強型別集合。
    /// </summary>
    /// <typeparam name="T">集合成員型別。</typeparam>
    [Serializable]
    public abstract class TCollectionBase<T> : Collection<T>, ICollectionBase, IObjectSerialize, ITagProperty
        where T : class, ICollectionItem
    {
        #region 建構函式

        /// <summary>
        /// 建構函式。
        /// </summary>
        public TCollectionBase()
        {
            Owner = null;
        }

        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="owner">擁有者。</param>
        public TCollectionBase(object owner)
        {
            Owner = owner;
        }

        #endregion

        #region ICollectionBase 介面

        /// <summary>
        ///  擁有者。
        /// </summary>
        [XmlIgnore]
        [JsonIgnore]
        [Browsable(false)]
        public object Owner { get; private set; }

        /// <summary>
        /// 設定擁有者。
        /// </summary>
        /// <param name="owner">擁有者。</param>
        protected void SetOwner(object owner)
        {
            Owner = owner;
        }

        /// <summary>
        /// 移除成員。
        /// </summary>
        /// <param name="value">成員。</param>
        public void Remove(ICollectionItem value)
        {
            base.Remove((T)value);
        }

        /// <summary>
        /// 加入成員。
        /// </summary>
        /// <param name="value">成員。</param>
        public void Add(ICollectionItem value)
        {
            base.Add((T)value);
        }

        /// <summary>
        /// 插入成員。
        /// </summary>
        /// <param name="index">索引位置。</param>
        /// <param name="value">成員。</param>
        public void Insert(int index, ICollectionItem value)
        {
            base.Insert(index, (T)value);
        }

        #endregion

        #region IObjectSerialize 介面

        /// <summary>
        /// 序列化狀態。
        /// </summary>
        [XmlIgnore]
        [JsonIgnore]
        [Browsable(false)]
        public ESerializeState SerializeState { get; private set; }

        /// <summary>
        /// 設定序列化狀態。
        /// </summary>
        /// <param name="serializeState">序列化狀態。</param>
        public virtual void SetSerializeState(ESerializeState serializeState)
        {
            SerializeState = serializeState;
            foreach (var item in this)
            {
                if (item is IObjectSerialize serializable)
                    serializable.SetSerializeState(serializeState);
            }
        }

        #endregion

        #region ITagProperty 介面

        /// <summary>
        /// 儲存額外資訊。
        /// </summary>
        [XmlIgnore]
        [JsonIgnore]
        [Browsable(false)]
        public object Tag { get; set; } = null;

        #endregion

        /// <summary>
        /// 覆寫 InsertItem 方法。
        /// </summary>
        /// <param name="index">索引。</param>
        /// <param name="item">成員。</param>
        protected override void InsertItem(int index, T item)
        {
            // 加入成員
            base.InsertItem(index, item);
            // 設定成員的集合類別
            (item as ICollectionItem).SetCollection(this);
        }

        /// <summary>
        /// 覆寫 RemoveItem 方法。
        /// </summary>
        /// <param name="index">索引。</param>
        protected override void RemoveItem(int index)
        {
            // 移除成員的集合類別
            (this[index] as ICollectionItem).SetCollection(null);
            // 移除成員
            base.RemoveItem(index);
        }
    }
}
