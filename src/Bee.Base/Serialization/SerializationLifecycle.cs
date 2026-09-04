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

        /// <summary>
        /// Marks the value as serializing and clears that state when the returned scope is disposed,
        /// whether serialization succeeded or threw.
        /// </summary>
        /// <param name="value">The value being serialized.</param>
        /// <remarks>
        /// <para>
        /// WARNING: the pairing is load-bearing and the failure is permanent. Both codecs used to
        /// call <see cref="NotifyBefore"/>, serialize, then <see cref="NotifyAfter"/> in sequence —
        /// so a serializer that threw left the object stuck in
        /// <see cref="SerializeState.Serialize"/> <b>for good</b>. Since the values being serialized
        /// include process-wide cached definitions, one failure would make every empty-collection
        /// getter on that instance return <c>null</c> from then on, and several call sites
        /// dereference those with <c>!</c> because an empty collection is a perfectly normal state.
        /// </para>
        /// <para>
        /// A scope rather than a <c>try</c>/<c>finally</c> at each call site: the two codecs had the
        /// same shape and the same bug, and a shape repeated twice is a shape that diverges. This
        /// makes the invariant structural instead of remembered. It is a <c>readonly struct</c>,
        /// so a <c>using</c> on the concrete type costs no allocation on the serialization path.
        /// </para>
        /// </remarks>
        public static SerializeScope BeginSerialize(object? value)
        {
            NotifyBefore(value);
            return new SerializeScope(value);
        }

        /// <summary>
        /// Clears the serialize state of <paramref name="value"/> on disposal.
        /// </summary>
        /// <param name="value">The value being serialized.</param>
        public readonly struct SerializeScope(object? value) : IDisposable
        {
            /// <inheritdoc/>
            public void Dispose() => NotifyAfter(value);
        }
    }
}
