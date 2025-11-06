using Bee.Define;

namespace Bee.Db.UnitTests
{
    [Collection("Initialize")]
    public class BuildSelectCommandTests
    {
        [Fact]
        public void BuildSelectCommand_SelectOnlyMasterFields()
        {
            // 測試：只 Select 主檔欄位，不應產生任何 JOIN
            var builder = new SqlFormCommandBuilder("Project");
            var command = builder.BuildSelectCommand("Project", "sys_id,sys_name");

            Assert.NotNull(command);
            Assert.NotNull(command.CommandText);
            // 驗證 SQL 不包含 JOIN 關鍵字
            Assert.DoesNotContain("JOIN", command.CommandText, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void BuildSelectCommand_WhereOnReferencedField()
        {
            // 測試：Select 主檔欄位，但 Where 條件使用參考欄位，應只 JOIN 該參考表
            var builder = new SqlFormCommandBuilder("Project");
            // 查詢 PM 的專案資料，PM 姓名開頭為「張」
            var filter = new FilterCondition("ref_pm_name", ComparisonOperator.StartsWith, "張");
            // 建立 Select 語法
            var command = builder.BuildSelectCommand("Project", "sys_id,sys_name", filter);

            Assert.NotNull(command);
            Assert.NotNull(command.CommandText);
            // 驗證 SQL 包含 JOIN（因為 Where 需要）
            Assert.Contains("JOIN", command.CommandText, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void BuildSelectCommand_OrderByReferencedField()
        {
            // 測試：Select 主檔欄位，但 Order By 使用參考欄位，應只 JOIN 該參考表
            var builder = new SqlFormCommandBuilder("Project");
            // 以 PM 姓名做排序
            var sortFields = new SortFIeldCollection();
            sortFields.Add(new SortField("ref_pm_name", SortDirection.Asc));

            var command = builder.BuildSelectCommand("Project", "sys_id,sys_name", null, sortFields);

            Assert.NotNull(command);
            Assert.NotNull(command.CommandText);
            // 驗證 SQL 包含 JOIN（因為 Order By 需要）
            Assert.Contains("JOIN", command.CommandText, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void BuildSelectCommand_SelectWithMultipleReferences()
        {
            // 測試：Select 包含多個參考欄位，應 JOIN 對應的多個參考表
            var builder = new SqlFormCommandBuilder("Project");

            // 假設 ref_owner_dept_name 和 ref_pm_dept_name 來自不同的參考表
            var command = builder.BuildSelectCommand("Project", "sys_id,sys_name,ref_owner_dept_name,ref_pm_dept_name");

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

        [Fact]
        public void BuildSelectCommand_FilterGroupWithMultipleConditions()
        {
            // 測試：FilterGroup 包含多個條件，使用不同參考欄位
            var builder = new SqlFormCommandBuilder("Project");

            var filterGroup = FilterGroup.All(
                FilterCondition.Contains("sys_name", "專案"),
                FilterCondition.Equal("ref_pm_name", "張三")
            );

            var sortFields = new SortFIeldCollection
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
            // 驗證只 JOIN ref_pm_name 相關的表（因為 Select 不需要其他參考欄位）
            Assert.Contains("JOIN", command.CommandText, StringComparison.OrdinalIgnoreCase);
        }
    }
}
