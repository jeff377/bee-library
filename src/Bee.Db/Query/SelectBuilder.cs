using Bee.Definition.Forms;
using Bee.Base;
using Bee.Base.Collections;
using Bee.Definition;
using System;
using System.Collections.Generic;

using Bee.Db;

namespace Bee.Db.Query
{
    /// <summary>
    /// Builds the SQL SELECT clause.
    /// </summary>
    public class SelectBuilder : ISelectBuilder
    {
        private readonly DatabaseType _databaseType;

        /// <summary>
        /// Initializes a new instance of <see cref="SelectBuilder"/>.
        /// </summary>
        /// <param name="databaseType">The database type.</param>
        public SelectBuilder(DatabaseType databaseType)
        {
            _databaseType = databaseType;
        }

        /// <summary>
        /// Builds the SELECT clause.
        /// </summary>
        /// <param name="formTable">The form table.</param>
        /// <param name="selectFields">A comma-separated string of field names to retrieve; an empty string retrieves all fields.</param>
        /// <param name="selectContext">The field source mappings and table JOIN relationships for the query.</param>
        public string Build(FormTable formTable, string selectFields, SelectContext selectContext)
        {
            var selectFieldNames = GetSelectFields(formTable, selectFields);
            var selectParts = new List<string>();
            foreach (var fieldName in selectFieldNames)
            {
                var field = formTable.Fields!.GetOrDefault(fieldName);
                if (field == null)
                    throw new InvalidOperationException($"Field '{fieldName}' does not exist in table '{formTable.TableName}'.");
                if (field.Type == FieldType.DbField)
                {
                    selectParts.Add($"    A.{QuoteIdentifier(fieldName)}");
                }
                else
                {
                    var mapping = selectContext.FieldMappings.GetOrDefault(fieldName);
                    if (mapping == null)
                        throw new InvalidOperationException($"Field mapping for '{fieldName}' is null.");
                    selectParts.Add($"    {mapping.SourceAlias}.{QuoteIdentifier(mapping.SourceField)} AS {QuoteIdentifier(fieldName)}");
                }
            }
            return "SELECT\n" + string.Join(",\n", selectParts);
        }

        /// <summary>
        /// Returns the set of field names to include in the SELECT clause.
        /// </summary>
        /// <param name="formTable">The form table.</param>
        /// <param name="selectFields">A comma-separated string of field names to retrieve; an empty string retrieves all fields.</param>
        private static StringHashSet GetSelectFields(FormTable formTable, string selectFields)
        {
            var set = new StringHashSet();
            if (string.IsNullOrWhiteSpace(selectFields))
            {
                // Retrieve all fields
                foreach (var field in formTable.Fields!)
                {
                    set.Add(field.FieldName);
                }
            }
            else
            {
                // Retrieve only the specified fields
                set.Add(selectFields, ",");
            }
            return set;
        }

        private string QuoteIdentifier(string identifier)
        {
            return DbFunc.QuoteIdentifier(_databaseType, identifier);
        }
    }
}
