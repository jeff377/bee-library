using MessagePack.Formatters;

namespace Bee.Api.Core.MessagePack
{
    internal static partial class WireContracts
    {
        /// <summary>
        /// Registers the DataTable / DataSet transport wire contracts.
        /// </summary>
        private static void AddDataTypes(List<IMessagePackFormatter> list)
        {
            list.Add(WireContract.For<Bee.Api.Core.MessagePack.SerializableDataColumn>()
                .Member(nameof(Bee.Api.Core.MessagePack.SerializableDataColumn.ColumnName), static x => x.ColumnName, static (x, v) => x.ColumnName = v)
                .Member(nameof(Bee.Api.Core.MessagePack.SerializableDataColumn.DataType), static x => x.DataType, static (x, v) => x.DataType = v)
                .Member(nameof(Bee.Api.Core.MessagePack.SerializableDataColumn.DisplayName), static x => x.DisplayName, static (x, v) => x.DisplayName = v)
                .Member(nameof(Bee.Api.Core.MessagePack.SerializableDataColumn.AllowDBNull), static x => x.AllowDBNull, static (x, v) => x.AllowDBNull = v)
                .Member(nameof(Bee.Api.Core.MessagePack.SerializableDataColumn.ReadOnly), static x => x.ReadOnly, static (x, v) => x.ReadOnly = v)
                .Member(nameof(Bee.Api.Core.MessagePack.SerializableDataColumn.MaxLength), static x => x.MaxLength, static (x, v) => x.MaxLength = v)
                .Member(nameof(Bee.Api.Core.MessagePack.SerializableDataColumn.DefaultValue), static x => x.DefaultValue, static (x, v) => x.DefaultValue = v)
                .Build());
            list.Add(WireContract.For<Bee.Api.Core.MessagePack.SerializableDataRelation>()
                .Member(nameof(Bee.Api.Core.MessagePack.SerializableDataRelation.RelationName), static x => x.RelationName, static (x, v) => x.RelationName = v)
                .Member(nameof(Bee.Api.Core.MessagePack.SerializableDataRelation.ParentTable), static x => x.ParentTable, static (x, v) => x.ParentTable = v)
                .Member(nameof(Bee.Api.Core.MessagePack.SerializableDataRelation.ChildTable), static x => x.ChildTable, static (x, v) => x.ChildTable = v)
                .Member(nameof(Bee.Api.Core.MessagePack.SerializableDataRelation.ParentColumns), static x => x.ParentColumns, static (x, v) => x.ParentColumns = v)
                .Member(nameof(Bee.Api.Core.MessagePack.SerializableDataRelation.ChildColumns), static x => x.ChildColumns, static (x, v) => x.ChildColumns = v)
                .Build());
            list.Add(WireContract.For<Bee.Api.Core.MessagePack.SerializableDataRow>()
                .Member(nameof(Bee.Api.Core.MessagePack.SerializableDataRow.CurrentValues), static x => x.CurrentValues, static (x, v) => x.CurrentValues = v)
                .Member(nameof(Bee.Api.Core.MessagePack.SerializableDataRow.OriginalValues), static x => x.OriginalValues, static (x, v) => x.OriginalValues = v)
                .Member(nameof(Bee.Api.Core.MessagePack.SerializableDataRow.RowState), static x => x.RowState, static (x, v) => x.RowState = v)
                .Build());
            list.Add(WireContract.For<Bee.Api.Core.MessagePack.SerializableDataSet>()
                .Member(nameof(Bee.Api.Core.MessagePack.SerializableDataSet.DataSetName), static x => x.DataSetName, static (x, v) => x.DataSetName = v)
                .Member(nameof(Bee.Api.Core.MessagePack.SerializableDataSet.Tables), static x => x.Tables, static (x, v) => x.Tables = v)
                .Member(nameof(Bee.Api.Core.MessagePack.SerializableDataSet.Relations), static x => x.Relations, static (x, v) => x.Relations = v)
                .Build());
            list.Add(WireContract.For<Bee.Api.Core.MessagePack.SerializableDataTable>()
                .Member(nameof(Bee.Api.Core.MessagePack.SerializableDataTable.TableName), static x => x.TableName, static (x, v) => x.TableName = v)
                .Member(nameof(Bee.Api.Core.MessagePack.SerializableDataTable.Columns), static x => x.Columns, static (x, v) => x.Columns = v)
                .Member(nameof(Bee.Api.Core.MessagePack.SerializableDataTable.Rows), static x => x.Rows, static (x, v) => x.Rows = v)
                .Member(nameof(Bee.Api.Core.MessagePack.SerializableDataTable.PrimaryKeys), static x => x.PrimaryKeys, static (x, v) => x.PrimaryKeys = v)
                .Build());
        }
    }
}
