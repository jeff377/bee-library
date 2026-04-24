using Bee.Definition.Database;
using Bee.Base;
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
            var real = this.RealTable!;
            // Table-level: DisplayName
            AddDescriptionDrift(DescriptionLevel.Table, string.Empty,
                this.DefineTable.DisplayName, real.DisplayName);
            // Column-level: Caption — only when the column exists in both schemas
            foreach (DbField defineField in this.DefineTable.Fields!)
            {
                if (!real.Fields!.Contains(defineField.FieldName))
                    continue;
                var realField = real.Fields[defineField.FieldName];
                AddDescriptionDrift(DescriptionLevel.Column, defineField.FieldName,
                    defineField.Caption, realField.Caption);
            }
        }

        /// <summary>
        /// Adds a <see cref="DescriptionChange"/> if the defined value differs from the real value,
        /// following the conservative policy (empty define → no drift).
        /// </summary>
        private void AddDescriptionDrift(DescriptionLevel level, string fieldName, string defineValue, string realValue)
        {
            // Conservative: empty define is treated as "not specified", do not remove existing DB description
            if (StrFunc.IsEmpty(defineValue))
                return;
            // No drift when values match
            if (StrFunc.IsEquals(defineValue, realValue))
                return;
            DescriptionChanges.Add(new DescriptionChange
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
    }
}
