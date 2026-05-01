namespace Bee.Base.Serialization
{
    /// <summary>
    /// Marker interface indicating that the object must be deep-copied before serialization.
    /// </summary>
    /// <remarks>
    /// Implement this when the object lives in a server-side cache AND the serialization
    /// pipeline mutates property values in place (e.g. encrypting sensitive fields like
    /// connection-string passwords during <c>BeforeSerialize</c>). Without the deep copy,
    /// the cached instance gets the encrypted value written back, which both leaks
    /// ciphertext into in-memory reads and breaks the encryption-idempotency invariant
    /// on the next serialization.
    /// <para>
    /// Callers (typically the request pipeline serving cached definitions to the client)
    /// must invoke <see cref="CreateSerializableCopy"/> and serialize the returned copy,
    /// never the original cached instance.
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
