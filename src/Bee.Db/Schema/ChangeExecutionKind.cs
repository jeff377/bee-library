namespace Bee.Db.Schema
{
    /// <summary>
    /// Indicates how a <see cref="Changes.TableChange"/> can be executed by a provider.
    /// </summary>
    public enum ChangeExecutionKind
    {
        /// <summary>
        /// Executable via an in-place ALTER operation.
        /// </summary>
        Alter,

        /// <summary>
        /// Cannot be applied via ALTER; the whole table must be rebuilt.
        /// </summary>
        Rebuild,

        /// <summary>
        /// Cannot be applied by the current provider.
        /// </summary>
        NotSupported,
    }
}
