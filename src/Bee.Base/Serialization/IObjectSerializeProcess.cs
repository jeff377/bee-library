namespace Bee.Base.Serialization
{
    /// <summary>
    /// Interface for object serialization process callbacks.
    /// </summary>
    public interface IObjectSerializeProcess
    {
        /// <summary>
        /// Callback invoked before serialization.
        /// </summary>
        /// <param name="serializeFormat">The serialization format.</param>
        void BeforeSerialize(SerializeFormat serializeFormat);

        /// <summary>
        /// Callback invoked after serialization.
        /// </summary>
        /// <param name="serializeFormat">The serialization format.</param>
        void AfterSerialize(SerializeFormat serializeFormat);

        /// <summary>
        /// Callback invoked after deserialization.
        /// </summary>
        /// <param name="serializeFormat">The serialization format.</param>
        void AfterDeserialize(SerializeFormat serializeFormat);
    }
}
