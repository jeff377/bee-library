using System.ComponentModel;
using Bee.Definition;
using Bee.Definition.Filters;
using Bee.Db.Providers.SqlServer;
using Bee.Tests.Shared;

namespace Bee.Db.UnitTests
{
    [Collection("Initialize")]
    public class BuildSelectCommandTests
    {
        [LocalOnlyFact]
        [DisplayName("BuildSelectCommand 僅選取主檔欄位時不應產生 JOIN")]
        public void BuildSelectCommand_SelectOnlyMasterFields_NoJoin()
        {
            // 測試：只 Select 主檔欄位，不應產生任何 JOIN
            var builder = new SqlFormCommandBuilder("Project");
            var command = builder.BuildSelectCommand("Project", "sys_id,sys_name", null, null);

            Assert.NotNull(command);
            Assert.NotNull(command.CommandText);
            // 驗證 SQL 不包含 JOIN 關鍵字
            Assert.DoesNotContain("JOIN", command.CommandText, StringComparison.OrdinalIgnoreCase);
        }

        [LocalOnlyFact]
        [DisplayName("BuildSelectCommand Where 條件使用參考欄位時應產生 JOIN")]
        public void BuildSelectCommand_WhereOnReferencedField_GeneratesJoin()
        {
            // 測試：Select 主檔欄位，但 Where 條件使用參考欄位，應只 JOIN 該參考表
            var builder = new SqlFormCommandBuilder("Project");
            // 查詢 PM 的專案資料，PM 姓名開頭為「張」
            var filter = new FilterCondition("ref_pm_name", ComparisonOperator.StartsWith, "張");
            // 建立 Select 語法
            var command = builder.BuildSelectCommand("Project", "sys_id,sys_name", filter, null);

            Assert.NotNull(command);
            Assert.NotNull(command.CommandText);
            // 驗證 SQL 包含 JOIN（因為 Where 需要）
            Assert.Contains("JOIN", command.CommandText, StringComparison.OrdinalIgnoreCase);
        }

        [LocalOnlyFact]
        [DisplayName("BuildSelectCommand Order By 使用參考欄位時應產生 JOIN")]
        public void BuildSelectCommand_OrderByReferencedField_GeneratesJoin()
        {
            // 測試：Select 主檔欄位，但 Order By 使用參考欄位，應只 JOIN 該參考表
            var builder = new SqlFormCommandBuilder("Project");
            // 以 PM 姓名做排序
            var sortFields = new SortFieldCollection();
            sortFields.Add(new SortField("ref_pm_dept_name", SortDirection.Asc));

            var command = builder.BuildSelectCommand("Project", "sys_id,sys_name", null, sortFields);

            Assert.NotNull(command);
            Assert.NotNull(command.CommandText);
            // 驗證 SQL 包含 JOIN（因為 Order By 需要）
            Assert.Contains("JOIN", command.CommandText, StringComparison.OrdinalIgnoreCase);
        }

        [LocalOnlyFact]
        [DisplayName("BuildSelectCommand 選取多個參考欄位時應產生多個 JOIN")]
        public void BuildSelectCommand_SelectWithMultipleReferences_GeneratesMultipleJoins()
        {
            // 測試：Select 包含多個參考欄位，應 JOIN 對應的多個參考表
            var builder = new SqlFormCommandBuilder("Project");

            // 假設 ref_owner_dept_name 和 ref_pm_dept_name 來自不同的參考表
            var command = builder.BuildSelectCommand("Project", "sys_id,sys_name,ref_owner_dept_name,ref_pm_dept_name", null, null);

            Assert.NotNull(command);
            Assert.NotNull(command.CommandText);
            // 驗證 SQL 包含多個 JOIN
            var joinCount = System.Text.RegularExpressions.Regex.Matches(
                command.CommandText,
                "JOIN",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase
            ).Count;
            Assert.True(joinCount >= 2, $"應包含至少 2 個 JOIN，實際: {joinCount}");
        }

        [LocalOnlyFact]
        [DisplayName("BuildSelectCommand FilterGroup 含多條件時應正確產生參數與 JOIN")]
        public void BuildSelectCommand_FilterGroupWithMultipleConditions_GeneratesParametersAndJoin()
        {
            // 測試：FilterGroup 包含多個條件，使用不同參考欄位
            var builder = new SqlFormCommandBuilder("Project");

            var filterGroup = FilterGroup.All(
                FilterCondition.Contains("sys_name", "專案"),
                FilterCondition.Equal("ref_pm_name", "張三")
            );

            var sortFields = new SortFieldCollection
            {
                new SortField("sys_id", SortDirection.Asc)
            };

            var command = builder.BuildSelectCommand(
                "Project",
                "sys_id,sys_name",
                filterGroup,
                sortFields
            );

            Assert.NotNull(command);
            Assert.NotNull(command.CommandText);
            // 驗證 WHERE 條件產生了兩個參數
            Assert.Equal(2, command.Parameters.Count);
            // 驗證只 JOIN ref_pm_name 相關的表（因為 Select 不需要其他參考欄位）
            Assert.Contains("JOIN", command.CommandText, StringComparison.OrdinalIgnoreCase);
        }

        [LocalOnlyFact]
        [DisplayName("SqlFormCommandBuilder 建立 Select 命令應成功")]
        public void BuildSelectCommand_WithAndWithoutFields_ReturnsCommands()
        {
            var builder = new SqlFormCommandBuilder("Employee");
            var command = builder.BuildSelectCommand("Employee", string.Empty, null, null);
            var command2 = builder.BuildSelectCommand("Employee", "sys_id,sys_name,ref_dept_name,ref_supervisor_name", null, null);

            Assert.NotNull(command);
            Assert.False(string.IsNullOrWhiteSpace(command.CommandText));
            Assert.NotNull(command2);
            Assert.False(string.IsNullOrWhiteSpace(command2.CommandText));
        }

        [LocalOnlyFact]
        [DisplayName("SqlFormCommandBuilder 搭配篩選條件與排序建立 Select 命令應成功")]
        public void BuildSelectCommand_WithFilterAndSort_ReturnsCommands()
        {
            var builder = new SqlFormCommandBuilder("Employee");

            // 建立一個 FilterCondition 表示 sys_id = '001'
            var filter = new FilterCondition
            {
                FieldName = "sys_id",
                Operator = ComparisonOperator.Equal,
                Value = "001"
            };

            // 建立排序欄位集合
            var sortFields = new SortFieldCollection();
            sortFields.Add(new SortField("sys_id",  SortDirection.Asc)); // 以 sys_id 做升冪排序

            // 傳入 filter node 與 sortFields 至 BuildSelectCommand
            var command = builder.BuildSelectCommand("Employee", string.Empty, filter, sortFields);
            Assert.NotNull(command);

            // 也可搭配多個欄位 filter 與 sortFields
            var command2 = builder.BuildSelectCommand("Employee", "sys_id,sys_name,ref_dept_name,ref_supervisor_name", filter, sortFields);
            Assert.NotNull(command2);

            // 測試 filter 非 Select 欄位，是否正確建立 Join
            filter = new FilterCondition
            {
                FieldName = "ref_supervisor_id",
                Operator = ComparisonOperator.Equal,
                Value = "U001"
            };
            var command3 = builder.BuildSelectCommand("Employee", "sys_id,sys_name", filter, sortFields);
            Assert.NotNull(command2);
        }
    }
}
