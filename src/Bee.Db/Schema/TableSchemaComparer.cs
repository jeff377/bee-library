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
        public TableSchemaComparer(TableSchema defineTable, TableSchema realTable)
        {
            DefineTable = defineTable;
            RealTable = realTable;
        }

        /// <summary>
        /// Gets the defined table schema.
        /// </summary>
        public TableSchema DefineTable { get; } = null;

        /// <summary>
        /// Gets the actual table schema from the database.
        /// </summary>
        public TableSchema RealTable { get; } = null;

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
            return compareTable;
        }

        /// <summary>
        /// Compares field definitions between the defined and actual table schemas.
        /// </summary>
        /// <param name="compareTable">The table schema used as the comparison result.</param>
        private bool CompareFields(TableSchema compareTable)
        {
            bool isMatch = true;
            foreach (DbField field in compareTable.Fields)
            {
                if (this.RealTable.Fields.Contains(field.FieldName))
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
            foreach (TableSchemaIndex index in compareTable.Indexes)
            {
                string name = StrFunc.Format(index.Name, compareTable.TableName);
                if (this.RealTable.Indexes.Contains(name))
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
            foreach (DbField field in this.RealTable.Fields)
            {
                if (!compareTable.Fields.Contains(field.FieldName))
                    compareTable.Fields.Add(field.Clone());
            }
        }
    }
}
