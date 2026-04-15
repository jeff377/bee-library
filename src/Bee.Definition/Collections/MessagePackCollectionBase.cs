using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Xml.Serialization;
using Bee.Base;
using Bee.Base.Serialization;
using Bee.Base.Collections;
using MessagePack;
using System.Text.Json.Serialization;

namespace Bee.Definition.Collections
{
    /// <summary>
    /// Strongly typed collection with MessagePack support.
    /// </summary>
    /// <typeparam name="T">The collection item type.</typeparam>
    [Serializable]
    public abstract class MessagePackCollectionBase<T> : Collection<T>, ICollectionBase, IObjectSerialize, ITagProperty
        where T : class, ICollectionItem  // Item type must implement ICollectionItem interface
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of <see cref="MessagePackCollectionBase{T}"/>.
        /// </summary>
        public MessagePackCollectionBase() : base()
        {
            Owner = null;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="MessagePackCollectionBase{T}"/>.
        /// </summary>
        /// <param name="owner">The owner object.</param>
        public MessagePackCollectionBase(object owner) : base()
        {
            Owner = owner;
        }

        #endregion

        #region ICollectionBase Interface

        /// <summary>
        /// Gets the owner object.
        /// </summary>
        [XmlIgnore, JsonIgnore, IgnoreMember]
        [Browsable(false)]
        public object Owner { get; private set; }

        /// <summary>
        /// Sets the owner object.
        /// </summary>
        /// <param name="owner">The owner object.</param>
        protected void SetOwner(object owner)
        {
            Owner = owner;
        }

        /// <summary>
        /// Removes an item from the collection.
        /// </summary>
        /// <param name="value">The item to remove.</param>
        public void Remove(ICollectionItem value)
        {
            base.Remove((T)value);
        }

        /// <summary>
        /// Adds an item to the collection.
        /// </summary>
        /// <param name="value">The item to add.</param>
        public void Add(ICollectionItem value)
        {
            base.Add((T)value);
        }

        /// <summary>
        /// Inserts an item at the specified index.
        /// </summary>
        /// <param name="index">The index position.</param>
        /// <param name="value">The item to insert.</param>
        public void Insert(int index, ICollectionItem value)
        {
            base.Insert(index, (T)value);
        }

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
            foreach (var item in this)
            {
                if (item is IObjectSerialize serializable)
                    serializable.SetSerializeState(serializeState);
            }
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

        /// <summary>
        /// Overrides the InsertItem method.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="item">The item.</param>
        protected override void InsertItem(int index, T item)
        {
            // Add item
            base.InsertItem(index, item);
            // Set the item's collection reference
            item.SetCollection(this);
        }

        /// <summary>
        /// Overrides the RemoveItem method.
        /// </summary>
        /// <param name="index">The index.</param>
        protected override void RemoveItem(int index)
        {
            // Clear the item's collection reference
            this[index].SetCollection(null);
            // Remove item
            base.RemoveItem(index);
        }
    }
}
