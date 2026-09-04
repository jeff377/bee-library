using MessagePack.Formatters;

namespace Bee.Api.Core.MessagePack
{
    internal static partial class WireContracts
    {
        /// <summary>
        /// Registers the closed generic instantiations the wire needs.
        /// </summary>
        private static void AddGenericInstantiations(List<IMessagePackFormatter> list)
        {
            list.Add(new WireEnumFormatter<Bee.Api.Contracts.System.PackageDelivery>());
            list.Add(new WireEnumFormatter<Bee.Api.Core.Messages.PayloadFormat>());
            list.Add(new WireEnumFormatter<Bee.Base.Data.FieldDbType>());
            list.Add(new WireEnumFormatter<Bee.Definition.DefineType>());
            list.Add(new WireEnumFormatter<Bee.Definition.Filters.ComparisonOperator>());
            list.Add(new WireEnumFormatter<Bee.Definition.Filters.LogicalOperator>());
            list.Add(new WireEnumFormatter<Bee.Definition.Logging.AnomalyKind>());
            list.Add(new WireEnumFormatter<Bee.Definition.Logging.ChangeKind>());
            list.Add(new WireEnumFormatter<Bee.Definition.Logging.LoginEvent>());
            list.Add(new WireEnumFormatter<Bee.Definition.NumberKind>());
            list.Add(new WireEnumFormatter<Bee.Definition.Security.ApiKeyStatus>());
            list.Add(new WireEnumFormatter<Bee.Definition.Security.ApiKeyType>());
            list.Add(new WireEnumFormatter<Bee.Definition.Settings.PermissionAction>());
            list.Add(new WireEnumFormatter<Bee.Definition.Sorting.SortDirection>());
            list.Add(new WireEnumFormatter<System.Data.DataRowState>());

            list.Add(new NullableFormatter<Bee.Definition.Logging.AnomalyKind>());
            list.Add(new NullableFormatter<Bee.Definition.Logging.ChangeKind>());
            list.Add(new NullableFormatter<Bee.Definition.Logging.LoginEvent>());
            list.Add(new NullableFormatter<System.DateTime>());
            list.Add(new NullableFormatter<System.Int32>());

            list.Add(new ListFormatter<Bee.Api.Contracts.AuditLog.RecordFieldChange>());
            list.Add(new ListFormatter<Bee.Api.Core.MessagePack.SerializableDataColumn>());
            list.Add(new ListFormatter<Bee.Api.Core.MessagePack.SerializableDataRelation>());
            list.Add(new ListFormatter<Bee.Api.Core.MessagePack.SerializableDataRow>());
            list.Add(new ListFormatter<Bee.Api.Core.MessagePack.SerializableDataTable>());
            list.Add(new ListFormatter<Bee.Definition.Security.ApiKeySummary>());
            list.Add(new ListFormatter<System.String>());

            list.Add(new DictionaryFormatter<System.String, Bee.Definition.Settings.PermissionAction>());
            list.Add(new DictionaryFormatter<System.String, System.Int32>());
            list.Add(new DictionaryFormatter<System.String, System.Object>());

            list.Add(new ArrayFormatter<System.String>());
        }
    }
}
