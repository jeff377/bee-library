using Bee.Base;
using Bee.Base.Serialization;
using Bee.Base.Collections;
using MessagePack;
using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Xml.Serialization;

namespace Bee.Define.Collections
{
    /// <summary>
    /// Strongly typed keyed collection with MessagePack support.
    /// </summary>
    /// <typeparam name="T">The collection item type.</typeparam>
    [Serializable]
    public class MessagePackKeyCollectionBase<T> : KeyedCollection<string, T>, IKeyCollectionBase, IObjectSerialize, ITagProperty, IMessagePackSerializationCallbackReceiver
        where T : class, IKeyCollectionItem  // Item type must implement IKeyCollectionItem interface
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of <see cref="MessagePackKeyCollectionBase{T}"/>.
        /// </summary>
        public MessagePackKeyCollectionBase() : base(StringComparer.CurrentCultureIgnoreCase)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="MessagePackKeyCollectionBase{T}"/>.
        /// </summary>
        /// <param name="owner">The owner object.</param>
        public MessagePackKeyCollectionBase(object owner) : this()
        {
            Owner = owner;
        }

        #endregion

        #region IKeyCollectionBase Interface

        /// <summary>
        /// Gets the owner object.
        /// </summary>
        [XmlIgnore, JsonIgnore, IgnoreMember]
        [Browsable(false)]
        public object Owner { get; } = null;

        /// <summary>
        /// Changes the key of an item.
        /// </summary>
        /// <param name="key">The new key value.</param>
        /// <param name="value">The item.</param>
        public void ChangeItemKey(string key, IKeyCollectionItem value)
        {
            base.ChangeItemKey((T)value, key);
        }

        /// <summary>
        /// Removes an item from the collection.
        /// </summary>
        /// <param name="value">The item to remove.</param>
        public void Remove(IKeyCollectionItem value)
        {
            base.Remove(value.Key);
        }

        /// <summary>
        /// Adds an item to the collection.
        /// </summary>
        /// <param name="value">The item to add.</param>
        public void Add(IKeyCollectionItem value)
        {
            base.Add((T)value);
        }

        /// <summary>
        /// Inserts an item at the specified index.
        /// </summary>
        /// <param name="index">The index position.</param>
        /// <param name="value">The item to insert.</param>
        public void Insert(int index, IKeyCollectionItem value)
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
            foreach (object item in this)
            {
                if (item is IObjectSerialize)
                    ((IObjectSerialize)item).SetSerializeState(serializeState);
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

        #region IMessagePackSerializationCallbackReceiver Interface

        private System.Collections.Generic.List<T> _itemsBuffer;

        /// <summary>
        /// Proxy property used by MessagePack to serialize the Items content.
        /// </summary>
        /// <remarks>The base class KeyedCollection does not support MessagePack serialization; data must be serialized via the ItemsForSerialization property.</remarks>
        [Key(0)]
        public System.Collections.Generic.List<T> ItemsForSerialization
        {
            get => Items.ToList();      // Convert internal items to List for MessagePack
            set => _itemsBuffer = value;
        }

        /// <summary>
        /// Called by MessagePack before serialization.
        /// </summary>
        void IMessagePackSerializationCallbackReceiver.OnBeforeSerialize()
        {
            _itemsBuffer = null; // Ensure no buffered data before serialization
        }

        /// <summary>
        /// Called by MessagePack after deserialization.
        /// </summary>
        void IMessagePackSerializationCallbackReceiver.OnAfterDeserialize()
        {
            if (_itemsBuffer != null)
            {
                foreach (var item in _itemsBuffer)
                {
                    Add(item);
                }
                _itemsBuffer = null;
            }
        }

        #endregion

        /// <summary>
        /// Gets the key for the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        protected override string GetKeyForItem(T item)
        {
            return (item as IKeyCollectionItem).Key;
        }

        /// <summary>
        /// Overrides the InsertItem method.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="item">The item.</param>
        protected override void InsertItem(int index, T item)
        {
            base.InsertItem(index, item);
            item.SetCollection(this);
        }

        /// <summary>
        /// Overrides the RemoveItem method.
        /// </summary>
        /// <param name="index">The index.</param>
        protected override void RemoveItem(int index)
        {
            this[index].SetCollection(null);
            base.RemoveItem(index);
        }

    }
}
