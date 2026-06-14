using Bee.Definition.Forms;
using Bee.Base;
using Bee.Definition;
using Bee.Definition.Database;
using Bee.Definition.Storage;

namespace Bee.Db.Dml
{
    /// <summary>
    /// Builds a complete <see cref="SelectContext"/> based on the query requirements.
    /// Constructs the corresponding <see cref="QueryFieldMapping"/> and <see cref="TableJoin"/> collections
    /// from the specified query fields, conditions, sort orders, and other criteria.
    /// </summary>
    public class SelectContextBuilder
    {
        private readonly FormTable _formTable;
        private readonly HashSet<string> _usedFieldNames;
        private readonly IDefineAccess _defineAccess;
        private string _currentTableAlias = "A";  // Current table alias in use

        /// <summary>
        /// Initializes a new instance of <see cref="SelectContextBuilder"/>.
        /// </summary>
        /// <param name="formTable">The form table.</param>
        /// <param name="usedFieldNames">The set of field names used by the query.</param>
        /// <param name="defineAccess">The define access service used to resolve relation-form schemas.</param>
        public SelectContextBuilder(FormTable formTable, HashSet<string> usedFieldNames, IDefineAccess defineAccess)
        {
            _formTable = formTable;
            _usedFieldNames = usedFieldNames;
            _defineAccess = defineAccess ?? throw new ArgumentNullException(nameof(defineAccess));
        }

        /// <summary>
        /// Builds and returns the <see cref="SelectContext"/>.
        /// </summary>
        public SelectContext Build()
        {
            var context = new SelectContext();

            // The main table uses alias "A". Relation joins get fresh right-side aliases
            // (B, C, …) from GetActiveTableAlias, which advances _currentTableAlias as a
            // side effect. Each TOP-LEVEL relation joins off the main table, so its left
            // alias is always the main alias — passing the mutating _currentTableAlias
            // instead made the 2nd relation join off the 1st relation's table (producing
            // e.g. "B.category_rowid", a column that does not exist on the supplier join).
            const string mainAlias = "A";
            _currentTableAlias = mainAlias;

            // For foreign key fields, build JOIN relationships between tables
            foreach (var field in _formTable.Fields!)
            {
                // Skip non-foreign-key fields
                if (field.Type != FieldType.DbField || StringUtilities.IsEmpty(field.RelationProgId)) { continue; }

                // Retrieve the referenced field mappings resolved through this foreign key field
                var fieldMappings = GetUsedRelationFieldMappings(field);
                if (ValueUtilities.IsEmpty(fieldMappings)) { continue; }

                // Use "<MainTable>.<FieldName>.<SourceProgId>" as the unique JOIN key
                string key = $"{_formTable.TableName}.{field.FieldName}.{field.RelationProgId}";
                AddTableJoin(context, key, field, fieldMappings, _formTable.DbTableName, mainAlias);
            }
            return context;
        }

        /// <summary>
        /// Adds the JOIN relationship between two tables to the <see cref="SelectContext"/> based on a foreign key field.
        /// </summary>
        /// <param name="context">The <see cref="SelectContext"/> instance.</param>
        /// <param name="key">The unique key identifying the JOIN relationship.</param>
        /// <param name="foreignKeyField">The foreign key field.</param>
        /// <param name="fieldMappings">The referenced field mappings resolved through the foreign key field.</param>
        /// <param name="leftTable">The left-side table name.</param>
        /// <param name="leftAlias">The left-side table alias.</param>
        /// <param name="queryFieldName">The field name to use when creating a <see cref="QueryFieldMapping"/>; required when handling nested relations recursively.</param>
        private void AddTableJoin(SelectContext context, string key, FormField foreignKeyField, FieldMappingCollection fieldMappings,
            string leftTable, string leftAlias, string queryFieldName = "")
        {
            var srcFormDefine = _defineAccess.GetFormSchema(foreignKeyField.RelationProgId);
            if (srcFormDefine == null)
            {
                throw new InvalidOperationException(
                    $"Form definition '{foreignKeyField.RelationProgId}' not found for field '{foreignKeyField.FieldName}'.");
            }
            var srcTable = srcFormDefine.MasterTable!;

            // Create the JOIN entry if it does not already exist
            var join = context.Joins.GetOrDefault(key);
            if (join == null)
            {
                join = new TableJoin()
                {
                    Key = key,
                    LeftTable = leftTable,
                    LeftAlias = leftAlias,
                    LeftField = foreignKeyField.FieldName,
                    RightTable = srcTable.DbTableName,
                    RightAlias = GetActiveTableAlias(),
                    RightField = SysFields.RowId
                };
                context.Joins.Add(join);
            }

            foreach (var mapping in fieldMappings)
            {
                var srcField = srcTable.Fields!.GetOrDefault(mapping.SourceField);
                if (srcField == null)
                {
                    throw new InvalidOperationException(
                        $"Source field '{mapping.SourceField}' not found in table '{srcTable.TableName}' " +
                        $"for foreign key field '{foreignKeyField.FieldName}' in relation '{foreignKeyField.RelationProgId}'.");
                }

                if (srcField.Type == FieldType.RelationField)
                {
                    // If the source field is itself a relation field, recurse to handle nested relations
                    var reference = srcTable.RelationFieldReferences[srcField.FieldName];
                    string srcKey = key + "." + reference.ForeignKeyField.RelationProgId;
                    var srcMappings = GetSingleRelationFieldMappings(reference.ForeignKeyField, reference.FieldName);
                    AddTableJoin(context, srcKey, reference.ForeignKeyField, srcMappings, join.RightTable, join.RightAlias, mapping.DestinationField);
                }
                else
                {
                    var fieldMapping = new QueryFieldMapping()
                    {
                        FieldName = StringUtilities.IsEmpty(queryFieldName) ? mapping.DestinationField : queryFieldName,
                        SourceAlias = join.RightAlias,
                        SourceField = srcField.FieldName,
                        TableJoin = join
                    };
                    context.FieldMappings.Add(fieldMapping);
                }
            }
        }

        /// <summary>
        /// A set of reserved SQL keywords used to avoid alias collisions.
        /// </summary>
        private static readonly HashSet<string> SqlKeywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "AS", "BY", "IF", "IN", "IS", "OF", "OR", "TO", "ON",
            "GO", "NO", "DO", "AT", "IT"
        };

        /// <summary>
        /// Returns the next available table alias after the specified one.
        /// </summary>
        /// <param name="tableAlias">The current table alias.</param>
        private static string GetNextTableAlias(string tableAlias)
        {
            // Uses base-26 progression: A → B → C ... → Z → ZA → ZB ... (multi-character expansion)
            string baseValues = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            string nextAlias = StringUtilities.GetNextId(tableAlias, baseValues);
            // If the generated alias is a reserved SQL keyword, advance to the next one
            while (SqlKeywords.Contains(nextAlias))
            {
                nextAlias = StringUtilities.GetNextId(nextAlias, baseValues);
            }
            return nextAlias;
        }

        /// <summary>
        /// Advances to and returns the next active table alias.
        /// </summary>
        private string GetActiveTableAlias()
        {
            _currentTableAlias = GetNextTableAlias(_currentTableAlias);
            return _currentTableAlias;
        }

        /// <summary>
        /// For the specified foreign key field, returns a new field mapping collection containing only
        /// the relation fields that are present in <c>_usedFieldNames</c>.
        /// </summary>
        /// <param name="foreignKeyField">The foreign key field.</param>
        /// <returns>The matching field mapping collection.</returns>
        private FieldMappingCollection GetUsedRelationFieldMappings(FormField foreignKeyField)
        {
            var result = new FieldMappingCollection();
            if (foreignKeyField.RelationFieldMappings == null)
                return result;

            foreach (var mapping in foreignKeyField.RelationFieldMappings.Where(m => _usedFieldNames.Contains(m.DestinationField)))
            {
                result.Add(mapping.SourceField, mapping.DestinationField);
            }
            return result;
        }

        /// <summary>
        /// Returns a single-entry field mapping collection for the specified foreign key field and destination field.
        /// </summary>
        /// <param name="foreignKeyField">The foreign key field.</param>
        /// <param name="destinationField">The destination field name.</param>
        private static FieldMappingCollection GetSingleRelationFieldMappings(FormField foreignKeyField, string destinationField)
        {
            var fieldMapping = foreignKeyField.RelationFieldMappings!.FindByDestination(destinationField);
            var result = new FieldMappingCollection
            {
                { fieldMapping!.SourceField, fieldMapping.DestinationField }
            };
            return result;
        }
    }
}
