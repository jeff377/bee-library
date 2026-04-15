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
    /// Strongly typed collection item with MessagePack support.
    /// </summary>
    [Serializable]
    public abstract class MessagePackCollectionItem : ICollectionItem, ITagProperty, IObjectSerialize
    {
        private ICollectionBase _collection = null;

        #region ICollectionItem Interface

        /// <summary>
        /// Sets the owning collection.
        /// </summary>
        /// <param name="collection">The collection.</param>
        public void SetCollection(ICollectionBase collection)
        {
            _collection = collection;
        }

        /// <summary>
        /// Removes this item from its collection.
        /// </summary>
        public void Remove()
        {
            if (_collection != null)
                _collection.Remove(this);
        }

        #endregion

        #region ITagProperty Interface

        /// <summary>
        /// Gets or sets the tag for storing additional information.
        /// </summary>
        [XmlIgnore, JsonIgnore, IgnoreMember]
        [Browsable(false)]
        public object Tag { get; set; } = null;

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
        public ICollectionBase Collection
        {
            get { return _collection; }
        }
    }
}
