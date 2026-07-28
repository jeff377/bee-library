using Bee.Base.Data;
using Bee.Db.Schema;

namespace Bee.Db.Providers.Sqlite
{
    /// <summary>
    /// The one place where SQLite diverges from the shared
    /// <see cref="AlterCompatibilityRules"/>: no column can be altered in place, so every type
    /// change resolves to a rebuild.
    /// </summary>
    /// <remarks>
    /// SQLite's <c>ALTER TABLE</c> only supports <c>ADD COLUMN</c>, <c>RENAME COLUMN</c>
    /// (3.25+) and <c>DROP COLUMN</c> (3.35+); changing a column's type, nullability, default,
    /// or primary-key membership is not supported. The narrowing hint has no such restriction
    /// and comes from <see cref="AlterCompatibilityRules.IsNarrowing"/> unchanged, so callers
    /// see consistent narrowing semantics across providers even though the upgrade always
    /// falls back to rebuild.
    /// </remarks>
    internal static class SqliteAlterCompatibilityRules
    {
        /// <summary>
        /// Returns <see cref="ChangeExecutionKind.Rebuild"/> for any known type change and
        /// <see cref="ChangeExecutionKind.NotSupported"/> for unknown types.
        /// </summary>
        /// <param name="from">The source type (current DB column type).</param>
        /// <param name="to">The target type (defined column type).</param>
        public static ChangeExecutionKind GetKindForTypeChange(FieldDbType from, FieldDbType to)
        {
            if (from == FieldDbType.Unknown || to == FieldDbType.Unknown)
                return ChangeExecutionKind.NotSupported;
            return ChangeExecutionKind.Rebuild;
        }
    }
}
