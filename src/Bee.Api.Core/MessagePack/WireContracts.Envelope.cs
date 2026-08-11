using MessagePack.Formatters;

namespace Bee.Api.Core.MessagePack
{
    internal static partial class WireContracts
    {
        /// <summary>
        /// Registers the shared envelope and cross-area wire contracts.
        /// </summary>
        private static void AddEnvelopeMessages(List<IMessagePackFormatter> list)
        {
            list.Add(WireContract.For<Bee.Api.Contracts.AuditLog.RecordFieldChange>()
                .Member(nameof(Bee.Api.Contracts.AuditLog.RecordFieldChange.TableName), static x => x.TableName, static (x, v) => x.TableName = v)
                .Member(nameof(Bee.Api.Contracts.AuditLog.RecordFieldChange.RowKey), static x => x.RowKey, static (x, v) => x.RowKey = v)
                .Member(nameof(Bee.Api.Contracts.AuditLog.RecordFieldChange.RowState), static x => x.RowState, static (x, v) => x.RowState = v)
                .Member(nameof(Bee.Api.Contracts.AuditLog.RecordFieldChange.FieldName), static x => x.FieldName, static (x, v) => x.FieldName = v)
                .Member(nameof(Bee.Api.Contracts.AuditLog.RecordFieldChange.OldValue), static x => x.OldValue, static (x, v) => x.OldValue = v)
                .Member(nameof(Bee.Api.Contracts.AuditLog.RecordFieldChange.NewValue), static x => x.NewValue, static (x, v) => x.NewValue = v)
                .Build());
            list.Add(WireContract.For<Bee.Api.Contracts.System.PackageUpdateInfo>()
                .Member(nameof(Bee.Api.Contracts.System.PackageUpdateInfo.AppId), static x => x.AppId, static (x, v) => x.AppId = v)
                .Member(nameof(Bee.Api.Contracts.System.PackageUpdateInfo.ComponentId), static x => x.ComponentId, static (x, v) => x.ComponentId = v)
                .Member(nameof(Bee.Api.Contracts.System.PackageUpdateInfo.UpdateAvailable), static x => x.UpdateAvailable, static (x, v) => x.UpdateAvailable = v)
                .Member(nameof(Bee.Api.Contracts.System.PackageUpdateInfo.LatestVersion), static x => x.LatestVersion, static (x, v) => x.LatestVersion = v)
                .Member(nameof(Bee.Api.Contracts.System.PackageUpdateInfo.Mandatory), static x => x.Mandatory, static (x, v) => x.Mandatory = v)
                .Member(nameof(Bee.Api.Contracts.System.PackageUpdateInfo.PackageSize), static x => x.PackageSize, static (x, v) => x.PackageSize = v)
                .Member(nameof(Bee.Api.Contracts.System.PackageUpdateInfo.Sha256), static x => x.Sha256, static (x, v) => x.Sha256 = v)
                .Member(nameof(Bee.Api.Contracts.System.PackageUpdateInfo.Delivery), static x => x.Delivery, static (x, v) => x.Delivery = v)
                .Member(nameof(Bee.Api.Contracts.System.PackageUpdateInfo.PackageUrl), static x => x.PackageUrl, static (x, v) => x.PackageUrl = v)
                .Member(nameof(Bee.Api.Contracts.System.PackageUpdateInfo.ReleaseNotes), static x => x.ReleaseNotes, static (x, v) => x.ReleaseNotes = v)
                .Build());
            list.Add(WireContract.For<Bee.Api.Contracts.System.PackageUpdateQuery>()
                .Member(nameof(Bee.Api.Contracts.System.PackageUpdateQuery.AppId), static x => x.AppId, static (x, v) => x.AppId = v)
                .Member(nameof(Bee.Api.Contracts.System.PackageUpdateQuery.ComponentId), static x => x.ComponentId, static (x, v) => x.ComponentId = v)
                .Member(nameof(Bee.Api.Contracts.System.PackageUpdateQuery.CurrentVersion), static x => x.CurrentVersion, static (x, v) => x.CurrentVersion = v)
                .Member(nameof(Bee.Api.Contracts.System.PackageUpdateQuery.Platform), static x => x.Platform, static (x, v) => x.Platform = v)
                .Member(nameof(Bee.Api.Contracts.System.PackageUpdateQuery.Channel), static x => x.Channel, static (x, v) => x.Channel = v)
                .Build());
            list.Add(WireContract.For<Bee.Api.Core.Messages.ApiCallContext>()
                .Member(nameof(Bee.Api.Core.Messages.ApiCallContext.AccessToken), static x => x.AccessToken, static (x, v) => x.AccessToken = v)
                .Member(nameof(Bee.Api.Core.Messages.ApiCallContext.IsLocalCall), static x => x.IsLocalCall, static (x, v) => x.IsLocalCall = v)
                .Member(nameof(Bee.Api.Core.Messages.ApiCallContext.Format), static x => x.Format, static (x, v) => x.Format = v)
                .Build());
            list.Add(WireContract.For<Bee.Api.Core.Messages.ExecFuncRequest>()
                .Member(nameof(Bee.Api.Core.Messages.ExecFuncRequest.Parameters), static x => x.Parameters, static (x, v) => x.Parameters = v)
                .Member(nameof(Bee.Api.Core.Messages.ExecFuncRequest.FuncId), static x => x.FuncId, static (x, v) => x.FuncId = v)
                .Build());
            list.Add(WireContract.For<Bee.Api.Core.Messages.ExecFuncResponse>()
                .Member(nameof(Bee.Api.Core.Messages.ExecFuncResponse.Parameters), static x => x.Parameters, static (x, v) => x.Parameters = v)
                .Build());
        }
    }
}
