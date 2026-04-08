using System.Data;

namespace Bee.Core.Data
{
    /// <summary>
    /// Extension methods for <see cref="DataSet"/>.
    /// </summary>
    public static class DataSetExtensions
    {
        /// <summary>
        /// Gets the master table from the dataset.
        /// </summary>
        /// <param name="dataSet">The dataset.</param>
        public static DataTable GetMasterTable(this DataSet dataSet)
        {
            if (dataSet == null) { return null; }
            if (StrFunc.IsEmpty(dataSet.DataSetName)) { return null; }
            if (!dataSet.Tables.Contains(dataSet.DataSetName)) { return null; }
            // The master table is the one whose TableName equals the DataSetName
            return dataSet.Tables[dataSet.DataSetName];
        }

        /// <summary>
        /// Gets the first row of the master table.
        /// </summary>
        /// <param name="dataSet">The dataset.</param>
        public static DataRow GetMasterRow(this DataSet dataSet)
        {
            var table = GetMasterTable(dataSet);
            if (table.IsEmpty()) { return null; }
            return table.Rows[0];
        }

        /// <summary>
        /// Determines whether the dataset contains no data.
        /// </summary>
        /// <param name="dataSet">The dataset to check.</param>
        public static bool IsEmpty(this DataSet dataSet)
        {
            // A null dataset or one with no tables is considered empty
            if (dataSet == null || (dataSet.Tables.Count == 0)) { return true; }
            // Also considered empty if the master table has no rows
            var table = GetMasterTable(dataSet);
            return table.IsEmpty();
        }
    }
}
