using System.Data;
using MessagePack;

namespace Bee.Api.Core.MessagePack
{
    /// <summary>
    /// Serializable DataSet object used to support cross-platform transmission and storage.
    /// </summary>
    [MessagePackObject]
    public class SerializableDataSet
    {
        /// <summary>
        /// Gets or sets the dataset name.
        /// </summary>
        [Key(0)]
        public string DataSetName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the collection of all tables.
        /// </summary>
        [Key(1)]
        public List<SerializableDataTable> Tables { get; set; }

        /// <summary>
        /// Gets or sets the collection of relation definitions between tables.
        /// </summary>
        [Key(2)]
        public List<SerializableDataRelation> Relations { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SerializableDataSet"/> class and initializes the collections.
        /// </summary>
        public SerializableDataSet()
        {
            Tables = new List<SerializableDataTable>();
            Relations = new List<SerializableDataRelation>();
        }

        /// <summary>
        /// Converts a standard DataSet to a serializable object.
        /// </summary>
        /// <param name="ds">The source DataSet.</param>
        /// <returns>The serializable dataset object.</returns>
        public static SerializableDataSet FromDataSet(DataSet ds)
        {
            var sds = new SerializableDataSet
            {
                DataSetName = ds.DataSetName
            };

            foreach (DataTable table in ds.Tables)
                sds.Tables.Add(SerializableDataTable.FromDataTable(table));

            foreach (DataRelation rel in ds.Relations)
            {
                sds.Relations.Add(new SerializableDataRelation
                {
                    RelationName = rel.RelationName,
                    ParentTable = rel.ParentTable.TableName,
                    ChildTable = rel.ChildTable.TableName,
                    ParentColumns = rel.ParentColumns.Select(c => c.ColumnName).ToList(),
                    ChildColumns = rel.ChildColumns.Select(c => c.ColumnName).ToList()
                });
            }

            return sds;
        }

        /// <summary>
        /// Restores a serializable dataset back to a standard DataSet.
        /// </summary>
        /// <param name="sds">The serializable dataset.</param>
        /// <returns>The restored standard DataSet.</returns>
        public static DataSet ToDataSet(SerializableDataSet sds)
        {
            var ds = new DataSet(sds.DataSetName);

            foreach (var table in sds.Tables)
                ds.Tables.Add(SerializableDataTable.ToDataTable(table));

            foreach (var rel in sds.Relations)
            {
                var parentCols = rel.ParentColumns.Select(c => ds.Tables[rel.ParentTable]!.Columns[c]!).ToArray();
                var childCols = rel.ChildColumns.Select(c => ds.Tables[rel.ChildTable]!.Columns[c]!).ToArray();
                ds.Relations.Add(new DataRelation(rel.RelationName, parentCols, childCols));
            }

            return ds;
        }
    }

}
