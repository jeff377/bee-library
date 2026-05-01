using System.Text.Json.Serialization;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Xml.Serialization;
using Bee.Base.Serialization;

namespace Bee.Base.Collections
{
    /// <summary>
    /// Base class for strongly-typed collections.
    /// </summary>
    /// <typeparam name="T">The type of the collection items.</typeparam>
    public abstract class CollectionBase<T> : Collection<T>, ICollectionBase, IObjectSerialize, ITagProperty
        where T : class, ICollectionItem  // Item type must implement ICollectionItem
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of <see cref="CollectionBase{T}"/>.
        /// </summary>
        protected CollectionBase() : base()
        {
            Owner = null;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="CollectionBase{T}"/> with the specified owner.
        /// </summary>
        /// <param name="owner">The owner of this collection.</param>
        protected CollectionBase(object owner) : base()
        {
            Owner = owner;
        }

        #endregion

        #region ICollectionBase Interface

        /// <summary>
        /// Gets the owner of this collection.
        /// </summary>
        [XmlIgnore, JsonIgnore]
        [Browsable(false)]
        public object? Owner { get; private set; }

        /// <summary>
        /// Sets the owner of this collection.
        /// </summary>
        /// <param name="owner">The owner object.</param>
        protected void SetOwner(object owner)
        {
            Owner = owner;
        }

        /// <summary>
        /// Removes the specified item from the collection.
        /// </summary>
        /// <param name="value">The item to remove.</param>
        public void Remove(ICollectionItem value)
        {
            base.Remove((T)value);
        }

        /// <summary>
        /// Adds the specified item to the collection.
        /// </summary>
        /// <param name="value">The item to add.</param>
        public void Add(ICollectionItem value)
        {
            base.Add((T)value);
        }

        /// <summary>
        /// Inserts the specified item at the given index.
        /// </summary>
        /// <param name="index">The zero-based index at which to insert the item.</param>
        /// <param name="value">The item to insert.</param>
        public void Insert(int index, ICollectionItem value)
        {
            base.Insert(index, (T)value);
        }

        #endregion

        #region IObjectSerialize Interface

        /// <summary>
        /// Gets the current serialization state.
        /// </summary>
        [XmlIgnore, JsonIgnore]
        [Browsable(false)]
        public SerializeState SerializeState { get; private set; } = SerializeState.None;

        /// <summary>
        /// Sets the serialization state for this collection and all of its items.
        /// </summary>
        /// <param name="serializeState">The serialization state to set.</param>
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
        /// Gets or sets an arbitrary object for storing additional information.
        /// </summary>
        [XmlIgnore, JsonIgnore]
        [Browsable(false)]
        public object? Tag { get; set; }

        #endregion

        /// <summary>
        /// Overrides the <see cref="Collection{T}.InsertItem"/> method to set the owning collection on the inserted item.
        /// </summary>
        /// <param name="index">The zero-based index at which to insert the item.</param>
        /// <param name="item">The item to insert.</param>
        protected override void InsertItem(int index, T item)
        {
            // Add the item to the collection
            base.InsertItem(index, item);
            // Set the owning collection on the item
            item.SetCollection(this);
        }

        /// <summary>
        /// Overrides the <see cref="Collection{T}.RemoveItem"/> method to clear the owning collection reference before removal.
        /// </summary>
        /// <param name="index">The zero-based index of the item to remove.</param>
        protected override void RemoveItem(int index)
        {
            // Clear the owning collection reference on the item
            this[index].SetCollection(null);
            // Remove the item
            base.RemoveItem(index);
        }
    }
}
