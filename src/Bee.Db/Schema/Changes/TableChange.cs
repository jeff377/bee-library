namespace Bee.Db.Schema.Changes
{
    /// <summary>
    /// Base type for a single structural change detected by <see cref="TableSchemaComparer.CompareToDiff"/>.
    /// Provider-agnostic; the execution kind (ALTER vs rebuild) is resolved later by the orchestrator.
    /// </summary>
    public abstract class TableChange
    {
        /// <summary>
        /// Returns a short human-readable description of this change for logging and warnings.
        /// </summary>
        public abstract string Describe();
    }
}
