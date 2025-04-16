using System;
using System.ComponentModel;
using System.Xml.Serialization;
using Newtonsoft.Json;

namespace Bee.Base
{
    /// <summary>
    /// 強型別集合成員。
    /// </summary>
    [Serializable]
    public abstract class TCollectionItem : ICollectionItem, ITagProperty, IObjectSerialize
    {
        private ICollectionBase _Collection = null;
        [NonSerialized]
        private object _Tag = null;
        private ESerializeState _SerializeState = ESerializeState.None;

        #region ICollectionItem 介面

        /// <summary>
        /// 設定所屬集合。
        /// </summary>
        /// <param name="collection">集合。</param>
        public void SetCollection(ICollectionBase collection)
        {
            _Collection = collection;
        }

        /// <summary>
        /// 由集合中移除此成員。
        /// </summary>
        public void Remove()
        {
            if (_Collection != null)
                _Collection.Remove(this);
        }

        #endregion

        #region ITagProperty 介面

        /// <summary>
        /// 儲存額外資訊。
        /// </summary>
        [JsonIgnore]
        [XmlIgnore]
        [Browsable(false)]
        public object Tag
        {
            get { return _Tag; }
            set { _Tag = value; }
        }

        #endregion

        #region IObjectSerialize 介面

        /// <summary>
        /// 序列化狀態。
        /// </summary>
        [JsonIgnore]
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
        }

        #endregion

        /// <summary>
        /// 所屬集合。
        /// </summary>
        [JsonIgnore]
        [XmlIgnore]
        [Browsable(false)]
        [TreeNodeIgnore]
        public ICollectionBase Collection
        {
            get { return _Collection; }
        }
    }
}
