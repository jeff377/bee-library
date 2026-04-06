namespace Bee.Base.Serialization
{
    /// <summary>
    /// Marker interface indicating that the object must be deep-copied before serialization.
    /// </summary>
    public interface ISerializableClone
    {
        /// <summary>
        /// Creates a deep copy of the object for use during serialization.
        /// </summary>
        object CreateSerializableCopy();
    }

}
