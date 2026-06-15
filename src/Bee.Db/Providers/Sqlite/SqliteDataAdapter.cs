using System.Data;
using System.Data.Common;

namespace Bee.Db.Providers.Sqlite
{
    /// <summary>
    /// A minimal <see cref="DbDataAdapter"/> for Microsoft.Data.Sqlite, which ships none of its
    /// own. The base class already implements <c>Fill</c> and <c>Update</c> (row-state dispatch and
    /// <c>SourceColumn</c> / <c>SourceVersion</c> parameter binding); this subclass only supplies
    /// the required row-update event plumbing, so the framework can drive SQLite reads and writes
    /// through the same adapter path as every other provider.
    /// </summary>
    public sealed class SqliteDataAdapter : DbDataAdapter
    {
        /// <summary>Raised before a row's command is executed during <c>Update</c>.</summary>
        public event EventHandler<RowUpdatingEventArgs>? RowUpdating;

        /// <summary>Raised after a row's command is executed during <c>Update</c>.</summary>
        public event EventHandler<RowUpdatedEventArgs>? RowUpdated;

        /// <inheritdoc/>
        protected override RowUpdatingEventArgs CreateRowUpdatingEvent(
            DataRow dataRow, IDbCommand? command, StatementType statementType, DataTableMapping tableMapping)
            => new(dataRow, command, statementType, tableMapping);

        /// <inheritdoc/>
        protected override RowUpdatedEventArgs CreateRowUpdatedEvent(
            DataRow dataRow, IDbCommand? command, StatementType statementType, DataTableMapping tableMapping)
            => new(dataRow, command, statementType, tableMapping);

        /// <inheritdoc/>
        protected override void OnRowUpdating(RowUpdatingEventArgs value) => RowUpdating?.Invoke(this, value);

        /// <inheritdoc/>
        protected override void OnRowUpdated(RowUpdatedEventArgs value) => RowUpdated?.Invoke(this, value);
    }
}
