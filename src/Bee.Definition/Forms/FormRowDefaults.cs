using System.Data;
using Bee.Base.Data;
using Bee.Definition.Database;

namespace Bee.Definition.Forms
{
    /// <summary>
    /// Seeds a freshly created <see cref="DataRow"/> with schema-driven non-null defaults so it
    /// never reaches the database with a NULL that would violate a NOT NULL constraint. Shared by
    /// the server (the <c>GetNewData</c> master row) and the client (newly added detail rows) so
    /// both produce identically initialized rows from a single place.
    /// </summary>
    public static class FormRowDefaults
    {
        /// <summary>
        /// Applies non-null defaults to a new row from its <paramref name="formTable"/> metadata:
        /// <c>sys_rowid</c> always gets a fresh <see cref="Guid"/>; <c>sys_master_rowid</c> gets
        /// <paramref name="masterRowId"/> when supplied (the master link); every other persisted
        /// column keeps any value it already carries and is otherwise filled per
        /// <see cref="FieldDbType"/> (text → empty string, numeric → 0, Date → today, DateTime → now,
        /// Guid → empty, …). Relation, virtual, and auto-increment fields are skipped.
        /// </summary>
        /// <param name="formTable">The form table whose fields describe the row's columns.</param>
        /// <param name="row">The freshly created row to seed.</param>
        /// <param name="masterRowId">
        /// The owning master row's <c>sys_rowid</c> value for a detail row's <c>sys_master_rowid</c>.
        /// Passed as the master row's raw value (not a re-parsed <see cref="Guid"/>) so its exact
        /// representation — including the string casing some providers store GUIDs in — is preserved;
        /// a mismatched case would orphan the detail under a case-sensitive key comparison.
        /// </param>
        public static void Apply(FormTable formTable, DataRow row, object? masterRowId = null)
        {
            ArgumentNullException.ThrowIfNull(row);
            if (formTable?.Fields is null) { return; }

            foreach (FormField field in formTable.Fields)
            {
                // Persisted columns only: the database generates AutoIncrement, and
                // relation / virtual fields are never stored.
                if (field.Type != FieldType.DbField || field.DbType == FieldDbType.AutoIncrement) { continue; }
                if (!row.Table.Columns.Contains(field.FieldName)) { continue; }

                if (string.Equals(field.FieldName, SysFields.RowId, StringComparison.OrdinalIgnoreCase))
                {
                    row[field.FieldName] = Guid.NewGuid();
                    continue;
                }
                if (masterRowId is not null && masterRowId != DBNull.Value
                    && string.Equals(field.FieldName, SysFields.MasterRowId, StringComparison.OrdinalIgnoreCase))
                {
                    row[field.FieldName] = masterRowId;
                    continue;
                }
                if (row[field.FieldName] != DBNull.Value) { continue; }

                var value = DefaultForDbType(field.DbType);
                if (value != DBNull.Value) { row[field.FieldName] = value; }
            }
        }

        /// <summary>
        /// The type-appropriate non-null default for a <see cref="FieldDbType"/>, or
        /// <see cref="DBNull.Value"/> for types with no natural empty value.
        /// </summary>
        /// <param name="dbType">The field database type.</param>
        public static object DefaultForDbType(FieldDbType dbType) => dbType switch
        {
            FieldDbType.String or FieldDbType.Text => string.Empty,
            FieldDbType.Boolean => false,
            FieldDbType.Short => (short)0,
            FieldDbType.Integer => 0,
            FieldDbType.Long => 0L,
            FieldDbType.Decimal or FieldDbType.Currency => 0m,
            FieldDbType.Date => DateTime.Today,
            FieldDbType.DateTime => DateTime.Now,
            FieldDbType.Guid => Guid.Empty,
            FieldDbType.Binary => Array.Empty<byte>(),
            _ => DBNull.Value,
        };
    }
}
