using Bee.Definition.Database;
using Bee.Base;
using Bee.Db.Schema.Changes;
using Bee.Definition;

namespace Bee.Db.Schema
{
    /// <summary>
    /// Compares a defined table schema against the actual database table schema.
    /// </summary>
    public class TableSchemaComparer
    {
        /// <summary>
        /// Initializes a new instance of <see cref="TableSchemaComparer"/>.
        /// </summary>
        /// <param name="defineTable">The defined table schema.</param>
        /// <param name="realTable">The actual table schema from the database.</param>
        public TableSchemaComparer(TableSchema defineTable, TableSchema? realTable)
        {
            DefineTable = defineTable;
            RealTable = realTable;
        }

        /// <summary>
        /// Gets the defined table schema.
        /// </summary>
        public TableSchema DefineTable { get; }

        /// <summary>
        /// Gets the actual table schema from the database.
        /// </summary>
        public TableSchema? RealTable { get; }

        /// <summary>
        /// Gets the description drift between the defined and the actual schema.
        /// Populated by <see cref="Compare"/>; independent of <see cref="DbUpgradeAction"/>.
        /// </summary>
        public List<DescriptionChange> DescriptionChanges { get; } = [];

        /// <summary>
        /// Executes the comparison and returns the resulting table schema with upgrade actions set.
        /// </summary>
        public TableSchema Compare()
        {
            // Create a clone of the defined table schema to use as the comparison result
            var compareTable = this.DefineTable.Clone();
            // No actual table exists; mark the entire table as new
            if (this.RealTable == null)
            {
                compareTable.UpgradeAction = DbUpgradeAction.New;
                return compareTable;
            }
            // Compare field definitions
            if (!CompareFields(compareTable))
                compareTable.UpgradeAction = DbUpgradeAction.Upgrade;
            // Compare indexes
            if (!CompareIndexes(compareTable))
                compareTable.UpgradeAction = DbUpgradeAction.Upgrade;
            // Append extra fields from the actual table
            if (compareTable.UpgradeAction != DbUpgradeAction.None)
                AddExtensionFields(compareTable);
            // Detect description drift; does not affect UpgradeAction
            CompareDescriptions();
            return compareTable;
        }

        /// <summary>
        /// Detects description drift between the defined and actual schema and populates <see cref="DescriptionChanges"/>.
        /// Empty-define / non-empty-real is treated as no drift (conservative policy; avoids accidental removal).
        /// </summary>
        private void CompareDescriptions()
        {
            PopulateDescriptionChanges(this.DescriptionChanges);
        }

        /// <summary>
        /// Populates the given list with description drift entries between the defined and actual schema.
        /// Empty-define / non-empty-real is treated as no drift (conservative policy; avoids accidental removal).
        /// </summary>
        /// <param name="target">The list to populate.</param>
        private void PopulateDescriptionChanges(List<DescriptionChange> target)
        {
            var real = this.RealTable!;
            // Table-level: DisplayName
            AddDescriptionDrift(target, DescriptionLevel.Table, string.Empty,
                this.DefineTable.DisplayName, real.DisplayName);
            // Column-level: Caption — only when the column exists in both schemas
            foreach (DbField defineField in this.DefineTable.Fields!)
            {
                if (!real.Fields!.Contains(defineField.FieldName))
                    continue;
                var realField = real.Fields[defineField.FieldName];
                AddDescriptionDrift(target, DescriptionLevel.Column, defineField.FieldName,
                    defineField.Caption, realField.Caption);
            }
        }

        /// <summary>
        /// Adds a <see cref="DescriptionChange"/> to the target list if the defined value differs from the real value,
        /// following the conservative policy (empty define → no drift).
        /// </summary>
        private static void AddDescriptionDrift(List<DescriptionChange> target, DescriptionLevel level, string fieldName, string defineValue, string realValue)
        {
            // Conservative: empty define is treated as "not specified", do not remove existing DB description
            if (StrFunc.IsEmpty(defineValue))
                return;
            // No drift when values match
            if (StrFunc.IsEquals(defineValue, realValue))
                return;
            target.Add(new DescriptionChange
            {
                Level = level,
                FieldName = fieldName,
                NewValue = defineValue,
                IsNew = StrFunc.IsEmpty(realValue),
            });
        }

        /// <summary>
        /// Compares field definitions between the defined and actual table schemas.
        /// </summary>
        /// <param name="compareTable">The table schema used as the comparison result.</param>
        private bool CompareFields(TableSchema compareTable)
        {
            bool isMatch = true;
            foreach (DbField field in compareTable.Fields!)
            {
                if (this.RealTable!.Fields!.Contains(field.FieldName))
                {
                    if (!field.Compare(this.RealTable.Fields[field.FieldName]))
                    {
                        // Field exists but differs; mark as upgrade
                        field.UpgradeAction = DbUpgradeAction.Upgrade;
                        isMatch = false;
                    }
                }
                else
                {
                    // Field does not exist; mark as new
                    field.UpgradeAction = DbUpgradeAction.New;
                    isMatch = false;
                }
            }
            return isMatch;
        }

        /// <summary>
        /// Compares indexes between the defined and actual table schemas.
        /// </summary>
        /// <param name="compareTable">The table schema used as the comparison result.</param>
        private bool CompareIndexes(TableSchema compareTable)
        {
            // Return false immediately if any index does not match
            foreach (TableSchemaIndex index in compareTable.Indexes!)
            {
                string name = StrFunc.Format(index.Name, compareTable.TableName);
                if (this.RealTable!.Indexes!.Contains(name))
                {
                    if (!index.Compare(this.RealTable.Indexes[name]))
                    {
                        // Index exists but differs; mark as upgrade
                        index.UpgradeAction = DbUpgradeAction.Upgrade;
                        return false;
                    }
                }
                else
                {
                    // Index does not exist; mark as new
                    index.UpgradeAction = DbUpgradeAction.New;
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Appends extra fields from the actual table that are not present in the defined schema.
        /// </summary>
        /// <param name="compareTable">The table schema used as the comparison result.</param>
        private void AddExtensionFields(TableSchema compareTable)
        {
            foreach (var field in this.RealTable!.Fields!.Where(f => !compareTable.Fields!.Contains(f.FieldName)))
            {
                compareTable.Fields!.Add(field.Clone());
            }
        }

        /// <summary>
        /// Produces a structured diff describing the differences between the defined schema and the actual database schema.
        /// Unlike <see cref="Compare"/>, this does not mutate <see cref="DbUpgradeAction"/> on the cloned schema;
        /// instead, each difference is represented as a <see cref="TableChange"/> record.
        /// Fields and indexes present only in the actual database (not in the defined schema) are preserved and produce no change entries.
        /// </summary>
        public TableSchemaDiff CompareToDiff()
        {
            var diff = new TableSchemaDiff(this.DefineTable, this.RealTable);
            // New-table path: caller inspects IsNewTable; no changes are emitted
            if (this.RealTable == null)
                return diff;
            // Field-level changes
            CollectFieldChanges(diff);
            // Index-level changes
            CollectIndexChanges(diff);
            // Description drift (table DisplayName and column Caption)
            PopulateDescriptionChanges(diff.DescriptionChanges);
            return diff;
        }

        /// <summary>
        /// Collects field-level changes into the given diff. Fields present only in the actual database are preserved.
        /// </summary>
        /// <param name="diff">The diff to populate.</param>
        private void CollectFieldChanges(TableSchemaDiff diff)
        {
            foreach (DbField defineField in this.DefineTable.Fields!)
            {
                if (this.RealTable!.Fields!.Contains(defineField.FieldName))
                {
                    var realField = this.RealTable.Fields[defineField.FieldName];
                    if (!defineField.Compare(realField))
                        diff.Changes.Add(new AlterFieldChange(realField.Clone(), defineField.Clone()));
                }
                else
                {
                    diff.Changes.Add(new AddFieldChange(defineField.Clone()));
                }
            }
        }

        /// <summary>
        /// Collects index-level changes into the given diff. Indexes present only in the actual database are preserved.
        /// When a defined index differs from its database counterpart, a <see cref="DropIndexChange"/> and
        /// a corresponding <see cref="AddIndexChange"/> are both emitted so the orchestrator can drop-then-recreate.
        /// </summary>
        /// <param name="diff">The diff to populate.</param>
        private void CollectIndexChanges(TableSchemaDiff diff)
        {
            foreach (TableSchemaIndex defineIndex in this.DefineTable.Indexes!)
            {
                string formattedName = StrFunc.Format(defineIndex.Name, this.DefineTable.TableName);
                if (this.RealTable!.Indexes!.Contains(formattedName))
                {
                    var realIndex = this.RealTable.Indexes[formattedName];
                    if (!defineIndex.Compare(realIndex))
                    {
                        diff.Changes.Add(new DropIndexChange(realIndex.Clone()));
                        diff.Changes.Add(new AddIndexChange(defineIndex.Clone()));
                    }
                }
                else
                {
                    diff.Changes.Add(new AddIndexChange(defineIndex.Clone()));
                }
            }
        }
    }
}
