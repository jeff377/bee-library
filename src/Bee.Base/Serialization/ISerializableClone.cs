namespace Bee.Base.Serialization
{
    /// <summary>
    /// Marker interface indicating that the object must be deep-copied before serialization.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Implement this when the instance is shared — typically held in a process-wide definition
    /// cache — and something about serializing it would be observable to whoever else holds it.
    /// The caller serializes the copy and leaves the shared instance alone.
    /// </para>
    /// <para>
    /// NOTE: what this actually guards today is the serialization lifecycle itself.
    /// <see cref="XmlCodec.Serialize"/> and <see cref="JsonCodec.Serialize"/> call
    /// <see cref="IObjectSerialize.SetSerializeState"/> on the source object and let it propagate
    /// down the whole tree, so for the duration of the call every empty collection getter on the
    /// shared instance returns <c>null</c> (see <c>SerializationUtilities.IsSerializeEmpty</c>).
    /// A concurrent reader of that instance sees the transient <c>null</c>.
    /// </para>
    /// <para>
    /// WARNING: this does **not** protect secrets. <c>DatabaseSettings</c> — the only implementer —
    /// encrypts its passwords in <c>CacheDefineAccess.SaveDatabaseSettings</c>, explicitly and
    /// outside the serialization pipeline, and its cached instance holds plain text once
    /// <c>DecryptInPlace</c> has run on first read. The copy carries those plain-text values
    /// verbatim. Do not treat implementing this interface as a confidentiality measure.
    /// </para>
    /// </remarks>
    public interface ISerializableClone
    {
        /// <summary>
        /// Creates a deep copy of the object for use during serialization.
        /// </summary>
        object CreateSerializableCopy();
    }

}
