namespace Bee.Core.Serialization
{
    /// <summary>
    /// Interface for objects that support serialization state management.
    /// </summary>
    public interface IObjectSerialize : IObjectSerializeBase
    {
        /// <summary>
        /// Gets the current serialization state.
        /// </summary>
        SerializeState SerializeState { get; }

        /// <summary>
        /// Sets the serialization state.
        /// </summary>
        /// <param name="serializeState">The serialization state to set.</param>
        void SetSerializeState(SerializeState serializeState);
    }
}
