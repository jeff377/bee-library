using System;
using System.ComponentModel;
using System.Xml.Serialization;
using Bee.Base;
using MessagePack;
using Newtonsoft.Json;

namespace Bee.Define
{
    /// <summary>
    /// 強型別集合成員，支援 MessagePack 的版本。
    /// </summary>
    [Serializable]
    public abstract class MessagePackCollectionItem : ICollectionItem, ITagProperty, IObjectSerialize
    {
        private ICollectionBase _collection = null;

        #region ICollectionItem 介面

        /// <summary>
        /// 設定所屬集合。
        /// </summary>
        /// <param name="collection">集合。</param>
        public void SetCollection(ICollectionBase collection)
        {
            _collection = collection;
        }

        /// <summary>
        /// 由集合中移除此成員。
        /// </summary>
        public void Remove()
        {
            if (_collection != null)
                _collection.Remove(this);
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

        #region IObjectSerialize 介面

        /// <summary>
        /// 序列化狀態。
        /// </summary>
        [XmlIgnore, JsonIgnore, IgnoreMember]
        [Browsable(false)]
        public SerializeState SerializeState { get; private set; } = SerializeState.None;

        /// <summary>
        /// 設定序列化狀態。
        /// </summary>
        /// <param name="serializeState">序列化狀態。</param>
        public virtual void SetSerializeState(SerializeState serializeState)
        {
            SerializeState = serializeState;
        }

        #endregion

        /// <summary>
        /// 所屬集合。
        /// </summary>
        [XmlIgnore, JsonIgnore, IgnoreMember]
        [Browsable(false)]
        [TreeNodeIgnore]
        public ICollectionBase Collection
        {
            get { return _collection; }
        }
    }
}
