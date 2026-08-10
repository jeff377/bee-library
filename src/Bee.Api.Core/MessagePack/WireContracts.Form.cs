using MessagePack.Formatters;

namespace Bee.Api.Core.MessagePack
{
    internal static partial class WireContracts
    {
        /// <summary>
        /// Registers the form API wire contracts.
        /// </summary>
        private static void AddFormMessages(List<IMessagePackFormatter> list)
        {
            list.Add(WireContract.For<Bee.Api.Core.Messages.Form.DeleteRequest>()
                .Member(nameof(Bee.Api.Core.Messages.Form.DeleteRequest.Parameters), static x => x.Parameters, static (x, v) => x.Parameters = v)
                .Member(nameof(Bee.Api.Core.Messages.Form.DeleteRequest.RowId), static x => x.RowId, static (x, v) => x.RowId = v)
                .Build());
            list.Add(WireContract.For<Bee.Api.Core.Messages.Form.DeleteResponse>()
                .Member(nameof(Bee.Api.Core.Messages.Form.DeleteResponse.Parameters), static x => x.Parameters, static (x, v) => x.Parameters = v)
                .Member(nameof(Bee.Api.Core.Messages.Form.DeleteResponse.RowsAffected), static x => x.RowsAffected, static (x, v) => x.RowsAffected = v)
                .Build());
            list.Add(WireContract.For<Bee.Api.Core.Messages.Form.GetDataRequest>()
                .Member(nameof(Bee.Api.Core.Messages.Form.GetDataRequest.Parameters), static x => x.Parameters, static (x, v) => x.Parameters = v)
                .Member(nameof(Bee.Api.Core.Messages.Form.GetDataRequest.RowId), static x => x.RowId, static (x, v) => x.RowId = v)
                .Build());
            list.Add(WireContract.For<Bee.Api.Core.Messages.Form.GetDataResponse>()
                .Member(nameof(Bee.Api.Core.Messages.Form.GetDataResponse.Parameters), static x => x.Parameters, static (x, v) => x.Parameters = v)
                .Member(nameof(Bee.Api.Core.Messages.Form.GetDataResponse.DataSet), static x => x.DataSet, static (x, v) => x.DataSet = v)
                .Build());
            list.Add(WireContract.For<Bee.Api.Core.Messages.Form.GetListRequest>()
                .Member(nameof(Bee.Api.Core.Messages.Form.GetListRequest.Parameters), static x => x.Parameters, static (x, v) => x.Parameters = v)
                .Member(nameof(Bee.Api.Core.Messages.Form.GetListRequest.SelectFields), static x => x.SelectFields, static (x, v) => x.SelectFields = v)
                .Member(nameof(Bee.Api.Core.Messages.Form.GetListRequest.Filter), static x => x.Filter, static (x, v) => x.Filter = v)
                .Member(nameof(Bee.Api.Core.Messages.Form.GetListRequest.SortFields), static x => x.SortFields, static (x, v) => x.SortFields = v)
                .Member(nameof(Bee.Api.Core.Messages.Form.GetListRequest.Paging), static x => x.Paging, static (x, v) => x.Paging = v)
                .Build());
            list.Add(WireContract.For<Bee.Api.Core.Messages.Form.GetListResponse>()
                .Member(nameof(Bee.Api.Core.Messages.Form.GetListResponse.Parameters), static x => x.Parameters, static (x, v) => x.Parameters = v)
                .Member(nameof(Bee.Api.Core.Messages.Form.GetListResponse.Table), static x => x.Table, static (x, v) => x.Table = v)
                .Member(nameof(Bee.Api.Core.Messages.Form.GetListResponse.Paging), static x => x.Paging, static (x, v) => x.Paging = v)
                .Build());
            list.Add(WireContract.For<Bee.Api.Core.Messages.Form.GetLookupRequest>()
                .Member(nameof(Bee.Api.Core.Messages.Form.GetLookupRequest.Parameters), static x => x.Parameters, static (x, v) => x.Parameters = v)
                .Member(nameof(Bee.Api.Core.Messages.Form.GetLookupRequest.SearchText), static x => x.SearchText, static (x, v) => x.SearchText = v)
                .Member(nameof(Bee.Api.Core.Messages.Form.GetLookupRequest.Paging), static x => x.Paging, static (x, v) => x.Paging = v)
                .Build());
            list.Add(WireContract.For<Bee.Api.Core.Messages.Form.GetLookupResponse>()
                .Member(nameof(Bee.Api.Core.Messages.Form.GetLookupResponse.Parameters), static x => x.Parameters, static (x, v) => x.Parameters = v)
                .Member(nameof(Bee.Api.Core.Messages.Form.GetLookupResponse.Table), static x => x.Table, static (x, v) => x.Table = v)
                .Member(nameof(Bee.Api.Core.Messages.Form.GetLookupResponse.Paging), static x => x.Paging, static (x, v) => x.Paging = v)
                .Build());
            list.Add(WireContract.For<Bee.Api.Core.Messages.Form.GetNewDataRequest>()
                .Member(nameof(Bee.Api.Core.Messages.Form.GetNewDataRequest.Parameters), static x => x.Parameters, static (x, v) => x.Parameters = v)
                .Build());
            list.Add(WireContract.For<Bee.Api.Core.Messages.Form.GetNewDataResponse>()
                .Member(nameof(Bee.Api.Core.Messages.Form.GetNewDataResponse.Parameters), static x => x.Parameters, static (x, v) => x.Parameters = v)
                .Member(nameof(Bee.Api.Core.Messages.Form.GetNewDataResponse.DataSet), static x => x.DataSet, static (x, v) => x.DataSet = v)
                .Build());
            list.Add(WireContract.For<Bee.Api.Core.Messages.Form.SaveRequest>()
                .Member(nameof(Bee.Api.Core.Messages.Form.SaveRequest.Parameters), static x => x.Parameters, static (x, v) => x.Parameters = v)
                .Member(nameof(Bee.Api.Core.Messages.Form.SaveRequest.DataSet), static x => x.DataSet, static (x, v) => x.DataSet = v)
                .Build());
            list.Add(WireContract.For<Bee.Api.Core.Messages.Form.SaveResponse>()
                .Member(nameof(Bee.Api.Core.Messages.Form.SaveResponse.Parameters), static x => x.Parameters, static (x, v) => x.Parameters = v)
                .Member(nameof(Bee.Api.Core.Messages.Form.SaveResponse.DataSet), static x => x.DataSet, static (x, v) => x.DataSet = v)
                .Member(nameof(Bee.Api.Core.Messages.Form.SaveResponse.AffectedRows), static x => x.AffectedRows, static (x, v) => x.AffectedRows = v)
                .Build());
        }
    }
}
