using System;
using System.ComponentModel;
using System.Xml.Serialization;
using Bee.Base;
using Bee.Base.Attributes;
using Bee.Base.Serialization;
using Bee.Base.Collections;
using MessagePack;
using System.Text.Json.Serialization;

namespace Bee.Definition.Collections
{
    /// <summary>
    /// A strongly-typed collection item with a key value.
    /// </summary>
    [Serializable]
    public abstract class MessagePackKeyCollectionItem : IKeyCollectionItem, ITagProperty, IObjectSerialize
    {
        private string _key = string.Empty;
        private IKeyCollectionBase? _collection = null;

        #region IKeyCollectionItem Interface

        /// <summary>
        /// Gets or sets the key value.
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
                    // Change the item key value
                    if (_collection != null && !StrFunc.IsEquals(_key, value))
                        _collection.ChangeItemKey(value, this);
                    _key = value;
                }
            }
        }

        /// <summary>
        /// Sets the owning collection.
        /// </summary>
        /// <param name="collection">The collection.</param>
        public void SetCollection(IKeyCollectionBase? collection)
        {
            _collection = collection;
        }

        /// <summary>
        /// Removes this item from the collection.
        /// </summary>
        public void Remove()
        {
            if (_collection != null)
                _collection.Remove(this);
        }

        #endregion

        #region ITagProperty Interface

        /// <summary>
        /// Gets or sets additional tag information.
        /// </summary>
        [XmlIgnore, JsonIgnore, IgnoreMember]
        [Browsable(false)]
        public object? Tag { get; set; } = null;

        #endregion

        #region IObjectSerialize Interface

        /// <summary>
        /// Gets the serialization state.
        /// </summary>
        [XmlIgnore, JsonIgnore, IgnoreMember]
        [Browsable(false)]
        public SerializeState SerializeState { get; private set; } = SerializeState.None;

        /// <summary>
        /// Sets the serialization state.
        /// </summary>
        /// <param name="serializeState">The serialization state.</param>
        public virtual void SetSerializeState(SerializeState serializeState)
        {
            SerializeState = serializeState;
        }

        #endregion

        /// <summary>
        /// Gets the owning collection.
        /// </summary>
        [XmlIgnore, JsonIgnore, IgnoreMember]
        [Browsable(false)]
        [TreeNodeIgnore]
        public IKeyCollectionBase? Collection
        {
            get { return _collection; }
        }
    }
}
