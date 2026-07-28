using Bee.Definition.Database;

namespace Bee.Db.Schema
{
    /// <summary>
    /// Dialect-neutral schema shaping shared by every provider's table-rebuild command builder:
    /// deriving the effective rebuild schema from a diff, cloning it under the temporary table
    /// name, and stripping secondary indexes.
    /// </summary>
    /// <remarks>
    /// The rebuild <em>script</em> is dialect-specific (drop / create / copy / rename all differ),
    /// but the schema objects fed into it are not. Extracted from five identical per-provider
    /// copies so a change to the extension-field policy applies to all providers at once.
    /// </remarks>
    internal static class RebuildSchemaFactory
    {
        /// <summary>
        /// Builds the effective rebuild schema: the defined table plus any real-only fields appended
        /// (extension field policy — columns present in the database but absent from the definition
        /// are preserved through the rebuild).
        /// </summary>
        /// <param name="diff">The schema diff being rebuilt.</param>
        public static TableSchema BuildEffectiveSchema(TableSchemaDiff diff)
        {
            var cloned = diff.DefineTable.Clone();
            if (diff.RealTable != null)
            {
                foreach (var realField in diff.RealTable.Fields!.Where(f => !cloned.Fields!.Contains(f.FieldName)))
                    cloned.Fields!.Add(realField.Clone());
            }
            return cloned;
        }

        /// <summary>
        /// Clones <paramref name="schema"/> under a different table name, used to create the
        /// temporary rebuild table.
        /// </summary>
        /// <param name="schema">The source schema.</param>
        /// <param name="tableName">The table name the clone should carry.</param>
        public static TableSchema CloneWithTableName(TableSchema schema, string tableName)
        {
            var tmpSchema = schema.Clone();
            tmpSchema.TableName = tableName;
            // DisplayName is carried over so the tmp table gets the same extended property;
            // after rename the properties remain attached to the (renamed) object.
            tmpSchema.DisplayName = schema.DisplayName;
            return tmpSchema;
        }

        /// <summary>
        /// Removes all non-primary-key indexes from the schema. Used when generating the
        /// temp table so secondary indexes can later be created with their real names.
        /// </summary>
        /// <param name="schema">The schema to strip in place.</param>
        public static void StripNonPrimaryKeyIndexes(TableSchema schema)
        {
            var toRemove = schema.Indexes!.Where(i => !i.PrimaryKey).ToList();
            foreach (var index in toRemove)
                schema.Indexes!.Remove(index);
        }
    }
}
