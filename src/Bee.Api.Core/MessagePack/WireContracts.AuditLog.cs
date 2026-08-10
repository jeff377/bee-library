using MessagePack.Formatters;

namespace Bee.Api.Core.MessagePack
{
    internal static partial class WireContracts
    {
        /// <summary>
        /// Registers the audit-log API wire contracts.
        /// </summary>
        private static void AddAuditLogMessages(List<IMessagePackFormatter> list)
        {
            list.Add(WireContract.For<Bee.Api.Core.Messages.AuditLog.GetAccessLogRequest>()
                .Member(nameof(Bee.Api.Core.Messages.AuditLog.GetAccessLogRequest.Parameters), static x => x.Parameters, static (x, v) => x.Parameters = v)
                .Member(nameof(Bee.Api.Core.Messages.AuditLog.GetAccessLogRequest.FromUtc), static x => x.FromUtc, static (x, v) => x.FromUtc = v)
                .Member(nameof(Bee.Api.Core.Messages.AuditLog.GetAccessLogRequest.ToUtc), static x => x.ToUtc, static (x, v) => x.ToUtc = v)
                .Member(nameof(Bee.Api.Core.Messages.AuditLog.GetAccessLogRequest.UserId), static x => x.UserId, static (x, v) => x.UserId = v)
                .Member(nameof(Bee.Api.Core.Messages.AuditLog.GetAccessLogRequest.ProgId), static x => x.ProgId, static (x, v) => x.ProgId = v)
                .Member(nameof(Bee.Api.Core.Messages.AuditLog.GetAccessLogRequest.RowKey), static x => x.RowKey, static (x, v) => x.RowKey = v)
                .Member(nameof(Bee.Api.Core.Messages.AuditLog.GetAccessLogRequest.Paging), static x => x.Paging, static (x, v) => x.Paging = v)
                .Build());
            list.Add(WireContract.For<Bee.Api.Core.Messages.AuditLog.GetApiAnomalyLogRequest>()
                .Member(nameof(Bee.Api.Core.Messages.AuditLog.GetApiAnomalyLogRequest.Parameters), static x => x.Parameters, static (x, v) => x.Parameters = v)
                .Member(nameof(Bee.Api.Core.Messages.AuditLog.GetApiAnomalyLogRequest.FromUtc), static x => x.FromUtc, static (x, v) => x.FromUtc = v)
                .Member(nameof(Bee.Api.Core.Messages.AuditLog.GetApiAnomalyLogRequest.ToUtc), static x => x.ToUtc, static (x, v) => x.ToUtc = v)
                .Member(nameof(Bee.Api.Core.Messages.AuditLog.GetApiAnomalyLogRequest.UserId), static x => x.UserId, static (x, v) => x.UserId = v)
                .Member(nameof(Bee.Api.Core.Messages.AuditLog.GetApiAnomalyLogRequest.Method), static x => x.Method, static (x, v) => x.Method = v)
                .Member(nameof(Bee.Api.Core.Messages.AuditLog.GetApiAnomalyLogRequest.Kind), static x => x.Kind, static (x, v) => x.Kind = v)
                .Member(nameof(Bee.Api.Core.Messages.AuditLog.GetApiAnomalyLogRequest.Paging), static x => x.Paging, static (x, v) => x.Paging = v)
                .Build());
            list.Add(WireContract.For<Bee.Api.Core.Messages.AuditLog.GetApiAnomalySummaryRequest>()
                .Member(nameof(Bee.Api.Core.Messages.AuditLog.GetApiAnomalySummaryRequest.Parameters), static x => x.Parameters, static (x, v) => x.Parameters = v)
                .Member(nameof(Bee.Api.Core.Messages.AuditLog.GetApiAnomalySummaryRequest.FromUtc), static x => x.FromUtc, static (x, v) => x.FromUtc = v)
                .Member(nameof(Bee.Api.Core.Messages.AuditLog.GetApiAnomalySummaryRequest.ToUtc), static x => x.ToUtc, static (x, v) => x.ToUtc = v)
                .Build());
            list.Add(WireContract.For<Bee.Api.Core.Messages.AuditLog.GetChangeDetailRequest>()
                .Member(nameof(Bee.Api.Core.Messages.AuditLog.GetChangeDetailRequest.Parameters), static x => x.Parameters, static (x, v) => x.Parameters = v)
                .Member(nameof(Bee.Api.Core.Messages.AuditLog.GetChangeDetailRequest.SysRowId), static x => x.SysRowId, static (x, v) => x.SysRowId = v)
                .Build());
            list.Add(WireContract.For<Bee.Api.Core.Messages.AuditLog.GetChangeDetailResponse>()
                .Member(nameof(Bee.Api.Core.Messages.AuditLog.GetChangeDetailResponse.Parameters), static x => x.Parameters, static (x, v) => x.Parameters = v)
                .Member(nameof(Bee.Api.Core.Messages.AuditLog.GetChangeDetailResponse.SysRowId), static x => x.SysRowId, static (x, v) => x.SysRowId = v)
                .Member(nameof(Bee.Api.Core.Messages.AuditLog.GetChangeDetailResponse.LogTime), static x => x.LogTime, static (x, v) => x.LogTime = v)
                .Member(nameof(Bee.Api.Core.Messages.AuditLog.GetChangeDetailResponse.UserId), static x => x.UserId, static (x, v) => x.UserId = v)
                .Member(nameof(Bee.Api.Core.Messages.AuditLog.GetChangeDetailResponse.UserName), static x => x.UserName, static (x, v) => x.UserName = v)
                .Member(nameof(Bee.Api.Core.Messages.AuditLog.GetChangeDetailResponse.ProgId), static x => x.ProgId, static (x, v) => x.ProgId = v)
                .Member(nameof(Bee.Api.Core.Messages.AuditLog.GetChangeDetailResponse.RowKey), static x => x.RowKey, static (x, v) => x.RowKey = v)
                .Member(nameof(Bee.Api.Core.Messages.AuditLog.GetChangeDetailResponse.ChangeKind), static x => x.ChangeKind, static (x, v) => x.ChangeKind = v)
                .Member(nameof(Bee.Api.Core.Messages.AuditLog.GetChangeDetailResponse.IsSensitive), static x => x.IsSensitive, static (x, v) => x.IsSensitive = v)
                .Member(nameof(Bee.Api.Core.Messages.AuditLog.GetChangeDetailResponse.Source), static x => x.Source, static (x, v) => x.Source = v)
                .Member(nameof(Bee.Api.Core.Messages.AuditLog.GetChangeDetailResponse.Fields), static x => x.Fields, static (x, v) => x.Fields = v)
                .Build());
            list.Add(WireContract.For<Bee.Api.Core.Messages.AuditLog.GetChangeLogRequest>()
                .Member(nameof(Bee.Api.Core.Messages.AuditLog.GetChangeLogRequest.Parameters), static x => x.Parameters, static (x, v) => x.Parameters = v)
                .Member(nameof(Bee.Api.Core.Messages.AuditLog.GetChangeLogRequest.FromUtc), static x => x.FromUtc, static (x, v) => x.FromUtc = v)
                .Member(nameof(Bee.Api.Core.Messages.AuditLog.GetChangeLogRequest.ToUtc), static x => x.ToUtc, static (x, v) => x.ToUtc = v)
                .Member(nameof(Bee.Api.Core.Messages.AuditLog.GetChangeLogRequest.UserId), static x => x.UserId, static (x, v) => x.UserId = v)
                .Member(nameof(Bee.Api.Core.Messages.AuditLog.GetChangeLogRequest.ProgId), static x => x.ProgId, static (x, v) => x.ProgId = v)
                .Member(nameof(Bee.Api.Core.Messages.AuditLog.GetChangeLogRequest.RowKey), static x => x.RowKey, static (x, v) => x.RowKey = v)
                .Member(nameof(Bee.Api.Core.Messages.AuditLog.GetChangeLogRequest.ChangeKind), static x => x.ChangeKind, static (x, v) => x.ChangeKind = v)
                .Member(nameof(Bee.Api.Core.Messages.AuditLog.GetChangeLogRequest.Paging), static x => x.Paging, static (x, v) => x.Paging = v)
                .Build());
            list.Add(WireContract.For<Bee.Api.Core.Messages.AuditLog.GetChangeLogResponse>()
                .Member(nameof(Bee.Api.Core.Messages.AuditLog.GetChangeLogResponse.Parameters), static x => x.Parameters, static (x, v) => x.Parameters = v)
                .Member(nameof(Bee.Api.Core.Messages.AuditLog.GetChangeLogResponse.Table), static x => x.Table, static (x, v) => x.Table = v)
                .Member(nameof(Bee.Api.Core.Messages.AuditLog.GetChangeLogResponse.Paging), static x => x.Paging, static (x, v) => x.Paging = v)
                .Build());
            list.Add(WireContract.For<Bee.Api.Core.Messages.AuditLog.GetDbAnomalyLogRequest>()
                .Member(nameof(Bee.Api.Core.Messages.AuditLog.GetDbAnomalyLogRequest.Parameters), static x => x.Parameters, static (x, v) => x.Parameters = v)
                .Member(nameof(Bee.Api.Core.Messages.AuditLog.GetDbAnomalyLogRequest.FromUtc), static x => x.FromUtc, static (x, v) => x.FromUtc = v)
                .Member(nameof(Bee.Api.Core.Messages.AuditLog.GetDbAnomalyLogRequest.ToUtc), static x => x.ToUtc, static (x, v) => x.ToUtc = v)
                .Member(nameof(Bee.Api.Core.Messages.AuditLog.GetDbAnomalyLogRequest.DatabaseId), static x => x.DatabaseId, static (x, v) => x.DatabaseId = v)
                .Member(nameof(Bee.Api.Core.Messages.AuditLog.GetDbAnomalyLogRequest.Kind), static x => x.Kind, static (x, v) => x.Kind = v)
                .Member(nameof(Bee.Api.Core.Messages.AuditLog.GetDbAnomalyLogRequest.Paging), static x => x.Paging, static (x, v) => x.Paging = v)
                .Build());
            list.Add(WireContract.For<Bee.Api.Core.Messages.AuditLog.GetDbAnomalySummaryRequest>()
                .Member(nameof(Bee.Api.Core.Messages.AuditLog.GetDbAnomalySummaryRequest.Parameters), static x => x.Parameters, static (x, v) => x.Parameters = v)
                .Member(nameof(Bee.Api.Core.Messages.AuditLog.GetDbAnomalySummaryRequest.FromUtc), static x => x.FromUtc, static (x, v) => x.FromUtc = v)
                .Member(nameof(Bee.Api.Core.Messages.AuditLog.GetDbAnomalySummaryRequest.ToUtc), static x => x.ToUtc, static (x, v) => x.ToUtc = v)
                .Build());
            list.Add(WireContract.For<Bee.Api.Core.Messages.AuditLog.GetLoginLogRequest>()
                .Member(nameof(Bee.Api.Core.Messages.AuditLog.GetLoginLogRequest.Parameters), static x => x.Parameters, static (x, v) => x.Parameters = v)
                .Member(nameof(Bee.Api.Core.Messages.AuditLog.GetLoginLogRequest.FromUtc), static x => x.FromUtc, static (x, v) => x.FromUtc = v)
                .Member(nameof(Bee.Api.Core.Messages.AuditLog.GetLoginLogRequest.ToUtc), static x => x.ToUtc, static (x, v) => x.ToUtc = v)
                .Member(nameof(Bee.Api.Core.Messages.AuditLog.GetLoginLogRequest.UserId), static x => x.UserId, static (x, v) => x.UserId = v)
                .Member(nameof(Bee.Api.Core.Messages.AuditLog.GetLoginLogRequest.Event), static x => x.Event, static (x, v) => x.Event = v)
                .Member(nameof(Bee.Api.Core.Messages.AuditLog.GetLoginLogRequest.Paging), static x => x.Paging, static (x, v) => x.Paging = v)
                .Build());
            list.Add(WireContract.For<Bee.Api.Core.Messages.AuditLog.GetTopApiMethodsRequest>()
                .Member(nameof(Bee.Api.Core.Messages.AuditLog.GetTopApiMethodsRequest.Parameters), static x => x.Parameters, static (x, v) => x.Parameters = v)
                .Member(nameof(Bee.Api.Core.Messages.AuditLog.GetTopApiMethodsRequest.FromUtc), static x => x.FromUtc, static (x, v) => x.FromUtc = v)
                .Member(nameof(Bee.Api.Core.Messages.AuditLog.GetTopApiMethodsRequest.ToUtc), static x => x.ToUtc, static (x, v) => x.ToUtc = v)
                .Member(nameof(Bee.Api.Core.Messages.AuditLog.GetTopApiMethodsRequest.TopN), static x => x.TopN, static (x, v) => x.TopN = v)
                .Build());
            list.Add(WireContract.For<Bee.Api.Core.Messages.AuditLog.LogAggregateResponse>()
                .Member(nameof(Bee.Api.Core.Messages.AuditLog.LogAggregateResponse.Parameters), static x => x.Parameters, static (x, v) => x.Parameters = v)
                .Member(nameof(Bee.Api.Core.Messages.AuditLog.LogAggregateResponse.Table), static x => x.Table, static (x, v) => x.Table = v)
                .Build());
            list.Add(WireContract.For<Bee.Api.Core.Messages.AuditLog.LogListResponse>()
                .Member(nameof(Bee.Api.Core.Messages.AuditLog.LogListResponse.Parameters), static x => x.Parameters, static (x, v) => x.Parameters = v)
                .Member(nameof(Bee.Api.Core.Messages.AuditLog.LogListResponse.Table), static x => x.Table, static (x, v) => x.Table = v)
                .Member(nameof(Bee.Api.Core.Messages.AuditLog.LogListResponse.Paging), static x => x.Paging, static (x, v) => x.Paging = v)
                .Build());
        }
    }
}
