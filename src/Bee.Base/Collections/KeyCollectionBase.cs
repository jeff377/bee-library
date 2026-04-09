using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Xml.Serialization;
using Bee.Base.Serialization;

namespace Bee.Base.Collections
{
    /// <summary>
    /// Base class for strongly-typed keyed collections.
    /// </summary>
    /// <typeparam name="T">The type of the collection items.</typeparam>
    [Serializable]
    public class KeyCollectionBase<T> : KeyedCollection<string, T>, IKeyCollectionBase, IObjectSerialize, ITagProperty
        where T : class, IKeyCollectionItem  // Item type must implement IKeyCollectionItem
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of <see cref="KeyCollectionBase{T}"/>.
        /// </summary>
        public KeyCollectionBase() : base(StringComparer.CurrentCultureIgnoreCase)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="KeyCollectionBase{T}"/> with the specified owner.
        /// </summary>
        /// <param name="owner">The owner of this collection.</param>
        public KeyCollectionBase(object owner) : this()
        {
            Owner = owner;
        }

        #endregion

        #region IKeyCollectionBase Interface

        /// <summary>
        /// Gets the owner of this collection.
        /// </summary>
        [XmlIgnore, JsonIgnore]
        [Browsable(false)]
        public object Owner { get; } = null;

        /// <summary>
        /// Changes the key of an existing item in the collection.
        /// </summary>
        /// <param name="key">The new key.</param>
        /// <param name="value">The item whose key should be changed.</param>
        public void ChangeItemKey(string key, IKeyCollectionItem value)
        {
            base.ChangeItemKey((T)value, key);
        }

        /// <summary>
        /// Removes the specified item from the collection.
        /// </summary>
        /// <param name="value">The item to remove.</param>
        public void Remove(IKeyCollectionItem value)
        {
            base.Remove(value.Key);
        }

        /// <summary>
        /// Adds the specified item to the collection.
        /// </summary>
        /// <param name="value">The item to add.</param>
        public void Add(IKeyCollectionItem value)
        {
            base.Add((T)value);
        }

        /// <summary>
        /// Inserts the specified item at the given index.
        /// </summary>
        /// <param name="index">The zero-based index at which to insert the item.</param>
        /// <param name="value">The item to insert.</param>
        public void Insert(int index, IKeyCollectionItem value)
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
            foreach (object item in this)
            {
                if (item is IObjectSerialize)
                    ((IObjectSerialize)item).SetSerializeState(serializeState);
            }
        }

        #endregion

        #region ITagProperty Interface

        /// <summary>
        /// Gets or sets an arbitrary object for storing additional information.
        /// </summary>
        [XmlIgnore, JsonIgnore]
        [Browsable(false)]
        public object Tag { get; set; } = null;

        #endregion

        /// <summary>
        /// Returns the key for the specified item.
        /// </summary>
        /// <param name="item">The item whose key to retrieve.</param>
        protected override string GetKeyForItem(T item)
        {
            return item.Key;
        }

        /// <summary>
        /// Overrides the <see cref="KeyedCollection{TKey,TItem}.InsertItem"/> method to set the owning collection on the inserted item.
        /// </summary>
        /// <param name="index">The zero-based index at which to insert the item.</param>
        /// <param name="item">The item to insert.</param>
        protected override void InsertItem(int index, T item)
        {
            base.InsertItem(index, item);
            item.SetCollection(this);
        }

        /// <summary>
        /// Overrides the <see cref="KeyedCollection{TKey,TItem}.RemoveItem"/> method to clear the owning collection reference before removal.
        /// </summary>
        /// <param name="index">The zero-based index of the item to remove.</param>
        protected override void RemoveItem(int index)
        {
            this[index].SetCollection(null);
            base.RemoveItem(index);
        }

        /// <summary>
        /// Returns the item with the specified key, or the default value if not found.
        /// </summary>
        /// <param name="key">The key to look up.</param>
        public T GetOrDefault(string key)
        {
            if (Contains(key))
                return this[key];
            else
                return default;
        }
    }
}
