namespace Bee.Base.Serialization
{
    /// <summary>
    /// Interface for determining whether an object has empty data during serialization.
    /// </summary>
    public interface IObjectSerializeEmpty
    {
        /// <summary>
        /// Gets a value indicating whether the object has empty data during serialization.
        /// </summary>
        bool IsSerializeEmpty { get; }
    }
}
