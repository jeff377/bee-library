using System;
using System.ComponentModel;
using System.Xml.Serialization;
using Bee.Base;
using MessagePack;
using Newtonsoft.Json;

namespace Bee.Define
{
    /// <summary>
    /// 具鍵值的強型別集合成員。
    /// </summary>
    [Serializable]
    public abstract class KeyCollectionItem : IKeyCollectionItem, ITagProperty, IObjectSerialize
    {
        private string _key = string.Empty;
        private IKeyCollectionBase _collection = null;

        #region IKeyCollectionItem 介面

        /// <summary>
        /// 鍵值。
        /// </summary>
        [XmlIgnore, JsonIgnore, IgnoreMember]
        [Browsable(false)]
        public virtual string Key
        {
            get
            {
                return _key;
            }
            set
            {
                if (_key != value)
                {
                    // 變更成員鍵值
                    if (_collection != null && !StrFunc.IsEquals(_key, value))
                        _collection.ChangeItemKey(value, this);
                    _key = value;
                }
            }
        }

        /// <summary>
        /// 設定所屬集合。
        /// </summary>
        /// <param name="collection">集合。</param>
        public void SetCollection(IKeyCollectionBase collection)
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
        public IKeyCollectionBase Collection
        {
            get { return _collection; }
        }
    }
}
