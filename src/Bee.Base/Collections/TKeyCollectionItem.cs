using System;
using System.ComponentModel;
using System.Xml.Serialization;
using Newtonsoft.Json;

namespace Bee.Base
{
    /// <summary>
    /// 具鍵值的強型別集合成員。
    /// </summary>
    [Serializable]
    public abstract class TKeyCollectionItem : IKeyCollectionItem, ITagProperty, IObjectSerialize
    {
        private string _Key = string.Empty;
        private IKeyCollectionBase _Collection = null;
        [NonSerialized]
        private object _Tag = null;
        private ESerializeState _SerializeState = ESerializeState.None;

        #region IKeyCollectionItem 介面

        /// <summary>
        /// 鍵值。
        /// </summary>
        [XmlIgnore, JsonIgnore, Browsable(false)]
        public virtual string Key
        {
            get
            {
                return _Key;
            }
            set
            {
                if (_Key != value)
                {
                    // 變更成員鍵值
                    if (_Collection != null && !StrFunc.IsEquals(_Key, value))
                        _Collection.ChangeItemKey(value, this);
                    _Key = value;
                }
            }
        }

        /// <summary>
        /// 設定所屬集合。
        /// </summary>
        /// <param name="collection">集合。</param>
        public void SetCollection(IKeyCollectionBase collection)
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
        [XmlIgnore]
        [JsonIgnore]
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
        public IKeyCollectionBase Collection
        {
            get { return _Collection; }
        }
    }
}
