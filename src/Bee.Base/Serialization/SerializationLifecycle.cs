namespace Bee.Base.Serialization
{
    /// <summary>
    /// Shared lifecycle hooks invoked by <see cref="XmlCodec"/> and <see cref="JsonCodec"/>
    /// when a value implements <see cref="IObjectSerialize"/>.
    /// </summary>
    internal static class SerializationLifecycle
    {
        /// <summary>
        /// Marks the value's state as <see cref="SerializeState.Serialize"/> before serialization.
        /// </summary>
        public static void NotifyBefore(object? value)
        {
            if (value is IObjectSerialize os) { os.SetSerializeState(SerializeState.Serialize); }
        }

        /// <summary>
        /// Clears the value's serialize state after serialization.
        /// </summary>
        public static void NotifyAfter(object? value)
        {
            if (value is IObjectSerialize os) { os.SetSerializeState(SerializeState.None); }
        }
    }
}
