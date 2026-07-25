using System.Data;
using Bee.Base.Data;

namespace Bee.Definition.Forms
{
    /// <summary>
    /// Extension methods for <see cref="FormTable"/>.
    /// </summary>
    public static class FormTableExtensions
    {
        /// <summary>
        /// Records each field's declared <see cref="FieldDbType"/> on the matching column of a table
        /// read back from the database.
        /// </summary>
        /// <param name="formTable">The form table describing the result shape.</param>
        /// <param name="table">The table just read from the database.</param>
        /// <remarks>
        /// ADO.NET decides column types from what the provider reports, and a provider cannot express
        /// the distinction the definition layer makes between <see cref="FieldDbType.Date"/> and
        /// <see cref="FieldDbType.DateTime"/> — both arrive as <see cref="DateTime"/>. Replaying the
        /// schema over the result restores it, so the table describes itself the same way whether it
        /// was built from the schema or read from SQL.
        /// <para>
        /// Columns absent from the schema are left alone: a query may legitimately return more than the
        /// declared fields, and an unmarked column still falls back to inferring its type from the CLR
        /// type. Hand-written SQL has no schema to replay and declares its calendar-day columns through
        /// <c>DataTableExtensions.SetDateColumns</c> instead.
        /// </para>
        /// </remarks>
        public static void ApplyFieldDbTypes(this FormTable formTable, DataTable table)
        {
            ArgumentNullException.ThrowIfNull(formTable);
            ArgumentNullException.ThrowIfNull(table);

            if (formTable.Fields == null) { return; }

            foreach (FormField field in formTable.Fields)
            {
                var column = table.Columns[field.FieldName];
                column?.ApplyFieldDbType(field.DbType);
            }
        }
    }
}
