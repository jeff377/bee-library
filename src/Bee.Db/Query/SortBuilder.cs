using System;
using System.Collections.Generic;
using Bee.Base;
using Bee.Definition;

using Bee.Db;

namespace Bee.Db.Query
{
    /// <summary>
    /// Builds the SQL ORDER BY clause.
    /// </summary>
    public sealed class SortBuilder : ISortBuilder
    {
        private readonly DatabaseType _databaseType;

        /// <summary>
        /// Initializes a new instance of <see cref="SortBuilder"/>.
        /// </summary>
        /// <param name="databaseType">The database type.</param>
        public SortBuilder(DatabaseType databaseType)
        {
            _databaseType = databaseType;
        }

        /// <summary>
        /// Builds the SQL ORDER BY clause (including the keyword prefix) from the specified sort fields.
        /// </summary>
        /// <param name="sortFields">The collection of sort fields.</param>
        /// <param name="selectContext">The field source mappings and table JOIN relationships for the query.</param>
        public string Build(SortFieldCollection? sortFields, SelectContext? selectContext)
        {
            if (sortFields == null || BaseFunc.IsEmpty(sortFields)) { return string.Empty; }

            var mappedSortFields = (selectContext != null)
                   ? RemapSortFields(sortFields, selectContext)
                   : sortFields;

            var parts = new List<string>(mappedSortFields.Count);
            for (int i = 0; i < mappedSortFields.Count; i++)
            {
                var item = mappedSortFields[i];
                var dir = (item.Direction == SortDirection.Desc) ? "DESC" : "ASC";
                parts.Add(item.FieldName + " " + dir);
            }

            return "ORDER BY " + string.Join(", ", parts);
        }

        /// <summary>
        /// Creates a copy of the <see cref="SortFieldCollection"/> with field names remapped to their correct SQL expressions based on the query field sources.
        /// </summary>
        /// <param name="sortFields">The original sort field collection.</param>
        /// <param name="selectContext">The field source mappings and table JOIN relationships for the query.</param>
        private SortFieldCollection RemapSortFields(SortFieldCollection sortFields, SelectContext selectContext)
        {
            var result = new SortFieldCollection();
            foreach (var sortField in sortFields)
            {
                var mapping = selectContext.FieldMappings.GetOrDefault(sortField.FieldName);
                string fieldExpr;
                if (mapping != null)
                {
                    fieldExpr = $"{mapping.SourceAlias}.{QuoteIdentifier(mapping.SourceField)}";
                }
                else
                {
                    fieldExpr = $"A.{QuoteIdentifier(sortField.FieldName)}";
                }
                result.Add(new SortField(fieldExpr, sortField.Direction));
            }
            return result;
        }

        private string QuoteIdentifier(string identifier)
        {
            return DbFunc.QuoteIdentifier(_databaseType, identifier);
        }
    }
}
