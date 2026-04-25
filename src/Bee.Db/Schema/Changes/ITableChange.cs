namespace Bee.Db.Schema.Changes
{
    /// <summary>
    /// Represents a single structural change detected by <see cref="TableSchemaComparer.CompareToDiff"/>.
    /// Provider-agnostic; the execution kind (ALTER vs rebuild) is resolved later by the orchestrator.
    /// </summary>
    public interface ITableChange
    {
        /// <summary>
        /// Returns a short human-readable description of this change for logging and warnings.
        /// </summary>
        string Describe();
    }
}
