namespace Bee.Definition.Filters
{
    /// <summary>
    /// The kind of a filter node.
    /// </summary>
    public enum FilterNodeKind
    {
        /// <summary>A single-field condition.</summary>
        Condition = 0,
        /// <summary>A condition group.</summary>
        Group = 1
    }
}
