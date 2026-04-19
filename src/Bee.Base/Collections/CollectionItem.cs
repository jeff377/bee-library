using System.Text.Json.Serialization;
using System.ComponentModel;
using System.Xml.Serialization;
using Bee.Base.Attributes;
using Bee.Base.Serialization;

namespace Bee.Base.Collections
{
    /// <summary>
    /// Base class for strongly-typed collection items.
    /// </summary>
    [Serializable]
    public abstract class CollectionItem : ICollectionItem, ITagProperty, IObjectSerialize
    {
        private ICollectionBase? _collection;

        #region ICollectionItem Interface

        /// <summary>
        /// Sets the collection that owns this item.
        /// </summary>
        /// <param name="collection">The owning collection.</param>
        public void SetCollection(ICollectionBase? collection)
        {
            _collection = collection;
        }

        /// <summary>
        /// Removes this item from its owning collection.
        /// </summary>
        public void Remove()
        {
            if (_collection != null)
                _collection.Remove(this);
        }

        #endregion

        #region ITagProperty Interface

        /// <summary>
        /// Gets or sets an arbitrary object for storing additional information.
        /// </summary>
        [XmlIgnore, JsonIgnore]
        [Browsable(false)]
        public object? Tag { get; set; }

        #endregion

        #region IObjectSerialize Interface

        /// <summary>
        /// Gets the current serialization state.
        /// </summary>
        [XmlIgnore, JsonIgnore]
        [Browsable(false)]
        public SerializeState SerializeState { get; private set; } = SerializeState.None;

        /// <summary>
        /// Sets the serialization state.
        /// </summary>
        /// <param name="serializeState">The serialization state to set.</param>
        public virtual void SetSerializeState(SerializeState serializeState)
        {
            SerializeState = serializeState;
        }

        #endregion

        /// <summary>
        /// Gets the collection that owns this item.
        /// </summary>
        [XmlIgnore, JsonIgnore]
        [Browsable(false)]
        [TreeNodeIgnore]
        public ICollectionBase? Collection
        {
            get { return _collection; }
        }
    }
}
