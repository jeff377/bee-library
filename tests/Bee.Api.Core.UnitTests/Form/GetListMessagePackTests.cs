using System.ComponentModel;
using System.Data;
using Bee.Api.Core.MessagePack;
using Bee.Api.Core.Messages.Form;
using Bee.Definition.Filters;
using Bee.Definition.Sorting;

namespace Bee.Api.Core.UnitTests.Form
{
    /// <summary>
    /// GetListRequest / GetListResponse 經 <see cref="MessagePackCodec"/> 的 wire 層
    /// round-trip 序列化驗證，重點：FilterNode union（FilterGroup + FilterCondition）
    /// 與 DataTable formatter 在框架的 composite resolver 下能正確還原。
    /// </summary>
    public class GetListMessagePackTests
    {
        [Fact]
        [DisplayName("GetListRequest 帶 FilterGroup + FilterCondition 應 round-trip 還原")]
        public void GetListRequest_RoundTrip_PreservesFilterUnion()
        {
            var rowId = Guid.NewGuid();
            var request = new GetListRequest
            {
                SelectFields = "sys_id,sys_name,ref_dept_name",
                Filter = FilterGroup.All(
                    FilterCondition.Equal("sys_rowid", rowId),
                    FilterGroup.Any(
                        FilterCondition.Equal("ref_dept_id", "D001"),
                        FilterCondition.Equal("ref_dept_id", "D002"))),
                SortFields = [new SortField("ref_dept_name", SortDirection.Asc)],
            };

            var bytes = MessagePackCodec.Serialize(request);
            var restored = MessagePackCodec.Deserialize<GetListRequest>(bytes);

            Assert.NotNull(restored);
            Assert.Equal(request.SelectFields, restored!.SelectFields);
            Assert.NotNull(restored.Filter);

            // FilterGroup 還原為 FilterGroup（外層 All）
            var outerGroup = Assert.IsType<FilterGroup>(restored.Filter);
            Assert.Equal(LogicalOperator.And, outerGroup.Operator);
            Assert.Equal(2, outerGroup.Nodes.Count);

            // 第一個 child：FilterCondition (sys_rowid)
            var firstCondition = Assert.IsType<FilterCondition>(outerGroup.Nodes[0]);
            Assert.Equal("sys_rowid", firstCondition.FieldName);
            Assert.Equal(rowId, firstCondition.Value);

            // 第二個 child：FilterGroup (內層 Any)
            var innerGroup = Assert.IsType<FilterGroup>(outerGroup.Nodes[1]);
            Assert.Equal(LogicalOperator.Or, innerGroup.Operator);
            Assert.Equal(2, innerGroup.Nodes.Count);
            Assert.All(innerGroup.Nodes, n => Assert.IsType<FilterCondition>(n));

            // SortFields 還原
            Assert.NotNull(restored.SortFields);
            Assert.Single(restored.SortFields!);
            Assert.Equal("ref_dept_name", restored.SortFields![0].FieldName);
            Assert.Equal(SortDirection.Asc, restored.SortFields[0].Direction);
        }

        [Fact]
        [DisplayName("GetListRequest 三欄都為預設值應 round-trip 為相等內容")]
        public void GetListRequest_DefaultValues_RoundTrip()
        {
            var request = new GetListRequest();

            var bytes = MessagePackCodec.Serialize(request);
            var restored = MessagePackCodec.Deserialize<GetListRequest>(bytes);

            Assert.NotNull(restored);
            Assert.Equal(string.Empty, restored!.SelectFields);
            Assert.Null(restored.Filter);
            Assert.Null(restored.SortFields);
        }

        [Fact]
        [DisplayName("GetListResponse.Table 帶 DataTable 應 round-trip 還原欄位與列")]
        public void GetListResponse_RoundTrip_PreservesDataTable()
        {
            var table = new DataTable("Employee");
            table.Columns.Add("sys_id", typeof(string));
            table.Columns.Add("sys_name", typeof(string));
            table.Columns.Add("ref_dept_name", typeof(string));
            table.Rows.Add("E001", "員工甲", "工程部");
            table.Rows.Add("E002", "員工乙", "業務部");
            var response = new GetListResponse { Table = table };

            var bytes = MessagePackCodec.Serialize(response);
            var restored = MessagePackCodec.Deserialize<GetListResponse>(bytes);

            Assert.NotNull(restored);
            Assert.NotNull(restored!.Table);
            Assert.Equal(3, restored.Table!.Columns.Count);
            Assert.Equal(2, restored.Table.Rows.Count);
            Assert.Equal("E001", restored.Table.Rows[0]["sys_id"]);
            Assert.Equal("員工甲", restored.Table.Rows[0]["sys_name"]);
            Assert.Equal("工程部", restored.Table.Rows[0]["ref_dept_name"]);
            Assert.Equal("E002", restored.Table.Rows[1]["sys_id"]);
        }

        [Fact]
        [DisplayName("GetListResponse.Table = null 應 round-trip 為 null")]
        public void GetListResponse_NullTable_RoundTrip()
        {
            var response = new GetListResponse { Table = null };

            var bytes = MessagePackCodec.Serialize(response);
            var restored = MessagePackCodec.Deserialize<GetListResponse>(bytes);

            Assert.NotNull(restored);
            Assert.Null(restored!.Table);
        }
    }
}
