namespace Bee.Base.Serialization
{
    /// <summary>
    /// Shared lifecycle hooks invoked by <see cref="XmlCodec"/> and <see cref="JsonCodec"/>
    /// when a value implements <see cref="IObjectSerialize"/> or <see cref="IObjectSerializeProcess"/>.
    /// </summary>
    internal static class SerializationLifecycle
    {
        /// <summary>
        /// Notifies pre-serialization: invokes <see cref="IObjectSerializeProcess.BeforeSerialize"/>
        /// and marks state as <see cref="SerializeState.Serialize"/>.
        /// </summary>
        public static void NotifyBefore(SerializeFormat format, object? value)
        {
            if (value is IObjectSerializeProcess sp) { sp.BeforeSerialize(format); }
            if (value is IObjectSerialize os) { os.SetSerializeState(SerializeState.Serialize); }
        }

        /// <summary>
        /// Notifies post-serialization: clears the serialize state and invokes
        /// <see cref="IObjectSerializeProcess.AfterSerialize"/>.
        /// </summary>
        public static void NotifyAfter(SerializeFormat format, object? value)
        {
            if (value is IObjectSerialize os) { os.SetSerializeState(SerializeState.None); }
            if (value is IObjectSerializeProcess sp) { sp.AfterSerialize(format); }
        }

        /// <summary>
        /// Notifies post-deserialization: invokes <see cref="IObjectSerializeProcess.AfterDeserialize"/>.
        /// </summary>
        public static void NotifyAfterDeserialize(SerializeFormat format, object? value)
        {
            if (value is IObjectSerializeProcess sp) { sp.AfterDeserialize(format); }
        }
    }
}
