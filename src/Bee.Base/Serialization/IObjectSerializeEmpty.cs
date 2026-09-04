namespace Bee.Base.Serialization
{
    /// <summary>
    /// Lets a nested complex type report itself as empty during serialization, so the
    /// containing property can return null and omit its XML element entirely.
    /// </summary>
    /// <remarks>
    /// The intended targets are the settings types marked with
    /// <c>TypeConverter(typeof(ExpandableObjectConverter))</c>, whose XML subtree is currently
    /// written out in full even when every value sits at its default. Collections do not need
    /// this interface. <see cref="SerializationUtilities.IsSerializeEmpty"/> already handles
    /// <c>IList</c> and <c>IEnumerable</c> on its own, and every call site today passes one.
    ///
    /// No production type implements this yet. Wiring it up takes two halves: an implementation
    /// on the complex type, plus a null-returning gate on the containing property, shaped like
    /// the one on <c>ExtendedProperties</c>.
    /// </remarks>
    public interface IObjectSerializeEmpty
    {
        /// <summary>
        /// Gets a value indicating whether the object has empty data during serialization.
        /// </summary>
        bool IsSerializeEmpty { get; }
    }
}
